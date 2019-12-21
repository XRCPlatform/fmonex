using FreeMarketOne.Extensions.Bases;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Tor.Models.Fields.OctetFields;

namespace FreeMarketOne.Tor.Models.Messages
{
	public class UsernamePasswordResponse : ByteArraySerializableBase
	{
		#region PropertiesAndMembers

		public AuthVerField Ver { get; set; }

		public AuthStatusField Status { get; set; }

		#endregion PropertiesAndMembers

		#region ConstructorsAndInitializers

		public UsernamePasswordResponse()
		{
		}

		public UsernamePasswordResponse(AuthStatusField status)
		{
			Status = Guard.NotNull(nameof(status), status);
			Ver = AuthVerField.Version1;
		}

		#endregion ConstructorsAndInitializers

		#region Serialization

		public override void FromBytes(byte[] bytes)
		{
			Guard.NotNullOrEmpty(nameof(bytes), bytes);
			Guard.Same($"{nameof(bytes)}.{nameof(bytes.Length)}", 2, bytes.Length);

			Ver = new AuthVerField();
			Ver.FromByte(bytes[0]);

			Status = new AuthStatusField();
			Status.FromByte(bytes[1]);
		}

		public override byte[] ToBytes() => new byte[] { Ver.ToByte(), Status.ToByte() };

		#endregion Serialization
	}
}
