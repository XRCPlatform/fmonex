using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Bencodex.Types;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Net.Messages;
using Libplanet.Store;
using Libplanet.Tx;
using Serilog.Events;

namespace Libplanet.Net
{
    public partial class Swarm<T>
    {
        private void ProcessMessageHandler(object target, Message message)
        {
            switch (message)
            {
                case Ping ping:
                {
                    _logger.Debug($"Received a {nameof(Ping)} message.");

                    // This case can be dealt in Transport.
                    var pong = new Pong()
                    {
                        Identity = ping.Identity,
                    };

                    Transport.ReplyMessage(pong);
                    break;
                }

                case GetChainStatus getChainStatus:
                {
                    _logger.Debug($"Received a {nameof(GetChainStatus)} message.");

                    // This is based on the assumption that genesis block always exists.
                    var chainStatus = new ChainStatus(
                        BlockChain.Genesis.Hash,
                        BlockChain.Tip.Index,
                        BlockChain.Tip.TotalDifficulty)
                    {
                        Identity = getChainStatus.Identity,
                    };

                    Transport.ReplyMessage(chainStatus);
                    break;
                }

                case FindNeighbors _:
                    _logger.Debug($"Received a {nameof(FindNeighbors)} message.");
                    break;

                case GetBlockHashes getBlockHashes:
                {
                    BlockChain.FindNextHashes(
                        getBlockHashes.Locator,
                        getBlockHashes.Stop,
                        FindNextHashesChunkSize
                    ).Deconstruct(
                        out long? offset,
                        out IReadOnlyList<HashDigest<SHA256>> hashes
                    );
                    var reply = new BlockHashes(offset, hashes)
                    {
                        Identity = getBlockHashes.Identity,
                    };
                    Transport.ReplyMessage(reply);
                    break;
                }

                case GetRecentStates getRecentStates:
                    TransferRecentStates(getRecentStates);
                    break;

                case GetBlocks getBlocks:
                    TransferBlocks(getBlocks);
                    break;

                case GetTxs getTxs:
                    TransferTxs(getTxs);
                    break;

                case Messages.Tx transaction:
                    ReceiveTransactionBroadcast(transaction);
                    break;
                
                case Messages.Blocks blocks:
                    var task = Task.Run(async () => await ProcessBlock(blocks, _cancellationToken)); 
                    task.Wait();
                    break;

                case TxIds txIds:
                    ProcessTxIds(txIds);
                    break;
               
                case BlockHashes _:
                    _logger.Error($"{nameof(BlockHashes)} messages are only for IBD.");
                    break;

                case BlockHeaderMessage blockHeader:
                    Task.Run(
                        async () => await ProcessBlockHeader(blockHeader, _cancellationToken),
                        _cancellationToken
                    );
                    break;

                case GetBlockStates getBlockStates:
                    TransferBlockStates(getBlockStates);
                    break;

                default:
                    throw new InvalidMessageException(
                        $"Failed to handle message: {message}",
                        message
                    );
            }
        }

        private void ReceiveTransactionBroadcast(Messages.Tx message)
        {
            if (message is Messages.Tx parsed)
            {

                if (!(message.Remote is BoundPeer peer))
                {
                    _logger.Information(
                        $"Ignores a {nameof(Messages.Tx)} message because it was sent by an invalid peer: " +
                        "{PeerAddress}.",
                        message.Remote?.Address.ToHex()
                    );
                    return;
                }

                Transaction<T> tx = Transaction<T>.Deserialize(parsed.Payload);

                _logger.Debug($"Received a {nameof(Messages.Tx)} message: {tx}.");

                if (!_store.ContainsTransaction(tx.Id))
                {
                    bool valid = BlockChain.Policy.DoesTransactionFollowsPolicy(tx, BlockChain);
                    if (valid)
                    {
                        try
                        {
                            BlockChain.StageTransaction(tx);
                            TxReceived.Set();
                            _logger.Debug($"Txs staged successfully: {tx.Id}");
                            BroadcastMessage(message.Remote.Address, message);
                        }
                        catch (InvalidTxException ite)
                        {
                            _logger.Error(ite, "{TxId} will not be staged since it is invalid.", tx.Id);
                        }
                    }
                }
            }
            else
            {
                string errorMessage =
                    $"Expected {nameof(Tx)} messages as response of " +
                    $"the {nameof(Tx)} message, but got a {message.GetType().Name} " +
                    $"message instead: {message}";
                throw new InvalidMessageException(errorMessage, message);
            }           
        }

