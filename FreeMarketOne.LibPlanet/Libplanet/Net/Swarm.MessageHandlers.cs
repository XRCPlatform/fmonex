using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Bencodex.Types;
using FreeMarketOne.Tor;
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
        private void ProcessMessageHandler(object target, ReceivedRequestEventArgs message)
        {
            TotClient client = message.Client;

            _logger.Debug($"Received {message.MessageType} message.");

            switch (message.MessageType)
            {
                case MessageType.Ping:
                    Transport.ReplyMessage<Pong>(message.Request, client, new Pong());
                    break;

                case MessageType.GetChainStatus:
                    // This is based on the assumption that genesis block always exists.
                    var chainStatus = new ChainStatus(
                        BlockChain.Genesis.Hash,
                        BlockChain.Tip.Index,
                        BlockChain.Tip.TotalDifficulty);

                    Transport.ReplyMessage<ChainStatus>(message.Request, client, chainStatus);
                    break;

                case MessageType.FindNeighbors:

                    // it's handled in KademliaProtocol ReceiveMessage() event handler
                    break;

                case MessageType.GetBlockHashes:

                    var getBlockHashes = message.Envelope.GetBody<GetBlockHashes>();
                    BlockChain.FindNextHashes(
                                           getBlockHashes.Locator,
                                           getBlockHashes.Stop,
                                           FindNextHashesChunkSize
                                       ).Deconstruct<long?, IReadOnlyList<HashDigest<SHA256>>>(
                                           out long? offset,
                                           out IReadOnlyList<HashDigest<SHA256>> hashes
                                       );
                    var reply = new BlockHashes(offset, hashes);
                    Transport.ReplyMessage<BlockHashes>(message.Request, client, reply);
                    break;

                case MessageType.GetRecentStates:

                    var getRecentStates = message.Envelope.GetBody<GetRecentStates>();
                    var states = TransferRecentStates(getRecentStates);
                    Transport.ReplyMessage<RecentStates>(message.Request, client, states);
                    break;

                case MessageType.GetBlocks:

                    var getBlocks = message.Envelope.GetBody<GetBlocks>();
                    var blocks = TransferBlocks(getBlocks, message.Peer);
                    Transport.ReplyMessage<Messages.Blocks>(message.Request, client, blocks);
                    break;

                case MessageType.GetTxs:

                    var getTxs = message.Envelope.GetBody<GetTxs>();
                    var txs = TransferTxs(getTxs);
                    Transport.ReplyMessage<Transactions>(message.Request, client, txs);
                    break;

                case MessageType.TxBroadcast:

                    var transaction = message.Envelope.GetBody<TxBroadcast>();                    
                    var trxBroadcast = Task.Run(() => ReceiveTransactionBroadcast(transaction, message.Peer));
                    try
                    {
                        //ack the reciept
                        Task.Run(() => Transport.ReplyMessage<Pong>(message.Request, client, new Pong()));
                        trxBroadcast.Wait();
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Processing received TxBroadcast message, completed with error {e}.");
                    }
                   
                    break;
                case MessageType.Blocks:
                    var blocksBroadcast = message.Envelope.GetBody<Messages.Blocks>();
                    var task = Task.Run(async () => await ProcessBlock(message.Peer, blocksBroadcast, _cancellationToken));
                    try
                    {
                        //ack the reciept
                        var task2 = Task.Run(() => Transport.ReplyMessage<Pong>(message.Request, client, new Pong()));                        
                        task.Wait();
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Processing received {nameof(blocks)} message, completed with error {e}.");
                    }
                    break;

                case MessageType.TxIds:

                    var txIds = message.Envelope.GetBody<TxIds>();
                    ProcessTxIds(txIds, message.Peer);
                    break;

                case MessageType.BlockHashes:

                    var blockHashes = message.Envelope.GetBody<BlockHashes>();
                    _logger.Error($"{nameof(BlockHashes)} messages are only for IBD.");
                    break;

                case MessageType.BlockHeaderMessage:

                    var blockHeader = message.Envelope.GetBody<BlockHeaderMessage>();
                    Task.Run(
                        async () => await ProcessBlockHeader(blockHeader, message.Peer, _cancellationToken),
                        _cancellationToken
                    );
                    break;

                case MessageType.GetBlockStates:

                    var getBlockStates = message.Envelope.GetBody<GetBlockStates>();
                    var blockStates = TransferBlockStates(getBlockStates);
                    if (blockStates != null)
                    {
                        Transport.ReplyMessage<BlockStates>(message.Request, client, blockStates);
                    }
                    break;

                default:
                    throw new InvalidMessageException($"Failed to handle message: {message}", message.MessageType);
            }
        }

        private void ReceiveTransactionBroadcast(TxBroadcast message, BoundPeer peer)
        {
            Transaction<T> tx = Transaction<T>.Deserialize(message.Payload);

            _logger.Debug($"Received a {nameof(TxBroadcast)} message: {tx}.");

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

                        TxBroadcast newMessage = new TxBroadcast(tx.Serialize(true), false);

                        BroadcastMessage(peer, newMessage);
                    }
                    catch (InvalidTxException ite)
                    {
                        _logger.Error(ite, "{TxId} will not be staged since it is invalid.", tx.Id);
                    }
                }
            }
        }

        private BlockStates TransferBlockStates(GetBlockStates getBlockStates)
        {
            if (BlockChain.StateStore is IBlockStatesStore blockStatesStore)
            {
                IImmutableDictionary<string, IValue> states =
                    blockStatesStore.GetBlockStates(getBlockStates.BlockHash);
                _logger.Debug(
                    (states is null ? "Not found" : "Found") + " the block {BlockHash}'s states.",
                    getBlockStates.BlockHash
                );
                return new BlockStates(getBlockStates.BlockHash, states);
            }

            return null;
        }

        private async Task ProcessBlockHeader(
            BlockHeaderMessage message,
            BoundPeer peer,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (peer == null)
            {
                _logger.Information($"BlockHeaderMessage was sent from invalid peer {peer}; ignored.");
                return;
            }

            if (!message.GenesisHash.Equals(BlockChain.Genesis.Hash))
            {
                _logger.Information(
                    "BlockHeaderMessage was sent from the peer " +
                    "{PeerAddress} with different genesis block {hash}; ignored.",
                    peer,
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

        private async Task ProcessBlock(BoundPeer remote, Messages.Blocks blockMessage, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!(remote is BoundPeer peer))
            {
                _logger.Information($"Blocks was sent from an invalid peer {remote.EndPoint} ignored.");
                return;
            }

            IEnumerable<byte[]> payloads = blockMessage.Payloads;

            _logger.Debug($"Received {payloads.Count()} blocks from {remote}.");

            foreach (byte[] payload in payloads)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var genesis = blockMessage.GenesisHash;

                Block<T> block = new Block<T>().Deserialize(payload);

                if (!genesis.Equals(BlockChain.Genesis.Hash))
                {
                    _logger.Information(
                        "Blocks was sent from the peer " +
                        "{PeerAddress} with different genesis block {hash}; ignored.",
                        remote,
                        blockMessage.GenesisHash
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

                if (prevTip == block.Index)
                {
                    //received re-broadcast
                    if (BlockChain.Tip.Hash.Equals(block.Hash))
                    {
                        _logger.Information($"Previous index #{prevTip} and recieved index #{block.Index} and their hashes are same tip:{ BlockChain.Tip} vs received:{ block.Hash } rejecting recieved block");
                        //should not broadcast as broadcasts will loop and never stop
                    }
                    else
                    {
                        _logger.Information($"Previous index #{prevTip} and recieved index #{block.Index} and their hashes are different  tip:{ BlockChain.Tip} vs received:{ block.Hash } rejecting recieved block, chain was split and will need re-org.");
                        //broadcast as chain was split and will need re-org
                        //BroadcastBlock(peer.Address, block);
                    }
                    return;
                }
                //single block gap try accept simply
                long gap = block.Index - prevTip;
                if (gap == 1)
                {
                    _logger.Information($"Accepting a received block #{block.Index} { block.Hash } tip before this message {prevTip}");
                    //if possible block will be appended here
                    try
                    {
                        var chain = AcceptBlock(peer, BlockChain, block, cancellationToken);
                    }
                    catch (InvalidBlockPreviousHashException iph)
                    {

                        _logger.Error(iph, $"Failed to append a received block #{block.Index} { block.Hash } tip before this message {prevTip}. Error {iph}");

                        _logger.Information($"Starting SyncPreviousBlocksAsync due to InvalidBlockPreviousHashException to resolve fork.");
                        await SyncPreviousBlocksAsync(BlockChain,
                            peer,
                            block.Hash,
                            null,
                            ImmutableHashSet<Address>.Empty,
                            null,
                            0,
                            cancellationToken);

                        // FIXME: Clean up events
                        BlockReceived.Set();
                        BlockAppended.Set();
                    }

                    BroadcastBlock(peer, BlockChain.Tip);
                }
                else if (gap > 1)
                {
                    _logger.Information($"Found chain gap #{gap} larger than single block, tip before this message {prevTip}, a received header #{block.Index} { block.Hash } discarding received and starting SyncPreviousBlocksAsync");
                    //if there is a GAP then go for a long download of previous blocks
                    try
                    {
                        await SyncPreviousBlocksAsync(BlockChain,
                            peer,
                            block.Hash,
                            null,
                            ImmutableHashSet<Address>.Empty,
                            null,
                            0,
                            cancellationToken);

                        // FIXME: Clean up events
                        BlockReceived.Set();
                        BlockAppended.Set();
                        BroadcastBlock(peer, BlockChain.Tip);
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

            _logger.Information($"Processed block(s) broadcast from {peer.EndPoint}@{peer.Address.ToHex()}.");

        }

        private Transactions TransferTxs(GetTxs getTxs)
        {
            IEnumerable<Transaction<T>> txs = getTxs.TxIds
                .Where(txId => _store.ContainsTransaction(txId))
                .Select(BlockChain.GetTransaction);
            List<byte[]> response = new List<byte[]>();
            foreach (Transaction<T> tx in txs)
            {
                _logger.Debug($"Adding {tx.Id} to response.");
                response.Add(tx.Serialize(true));
            }
            return new Transactions(response);
        }

        private void ProcessTxIds(TxIds message, BoundPeer peer)
        {
            if (peer == null)
            {
                _logger.Information($"Ignores a {nameof(TxIds)} message because it was sent by an invalid peer: {peer.EndPoint.Host}:{peer.EndPoint.Port}.");
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

        private Messages.Blocks TransferBlocks(GetBlocks getData, BoundPeer peer)
        {
            _logger.Verbose($"Preparing a {nameof(Blocks)} message to reply to {peer}...");

            var blocks = new List<byte[]>();

            List<HashDigest<SHA256>> hashes = getData.BlockHashes.ToList();
            int i = 1;
            int total = hashes.Count;
            const string logMsg =
                "Fetching a block #{Index}/{Total} ({Hash}) to include to " +
                "a reply to {Peer}...";
            foreach (HashDigest<SHA256> hash in hashes)
            {
                _logger.Verbose(logMsg, i, total, hash, peer);
                if (_store.ContainsBlock(hash))
                {
                    Block<T> block = _store.GetBlock<T>(hash);
                    byte[] payload = block.Serialize();
                    blocks.Add(payload);
                }
                else
                {
                    if (BlockChain.Genesis.Hash.Equals(hash))
                    {
                        _logger.Verbose($"Requested block {hash} is genesis.");
                        Block<T> block = BlockChain.Genesis;
                        byte[] payload = block.Serialize();
                        blocks.Add(payload);
                    }
                    else
                    {
                        _logger.Verbose($"Block {hash} not found. Sending empty reply for {hash}.");
                    }
                }
                //chunking must be lead by client request/response as we can't handle more than 1 response for now
                //if (blocks.Count == getData.ChunkSize)
                //{
                //    var response = new Messages.Blocks(blocks, BlockChain.Genesis.Hash);
                //    _logger.Verbose("Enqueuing a blocks reply (...{Index}/{Total})...", i, total);
                //    Transport.ReplyMessage(response);
                //    blocks.Clear();
                //}

                //i++;
            }
            _logger.Verbose("Sendig a blocks reply (...{Index}/{Total}) to {Identity}...", total, total, peer);
            return new Messages.Blocks(blocks, BlockChain.Genesis.Hash);
        }

        private RecentStates TransferRecentStates(GetRecentStates getRecentStates)
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

            return new RecentStates(
                target,
                nextOffset,
                iteration,
                blockStates,
                stateRefs?.ToImmutableDictionary());
        }
    }
}
