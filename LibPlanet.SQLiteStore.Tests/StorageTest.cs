using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Tests;
using Libplanet.Tests.Blockchain;
using Libplanet.Tests.Common.Action;
using Libplanet.Tx;
using NUnit.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace LibPlanet.SQLiteStore.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void CreateSQLiteDb()
        {
            var Address1 = new Address(new byte[]
            {
                0x45, 0xa2, 0x21, 0x87, 0xe2, 0xd8, 0x85, 0x0b, 0xb3, 0x57,
                0x88, 0x69, 0x58, 0xbc, 0x3e, 0x85, 0x60, 0x92, 0x9c, 0xcc,
            });

            var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(
System.Reflection.Assembly.GetExecutingAssembly().Location),
                $"sqlitedb_test_{Guid.NewGuid()}"
            );

            var storeSQLite = new SQLite.SQLiteStore(path, blockCacheSize: 2, txCacheSize: 2);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(System.IO.Path.Combine(path, "log.txt"),
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{Exception}{NewLine}",
                rollingInterval: RollingInterval.Day, shared: true)
                .CreateLogger();

            //generate sqllite
            Block<DumbAction> genesis = TestUtils.MineGenesis<DumbAction>();
            var _blockChainSQLite = new BlockChain<DumbAction>(
                new NullPolicy<DumbAction>(),
                storeSQLite,
                storeSQLite,
                genesis);

            for (var i = 0; i < 10; i++)
            {
                MakeTenTransactionsWithActions(_blockChainSQLite);
                _blockChainSQLite.MineBlock(Address1).Wait();
            }

            //stage tx only for db test
            MakeTenTransactionsWithActions(_blockChainSQLite);

            var chainIdS = storeSQLite.GetCanonicalChainId();

            var countBlocksS = storeSQLite.CountBlocks();

            var countIndexS = storeSQLite.CountIndex(chainIdS.Value);

            var countTxS = storeSQLite.CountTransactions();

            var txNonceS = storeSQLite.GetTxNonce(chainIdS.Value, Address1);

            var listBlocksS = storeSQLite.IterateBlockHashes().ToList();
            foreach (var hashS in listBlocksS)
            {
                var blockS = storeSQLite.GetBlock<DumbAction>(hashS);
                if (blockS.Transactions.Count() == 0) continue;
                var blockTxS = blockS.Transactions.First();
                var containTxS = storeSQLite.ContainsTransaction(blockTxS.Id);
                var getTxS = storeSQLite.GetTransaction<DumbAction>(blockTxS.Id);
                var containBlockS = storeSQLite.ContainsBlock(hashS);

                var blockDigestS = storeSQLite.GetBlockDigest(hashS);
                var blockIndexS = storeSQLite.GetBlockIndex(hashS);

                var blockStatesS = storeSQLite.GetBlockStates(hashS);
                var containsStatesS = storeSQLite.ContainsBlockStates(hashS);
                storeSQLite.SetBlockStates(hashS, blockStatesS);
                storeSQLite.SetStates(blockS, blockStatesS);

                var indexBlockS = storeSQLite.IndexBlockHash(chainIdS.Value, 1);
                var blockSI = storeSQLite.GetBlock<DumbAction>(indexBlockS.Value);
                storeSQLite.PruneBlockStates(chainIdS.Value, blockSI);

                foreach (var stateS in blockStatesS)
                {
                    var stateBenS = storeSQLite.GetState(stateS.Key, hashS, chainIdS.Value);
                    break;
                }
            }

            //list chain ids
            foreach (var chainSId in storeSQLite.ListChainIds())
            {
                var id = chainSId;
            }

            //list chain ids
            var listIndexS = storeSQLite.IterateIndexes(chainIdS.Value, 0, 100).ToList();
            foreach (var indexS in listIndexS)
            {
                var idx = indexS;
            }

            //list stagedTx
            var allStagedS = storeSQLite.IterateStagedTransactionIds();
            foreach (var stagedTxS in allStagedS)
            {
                var stagedTx = stagedTxS;
            }
            storeSQLite.UnstageTransactionIds(ImmutableHashSet.Create(allStagedS.Last()));

            //list transactionsId
            var listtxS = storeSQLite.IterateTransactionIds().ToList();
            foreach (var txIdS in listtxS)
            {
                var txTd = txIdS;
            }

            //list references
            var liststateS = storeSQLite.ListAllStateReferences(chainIdS.Value).ToList();
            foreach (var stareRefS in liststateS)
            {
                var stareRef = stareRefS;
            }

            //list txNonces
            var listtxNoncesS = storeSQLite.ListTxNonces(chainIdS.Value).ToList();
            foreach (KeyValuePair<Address, long> txNonceItemS in listtxNoncesS)
            {
                var txNonce = txNonceItemS;
            }

            //list ListStateKeys
            var listKeysS = storeSQLite.ListStateKeys(chainIdS.Value).ToList();
            foreach (var txStateKeyS in listKeysS)
            {
                var stateKey = txStateKeyS;

                var itRefS = storeSQLite.IterateStateReferences(chainIdS.Value, stateKey, null, null, null).ToList();
                foreach (var indexS in itRefS)
                {
                    var idx = indexS;

                    storeSQLite.StoreStateReference(chainIdS.Value, ImmutableHashSet.Create(stateKey), indexS.Item1, indexS.Item2);
                }

                var lookupS = storeSQLite.LookupStateReference(chainIdS.Value, stateKey, 11);
            }

            //list ListStateKeys
            var listStateKeysS = storeSQLite.ListStateKeys(chainIdS.Value).ToList();
            foreach (var txStateKeyS in listStateKeysS)
            {
                var stateKey = txStateKeyS;
            }

            //test forks
            var destChainIdS = Guid.NewGuid();
            storeSQLite.SetCanonicalChainId(destChainIdS);

            var forkBlockHashS = storeSQLite.IndexBlockHash(chainIdS.Value, 3);
            var forkBlockS = storeSQLite.GetBlock<DumbAction>(forkBlockHashS.Value);
            storeSQLite.ForkBlockIndexes(chainIdS.Value, destChainIdS, forkBlockHashS.Value);
            var listForkIndexSAll = storeSQLite.IterateIndexes(chainIdS.Value, 0, 100).ToList();
            var listForkIndexS = storeSQLite.IterateIndexes(destChainIdS, 0, 100).ToList();

            storeSQLite.ForkStateReferences(chainIdS.Value, destChainIdS, forkBlockS);
            var listStateKeysForkSorceS = storeSQLite.ListStateKeys(chainIdS.Value).ToList();
            var listStateKeysForkS = storeSQLite.ListStateKeys(destChainIdS).ToList();

            //duplication
            //storeSQLite.ForkStates(chainIdS.Value, destChainIdS, forkBlockS);

            //delete
            var lastBlockHashS = storeSQLite.IterateBlockHashes().Last();
            var lastBlockS = storeSQLite.GetBlock<DumbAction>(lastBlockHashS);
            var deleteBlockS = storeSQLite.DeleteBlock(lastBlockHashS);
            
            var txLastBlockS = lastBlockS.Transactions.First();
            var deleteTxS = storeSQLite.DeleteTransaction(txLastBlockS.Id);

            storeSQLite.DeleteChainId(chainIdS.Value);
        }

        public void MakeTenTransactionsWithActions(BlockChain<DumbAction> blockChain)
        {
            for (var i = 0; i < 5; i++)
            {
                var privateKey = new PrivateKey();
                var address = privateKey.ToAddress();
                var actions = new[]
                {
                    new DumbAction(address, i + "foo"),
                    new DumbAction(address, i + "bar"),
                    new DumbAction(address, i + "baz"),
                    new DumbAction(address, i + "qux"),
                };
                var tx = blockChain.MakeTransaction(privateKey, actions);
                blockChain.StageTransaction(tx);
            }
        }
    }
}