        private void TransferBlockStates(GetBlockStates getBlockStates)
        {
            if (BlockChain.StateStore is IBlockStatesStore blockStatesStore)
            {
                IImmutableDictionary<string, IValue> states =
                    blockStatesStore.GetBlockStates(getBlockStates.BlockHash);
                _logger.Debug(
                    (states is null ? "Not found" : "Found") + " the block {BlockHash}'s states.",
                    getBlockStates.BlockHash
                );
                var reply = new BlockStates(getBlockStates.BlockHash, states)
                {
                    Identity = getBlockStates.Identity,
                };
                Transport.ReplyMessage(reply);
            }
        }

        private async Task ProcessBlockHeader(
            BlockHeaderMessage message,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!(message.Remote is BoundPeer peer))
            {
                _logger.Information(
                    "BlockHeaderMessage was sent from invalid peer " +
                    "{PeerAddress}; ignored.",
                    message.Remote.Address
                );
                return;
            }

            if (!message.GenesisHash.Equals(BlockChain.Genesis.Hash))
            {
                _logger.Information(
                    "BlockHeaderMessage was sent from the peer " +
                    "{PeerAddress} with different genesis block {hash}; ignored.",
                    message.Remote.Address,
                    message.GenesisHash
                );
                return;
            }

            BlockHeaderReceived.Set();
            BlockHeader header = message.Header;

            _logger.Debug(
                $"Received a {nameof(BlockHeader)} #{{BlockIndex}} {{BlockHash}}.",
                header.Index,
                ByteUtil.Hex(header.Hash)
            );

            try
            {
                header.Validate(DateTimeOffset.UtcNow);
            }
            catch (InvalidBlockException ibe)
            {
                _logger.Information(
                    ibe,
                    "A received header #{BlockIndex} {BlockHash} seems invalid; ignored.",
                    header.Index,
                    ByteUtil.Hex(header.Hash)
                );
                return;
            }

            using (await _blockSyncMutex.LockAsync(cancellationToken))
            {
                if (IsDemandNeeded(header))
                {
                    _demandBlockHash = new BlockHashDemand(header, peer);
                }
                else
                {
                    _logger.Debug(
                        "No blocks are required " +
                        "(current: {Current}, demand: {Demand}, received: {Received});" +
                        $" {nameof(BlockHeaderMessage)} is ignored.",
                        BlockChain.Tip.Index,
                        _demandBlockHash?.Header.Index,
                        header.Index);
                }
            }
        }

        private async Task ProcessBlock(Messages.Blocks message, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!(message.Remote is BoundPeer peer))
            {
                _logger.Information($"Blocks was sent from an invalid peer {message.Remote.Address} ignored.");
                return;
            }

