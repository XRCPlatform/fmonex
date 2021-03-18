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
                    cmd.CommandText = "DELETE FROM " + dbName + " WHERE [Key] = @key;";
                    cmd.ExecuteNonQuery();
                }

                firstTransaction.Commit();
            }
        }

        /// <summary>
        /// Get <c>long</c> representation of the <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The Big-endian byte-array value to convert to <c>long</c>.</param>
        /// <returns>The <c>long</c> representation of the <paramref name="value"/>.</returns>
        internal long ToInt64(byte[] value)
        {
            byte[] bytes = new byte[sizeof(long)];
            value.CopyTo(bytes, 0);

            // Use Big-endian to order index lexicographically.
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToInt64(bytes, 0);
        }

        /// <summary>
        /// Get <c>string</c> representation of the <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The byte-array value to convert to <c>string</c>.</param>
        /// <returns>The <c>string</c> representation of the <paramref name="value"/>.</returns>
        internal string GetString(byte[] value)
        {
            return Encoding.UTF8.GetString(value);
        }

        /// <summary>
        /// Get Big-endian byte-array representation of the <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The <c>long</c> value to convert to byte-array.</param>
        /// <returns>The Big-endian byte-array representation of the <paramref name="value"/>.
        /// </returns>
        internal byte[] GetBytes(long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);

            // Use Big-endian to order index lexicographically.
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        /// <summary>
        /// Get encoded byte-array representation of the <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The <c>string</c> to convert to byte-array.</param>
        /// <returns>The encoded representation of the <paramref name="value"/>.</returns>
        internal byte[] GetBytes(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }
    }
}
