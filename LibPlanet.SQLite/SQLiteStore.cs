using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Blocks;
using Libplanet.Store;
using Libplanet.Tx;
using LibPlanet.SQLiteStore;
using LruCacheNet;
using Microsoft.Data.Sqlite;
using Serilog;

namespace LibPlanet.SQLite
{
    public class SQLiteStore : BaseBlockStatesStore
    {
        private const string BlockDbName = "block";
        private const string TxDbName = "tx";
        private const string StateDbName = "state";
        private const string StagedTxDbName = "stagedtx";
        private const string ChainDbName = "chain";
        private const string StateRefDbName = "stateref";

        private const string DbFile = "blockchain.db";

        private static readonly byte[] IndexKeyPrefix = { (byte)'I' };
        private static readonly byte[] BlockKeyPrefix = { (byte)'B' };
        private static readonly byte[] BlockStateKeyPrefix = { (byte)'S' };
        private static readonly byte[] TxKeyPrefix = { (byte)'T' };
        private static readonly byte[] TxNonceKeyPrefix = { (byte)'N' };
        private static readonly byte[] StagedTxKeyPrefix = { (byte)'t' };
        private static readonly byte[] IndexCountKey = { (byte)'c' };
        private static readonly byte[] CanonicalChainIdIdKey = { (byte)'C' };
        private static readonly byte[] StateRefKeyPrefix = { (byte)'s' };

        private static readonly byte[] EmptyBytes = new byte[0];

        private readonly ILogger _logger;

        private readonly LruCache<TxId, object> _txCache;
        private readonly LruCache<HashDigest<SHA256>, BlockDigest> _blockCache;
        private readonly LruCache<HashDigest<SHA256>, IImmutableDictionary<string, IValue>>
            _statesCache;

        private readonly Dictionary<Guid, LruCache<string, Tuple<HashDigest<SHA256>, long>>>
            _lastStateRefCaches;

        private readonly SqliteConnection _connection;
        private readonly string _path;

        //private readonly RocksDb _blockDb;
        //private readonly RocksDb _txDb;
        //private readonly RocksDb _stateDb;
        //private readonly RocksDb _stagedTxDb;
        //private readonly RocksDb _chainDb;
        //private readonly RocksDb _stateRefDb;

        private string SQLiteDbPath(string dbName) => Path.Combine(_path, dbName);

