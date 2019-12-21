using FreeMarketOne.Extensions.Bases;
using FreeMarketOne.Extensions.Helpers;

namespace FreeMarketOne.Tor.Models.Fields.OctetFields
{
	public class VerField : OctetSerializableBase
	{
		#region Statics

		public static VerField Socks5 => new VerField(5);

		#endregion Statics

		#region PropertiesAndMembers

		public int Value => ByteValue;

		#endregion PropertiesAndMembers

		#region ConstructorsAndInitializers

		public VerField()
		{
		}

		public VerField(int value)
		{
			ByteValue = (byte)Guard.InRangeAndNotNull(nameof(value), value, 0, 255);
		}

		#endregion ConstructorsAndInitializers
	}
}
