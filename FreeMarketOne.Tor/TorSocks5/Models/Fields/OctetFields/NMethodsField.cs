using FreeMarketOne.Extensions.Bases;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Tor.Models.TorSocks5.Fields.ByteArrayFields;

namespace FreeMarketOne.Tor.Models.Fields.OctetFields
{
	public class NMethodsField : OctetSerializableBase
	{
		#region PropertiesAndMembers

		public int Value => ByteValue;

		#endregion PropertiesAndMembers

		#region ConstructorsAndInitializers

		public NMethodsField()
		{
		}

		public NMethodsField(int value)
		{
			ByteValue = (byte)Guard.InRangeAndNotNull(nameof(value), value, 0, 255);
		}

		#endregion ConstructorsAndInitializers

		#region Serialization

		public void FromMethodsField(MethodsField methods)
		{
			Guard.NotNull(nameof(methods), methods);

			ByteValue = (byte)methods.ToBytes().Length;
		}

		#endregion Serialization
	}
}
