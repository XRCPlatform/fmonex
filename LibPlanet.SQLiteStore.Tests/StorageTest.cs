using Libplanet;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Tests.Common.Action;
using Libplanet.Tx;
using NUnit.Framework;
using System;
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
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"sqlitedb_test_{Guid.NewGuid()}"
            );

            var store = new SQLite.SQLiteStore(path, blockCacheSize: 2, txCacheSize: 2);

            PrivateKey[] privKeys =
    Enumerable.Repeat((object)null, 5).Select(_ => new PrivateKey()).ToArray();
            var random = new System.Random();
            Transaction<DumbAction>[] txs = Enumerable.Range(0, 50)
                .Select(i => (privKeys[i % privKeys.Length], i / privKeys.Length))
                .Select(pair =>
                    Transaction<DumbAction>.Create(
                        nonce: pair.Item2,
                        privateKey: pair.Item1,
                        genesisHash: null,
                        actions: new DumbAction[0]
                    )
                )
                .OrderBy(_ => random.Next())
                .ToArray();
            var block = new Block<DumbAction>(
                index: 0,
                difficulty: 0,
                totalDifficulty: 0,
                nonce: new Nonce(new byte[0]),
                miner: null,
                previousHash: null,
                timestamp: DateTimeOffset.UtcNow,
                transactions: txs
            );

            store.PutBlock(block);
        }
    }
}