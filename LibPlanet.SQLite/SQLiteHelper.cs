using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace LibPlanet.SQLiteStore
{
    internal class SQLiteHelper
    {
        internal byte[] GetBytes(string key, string dbName, string connectionString)
        {
            using (var _connection = new SqliteConnection(connectionString))
            {
                _connection.Open();

                using (var cmd = _connection.CreateCommand())
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
            }

            return null;
        }

        internal void PutBytes(string key, string dbName, byte[] data, string connectionString)
        {
            using (var _connection = new SqliteConnection(connectionString))
            {
                _connection.Open();

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.Parameters.Add("@data", SqliteType.Blob, data.Length).Value = data;
                    cmd.Parameters.AddWithValue("@key", key);
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "INSERT INTO " + dbName + " ([Key], [Data]) VALUES (@key, @data);";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        internal void RemoveBytes(string key, string dbName, string connectionString)
        {
            using (var _connection = new SqliteConnection(connectionString))
            {
                _connection.Open();

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.Parameters.AddWithValue("@key", key);
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "DELETE FROM " + dbName + " WHERE [Key] = @key;";
                    cmd.ExecuteNonQuery();
                }
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

        /// <summary>
        /// Converts a hexadecimal string to a <see cref="byte"/> array.
        /// </summary>
        /// <param name="hex">A <see cref="string"/> which encodes
        /// <see cref="byte"/>s in hexadecimal.  Its length must be zero or
        /// an even number.  It must not be <c>null</c>.</param>
        /// <returns>A <see cref="byte"/> array that the given
        /// <paramref name="hex"/> string represented in hexadecimal.
        /// It lengthens the half of the given <paramref name="hex"/> string.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the given
        /// <paramref name="hex"/> string is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the length
        /// of the given <paramref name="hex"/> string is an odd number.
        /// </exception>
        /// <exception cref="FormatException">Thrown when the given
        /// <paramref name="hex"/> string is not a valid hexadecimal string.
        /// </exception>
        public byte[] ParseHex(string hex)
        {
            if (hex == null)
            {
                throw new ArgumentNullException(nameof(hex));
            }

            if (hex.Length % 2 > 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hex),
                    "A length of a hexadecimal string must be an even number."
                );
            }

            var bytes = new byte[hex.Length / 2];
            for (var i = 0; i < hex.Length / 2; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return bytes;
        }

        /// <summary>
        /// Renders a hexadecimal string from a <see cref="byte"/> array.
        /// </summary>
        /// <param name="bytes">A <see cref="byte"/> array to renders
        /// the corresponding hexadecimal string.  It must not be <c>null</c>.
        /// </param>
        /// <returns>A hexadecimal string which encodes the given
        /// <paramref name="bytes"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the given
        /// <paramref name="bytes"/> is <c>null</c>.</exception>
        public string Hex(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            string s = BitConverter.ToString(bytes);
            return s.Replace("-", string.Empty).ToLower(CultureInfo.InvariantCulture);
        }
    }
}
