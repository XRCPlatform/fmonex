using System;
using System.Linq;
using FreeMarketOne.Extensions.Bases;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Tor.Models.Fields.OctetFields;
using FreeMarketOne.Tor.Models.TorSocks5.Fields.ByteArrayFields;
using FreeMarketOne.Tor.TorSocks5.Models.Fields.ByteArrayFields;

namespace FreeMarketOne.Tor.Models.Messages
{
	public class TorSocks5Response : ByteArraySerializableBase
	{
		#region PropertiesAndMembers

		public VerField Ver { get; set; }

		public RepField Rep { get; set; }

		public RsvField Rsv { get; set; }

		public AtypField Atyp { get; set; }

		public AddrField BndAddr { get; set; }

		public PortField BndPort { get; set; }

		#endregion PropertiesAndMembers

		#region ConstructorsAndInitializers

		public TorSocks5Response()
		{
		}

		public TorSocks5Response(RepField rep, AddrField bndAddr, PortField bndPort)
		{
			Rep = Guard.NotNull(nameof(rep), rep);
			BndAddr = Guard.NotNull(nameof(bndAddr), bndAddr);
			BndPort = Guard.NotNull(nameof(bndPort), bndPort);
			Ver = VerField.Socks5;
			Rsv = RsvField.X00;
			Atyp = bndAddr.Atyp;
		}

		#endregion ConstructorsAndInitializers

		#region Serialization

		public override void FromBytes(byte[] bytes)
		{
			Guard.NotNullOrEmpty(nameof(bytes), bytes);
			Guard.MinimumAndNotNull($"{nameof(bytes)}.{nameof(bytes.Length)}", bytes.Length, 6);

			Ver = new VerField();
			Ver.FromByte(bytes[0]);

			Rep = new RepField();
			Rep.FromByte(bytes[1]);

			Rsv = new RsvField();
			Rsv.FromByte(bytes[2]);

			Atyp = new AtypField();
			Atyp.FromByte(bytes[3]);

			BndAddr = new AddrField();
			BndAddr.FromBytes(bytes.Skip(4).Take(bytes.Length - 4 - 2).ToArray());

			BndPort = new PortField();
			BndPort.FromBytes(bytes.Skip(bytes.Length - 2).ToArray());
		}

		public override byte[] ToBytes() => ByteHelper.Combine(new byte[] { Ver.ToByte(), Rep.ToByte(), Rsv.ToByte(), Atyp.ToByte() }, BndAddr.ToBytes(), BndPort.ToBytes());

		#endregion Serialization
	}
}
