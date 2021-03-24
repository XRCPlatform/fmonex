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

            var store = new SQLite.SQLiteStore(path, blockCacheSize: 2, txCacheSize: 2);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(System.IO.Path.Combine(path, "log.txt"),
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{Exception}{NewLine}",
                rollingInterval: RollingInterval.Day, shared: true)
                .CreateLogger();

            Block<DumbAction> genesis = TestUtils.MineGenesis<DumbAction>();
            var _blockChain = new BlockChain<DumbAction>(
                new NullPolicy<DumbAction>(),
                store,
                store,
                genesis);
           
            for (var i = 0; i < 10; i++)
            {
                MakeTenTransactionsWithActions(_blockChain);
                _blockChain.MineBlock(Address1).Wait();
            }

            //stage tx only for db test
            MakeTenTransactionsWithActions(_blockChain);
        }

        public void MakeTenTransactionsWithActions(BlockChain<DumbAction> blockChain)
        {
            for (var i = 0; i < 5; i++)
            {
                var privateKey = new PrivateKey();
                var address = privateKey.ToAddress();
                var actions = new[]
                {
                    new DumbAction(address, "foo"),
                    new DumbAction(address, "bar"),
                    new DumbAction(address, "baz"),
                    new DumbAction(address, "qux"),
                };
                var tx = blockChain.MakeTransaction(privateKey, actions);
                blockChain.StageTransaction(tx);
            }
        }
    }
}