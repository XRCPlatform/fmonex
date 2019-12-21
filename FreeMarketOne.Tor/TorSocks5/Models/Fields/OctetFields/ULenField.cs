using FreeMarketOne.Extensions.Bases;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Tor.TorSocks5.Models.Fields.ByteArrayFields;

namespace FreeMarketOne.Tor.Models.Fields.OctetFields
{
	public class ULenField : OctetSerializableBase
	{
		#region PropertiesAndMembers

		public int Value => ByteValue;

		#endregion PropertiesAndMembers

		#region ConstructorsAndInitializers

		public ULenField()
		{
		}

		public ULenField(int value)
		{
			ByteValue = (byte)Guard.InRangeAndNotNull(nameof(value), value, 0, 255);
		}

		#endregion ConstructorsAndInitializers

		#region Serialization

		public void FromUNameField(UNameField uName)
		{
			ByteValue = (byte)uName.ToBytes().Length;
		}

		#endregion Serialization
	}
}
