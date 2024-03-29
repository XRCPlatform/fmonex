﻿using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Tor.TorOverTcp.Models.Fields;
using FreeMarketOne.Tor.TorOverTcp.Models.Messages.Bases;
using System;

namespace FreeMarketOne.Tor.TorOverTcp.Models.Messages
{
    /// <summary>
    /// A Ping MUST precede it.
    /// </summary>
    public class TotPong : TotMessageBase
    {
		#region Statics

		public static TotPong Instance(TotMessageId messageId) => new TotPong(TotContent.Empty, messageId);

		#endregion
		
		#region ConstructorsAndInitializers

		public TotPong() : base()
		{

		}

		public TotPong(TotContent content, TotMessageId messageId) : base(TotMessageType.Pong, messageId, TotPurpose.Pong, content)
		{

		}

		#endregion

		#region Serialization

		public override void FromBytes(byte[] bytes)
		{
			Guard.NotNullOrEmpty(nameof(bytes), bytes);

			base.FromBytes(bytes);

			var expectedMessageType = TotMessageType.Pong;
			if (MessageType != expectedMessageType)
			{
				throw new FormatException($"Wrong {nameof(MessageType)}. Expected: {expectedMessageType}. Actual: {MessageType}.");
			}

			var expectedPurpose = TotPurpose.Pong;
			if (Purpose != expectedPurpose)
			{
				throw new FormatException($"Wrong {nameof(Purpose)}. Expected: {expectedPurpose}. Actual: {Purpose}.");
			}
		}

		#endregion
	}
}
