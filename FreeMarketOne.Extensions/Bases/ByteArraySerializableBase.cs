using System;
using System.Text;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Extensions.Interfaces;

namespace FreeMarketOne.Extensions.Bases
{
	public abstract class ByteArraySerializableBase : IByteArraySerializable, IEquatable<ByteArraySerializableBase>, IEquatable<byte[]>
	{
		#region ConstructorsAndInitializers

		protected ByteArraySerializableBase()
		{
		}

		#endregion ConstructorsAndInitializers

		#region Serialization

		public abstract byte[] ToBytes();

		public abstract void FromBytes(byte[] bytes);

		public string ToHex(bool xhhSyntax = false)
		{
			if (xhhSyntax)
			{
				return $"X'{ByteHelper.ToHex(ToBytes())}'";
			}
			return ByteHelper.ToHex(ToBytes());
		}

		public void FromHex(string hex)
		{
			hex = Guard.NotNullOrEmptyOrWhitespace(nameof(hex), hex, true);

			var bytes = ByteHelper.FromHex(hex);
			FromBytes(bytes);
		}

		public string ToString(Encoding encoding)
		{
			Guard.NotNull(nameof(encoding), encoding);

			return encoding.GetString(ToBytes());
		}

		public override string ToString()
		{
			return ToHex(xhhSyntax: true);
		}

		#endregion Serialization

		#region EqualityAndComparison

		public override bool Equals(object obj) => obj is ByteArraySerializableBase serializableBase && this == serializableBase;

		public bool Equals(ByteArraySerializableBase other) => this == other;

		public override int GetHashCode()
		{
			// https://github.com/bcgit/bc-csharp/blob/b19e68a517e56ef08cd2e50df4dcb8a96ddbe507/crypto/src/util/Arrays.cs#L206
			var bytes = ToBytes();
			if (bytes is null)
			{
				return 0;
			}

			int i = bytes.Length;
			int hash = i + 1;

			while (--i >= 0)
			{
				hash *= 257;
				hash ^= bytes[i];
			}

			return hash;
		}

		public static bool operator ==(ByteArraySerializableBase x, ByteArraySerializableBase y) => ByteHelper.CompareFastUnsafe(x?.ToBytes(), y?.ToBytes());

		public static bool operator !=(ByteArraySerializableBase x, ByteArraySerializableBase y) => !(x == y);

		public bool Equals(byte[] other) => ByteHelper.CompareFastUnsafe(ToBytes(), other);

		public static bool operator ==(byte[] x, ByteArraySerializableBase y) => ByteHelper.CompareFastUnsafe(x, y?.ToBytes());

		public static bool operator ==(ByteArraySerializableBase x, byte[] y) => ByteHelper.CompareFastUnsafe(x?.ToBytes(), y);

		public static bool operator !=(byte[] x, ByteArraySerializableBase y) => !(x == y);

		public static bool operator !=(ByteArraySerializableBase x, byte[] y) => !(x == y);

		#endregion EqualityAndComparison
	}
}
