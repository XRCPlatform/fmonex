using System.Linq;
using FreeMarketOne.Extensions.Helpers;

namespace System
{
	public static class RandomString
	{
		private static Random Random { get; } = new Random();

        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string Generate(int length)
		{
			Guard.MinimumAndNotNull(nameof(length), length, 1);

			return new string(Enumerable.Repeat(Chars, length)
				.Select(s => s[Random.Next(s.Length)]).ToArray());
		}
	}
}