            if (message is Messages.Blocks blockMessage)
            {
                IList<byte[]> payloads = blockMessage.Payloads;

                _logger.Debug($"Received {payloads.Count} blocks from {message.Remote}.");

                foreach (byte[] payload in payloads)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var genesis = message.GenesisHash;

                    Block<T> block = new Block<T>().Deserialize(payload);

                    if (!genesis.Equals(BlockChain.Genesis.Hash))
                    {
                        _logger.Information(
                            "Blocks was sent from the peer " +
                            "{PeerAddress} with different genesis block {hash}; ignored.",
                            message.Remote.Address,
                            message.GenesisHash
                        );
                        return;
                    }

                    BlockHeaderReceived.Set();
                    BlockHeader header = block.GetBlockHeader();
                    _logger.Debug($"Received a {nameof(BlockHeader)} #{ header.Index} {ByteUtil.Hex(header.Hash)}.");

                    try
                    {
                        header.Validate(DateTimeOffset.UtcNow);
                    }
                    catch (InvalidBlockException ibe)
                    {
                        _logger.Information(
                            ibe,
                            "A received header #{BlockIndex} {BlockHash} seems invalid; ignored.",
                            header.Index,
                            ByteUtil.Hex(header.Hash)
                        );
                        return;
                    }
                    
                    long prevTip = BlockChain.Tip.Index;

                    //if possible block will be appended here
                    var chain = AcceptBlock(peer, BlockChain, block, cancellationToken);
                    //if block appended things are simple!
                    if (prevTip < chain.Tip.Index)
                    {
                        BlockReceived.Set();
                        BlockAppended.Set();
                        BroadcastBlock(peer.Address, BlockChain.Tip);
                    }
                    else
                    {
                        //if there is a GAP then go for a long download of previous blocks
                        try
                        {
                            await SyncPreviousBlocksAsync(BlockChain, peer, block.Hash, null, null, null, 0, cancellationToken);

                            // FIXME: Clean up events
                            BlockReceived.Set();
                            BlockAppended.Set();
                            BroadcastBlock(peer.Address, BlockChain.Tip);
                        }
                        catch (TimeoutException)
                        {
                            _logger.Debug($"Timeout occurred during {nameof(SyncPreviousBlocksAsync)}");
                        }
                        catch (InvalidBlockIndexException ibie)
                        {
                            _logger.Warning(
                                $"{nameof(InvalidBlockIndexException)} occurred during " +
                                $"{nameof(SyncPreviousBlocksAsync)}: " +
                                "{ibie}", ibie);
                        }
                        catch (Exception e)
                        {
                            var msg =
                                $"Unexpected exception occurred during" +
                                $" {nameof(SyncPreviousBlocksAsync)}: {{e}}";
                            _logger.Error(e, msg, e);
                        }
                    }                   
                }
            }
            else
            {
                string errorMessage =
                    $"Expected a {nameof(Blocks)} message as a response of " +
                    $"the {nameof(Messages.Blocks)} message, but got a {message.GetType().Name} " +
                    $"message instead: {message}";
                throw new InvalidMessageException(errorMessage, message);
            }

            _logger.Information($"Processed block(s) broadcast from {peer.EndPoint}@{peer.Address.ToHex()}.");

        }

        private void TransferTxs(GetTxs getTxs)
        {
            IEnumerable<Transaction<T>> txs = getTxs.TxIds
                .Where(txId => _store.ContainsTransaction(txId))
                .Select(BlockChain.GetTransaction);

            foreach (Transaction<T> tx in txs)
            {
                _logger.Debug($"Responding to GetTxs {tx.Id}");
                Message response = new Messages.Tx(tx.Serialize(true))
                {
                    Identity = getTxs.Identity,
                };
                Transport.ReplyMessage(response);
            }
        }

        private void ProcessTxIds(TxIds message)
        {
            if (!(message.Remote is BoundPeer peer))
            {
                _logger.Information(
                    $"Ignores a {nameof(TxIds)} message because it was sent by an invalid peer: " +
                    "{PeerAddress}.",
                    message.Remote?.Address.ToHex()
                );
                return;
            }

            _logger.Debug(
                $"Received a {nameof(TxIds)} message: {{@TxIds}}.",
                message.Ids.Select(txid => txid.ToString())
            );

            ImmutableHashSet<TxId> newTxIds = message.Ids
                .Where(id => !_demandTxIds.ContainsKey(id))
                .Where(id => !_store.ContainsTransaction(id))
                .ToImmutableHashSet();

            if (!newTxIds.Any())
            {
                _logger.Debug("No unaware transactions to receive.");
                return;
            }

            _logger.Debug(
                "Unaware transactions to receive: {@TxIds}.",
                newTxIds.Select(txid => txid.ToString())
            );
            foreach (TxId txid in newTxIds)
            {
                _demandTxIds.TryAdd(txid, peer);
            }
        }

        private void TransferBlocks(GetBlocks getData)
        {
            string identityHex = ByteUtil.Hex(getData.Identity);
            _logger.Verbose(
                $"Preparing a {nameof(Blocks)} message to reply to {{Identity}}...",
                identityHex
            );

            var blocks = new List<byte[]>();

            List<HashDigest<SHA256>> hashes = getData.BlockHashes.ToList();
            int i = 1;
            int total = hashes.Count;
            const string logMsg =
                "Fetching a block #{Index}/{Total} ({Hash}) to include to " +
                "a reply to {Identity}...";
            foreach (HashDigest<SHA256> hash in hashes)
            {
                _logger.Verbose(logMsg, i, total, hash, identityHex);
                if (_store.ContainsBlock(hash))
                {
                    Block<T> block = _store.GetBlock<T>(hash);
                    byte[] payload = block.Serialize();
                    blocks.Add(payload);
                }

                if (blocks.Count == getData.ChunkSize)
                {
                    var response = new Messages.Blocks(blocks, BlockChain.Genesis.Hash)
                    {
                        Identity = getData.Identity,
                    };
                    _logger.Verbose(
                        "Enqueuing a blocks reply (...{Index}/{Total})...",
                        i,
                        total
                    );
                    Transport.ReplyMessage(response);
                    blocks.Clear();
                }

                i++;
            }

            if (blocks.Any())
            {
                var response = new Messages.Blocks(blocks, BlockChain.Genesis.Hash)
                {
                    Identity = getData.Identity,
                };
                _logger.Verbose(
                    "Enqueuing a blocks reply (...{Index}/{Total}) to {Identity}...",
                    total,
                    total,
                    identityHex
                );
                Transport.ReplyMessage(response);
            }

            _logger.Debug("Blocks were transferred to {Identity}.", identityHex);
        }

        private void TransferRecentStates(GetRecentStates getRecentStates)
        {
            BlockLocator baseLocator = getRecentStates.BaseLocator;
            HashDigest<SHA256>? @base = BlockChain.FindBranchPoint(baseLocator);
            HashDigest<SHA256> target = getRecentStates.TargetBlockHash;
            IImmutableDictionary<HashDigest<SHA256>,
                IImmutableDictionary<string, IValue>
            > blockStates = null;
            IImmutableDictionary<string, IImmutableList<HashDigest<SHA256>>> stateRefs = null;
            long nextOffset = -1;
            int iteration = 0;

            if (BlockChain.StateStore is IBlockStatesStore blockStatesStore &&
                BlockChain.ContainsBlock(target))
            {
                ReaderWriterLockSlim rwlock = BlockChain._rwlock;
                rwlock.EnterReadLock();
                try
                {
                    Guid chainId = BlockChain.Id;

                    _logger.Debug(
                        "Getting state references from {Offset}",
                        getRecentStates.Offset);

                    long baseIndex =
                        (@base is HashDigest<SHA256> bbh &&
                         _store.GetBlockIndex(bbh) is long bbIdx)
                            ? bbIdx
                            : 0;
                    long lowestIndex = baseIndex + getRecentStates.Offset;
                    long targetIndex =
                        (target is HashDigest<SHA256> tgt &&
                         _store.GetBlockIndex(tgt) is long tgtIdx)
                            ? tgtIdx
                            : long.MaxValue;

                    iteration =
                        (int)Math.Ceiling(
                            (double)(targetIndex - baseIndex + 1) / FindNextStatesChunkSize);

                    long highestIndex = lowestIndex + FindNextStatesChunkSize - 1 > targetIndex
                        ? targetIndex
                        : lowestIndex + FindNextStatesChunkSize - 1;

                    nextOffset = highestIndex == targetIndex
                        ? -1
                        : getRecentStates.Offset + FindNextStatesChunkSize;

                    stateRefs = blockStatesStore.ListAllStateReferences(
                        chainId,
                        lowestIndex: lowestIndex,
                        highestIndex: highestIndex
                    );
                    if (_logger.IsEnabled(LogEventLevel.Verbose))
                    {
                        _logger.Verbose(
                            "List state references from {From} to {To}:\n{StateReferences}",
                            lowestIndex,
                            highestIndex,
                            string.Join(
                                "\n",
                                stateRefs.Select(kv => $"{kv.Key}: {string.Join(", ", kv.Value)}")
                            )
                        );
                    }

                    // GetBlockStates may return null since swarm may not have deep states.
                    blockStates = stateRefs.Values
                        .Select(refs => refs.Last())
                        .ToImmutableHashSet()
                        .Select(bh => (bh, blockStatesStore.GetBlockStates(bh)))
                        .Where(pair => !(pair.Item2 is null))
                        .ToImmutableDictionary(
                            pair => pair.Item1,
                            pair => (IImmutableDictionary<string, IValue>)pair.Item2
                                .ToImmutableDictionary(kv => kv.Key, kv => kv.Value)
                        );
                }
                finally
                {
                    rwlock.ExitReadLock();
                }

                if (_logger.IsEnabled(LogEventLevel.Verbose))
                {
                    if (BlockChain.ContainsBlock(target))
                    {
                        var baseString = @base is HashDigest<SHA256> h
                            ? $"{BlockChain[h].Index}:{h}"
                            : null;
                        var targetString = $"{BlockChain[target].Index}:{target}";
                        _logger.Verbose(
                            "State references to send (preload):" +
                            " {StateReferences} ({Base}-{Target})",
                            stateRefs.Select(kv =>
                                (
                                    kv.Key,
                                    string.Join(", ", kv.Value.Select(v => v.ToString()))
                                )
                            ).ToArray(),
                            baseString,
                            targetString
                        );
                        _logger.Verbose(
                            "Block states to send (preload): {BlockStates} ({Base}-{Target})",
                            blockStates.Select(kv => (kv.Key, kv.Value)).ToArray(),
                            baseString,
                            targetString
                        );
                    }
                    else
                    {
                        _logger.Verbose(
                            "Nothing to reply because {TargetHash} doesn't exist.", target);
                    }
                }
            }

            var reply = new RecentStates(
                target,
                nextOffset,
                iteration,
                blockStates,
                stateRefs?.ToImmutableDictionary())
            {
                Identity = getRecentStates.Identity,
            };

            Transport.ReplyMessage(reply);
        }
    }
}
