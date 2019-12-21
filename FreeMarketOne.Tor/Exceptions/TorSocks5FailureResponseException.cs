using System;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Tor.Models.Fields.OctetFields;

namespace FreeMarketOne.Tor.Exceptions
{
	public class TorSocks5FailureResponseException : Exception
	{
		public RepField RepField { get; }

		public TorSocks5FailureResponseException(RepField rep) : base($"Tor SOCKS5 proxy responded with {rep}.")
		{
			RepField = Guard.NotNull(nameof(rep), rep);
		}
	}
}
