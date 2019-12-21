using FreeMarketOne.Extensions.Bases;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Tor.TorSocks5.Models.Fields.ByteArrayFields;

namespace FreeMarketOne.Tor.Models.Fields.OctetFields
{
	public class PLenField : OctetSerializableBase
	{
		#region PropertiesAndMembers

		public int Value => ByteValue;

		#endregion PropertiesAndMembers

		#region ConstructorsAndInitializers

		public PLenField()
		{
		}

		public PLenField(int value)
		{
			ByteValue = (byte)Guard.InRangeAndNotNull(nameof(value), value, 0, 255);
		}

		#endregion ConstructorsAndInitializers

		#region Serialization

		public void FromPasswdField(PasswdField passwd)
		{
			Guard.NotNull(nameof(passwd), passwd);

			ByteValue = (byte)passwd.ToBytes().Length;
		}

		#endregion Serialization
	}
}
