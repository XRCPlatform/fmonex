using System;

namespace Libplanet.Net
{
    /// <summary>
    /// This static class serves to convert between byte-arrays, and various integer sizes
    /// - all of which assume the byte-data is in Big-endian, or "Network Byte Order".
    /// </summary>
    public static class NetworkOrderBitsConverter
    {
        /// <summary>
        /// Given a byte-array assumed to be in Big-endian order, and an offset into it
        /// - return a 16-bit integer derived from the 2 bytes starting at that offset.
        /// </summary>
        /// <param name="buffer">the byte-array to get the short from</param>
        /// <returns></returns>
        public static short ToInt16(byte[] buffer)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }
            return BitConverter.ToInt16(buffer);
        }

        /// <summary>
        /// Given a 16-bit integer, return it as a byte-array in Big-endian order.
        /// </summary>
        /// <param name="value">the short to convert</param>
        /// <returns>a 2-byte array containing that short's bits</returns>

        public static byte[] GetBytes(short value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }


        /// <summary>
        /// Given a byte-array assumed to be in Big-endian order, and an offset into it
        /// - return a 32-bit integer derived from the 4 bytes starting at that offset.
        /// </summary>
        /// <param name="buffer">the byte-array to get the integer from</param>
        /// <returns></returns>
        public static int ToInt32(byte[] buffer)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }
            return BitConverter.ToInt32(buffer);
        }

        /// <summary>
        /// Given a 32-bit integer, return it as a byte-array in Big-endian order.
        /// </summary>
        /// <param name="value">the int to convert</param>
        /// <returns>a 4-byte array containing that integer's bits</returns>
        public static byte[] GetBytes(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

       

        /// <summary>
        /// Given a byte-array assumed to be in Big-endian order, and an offset into it
        /// - return a 64-bit integer derived from the 8 bytes starting at that offset.
        /// </summary>
        /// <param name="buffer">the byte-array to get the Int64 from</param>
        /// <returns></returns>
        public static long ToInt64(byte[] buffer)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }
            return BitConverter.ToInt64(buffer); 
        }

        /// <summary>
        /// Given a 64-bit integer, return it as a byte-array in Big-endian order.
        /// </summary>
        /// <param name="value">The <c>long</c> value to convert from.</param>
        /// <returns>The network order presentation of <paramref name="value"/> as an 8-byte array.</returns>

        public static byte[] GetBytes(long value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }
    }
}
