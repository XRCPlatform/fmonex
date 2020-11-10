using FreeMarketOne.DataStructure.Objects.BaseItems;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace FreeMarketOne.Search
{
    public class NormalizedStore
    {
        private readonly string EXTRACTS_DB_FILE = "extracts.db";
        private string dbPath;

        private string connection { get; set; }

        public NormalizedStore(string dbPath)
        {
            this.dbPath = dbPath;

            this.connection = new SQLiteConnectionStringBuilder
            {
                DataSource = Path.Combine(this.dbPath, EXTRACTS_DB_FILE),
                JournalMode = SQLiteJournalModeEnum.Wal,
                Pooling = true
            }
            .ToString();
            EnsureSQLiteDbExists();
        }

        public bool Save(MarketItemV1 marketItem, OfferDirection offerDirection)
        {
            using (var dbConnection = new SQLiteConnection(this.connection))
            {
                dbConnection.Open();

                using (var dbTransaction = dbConnection.BeginTransaction())
                { 
                    var delete = "DELETE FROM Offer WHERE Signature = $Signature";
                    using (var deleteCommand = new SQLiteCommand(delete, dbConnection, dbTransaction))
                    {
                        deleteCommand.Parameters.AddWithValue("$Signature", marketItem.Signature);
                        deleteCommand.ExecuteNonQuery();
                    }

                    var sql = "INSERT INTO Offer ( Signature, OfferDirection, Data) VALUES ( $Signature, $OfferDirection, $Data)";
                    using (var insertCommand = new SQLiteCommand(sql, dbConnection, dbTransaction))
                    {
                        insertCommand.Parameters.AddWithValue("$Signature", marketItem.Signature);
                        insertCommand.Parameters.AddWithValue("$OfferDirection", (int)offerDirection);
                        insertCommand.Parameters.AddWithValue("$Data", JsonConvert.SerializeObject(marketItem));
                        insertCommand.ExecuteNonQuery();
                    }

                    dbTransaction.Commit();
                }
            }
            return true;
        }

        public SearchResult GetMyOffers(OfferDirection offerDirection, int pageSize, int page)
        {
            long totalHits = 0;
            List<MarketItemV1> results = new List<MarketItemV1>();
            int pageOffset = 0;
            if (page > 1)
            {
                pageOffset = page * pageSize;
            }

            var sqlCount = $"SELECT COUNT('') FROM Offer WHERE offerDirection = $offerDirection ";
            var sql = $"SELECT Data FROM Offer WHERE offerDirection = $offerDirection order by Id desc limit {pageSize} offset {pageOffset} ";
            using (var dbConnection = new SQLiteConnection(this.connection))
            {
                dbConnection.Open();
                using (var selectAccountsCmd = new SQLiteCommand(sqlCount, dbConnection))
                {
                    selectAccountsCmd.Parameters.AddWithValue("$OfferDirection", (int)offerDirection);
                    totalHits = (long)selectAccountsCmd.ExecuteScalar();
                }

                using (var selectAccountsCmd = new SQLiteCommand(sql, dbConnection))
                {
                    selectAccountsCmd.Parameters.AddWithValue("$OfferDirection", (int)offerDirection);
                    using (var reader = selectAccountsCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(JsonConvert.DeserializeObject<MarketItemV1>(reader.GetString(0)));
                        }
                    }
                }
            }


            return new SearchResult()
            {
                Results = results,
                CurrentPage = page,
                PageSize = pageSize,
                TotalHits = (int)totalHits
            };
        }

        public MarketItemV1 GetOfferById(string signature)
        {
            var sql = $"SELECT Data FROM Offer WHERE Signature = $Signature LIMIT 1";
            using (var dbConnection = new SQLiteConnection(this.connection))
            {
                dbConnection.Open();
                using (var selectAccountsCmd = new SQLiteCommand(sql, dbConnection))
                {
                    selectAccountsCmd.Parameters.AddWithValue("$Signature", signature);
                    using (var reader = selectAccountsCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            return JsonConvert.DeserializeObject<MarketItemV1>(reader.GetString(0));
                        }
                    }
                }
            }
            return null;
        }

        private void EnsureSQLiteDbExists()
        {
            string filePath = Path.Combine(this.dbPath, EXTRACTS_DB_FILE);
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Exists)
            {
                if (!Directory.Exists(this.dbPath))
                {
                    Directory.CreateDirectory(this.dbPath);
                }
                SQLiteConnection.CreateFile(filePath);
                CreateIt(this.connection);
            }
        }


            private void CreateIt(string connection)
            {
                using (var dbConnection = new SQLiteConnection(connection))
                {
                    dbConnection.Open();

                    using (var transaction = dbConnection.BeginTransaction())
                    {
                        try
                        {
                            var sql = "CREATE TABLE \"Offer\" (" +
                                    "\"Id\"  INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                    "\"Signature\"  TEXT NOT NULL UNIQUE, " +
                                    "\"OfferDirection\"  INTEGER NOT NULL, " +
                                    "\"Data\" TEXT NULL " +                                    
                                    "); ";
                            using (var command = new SQLiteCommand(sql, dbConnection, transaction))
                            {
                                command.ExecuteNonQuery();
                            }

                            sql = "CREATE TABLE \"Party\" (" +
                                  "\"Id\"  INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                                  "\"pubKey\"  TEXT NOT NULL UNIQUE, " +
                                  "\"blockHash\"  TEXT NOT NULL, " +
                                  "\"Data\" TEXT NULL " +
                                  "); ";
                            using (var command = new SQLiteCommand(sql, dbConnection, transaction))
                            {
                                command.ExecuteNonQuery();
                            }

                            sql = "CREATE UNIQUE INDEX \"ix_Signature\" ON \"Offer\" (\"Signature\");";

                            using (var command = new SQLiteCommand(sql, dbConnection, transaction))
                            {
                                command.ExecuteNonQuery();
                            }

                            sql = "CREATE UNIQUE INDEX \"ix_pubKey\" ON \"Party\" (\"pubKey\");";

                            using (var command = new SQLiteCommand(sql, dbConnection, transaction))
                            {
                                command.ExecuteNonQuery();
                            }

                        transaction.Commit();
                        }
                        catch (Exception e)
                        {
                            transaction.Rollback();
                            throw e;
                        }
                    }
                }
            }

        public bool Save(UserDataV1 item, string blockHash)
        {
            using (var dbConnection = new SQLiteConnection(this.connection))
            {
                dbConnection.Open();

                using (var dbTransaction = dbConnection.BeginTransaction())
                {
                    var delete = "DELETE FROM Party WHERE pubKey = $pubKey";
                    using (var deleteCommand = new SQLiteCommand(delete, dbConnection, dbTransaction))
                    {
                        deleteCommand.Parameters.AddWithValue("$pubKey", item.PublicKey);
                        deleteCommand.ExecuteNonQuery();
                    }

                    var sql = "INSERT INTO Party ( pubKey, blockHash,  Data) VALUES ( $pubKey, $blockHash, $Data)";
                    using (var insertCommand = new SQLiteCommand(sql, dbConnection, dbTransaction))
                    {
                        insertCommand.Parameters.AddWithValue("$pubKey", item.PublicKey);
                        insertCommand.Parameters.AddWithValue("$blockHash", blockHash);
                        insertCommand.Parameters.AddWithValue("$Data", JsonConvert.SerializeObject(item));
                        insertCommand.ExecuteNonQuery();
                    }

                    dbTransaction.Commit();
                }
            }
            return true;
        }


        public UserDataV1 Get(string pubKey)
        {
            using (var dbConnection = new SQLiteConnection(this.connection))
            {
                dbConnection.Open();

                var select = "SELECT data FROM Party WHERE pubKey = $pubKey";
                using (var selectCommand = new SQLiteCommand(select, dbConnection))
                {
                    selectCommand.Parameters.AddWithValue("$pubKey", pubKey);
                    using (var reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            return JsonConvert.DeserializeObject<UserDataV1>(reader.GetString(0));
                        }
                    }
                }
            }

            return null;
        }
    }
    
}
