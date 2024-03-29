﻿using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Tor.Bases;
using FreeMarketOne.Tor.TorOverTcp.Models.Fields;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FreeMarketOne.Tor.TorOverTcp.Models.Messages.Bases
{
    public abstract class TotMessageBase : ByteArraySerializableBase
	{
		#region Statics

		public static IEnumerable<byte[]> SplitByMessages(byte[] receivedBytes)
		{
			Guard.NotNull(nameof(receivedBytes), receivedBytes);

			int purposeLength = receivedBytes[4];
			var contentLength = BitConverter.ToInt32(receivedBytes.Skip(5 + purposeLength).Take(4).ToArray(), 0);

			int lengthSum = 5 + purposeLength + 4 + contentLength;
			var message = receivedBytes.Take(lengthSum).ToArray();

			yield return message;

			while (receivedBytes.Length != lengthSum)
			{
				var leftoverBytes = receivedBytes.Skip(lengthSum);
				purposeLength = leftoverBytes.ToArray()[4];
				contentLength = BitConverter.ToInt32(leftoverBytes.Skip(5 + purposeLength).Take(4).ToArray(), 0);
				var messageLength = 5 + purposeLength + 4 + contentLength;
				message = leftoverBytes.Take(messageLength).ToArray();

				yield return message;

				lengthSum += messageLength;
			}
		}

		#endregion

		#region PropertiesAndMembers

		public TotVersion Version { get; set; }

		public TotMessageType MessageType { get; set; }

		public TotMessageId MessageId { get; set; }

		public TotPurpose Purpose { get; set; }

		public TotContent Content { get; set; }

		#endregion

		#region ConstructorsAndInitializers

		protected TotMessageBase()
		{

		}

		protected TotMessageBase(TotMessageType messageType, TotMessageId messageId, TotPurpose purpose, TotContent content)
		{
			Version = TotVersion.Version1;
			MessageType = Guard.NotNull(nameof(messageType), messageType);
			MessageId = Guard.NotNull(nameof(messageId), messageId);
			Purpose = purpose ?? TotPurpose.Empty;
			Content = content ?? TotContent.Empty;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Tor sends data in chunks of 512 bytes, called cells,to make it harder for intermediaries to guess exactly how many bytes are being communicated at each step. 
		/// A developer that intends to build an application on top of ToT MAY utilize this information to gain more efficient network usage.
		/// </summary>
		public int GetNumberOfCells() => ToBytes().Length / 512 + 1;

		/// <summary>
		/// Tor sends data in chunks of 512 bytes, called cells,to make it harder for intermediaries to guess exactly how many bytes are being communicated at each step. 
		/// A developer that intends to build an application on top of ToT MAY utilize this information to gain more efficient network usage.
		/// </summary>
		public int GetLastCellFullnessPercentage()
		{
			int v = GetNumberOfDummyBytesInLastCell();
			decimal v1 = v / 512m;
			return (int)(v1 * 100);
		}

		/// <summary>
		/// Tor sends data in chunks of 512 bytes, called cells,to make it harder for intermediaries to guess exactly how many bytes are being communicated at each step. 
		/// A developer that intends to build an application on top of ToT MAY utilize this information to gain more efficient network usage.
		/// </summary>
		public int GetNumberOfDummyBytesInLastCell() => 512 - (ToBytes().Length % 512);

		#endregion

		#region Serialization

		public override void FromBytes(byte[] bytes)
		{
			Guard.NotNullOrEmpty(nameof(bytes), bytes);
			Guard.InRangeAndNotNull($"{nameof(bytes)}.{nameof(bytes.Length)}", bytes.Length, 7, 536870912 + 3 + 4 + 255);

			Version = new TotVersion();
			Version.FromByte(bytes[0]);

			MessageType = new TotMessageType();
			MessageType.FromByte(bytes[1]);

			MessageId = new TotMessageId();
			MessageId.FromBytes(bytes.Skip(2).Take(2).ToArray());

			int purposeLength = bytes[4];
			Purpose = new TotPurpose();
			Purpose.FromBytes(bytes.Skip(4).Take(purposeLength + 1).ToArray(), startsWithLength: true);

			int contentLength = BitConverter.ToInt32(bytes.Skip(5 + purposeLength).Take(4).ToArray(), 0);
			Content = new TotContent();
			Content.FromBytes(bytes.Skip(5 + purposeLength).ToArray(), startsWithLength: true);
		}

		public override byte[] ToBytes() => ByteHelpers.Combine(new byte[] { Version.ToByte(), MessageType.ToByte() }, MessageId.ToBytes() , Purpose.ToBytes(startsWithLength: true), Content.ToBytes(startsWithLength: true));

		public override string ToString()
		{
			return $"{Version} {MessageType} {MessageId} {Purpose.Length} {Purpose} {Content.Length} {Content}";
		}

		#endregion
	}
}
