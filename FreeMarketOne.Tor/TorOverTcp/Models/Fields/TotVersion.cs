using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Tor.Bases;

namespace FreeMarketOne.Tor.TorOverTcp.Models.Fields
{
    public class TotVersion : OctetSerializableBase
    {
		#region Statics

		public static TotVersion Version1 => new TotVersion(1);

		#endregion

		#region PropertiesAndMembers

		public int Value => ByteValue;

		#endregion

		#region ConstructorsAndInitializers

		public TotVersion()
		{

		}

		public TotVersion(int value)
		{
			ByteValue = (byte)Guard.InRangeAndNotNull(nameof(value), value, 0, 255);
		}

		#endregion
	}
}