        public SQLiteStore(
            string path,
            int blockCacheSize = 512,
            int txCacheSize = 1024,
            int statesCacheSize = 10000
        )
        {
            _logger = Log.ForContext<SQLiteStore>();

            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            path = Path.GetFullPath(path);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            _txCache = new LruCache<TxId, object>(capacity: txCacheSize);
            _blockCache = new LruCache<HashDigest<SHA256>, BlockDigest>(capacity: blockCacheSize);
            _statesCache = new LruCache<HashDigest<SHA256>, IImmutableDictionary<string, IValue>>(
                capacity: statesCacheSize
            );
            _lastStateRefCaches =
                new Dictionary<Guid, LruCache<string, Tuple<HashDigest<SHA256>, long>>>();

            _path = path;

            var initDb = false;
            if (!File.Exists(SQLiteDbPath(DbFile))) {
                initDb = true;
            }

            try
            {
                _connection = new SqliteConnection("Data Source=" + SQLiteDbPath(DbFile));
                _connection.Open();

                if (initDb)
                {
                    CreateTables();
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during SQLite db init: {e.Message}.");
            }
        }

        private void CreateTables()
        {
            using (var transaction = _connection.BeginTransaction())
            {
                var createCommand = _connection.CreateCommand();
                createCommand.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS " + BlockDbName + @"(
                        [Index] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        [Key] VARCHAR(128) NOT NULL,
                        [Data] BLOB NOT NULL);
                    CREATE INDEX " + BlockDbName + @"_index ON " + BlockDbName + @"([Key]);
                ";
                createCommand.ExecuteNonQuery();

                createCommand = _connection.CreateCommand();
                createCommand.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS " + TxDbName + @"(
                        [Index] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        [Key] VARCHAR(128) NOT NULL,
                        [Data] BLOB NOT NULL);
                    CREATE INDEX " + TxDbName + @"_index ON " + TxDbName + @"([Key]);
                ";
                createCommand.ExecuteNonQuery();

                transaction.Commit();
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }

        /// <inheritdoc/>
        public override void PutBlock<T>(Block<T> block)
        {
            try
            {
                var helper = new SQLiteHelper();

                if (_blockCache.ContainsKey(block.Hash))
                {
                    return;
                }

                var key = BlockKey(block.Hash);

                if (!(helper.GetBytes(key, BlockDbName, _connection) is null))
                {
                    return;
                }

                foreach (Transaction<T> tx in block.Transactions)
                {
                    PutTransaction(tx);
                }

                byte[] value = block.ToBlockDigest().Serialize();
                helper.PutBytes(key, BlockDbName, value, _connection);
                _blockCache.AddOrUpdate(block.Hash, block.ToBlockDigest());
            }
            catch (Exception e)
            {
                _logger.Error($"Error during PutBlock: {e.Message}.");
            }
        }

        /// <inheritdoc/>
        public override void PutTransaction<T>(Transaction<T> tx)
        {
            try
            {
                var helper = new SQLiteHelper();

                if (_txCache.ContainsKey(tx.Id))
                {
                    return;
                }

                var key = TxKey(tx.Id);

                if (!(helper.GetBytes(key, TxDbName, _connection) is null))
                {
                    return;
                }

                byte[] value = tx.Serialize(true);
                helper.PutBytes(key, TxDbName, value, _connection);
                _txCache.AddOrUpdate(tx.Id, tx);
            }
            catch (Exception e)
            {
                _logger.Error($"Error during PutTransaction: {e.Message}.");
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<HashDigest<SHA256>> IterateBlockHashes()
        {
            byte[] prefix = BlockKeyPrefix;

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT [Key] FROM " + BlockDbName + " ORDER BY [Index] ASC;";
                var reader = cmd.ExecuteReader();

                if ((reader != null) && (reader.HasRows))
                {
                    while (reader.Read())
                    {
                        var key = (byte[])reader.GetValue("Key");
                        byte[] hashBytes = key.Skip(prefix.Length).ToArray();

                        var blockHash = new HashDigest<SHA256>(hashBytes);
                        yield return blockHash;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override Transaction<T> GetTransaction<T>(TxId txid)
        {
            try
            {
                var helper = new SQLiteHelper();

                if (_txCache.TryGetValue(txid, out object cachedTx))
                {
                    return (Transaction<T>)cachedTx;
                }

                var key = TxKey(txid);
                byte[] bytes = helper.GetBytes(key, TxDbName, _connection);

                if (bytes is null)
                {
                    return null;
                }

                Transaction<T> tx = Transaction<T>.Deserialize(bytes);
                _txCache.AddOrUpdate(txid, tx);
                return tx;
            }
            catch (Exception e)
            {
                _logger.Error($"Error during GetTransaction: {e.Message}.");
            }

            return null;
        }

        /// <inheritdoc/>
        public override long CountBlocks()
        {
            try
            {
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT COUNT([Index]) FROM " + BlockDbName + ";";
                    return (long)cmd.ExecuteScalar();
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during CountBlocks: {e.Message}.");
            }

            return -1;
        }

        /// <inheritdoc/>
        public override long CountTransactions()
        {
            try
            {
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT COUNT([Index]) FROM " + TxDbName + ";";
                    return (long)cmd.ExecuteScalar();
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during CountTransactions: {e.Message}.");
            }

            return -1;
        }

        /// <inheritdoc/>
        public override bool ContainsBlock(HashDigest<SHA256> blockHash)
        {
            try
            {
                if (_blockCache.ContainsKey(blockHash))
                {
                    return true;
                }

                var helper = new SQLiteHelper();

                var key = BlockKey(blockHash);
                byte[] bytes = helper.GetBytes(key, BlockDbName, _connection);

                if (bytes != null) 
                    return true;
            }
            catch (Exception e)
            {
                _logger.Error($"Error during ContainsBlock: {e.Message}.");
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool ContainsTransaction(TxId txId)
        {
            try
            {
                if (_txCache.ContainsKey(txId))
                {
                    return true;
                }

                var helper = new SQLiteHelper();

                var key = TxKey(txId);
                byte[] bytes = helper.GetBytes(key, TxDbName, _connection);

                if (bytes != null)
                    return true;
            }
            catch (Exception e)
            {
                _logger.Error($"Error during ContainsTransaction: {e.Message}.");
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool DeleteBlock(HashDigest<SHA256> blockHash)
        {
            try
            {
                var helper = new SQLiteHelper();

                var key = BlockKey(blockHash);
                byte[] bytes = helper.GetBytes(key, BlockDbName, _connection);

                if (bytes != null)
                {
                    _blockCache.Remove(blockHash);
                    helper.RemoveBytes(key, BlockDbName, _connection);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during DeleteBlock: {e.Message}.");
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool DeleteTransaction(TxId txid)
        {
            try
            {
                var helper = new SQLiteHelper();

                var key = TxKey(txid);
                byte[] bytes = helper.GetBytes(key, TxDbName, _connection);

                if (bytes != null)
                {
                    _txCache.Remove(txid);
                    helper.RemoveBytes(key, TxDbName, _connection);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during DeleteTransaction: {e.Message}.");
            }

            return false;
        }

        private string BlockKey(HashDigest<SHA256> blockHash)
        {
            return string.Format("{0}{1}", Encoding.ASCII.GetString(BlockKeyPrefix), blockHash.ToString());
        }

        private string TxKey(TxId txId)
        {
            return string.Format("{0}{1}", Encoding.ASCII.GetString(TxKeyPrefix), txId.ToString());
        }







        public override long AppendIndex(Guid chainId, HashDigest<SHA256> hash)
        {
            throw new NotImplementedException();
        }

        public override long CountIndex(Guid chainId)
        {
            throw new NotImplementedException();
        }

        public override void DeleteChainId(Guid chainId)
        {
            throw new NotImplementedException();
        }

        public override void ForkBlockIndexes(Guid sourceChainId, Guid destinationChainId, HashDigest<SHA256> branchPoint)
        {
            throw new NotImplementedException();
        }

        public override void ForkStateReferences<T>(Guid sourceChainId, Guid destinationChainId, Block<T> branchPoint)
        {
            throw new NotImplementedException();
        }

        public override BlockDigest? GetBlockDigest(HashDigest<SHA256> blockHash)
        {
            throw new NotImplementedException();
        }

        public override IImmutableDictionary<string, IValue> GetBlockStates(HashDigest<SHA256> blockHash)
        {
            throw new NotImplementedException();
        }

        public override Guid? GetCanonicalChainId()
        {
            throw new NotImplementedException();
        }

        public override long GetTxNonce(Guid chainId, Address address)
        {
            throw new NotImplementedException();
        }

        public override void IncreaseTxNonce(Guid chainId, Address signer, long delta = 1)
        {
            throw new NotImplementedException();
        }

        public override HashDigest<SHA256>? IndexBlockHash(Guid chainId, long index)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<HashDigest<SHA256>> IterateIndexes(Guid chainId, int offset, int? limit)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<TxId> IterateStagedTransactionIds()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Tuple<HashDigest<SHA256>, long>> IterateStateReferences(Guid chainId, string key, long? highestIndex = null, long? lowestIndex = null, int? limit = null)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<TxId> IterateTransactionIds()
        {
            throw new NotImplementedException();
        }

        public override IImmutableDictionary<string, IImmutableList<HashDigest<SHA256>>> ListAllStateReferences(Guid chainId, long lowestIndex = 0, long highestIndex = long.MaxValue)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Guid> ListChainIds()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<string> ListStateKeys(Guid chainId)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<KeyValuePair<Address, long>> ListTxNonces(Guid chainId)
        {
            throw new NotImplementedException();
        }

        public override Tuple<HashDigest<SHA256>, long> LookupStateReference(Guid chainId, string key, long lookupUntilBlockIndex)
        {
            throw new NotImplementedException();
        }

        public override void PruneBlockStates<T>(Guid chainId, Block<T> until)
        {
            throw new NotImplementedException();
        }

        public override void SetBlockStates(HashDigest<SHA256> blockHash, IImmutableDictionary<string, IValue> states)
        {
            throw new NotImplementedException();
        }

        public override void SetCanonicalChainId(Guid chainId)
        {
            throw new NotImplementedException();
        }

        public override void StageTransactionIds(IImmutableSet<TxId> txids)
        {
            throw new NotImplementedException();
        }

        public override void StoreStateReference(Guid chainId, IImmutableSet<string> keys, HashDigest<SHA256> blockHash, long blockIndex)
        {
            throw new NotImplementedException();
        }

        public override void UnstageTransactionIds(ISet<TxId> txids)
        {
            throw new NotImplementedException();
        }
    }
}
