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
        private const string GeneralDbName = "general";

        private const string DbFile = "blockchain.db";
        private const int DbFileVersion = 1;

        private static readonly byte[] IndexKeyPrefix = { (byte)'I' };
        private static readonly byte[] TxNonceKeyPrefix = { (byte)'N' };
        private static readonly byte[] IndexCountKey = { (byte)'c' };
        private static readonly byte[] CanonicalChainIdIdKey = { (byte)'C' };
        private static readonly byte[] VersionKeyPrefix = { (byte)'V' };

        private static readonly byte[] EmptyBytes = new byte[0];

        private readonly ILogger _logger;

        private readonly LruCache<TxId, object> _txCache;
        private readonly LruCache<HashDigest<SHA256>, BlockDigest> _blockCache;
        private readonly LruCache<HashDigest<SHA256>, IImmutableDictionary<string, IValue>>
            _statesCache;

        private readonly Dictionary<Guid, LruCache<string, Tuple<HashDigest<SHA256>, long>>>
            _lastStateRefCaches;

        private readonly string _path;

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

        private string GetConnectionString()
        {
            return "Data Source=" + SQLiteDbPath(DbFile);
        }

        private void CreateTables()
        {
            var helper = new SQLiteHelper();

            using (var _connection = new SqliteConnection(GetConnectionString()))
            {
                _connection.Open();

                using (var transaction = _connection.BeginTransaction())
                {
                    using (var createCommand = _connection.CreateCommand())
                    {
                        createCommand.CommandText =
                        @"
                            CREATE TABLE IF NOT EXISTS " + BlockDbName + @"(
                                [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                [Key] VARCHAR(128) NOT NULL,
                                [Data] BLOB NOT NULL);
                            CREATE INDEX " + BlockDbName + @"_index ON " + BlockDbName + @"([Key]);
                        ";
                        createCommand.ExecuteNonQuery();
                    }

                    using (var createCommand = _connection.CreateCommand())
                    {
                        createCommand.CommandText =
                        @"
                            CREATE TABLE IF NOT EXISTS " + TxDbName + @"(
                                [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                [Key] VARCHAR(128) NOT NULL,
                                [Data] BLOB NOT NULL);
                            CREATE INDEX " + TxDbName + @"_index ON " + TxDbName + @"([Key]);
                        ";
                        createCommand.ExecuteNonQuery();
                    }

                    using (var createCommand = _connection.CreateCommand())
                    {
                        createCommand.CommandText =
                        @"
                            CREATE TABLE IF NOT EXISTS " + StateDbName + @"(
                                [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                [Key] VARCHAR(128) NOT NULL,
                                [Data] BLOB NOT NULL);
                            CREATE INDEX " + StateDbName + @"_index ON " + StateDbName + @"([Key]);
                        ";
                        createCommand.ExecuteNonQuery();
                    }

                    using (var createCommand = _connection.CreateCommand())
                    {
                        createCommand.CommandText =
                        @"
                            CREATE TABLE IF NOT EXISTS " + StagedTxDbName + @"(
                                [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                [Key] VARCHAR(128) NOT NULL,
                                [Data] BLOB NOT NULL);
                            CREATE INDEX " + StagedTxDbName + @"_index ON " + StagedTxDbName + @"([Key]);
                        ";
                        createCommand.ExecuteNonQuery();
                    }

                    using (var createCommand = _connection.CreateCommand())
                    {
                        createCommand.CommandText =
                        @"
                            CREATE TABLE IF NOT EXISTS " + StateRefDbName + @"(
                                [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                [ParentId] INTEGER,
                                [Key] VARCHAR(128) NOT NULL,
                                [KeyIndex] INTEGER NOT NULL,
                                [Data] BLOB NOT NULL);
                            CREATE INDEX " + StateRefDbName + @"_indexKey ON " + StateRefDbName + @"([Key]);
                            CREATE INDEX " + StateRefDbName + @"_indexParentId ON " + StateRefDbName + @"([ParentId]);
                        ";
                        createCommand.ExecuteNonQuery();
                    }

                    using (var createCommand = _connection.CreateCommand())
                    {
                        createCommand.CommandText =
                        @"
                            CREATE TABLE IF NOT EXISTS " + ChainDbName + @"(
                                [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                [ParentId] INTEGER,
                                [Type] VARCHAR(1) NOT NULL,
                                [Key] VARCHAR(128) NOT NULL,
                                [Data] BLOB NOT NULL);
                            CREATE INDEX " + ChainDbName + @"_indexType ON " + ChainDbName + @"([Type]);
                            CREATE INDEX " + ChainDbName + @"_indexKey ON " + ChainDbName + @"([Key]);
                            CREATE INDEX " + ChainDbName + @"_indexParentId ON " + ChainDbName + @"([ParentId]);
                        ";
                        createCommand.ExecuteNonQuery();
                    }

                    using (var createCommand = _connection.CreateCommand())
                    {
                        createCommand.CommandText =
                        @"
                            CREATE TABLE IF NOT EXISTS " + GeneralDbName + @"(
                                [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                [Type] VARCHAR(1) NOT NULL,
                                [Data] VARCHAR(128) NOT NULL);
                            CREATE INDEX " + GeneralDbName + @"_index ON " + ChainDbName + @"([Id])
                        ";
                        createCommand.ExecuteNonQuery();
                    }

                    using (var createCommand = _connection.CreateCommand())
                    {
                        createCommand.Parameters.AddWithValue("@type", helper.GetString(VersionKeyPrefix));
                        createCommand.Parameters.AddWithValue("@data", DbFileVersion);
                        createCommand.CommandType = CommandType.Text;
                        createCommand.CommandText = "INSERT INTO " + GeneralDbName + @" ([Type], [Data]) VALUES (@type, @data);";
                        createCommand.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {

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

                if (!(helper.GetBytes(key, BlockDbName, GetConnectionString()) is null))
                {
                    return;
                }

                foreach (Transaction<T> tx in block.Transactions)
                {
                    PutTransaction(tx);
                }

                byte[] value = block.ToBlockDigest().Serialize();
                helper.PutBytes(key, BlockDbName, value, GetConnectionString());
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

                if (!(helper.GetBytes(key, TxDbName, GetConnectionString()) is null))
                {
                    return;
                }

                byte[] value = tx.Serialize(true);
                helper.PutBytes(key, TxDbName, value, GetConnectionString());
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
            var helper = new SQLiteHelper();

            using (var _connection = new SqliteConnection(GetConnectionString()))
            {
                _connection.Open();

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT [Key] FROM " + BlockDbName + " ORDER BY [Id] ASC;";
                    var reader = cmd.ExecuteReader();

                    if ((reader != null) && (reader.HasRows))
                    {
                        while (reader.Read())
                        {
                            var key = (string)reader.GetValue("Key");

                            var blockHash = new HashDigest<SHA256>(helper.ParseHex(key));
                            yield return blockHash;
                        }
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
                byte[] bytes = helper.GetBytes(key, TxDbName, GetConnectionString());

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
                using (var _connection = new SqliteConnection(GetConnectionString()))
                {
                    _connection.Open();

                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT COUNT([Id]) FROM " + BlockDbName + ";";
                        return (long)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during CountBlocks: {e.Message}.");
            }

            return 0;
        }

        /// <inheritdoc/>
        public override long CountTransactions()
        {
            try
            {
                using (var _connection = new SqliteConnection(GetConnectionString()))
                {
                    _connection.Open();

                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT COUNT([Id]) FROM " + TxDbName + ";";
                        return (long)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during CountTransactions: {e.Message}.");
            }

            return 0;
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
                byte[] bytes = helper.GetBytes(key, BlockDbName, GetConnectionString());

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
                byte[] bytes = helper.GetBytes(key, TxDbName, GetConnectionString());

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
                byte[] bytes = helper.GetBytes(key, BlockDbName, GetConnectionString());

                if (bytes != null)
                {
                    _blockCache.Remove(blockHash);
                    helper.RemoveBytes(key, BlockDbName, GetConnectionString());

                    return true;
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
                byte[] bytes = helper.GetBytes(key, TxDbName, GetConnectionString());

                if (bytes != null)
                {
                    _txCache.Remove(txid);
                    helper.RemoveBytes(key, TxDbName, GetConnectionString());

                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during DeleteTransaction: {e.Message}.");
            }

            return false;
        }

        /// <inheritdoc/>
        public override void SetBlockStates(
            HashDigest<SHA256> blockHash,
            IImmutableDictionary<string, IValue> states)
        {
            try
            {
                var helper = new SQLiteHelper();

                var serialized = new Bencodex.Types.Dictionary(
                    states.ToImmutableDictionary(
                        kv => (IKey)(Text)kv.Key,
                        kv => kv.Value
                    )
                );

                var key = BlockStateKey(blockHash);

                var codec = new Codec();
                byte[] value = codec.Encode(serialized);

                helper.PutBytes(key, StateDbName, value, GetConnectionString());
                _statesCache.AddOrUpdate(blockHash, states);
            }
            catch (Exception e)
            {
                _logger.Error($"Error during SetBlockStates: {e.Message}.");
            }
        }

        /// <inheritdoc/>
        public override IImmutableDictionary<string, IValue> GetBlockStates(
            HashDigest<SHA256> blockHash
        )
        {
            try
            {
                var helper = new SQLiteHelper();

                if (_statesCache.TryGetValue(
                    blockHash,
                    out IImmutableDictionary<string, IValue> cached))
                {
                    return cached;
                }

                var key = BlockStateKey(blockHash);
                byte[] bytes = helper.GetBytes(key, StateDbName, GetConnectionString());

                if (bytes is null)
                {
                    return null;
                }

                IValue value = new Codec().Decode(bytes);
                if (!(value is Bencodex.Types.Dictionary dict))
                {
                    throw new DecodingException(
                        $"Expected {typeof(Bencodex.Types.Dictionary)} but " +
                        $"{value.GetType()}");
                }

                ImmutableDictionary<string, IValue> states = dict.ToImmutableDictionary(
                    kv => ((Text)kv.Key).Value,
                    kv => kv.Value
                );
                _statesCache.AddOrUpdate(blockHash, states);
                return states;

            }
            catch (Exception e)
            {
                _logger.Error($"Error during GetBlockStates: {e.Message}.");
            }

            return null;
        }

        /// <summary>
        /// Deletes the states with specified keys (i.e., <paramref name="stateKeys"/>)
        /// updated by actions in the specified block (i.e., <paramref name="blockHash"/>).
        /// </summary>
        /// <param name="blockHash"><see cref="Block{T}.Hash"/> to delete states.
        /// </param>
        /// <param name="stateKeys">The state keys to delete which were updated by actions
        /// in the specified block (i.e., <paramref name="blockHash"/>).
        /// </param>
        /// <seealso cref="GetBlockStates"/>
        private void DeleteBlockStates(
            HashDigest<SHA256> blockHash,
            IEnumerable<string> stateKeys)
        {
            try
            {
                var helper = new SQLiteHelper();

                IImmutableDictionary<string, IValue> dict = GetBlockStates(blockHash);
                if (dict is null)
                {
                    return;
                }

                dict = dict.RemoveRange(stateKeys);
                if (dict.Any())
                {
                    SetBlockStates(blockHash, dict);
                }
                else
                {
                    var key = BlockStateKey(blockHash);

                    helper.RemoveBytes(key, StateDbName, GetConnectionString());
                    _statesCache.Remove(blockHash);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during DeleteBlockStates: {e.Message}.");
            }
        }

        /// <inheritdoc/>
        public override long CountIndex(Guid chainId)
        {
            try
            {
                var helper = new SQLiteHelper();

                using (var _connection = new SqliteConnection(GetConnectionString()))
                {
                    _connection.Open();

                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@key", chainId.ToString());
                        cmd.Parameters.AddWithValue("@typeD", helper.GetString(IndexCountKey));
                        cmd.Parameters.AddWithValue("@type", helper.GetString(CanonicalChainIdIdKey));
                        cmd.CommandText = "SELECT TD.[Data] FROM " + ChainDbName + " AS T " +
                            "LEFT JOIN " + ChainDbName + " AS TD ON T.[Id] = TD.[ParentId] AND TD.[Type] = @typeD " +
                            "WHERE T.[Key] = @key AND T.[Type] = @type;";
                        var reader = cmd.ExecuteReader();

                        if ((reader != null) && (reader.HasRows))
                        {
                            reader.Read();
                            var data = (byte[])reader.GetValue("Data");
                            return helper.ToInt64(data);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during CountIndex: {e.Message}.");
            }

            return 0;
        }

        /// <inheritdoc/>
        public override long AppendIndex(Guid chainId, HashDigest<SHA256> hash)
        {
            try
            {
                var helper = new SQLiteHelper();

                var existChainId = ExistCanonicalChainId(chainId);
                if (!existChainId) SetCanonicalChainId(chainId);

                long index = CountIndex(chainId);

                byte[] indexBytes = helper.GetBytes(index);
                byte[] key = IndexKeyPrefix.Concat(indexBytes).ToArray();

                using (var _connection = new SqliteConnection(GetConnectionString()))
                {
                    _connection.Open();

                    using (var firstTransaction = _connection.BeginTransaction())
                    {
                        long parentChainDbId = 0;
                        using (var cmd = _connection.CreateCommand())
                        {
                            cmd.Parameters.AddWithValue("@key", chainId.ToString());
                            cmd.Parameters.AddWithValue("@typeC", helper.GetString(CanonicalChainIdIdKey));
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = "SELECT [Id] FROM " + ChainDbName + " WHERE [Key] = @key AND [Type] = @typeC;";
                            parentChainDbId = (long)cmd.ExecuteScalar();
                        }

                        var data = helper.GetBytes(index + 1);
                        using (var cmd = _connection.CreateCommand())
                        {
                            cmd.Parameters.Add("@data", SqliteType.Blob, data.Length).Value = data;
                            cmd.Parameters.AddWithValue("@type", helper.GetString(IndexCountKey));
                            cmd.Parameters.AddWithValue("@parentId", parentChainDbId);
                            cmd.Parameters.AddWithValue("@key", helper.GetString(IndexCountKey));
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = "UPDATE " + ChainDbName + " SET [Key] = @key, [Data] = @data " +
                                "WHERE [Type] = @type AND [ParentId] = @parentId;";
                            cmd.ExecuteNonQuery();
                        }

                        data = hash.ToByteArray();
                        using (var cmd = _connection.CreateCommand())
                        {
                            cmd.Parameters.Add("@data", SqliteType.Blob, data.Length).Value = data;
                            cmd.Parameters.AddWithValue("@type", helper.GetString(IndexKeyPrefix));
                            cmd.Parameters.AddWithValue("@parentId", parentChainDbId);
                            cmd.Parameters.AddWithValue("@key", helper.Hex(key));
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = "INSERT INTO " + ChainDbName + @" ([ParentId], [Type], [Key], [Data]) VALUES (@parentId, @type, @key, @data);";
                            cmd.ExecuteNonQuery();
                        }

                        firstTransaction.Commit();
                    }
                }

                return index;
            }
            catch (Exception e)
            {
                _logger.Error($"Error during AppendIndex: {e.Message}.");
            }

            return 0;
        }

        private bool ExistCanonicalChainId(Guid chainId)
        {
            try
            {
                var helper = new SQLiteHelper();

                using (var _connection = new SqliteConnection(GetConnectionString()))
                {
                    _connection.Open();

                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@key", chainId.ToString());
                        cmd.Parameters.AddWithValue("@type", helper.GetString(CanonicalChainIdIdKey));
                        cmd.CommandText = "SELECT COUNT([Id]) FROM " + ChainDbName + " " +
                            "WHERE [Key] = @key AND [Type] = @type;";
                        var count = (long)cmd.ExecuteScalar();

                        if (count > 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during CountIndex: {e.Message}.");
            }

            return false;
        }

        /// <inheritdoc />
        public override void SetCanonicalChainId(Guid chainId)
        {
            try
            {
                var helper = new SQLiteHelper();

                long parentChainDbId = 0;
                byte[] data = chainId.ToByteArray();

                using (var _connection = new SqliteConnection(GetConnectionString()))
                {
                    _connection.Open();

                    using (var firstTransaction = _connection.BeginTransaction(IsolationLevel.ReadUncommitted))
                    {
                        using (var cmd = _connection.CreateCommand())
                        {
                            cmd.Parameters.Add("@data", SqliteType.Blob, data.Length).Value = data;
                            cmd.Parameters.AddWithValue("@type", helper.GetString(CanonicalChainIdIdKey));
                            cmd.Parameters.AddWithValue("@key", chainId.ToString());
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = "INSERT INTO " + ChainDbName + @" ([Type], [Key], [Data]) VALUES (@type, @key, @data); " +
                                @"SELECT last_insert_rowid();";
                            parentChainDbId = (long)cmd.ExecuteScalar();
                        }

                        data = EmptyBytes;
                        using (var cmd = _connection.CreateCommand())
                        {
                            cmd.Parameters.Add("@data", SqliteType.Blob, data.Length).Value = data;
                            cmd.Parameters.AddWithValue("@type", helper.GetString(IndexCountKey));
                            cmd.Parameters.AddWithValue("@key", helper.GetString(IndexCountKey));
                            cmd.Parameters.AddWithValue("@parentId", parentChainDbId);
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = "INSERT INTO " + ChainDbName + @" ([ParentId], [Type], [Key], [Data]) VALUES (@parentId, @type, @key, @data);";
                            cmd.ExecuteNonQuery();
                        }

                        firstTransaction.Commit();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during SetCanonicalChainId: {e.Message}.");
            }
        }

        /// <inheritdoc/>
        public override void DeleteChainId(Guid chainId)
        {
            try
            {
                var helper = new SQLiteHelper();

                _logger.Debug($"Deleting chainID: {chainId}.");
                _lastStateRefCaches.Remove(chainId);

                using (var _connection = new SqliteConnection(GetConnectionString()))
                {
                    _connection.Open();

                    long parentChainDbId = 0;
                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.Parameters.AddWithValue("@key", chainId.ToString());
                        cmd.Parameters.AddWithValue("@typeC", helper.GetString(CanonicalChainIdIdKey));
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT [Id] FROM " + ChainDbName + " WHERE [Key] = @key AND [Type] = @typeC;";
                        parentChainDbId = (long)cmd.ExecuteScalar();
                    }

                    using (var firstTransaction = _connection.BeginTransaction())
                    {
                        using (var cmd = _connection.CreateCommand())
                        {
                            cmd.Parameters.AddWithValue("@parentId", parentChainDbId);
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = "DELETE FROM " + StateRefDbName + " WHERE [ParentId] = @parentId;";
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = _connection.CreateCommand())
                        {
                            cmd.Parameters.AddWithValue("@parentId", parentChainDbId);
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = "DELETE FROM " + ChainDbName + " WHERE [ParentId] = @parentId;";
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = _connection.CreateCommand())
                        {
                            cmd.Parameters.AddWithValue("@id", parentChainDbId);
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = "DELETE FROM " + ChainDbName + " WHERE [Id] = @id;";
                            cmd.ExecuteNonQuery();
                        }

                        firstTransaction.Commit();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during DeleteChainId: {e.Message}.");
            }
        }

        /// <inheritdoc/>
        public override BlockDigest? GetBlockDigest(HashDigest<SHA256> blockHash)
        {
            try
            {
                if (_blockCache.TryGetValue(blockHash, out BlockDigest cachedDigest))
                {
                    return cachedDigest;
                }

                var helper = new SQLiteHelper();
                var key = BlockKey(blockHash);

                byte[] bytes = helper.GetBytes(key, BlockDbName, GetConnectionString());
                if (bytes is null)
                {
                    return null;
                }

                BlockDigest blockDigest = BlockDigest.Deserialize(bytes);

                _blockCache.AddOrUpdate(blockHash, blockDigest);
                return blockDigest;
            }
            catch (Exception e)
            {
                _logger.Error($"Error during GetBlockDigest: {e.Message}.");
            }

            return null;
        }

        /// <inheritdoc />
        public override Guid? GetCanonicalChainId()
        {
            try
            {
                var helper = new SQLiteHelper();

                using (var _connection = new SqliteConnection(GetConnectionString()))
                {
                    _connection.Open();

                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.Parameters.AddWithValue("@typeC", helper.GetString(CanonicalChainIdIdKey));
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT [Data] FROM " + ChainDbName + " WHERE [Type] = @typeC ORDER BY [Id] ASC LIMIT 1;";
                        var bytes = (byte[])cmd.ExecuteScalar();

                        return bytes is null
                            ? (Guid?)null
                            : new Guid(bytes);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during GetCanonicalChainId: {e.Message}.");
            }

            return (Guid?)null;
        }

        /// <inheritdoc/>
        public override long GetTxNonce(Guid chainId, Address address)
        {
            try
            {
                var helper = new SQLiteHelper();

                var existChainId = ExistCanonicalChainId(chainId);
                if (!existChainId) SetCanonicalChainId(chainId);

                var key = TxNonceKey(address);

                using (var _connection = new SqliteConnection(GetConnectionString()))
                {
                    _connection.Open();

                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@key", key);
                        cmd.Parameters.AddWithValue("@keyChainId", chainId.ToString());
                        cmd.Parameters.AddWithValue("@typeD", helper.GetString(TxNonceKeyPrefix));
                        cmd.Parameters.AddWithValue("@type", helper.GetString(CanonicalChainIdIdKey));
                        cmd.CommandText = "SELECT TD.[Data] FROM " + ChainDbName + " AS T " +
                            "JOIN " + ChainDbName + " AS TD ON T.[Id] = TD.[ParentId] AND TD.[Type] = @typeD " +
                            "WHERE TD.[Key] = @key AND T.[Type] = @type AND T.[Key] = @keyChainId;";
                        var reader = cmd.ExecuteReader();

                        if ((reader != null) && (reader.HasRows))
                        {
                            reader.Read();
                            var bytes = (byte[])reader.GetValue("Data");

                            return bytes is null
                                ? 0
                                : helper.ToInt64(bytes);
                        }

                        return 0;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during GetTxNonce: {e.Message}.");
            }

            return 0;
        }

        /// <inheritdoc/>
        public override void IncreaseTxNonce(Guid chainId, Address signer, long delta = 1)
        {
            try
            {
                var helper = new SQLiteHelper();

                long nonce = GetTxNonce(chainId, signer);
                long nextNonce = nonce + delta;

                var key = TxNonceKey(signer);
                byte[] bytes = helper.GetBytes(nextNonce);

                using (var _connection = new SqliteConnection(GetConnectionString()))
                {
                    _connection.Open();

                    long parentChainDbId = 0;
                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.Parameters.AddWithValue("@key", chainId.ToString());
                        cmd.Parameters.AddWithValue("@typeC", helper.GetString(CanonicalChainIdIdKey));
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT [Id] FROM " + ChainDbName + " WHERE [Key] = @key AND [Type] = @typeC;";
                        parentChainDbId = (long)cmd.ExecuteScalar();
                    }

                    long nonceId = 0;
                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        cmd.Parameters.AddWithValue("@type", helper.GetString(TxNonceKeyPrefix));
                        cmd.Parameters.AddWithValue("@parentId", parentChainDbId);
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT [Id] FROM " + ChainDbName + " WHERE [ParentId] = @parentId AND [Type] = @type AND [Key] = @key LIMIT 1;";
                        var obj = cmd.ExecuteScalar();

                        if ((obj != null) && !obj.Equals(DBNull.Value))
                        {
                            nonceId = (long)obj;
                        }
                    }

                    using (var firstTransaction = _connection.BeginTransaction())
                    {
                        if (nonceId == 0)
                        {
                            using (var cmd = _connection.CreateCommand())
                            {
                                cmd.Parameters.Add("@data", SqliteType.Blob, bytes.Length).Value = bytes;
                                cmd.Parameters.AddWithValue("@type", helper.GetString(TxNonceKeyPrefix));
                                cmd.Parameters.AddWithValue("@parentId", parentChainDbId);
                                cmd.Parameters.AddWithValue("@key", key);
                                cmd.CommandType = CommandType.Text;
                                cmd.CommandText = "INSERT INTO " + ChainDbName + @" ([ParentId], [Type], [Key], [Data]) VALUES (@parentId, @type, @key, @data);";
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (var cmd = _connection.CreateCommand())
                            {
                                cmd.Parameters.Add("@data", SqliteType.Blob, bytes.Length).Value = bytes;
                                cmd.Parameters.AddWithValue("@nonceId", nonceId);
                                cmd.CommandType = CommandType.Text;
                                cmd.CommandText = "UPDATE " + ChainDbName + @" SET [Data] = @Data WHERE [Id] = @nonceId;";
                                cmd.ExecuteNonQuery();
                            }
                        }

                        firstTransaction.Commit();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during DeleteChainId: {e.Message}.");
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<HashDigest<SHA256>> IterateIndexes(
            Guid chainId,
            int offset,
            int? limit)
        {
            int count = 0;
            var helper = new SQLiteHelper();

            using (var _connection = new SqliteConnection(GetConnectionString()))
            {
                _connection.Open();

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@key", chainId.ToString());
                    cmd.Parameters.AddWithValue("@typeD", helper.GetString(IndexKeyPrefix));
                    cmd.Parameters.AddWithValue("@typeC", helper.GetString(CanonicalChainIdIdKey));
                    cmd.Parameters.AddWithValue("@offset", offset);
                    cmd.Parameters.AddWithValue("@limit", long.MaxValue);
                    cmd.CommandText = "SELECT TD.[Data] FROM " + ChainDbName + " AS T " +
                        "LEFT JOIN " + ChainDbName + " AS TD ON T.[Id] = TD.[ParentId] AND TD.[Type] = @typeD " +
                        "WHERE T.[Key] = @key AND T.[Type] = @typeC ORDER BY T.[Id] ASC LIMIT @limit OFFSET @offset;";
                    var reader = cmd.ExecuteReader();

                    if ((reader != null) && (reader.HasRows))
                    {
                        while (reader.Read())
                        {
                            if ((limit.HasValue) && (count >= limit))
                            {
                                break;
                            }

                            byte[] value = (byte[])reader.GetValue("Data");
                            yield return new HashDigest<SHA256>(value);

                            count += 1;
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<TxId> IterateStagedTransactionIds()
        {
            var helper = new SQLiteHelper();

            using (var _connection = new SqliteConnection(GetConnectionString()))
            {
                _connection.Open();

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT [Key] FROM " + StagedTxDbName + " ORDER BY [Id] ASC;";
                    var reader = cmd.ExecuteReader();

                    if ((reader != null) && (reader.HasRows))
                    {
                        while (reader.Read())
                        {
                            var key = (string)reader.GetValue("Key");
                            var txId = new TxId(helper.ParseHex(key));
                            yield return txId;
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override HashDigest<SHA256>? IndexBlockHash(Guid chainId, long index)
        {
            try
            {
                var helper = new SQLiteHelper();

                if (index < 0)
                {
                    index += CountIndex(chainId);

                    if (index < 0)
                    {
                        return null;
                    }
                }

                byte[] indexBytes = helper.GetBytes(index);
                byte[] key = IndexKeyPrefix.Concat(indexBytes).ToArray();

                using (var _connection = new SqliteConnection(GetConnectionString()))
                {
                    _connection.Open();

                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@key", helper.Hex(key));
                        cmd.Parameters.AddWithValue("@keyChainId", chainId.ToString());
                        cmd.Parameters.AddWithValue("@typeD", helper.GetString(IndexKeyPrefix));
                        cmd.Parameters.AddWithValue("@type", helper.GetString(CanonicalChainIdIdKey));
                        cmd.CommandText = "SELECT TD.[Data] FROM " + ChainDbName + " AS T " +
                            "JOIN " + ChainDbName + " AS TD ON T.[Id] = TD.[ParentId] AND TD.[Type] = @typeD " +
                            "WHERE TD.[Key] = @key AND T.[Type] = @type AND T.[Key] = @keyChainId;";
                        var reader = cmd.ExecuteReader();

                        if ((reader != null) && (reader.HasRows))
                        {
                            reader.Read();
                            var bytes = (byte[])reader.GetValue("Data");

                            return bytes is null
                                ? (HashDigest<SHA256>?)null
                                : new HashDigest<SHA256>(bytes);
                        }

                        return (HashDigest<SHA256>?)null;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during IndexBlockHash: {e.Message}.");
            }

            return (HashDigest<SHA256>?)null;
        }

        /// <inheritdoc/>
        public override IEnumerable<TxId> IterateTransactionIds()
        {
            var helper = new SQLiteHelper();

            using (var _connection = new SqliteConnection(GetConnectionString()))
            {
                _connection.Open();
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT [Key] FROM " + TxDbName + " ORDER BY [Id] ASC;";
                    var reader = cmd.ExecuteReader();

                    if ((reader != null) && (reader.HasRows))
                    {
                        while (reader.Read())
                        {
                            var key = (string)reader.GetValue("Key");
                            var txId = new TxId(helper.ParseHex(key));
                            yield return txId;
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<Guid> ListChainIds()
        {
            var helper = new SQLiteHelper();

            using (var _connection = new SqliteConnection(GetConnectionString()))
            {
                _connection.Open();
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@type", helper.GetString(CanonicalChainIdIdKey));
                    cmd.CommandText = "SELECT [Key] FROM " + ChainDbName + " WHERE [Type] = @type ORDER BY [Id] ASC;";
                    var reader = cmd.ExecuteReader();

                    if ((reader != null) && (reader.HasRows))
                    {
                        while (reader.Read())
                        {
                            var key = (string)reader.GetValue("Key");

                            Guid guid;

                            try
                            {
                                guid = Guid.Parse(key);
                            }
                            catch (FormatException)
                            {
                                continue;
                            }

                            yield return guid;
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void StageTransactionIds(IImmutableSet<TxId> txids)
        {
            var helper = new SQLiteHelper();

            foreach (TxId txId in txids)
            {
                var key = StagedTxKey(txId);
                helper.PutBytes(key, StagedTxDbName, EmptyBytes, GetConnectionString());
            }
        }

        /// <inheritdoc/>
        public override void UnstageTransactionIds(ISet<TxId> txids)
        {
            var helper = new SQLiteHelper();

            foreach (TxId txId in txids)
            {
                var key = StagedTxKey(txId);
                helper.RemoveBytes(key, StagedTxDbName, GetConnectionString());
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<KeyValuePair<Address, long>> ListTxNonces(Guid chainId)
        {
            var helper = new SQLiteHelper();

            using (var _connection = new SqliteConnection(GetConnectionString()))
            {
                _connection.Open();

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@key", chainId.ToString());
                    cmd.Parameters.AddWithValue("@typeD", helper.GetString(TxNonceKeyPrefix));
                    cmd.Parameters.AddWithValue("@typeC", helper.GetString(CanonicalChainIdIdKey));
                    cmd.CommandText = "SELECT TD.[Key], TD.[Data] FROM " + ChainDbName + " AS T " +
                        "LEFT JOIN " + ChainDbName + " AS TD ON T.[Id] = TD.[ParentId] AND TD.[Type] = @typeD " +
                        "WHERE T.[Key] = @key AND T.[Type] = @typeC;";
                    var reader = cmd.ExecuteReader();

                    if ((reader != null) && (reader.HasRows))
                    {
                        while (reader.Read())
                        {
                            var key = (string)reader.GetValue("Key");
                            byte[] data = (byte[])reader.GetValue("Data");

                            var address = new Address(key);
                            long nonce = helper.ToInt64(data);
                            yield return new KeyValuePair<Address, long>(address, nonce);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void ForkBlockIndexes(
            Guid sourceChainId,
            Guid destinationChainId,
            HashDigest<SHA256> branchPoint)
        {
            HashDigest<SHA256>? genesisHash = IterateIndexes(sourceChainId, 0, 1)
                .Cast<HashDigest<SHA256>?>()
                .FirstOrDefault();

            if (genesisHash is null || branchPoint.Equals(genesisHash))
            {
                return;
            }

            var hashIndexes = IterateIndexes(sourceChainId, 1, null).ToList();
            foreach (HashDigest<SHA256> hash in hashIndexes)
            {
                AppendIndex(destinationChainId, hash);

                if (hash.Equals(branchPoint))
                {
                    break;
                }
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<string> ListStateKeys(Guid chainId)
        {
            var prevStateKey = string.Empty;

            var helper = new SQLiteHelper();

            using (var _connection = new SqliteConnection(GetConnectionString()))
            {
                _connection.Open();

                long parentChainDbId = 0;
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.Parameters.AddWithValue("@key", chainId.ToString());
                    cmd.Parameters.AddWithValue("@typeC", helper.GetString(CanonicalChainIdIdKey));
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT [Id] FROM " + ChainDbName + " WHERE [Key] = @key AND [Type] = @typeC;";
                    parentChainDbId = (long)cmd.ExecuteScalar();
                }

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@parentId", parentChainDbId);
                    cmd.CommandText = "SELECT [Key], [KeyIndex], [Data] FROM " + StateRefDbName + " WHERE [ParentId] = @parentId ORDER BY [Id] ASC;";
                    var reader = cmd.ExecuteReader();

                    if ((reader != null) && (reader.HasRows))
                    {
                        while (reader.Read())
                        {
                            var stateKey = (string)reader.GetValue("Key");

                            if (stateKey != prevStateKey)
                            {
                                yield return stateKey;
                                prevStateKey = stateKey;
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void StoreStateReference(
            Guid chainId,
            IImmutableSet<string> keys,
            HashDigest<SHA256> blockHash,
            long blockIndex)
        {
            try
            {
                var helper = new SQLiteHelper();

                using (var _connection = new SqliteConnection(GetConnectionString()))
                {
                    _connection.Open();

                    long parentChainDbId = 0;
                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.Parameters.AddWithValue("@key", chainId.ToString());
                        cmd.Parameters.AddWithValue("@typeC", helper.GetString(CanonicalChainIdIdKey));
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT [Id] FROM " + ChainDbName + " WHERE [Key] = @key AND [Type] = @typeC;";
                        parentChainDbId = (long)cmd.ExecuteScalar();
                    }

                    if (keys.Count > 0)
                    {
                        using (var firstTransaction = _connection.BeginTransaction())
                        {
                            foreach (string key in keys)
                            {
                                var stateKey = StateRefKey(key);
                                long? stateRefId = null;
                                using (var cmd = _connection.CreateCommand())
                                {
                                    cmd.Parameters.AddWithValue("@parentId", parentChainDbId);
                                    cmd.Parameters.AddWithValue("@key", stateKey);
                                    cmd.Parameters.AddWithValue("@keyIndex", blockIndex);
                                    cmd.CommandType = CommandType.Text;
                                    cmd.CommandText = "SELECT [Id] FROM " + StateRefDbName + " WHERE [ParentId] = @parentId AND [Key] = @key AND [KeyIndex] = @keyIndex;";
                                    var obj = cmd.ExecuteScalar();

                                    if ((obj != null) && (!obj.Equals(DBNull.Value)))
                                    {
                                        stateRefId = (long)obj;
                                    }
                                }

                                var data = blockHash.ToByteArray();
                                if (stateRefId.HasValue)
                                {
                                    using (var cmd = _connection.CreateCommand())
                                    {
                                        cmd.Parameters.Add("@data", SqliteType.Blob, data.Length).Value = data;
                                        cmd.Parameters.AddWithValue("@key", stateKey);
                                        cmd.Parameters.AddWithValue("@keyIndex", blockIndex);
                                        cmd.Parameters.AddWithValue("@id", stateRefId.Value);
                                        cmd.CommandType = CommandType.Text;
                                        cmd.CommandText = "UPDATE " + StateRefDbName + @" SET [Key] = @key, [KeyIndex] = @keyIndex, [Data] = @data WHERE [Id] = @id;";
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    using (var cmd = _connection.CreateCommand())
                                    {
                                        cmd.Parameters.Add("@data", SqliteType.Blob, data.Length).Value = data;
                                        cmd.Parameters.AddWithValue("@parentId", parentChainDbId);
                                        cmd.Parameters.AddWithValue("@key", stateKey);
                                        cmd.Parameters.AddWithValue("@keyIndex", blockIndex);
                                        cmd.CommandType = CommandType.Text;
                                        cmd.CommandText = "INSERT INTO " + StateRefDbName + @" ([ParentId], [Key], [KeyIndex], [Data]) VALUES (@parentId, @key, @keyIndex, @data);";
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }

                            firstTransaction.Commit();
                        }
                    }
                }

                if (!_lastStateRefCaches.ContainsKey(chainId))
                {
                    _lastStateRefCaches[chainId] =
                        new LruCache<string, Tuple<HashDigest<SHA256>, long>>();
                }

                LruCache<string, Tuple<HashDigest<SHA256>, long>> stateRefCache =
                    _lastStateRefCaches[chainId];

                foreach (string key in keys)
                {
                    _logger.Debug($"Try to set cache {key}");
                    if (!stateRefCache.TryGetValue(key, out Tuple<HashDigest<SHA256>, long> cache)
                        || cache.Item2 < blockIndex)
                    {
                        stateRefCache[key] =
                            new Tuple<HashDigest<SHA256>, long>(blockHash, blockIndex);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during StoreStateReference: {e.Message}.");
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<Tuple<HashDigest<SHA256>, long>> IterateStateReferences(
            Guid chainId,
            string key,
            long? highestIndex,
            long? lowestIndex,
            int? limit)
        {
            highestIndex ??= long.MaxValue;
            lowestIndex ??= 0;
            limit ??= int.MaxValue;

            if (highestIndex < lowestIndex)
            {
                var message =
                    $"highestIndex({highestIndex}) must be greater than or equal to " +
                    $"lowestIndex({lowestIndex})";
                throw new ArgumentException(
                    message,
                    nameof(highestIndex));
            }

            var helper = new SQLiteHelper();

            byte[] keyBytes = helper.GetBytes(key);

            return IterateStateReferences(
                chainId, keyBytes, highestIndex.Value, lowestIndex.Value, limit.Value);
        }

        private IEnumerable<Tuple<HashDigest<SHA256>, long>> IterateStateReferences(
            Guid chainId,
            byte[] prefix,
            long highestIndex,
            long lowestIndex,
            int limit)
        {
            var helper = new SQLiteHelper();

            using (var _connection = new SqliteConnection(GetConnectionString()))
            {
                _connection.Open();

                long parentChainDbId = 0;
                using (var cmd = _connection.CreateCommand())
                {
                    cmd.Parameters.AddWithValue("@key", chainId.ToString());
                    cmd.Parameters.AddWithValue("@typeC", helper.GetString(CanonicalChainIdIdKey));
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT [Id] FROM " + ChainDbName + " WHERE [Key] = @key AND [Type] = @typeC;";
                    parentChainDbId = (long)cmd.ExecuteScalar();
                }

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@parentId", parentChainDbId);
                    cmd.Parameters.AddWithValue("@key", helper.GetString(prefix));
                    cmd.CommandText = "SELECT [Key], [KeyIndex], [Data] FROM " + StateRefDbName + " WHERE [ParentId] = @parentId AND [Key] = @key ORDER BY [KeyIndex] DESC;";
                    var reader = cmd.ExecuteReader();

                    if ((reader != null) && (reader.HasRows))
                    {
                        while (reader.Read())
                        {
                            long index = (long)reader.GetValue("KeyIndex");

                            if (index > highestIndex)
                            {
                                continue;
                            }

                            if (index < lowestIndex || limit <= 0)
                            {
                                break;
                            }

                            byte[] hashBytes = (byte[])reader.GetValue("Data");
                            var hash = new HashDigest<SHA256>(hashBytes);

                            yield return new Tuple<HashDigest<SHA256>, long>(hash, index);
                            limit--;
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override IImmutableDictionary<string, IImmutableList<HashDigest<SHA256>>>
            ListAllStateReferences(
                Guid chainId,
                long lowestIndex = 0,
                long highestIndex = long.MaxValue)
        {
            var stateRefs = new List<StateRef>();

            try
            {
                var helper = new SQLiteHelper();

                using (var _connection = new SqliteConnection(GetConnectionString()))
                {
                    _connection.Open();

                    long parentChainDbId = 0;
                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.Parameters.AddWithValue("@key", chainId.ToString());
                        cmd.Parameters.AddWithValue("@typeC", helper.GetString(CanonicalChainIdIdKey));
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT [Id] FROM " + ChainDbName + " WHERE [Key] = @key AND [Type] = @typeC;";
                        parentChainDbId = (long)cmd.ExecuteScalar();
                    }

                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.Parameters.AddWithValue("@parentId", parentChainDbId);
                        cmd.CommandText = "SELECT [Key], [KeyIndex], [Data] FROM " + StateRefDbName + " WHERE [ParentId] = @parentId ORDER BY [KeyIndex] DESC;";
                        var reader = cmd.ExecuteReader();

                        if ((reader != null) && (reader.HasRows))
                        {
                            while (reader.Read())
                            {
                                var stateKey = (string)reader.GetValue("Key");
                                long index = (long)reader.GetValue("KeyIndex");

                                if (index < lowestIndex || index > highestIndex)
                                {
                                    continue;
                                }

                                var data = (byte[])reader.GetValue("Data");
                                var hash = new HashDigest<SHA256>(data);
                                var stateRef = new StateRef
                                {
                                    StateKey = stateKey,
                                    BlockHash = hash,
                                    BlockIndex = index,
                                };

                                stateRefs.Add(stateRef);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during ListAllStateReferences: {e.Message}.");
            }

            return stateRefs
                .GroupBy(stateRef => stateRef.StateKey)
                .ToImmutableDictionary(
                    g => g.Key,
                    g => (IImmutableList<HashDigest<SHA256>>)g
                        .Select(r => r.BlockHash).ToImmutableList()
                );
        }

        /// <inheritdoc/>
        public override Tuple<HashDigest<SHA256>, long> LookupStateReference(
            Guid chainId,
            string key,
            long lookupUntilBlockIndex)
        {
            if (_lastStateRefCaches.TryGetValue(
                    chainId,
                    out LruCache<string, Tuple<HashDigest<SHA256>, long>> stateRefCache)
                && stateRefCache.TryGetValue(
                    key,
                    out Tuple<HashDigest<SHA256>, long> cache))
            {
                long cachedIndex = cache.Item2;

                if (cachedIndex <= lookupUntilBlockIndex)
                {
                    return cache;
                }
            }

            Tuple<HashDigest<SHA256>, long> stateRef =
                IterateStateReferences(chainId, key, lookupUntilBlockIndex, null, limit: 1)
                .FirstOrDefault();

            if (stateRef is null)
            {
                return null;
            }

            if (!_lastStateRefCaches.ContainsKey(chainId))
            {
                _lastStateRefCaches[chainId] =
                    new LruCache<string, Tuple<HashDigest<SHA256>, long>>();
            }

            stateRefCache = _lastStateRefCaches[chainId];

            if (!stateRefCache.TryGetValue(key, out cache) || cache.Item2 < stateRef.Item2)
            {
                stateRefCache[key] = new Tuple<HashDigest<SHA256>, long>(
                    stateRef.Item1,
                    stateRef.Item2);
            }

            return stateRef;
        }

        /// <inheritdoc/>
        public override void PruneBlockStates<T>(
            Guid chainId,
            Block<T> until)
        {
            try
            {
                var helper = new SQLiteHelper();

                string[] keys = ListStateKeys(chainId).ToArray();
                long untilIndex = until.Index;
                foreach (var key in keys)
                {
                    Tuple<HashDigest<SHA256>, long>[] stateRefs =
                        IterateStateReferences(chainId, key, untilIndex, null, null)
                            .OrderByDescending(tuple => tuple.Item2)
                            .ToArray();
                    var dict = new Dictionary<HashDigest<SHA256>, List<string>>();
                    foreach ((HashDigest<SHA256> blockHash, long index) in stateRefs.Skip(1))
                    {
                        if (!dict.ContainsKey(blockHash))
                        {
                            dict.Add(blockHash, new List<string>());
                        }

                        dict[blockHash].Add(key);
                    }

                    foreach (var kv in dict)
                    {
                        DeleteBlockStates(kv.Key, kv.Value);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error during PruneBlockStates: {e.Message}.");
            }
        }

        /// <inheritdoc/>
        public override void ForkStateReferences<T>(
            Guid sourceChainId,
            Guid destinationChainId,
            Block<T> branchPoint)
        {
            try
            {
                var isDestinationValid = false;
                var helper = new SQLiteHelper();

                long sourceParentChainDbId = 0;
                long destinationParentChainDbId = 0;

                using (var _connection = new SqliteConnection(GetConnectionString()))
                {
                    _connection.Open();

                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.Parameters.AddWithValue("@key", sourceChainId.ToString());
                        cmd.Parameters.AddWithValue("@typeC", helper.GetString(CanonicalChainIdIdKey));
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT [Id] FROM " + ChainDbName + " WHERE [Key] = @key AND [Type] = @typeC;";
                        sourceParentChainDbId = (long)cmd.ExecuteScalar();
                    }

                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.Parameters.AddWithValue("@key", destinationChainId.ToString());
                        cmd.Parameters.AddWithValue("@typeC", helper.GetString(CanonicalChainIdIdKey));
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT [Id] FROM " + ChainDbName + " WHERE [Key] = @key AND [Type] = @typeC;";
                        var obj = cmd.ExecuteScalar();

                        if ((obj != null) && (!obj.Equals(DBNull.Value)))
                        {
                            isDestinationValid = true;
                            destinationParentChainDbId = (long)obj;
                        }
                    }

                    if (isDestinationValid)
                    {
                        using (var firstTransaction = _connection.BeginTransaction())
                        {
                            var commit = false;

                            using (var cmd = _connection.CreateCommand())
                            {
                                cmd.CommandType = CommandType.Text;
                                cmd.Parameters.AddWithValue("@parentId", sourceParentChainDbId);
                                cmd.CommandText = "SELECT [Key], [KeyIndex], [Data] FROM " + StateRefDbName + " WHERE [ParentId] = @parentId ORDER BY [KeyIndex] DESC;";
                                var reader = cmd.ExecuteReader();

                                if ((reader != null) && (reader.HasRows))
                                {
                                    while (reader.Read())
                                    {
                                        var stateKey = (string)reader.GetValue("Key");
                                        long blockIndex = (long)reader.GetValue("KeyIndex");
                                        byte[] data = (byte[])reader.GetValue("Data");

                                        if (blockIndex > branchPoint.Index)
                                        {
                                            continue;
                                        }

                                        long? stateRefId = null;
                                        using (var subCmd = _connection.CreateCommand())
                                        {
                                            subCmd.Parameters.AddWithValue("@parentId", destinationParentChainDbId.ToString());
                                            subCmd.Parameters.AddWithValue("@key", stateKey);
                                            subCmd.Parameters.AddWithValue("@keyIndex", blockIndex);
                                            subCmd.CommandType = CommandType.Text;
                                            subCmd.CommandText = "SELECT [Id] FROM " + StateRefDbName + " WHERE [ParentId] = @parentId AND [Key] = @key AND [KeyIndex] = @keyIndex;";
                                            var obj = subCmd.ExecuteScalar();

                                            if ((obj != null) && (!obj.Equals(DBNull.Value)))
                                            {
                                                stateRefId = (long)obj;
                                            }
                                        }

                                        if (stateRefId.HasValue)
                                        {
                                            using (var subCmd = _connection.CreateCommand())
                                            {
                                                subCmd.Parameters.Add("@data", SqliteType.Blob, data.Length).Value = data;
                                                subCmd.Parameters.AddWithValue("@key", stateKey);
                                                subCmd.Parameters.AddWithValue("@keyIndex", blockIndex);
                                                subCmd.Parameters.AddWithValue("@id", stateRefId.Value);
                                                subCmd.CommandType = CommandType.Text;
                                                subCmd.CommandText = "UPDATE " + StateRefDbName + @" SET [Key] = @key, [KeyIndex] = @keyIndex, [Data] = @data WHERE [Id] = @id;";
                                                subCmd.ExecuteNonQuery();
                                            }

                                            commit = true;
                                        }
                                        else
                                        {
                                            using (var subCmd = _connection.CreateCommand())
                                            {
                                                subCmd.Parameters.Add("@data", SqliteType.Blob, data.Length).Value = data;
                                                subCmd.Parameters.AddWithValue("@parentId", destinationParentChainDbId);
                                                subCmd.Parameters.AddWithValue("@key", stateKey);
                                                subCmd.Parameters.AddWithValue("@keyIndex", blockIndex);
                                                subCmd.CommandType = CommandType.Text;
                                                subCmd.CommandText = "INSERT INTO " + StateRefDbName + @" ([ParentId], [Key], [KeyIndex], [Data]) VALUES (@parentId, @key, @keyIndex, @data);";
                                                subCmd.ExecuteNonQuery();
                                            }

                                            commit = true;
                                        }
                                    }
                                }
                            }

                            if (commit) firstTransaction.Commit();
                        }
                    }
                    else
                    {
                        throw new ChainIdNotFoundException(
                            destinationChainId,
                            "The destination chain does not exist.");
                    }
                }

                if (!isDestinationValid && CountIndex(sourceChainId) < 1)
                {
                    throw new ChainIdNotFoundException(
                        sourceChainId,
                        "The source chain to be forked does not exist.");
                }

                _lastStateRefCaches.Remove(destinationChainId);
            }
            catch (Exception e)
            {
                _logger.Error($"Error during ForkStateReferences: {e.Message}.");
            }
        }

        private string BlockKey(HashDigest<SHA256> blockHash)
        {
            return blockHash.ToString();
        }

        private string TxKey(TxId txId)
        {
            return txId.ToString();
        }

        private string BlockStateKey(HashDigest<SHA256> blockHash)
        {
            return blockHash.ToString();
        }

        private string TxNonceKey(Address address)
        {
            return address.ToHex();
        }

        private string StagedTxKey(TxId txId)
        {
            return txId.ToString();
        }

        private string StateRefKey(string stateKey)
        {
            return stateKey;
        }

        private class StateRef
        {
            public string StateKey { get; set; }

            public long BlockIndex { get; set; }

            public HashDigest<SHA256> BlockHash { get; set; }
        }
    }
}
