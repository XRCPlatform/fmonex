using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace LibPlanet.SQLiteStore
{
    internal class SQLiteHelper
    {
        internal byte[] GetBytes(string key, string dbName, SqliteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("@key", key);
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT [Data] FROM " + dbName + " WHERE [Key] == @key;";
                var reader = cmd.ExecuteReader();

                if ((reader != null) && (reader.HasRows))
                {
                    reader.Read();

                    return (byte[])reader.GetValue("Data");      
                }
            }

            return null;
        }

        internal void PutBytes(string key, string dbName, byte[] data, SqliteConnection connection)
        {
            using (var firstTransaction = connection.BeginTransaction())
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Parameters.Add("@data", SqliteType.Blob, data.Length).Value = data;
                    cmd.Parameters.AddWithValue("@key", key);
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "INSERT INTO " + dbName + " ([Key], [Data]) VALUES (@key, @data);";
                    cmd.ExecuteNonQuery();
                }

                firstTransaction.Commit();
            }
        }

        internal void RemoveBytes(string key, string dbName, SqliteConnection connection)
        {
            using (var firstTransaction = connection.BeginTransaction())
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Parameters.AddWithValue("@key", key);
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "DELETE FROM " + dbName + " WHERE [Key] == @key;";
                    cmd.ExecuteNonQuery();
                }

                firstTransaction.Commit();
            }
        }
    }
}
