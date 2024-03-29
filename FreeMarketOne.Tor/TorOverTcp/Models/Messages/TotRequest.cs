﻿using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Tor.TorOverTcp.Models.Fields;
using FreeMarketOne.Tor.TorOverTcp.Models.Messages.Bases;
using System;

namespace FreeMarketOne.Tor.TorOverTcp.Models.Messages
{
    /// <summary>
    /// Issued by the client. A Response MUST follow it.
    /// </summary>
    public class TotRequest : TotMessageBase
	{
		#region ConstructorsAndInitializers

		public TotRequest() : base()
		{

		}

		/// <param name="purpose">The Purpose of Request is arbitrary.</param>
		public TotRequest(string purpose) : this(purpose, TotContent.Empty)
		{

		}

		/// <param name="purpose">The Purpose of Request is arbitrary.</param>
		public TotRequest(string purpose, TotContent content) : base(TotMessageType.Request, TotMessageId.Random, new TotPurpose(purpose), content)
		{

		}

		#endregion

		#region Serialization

		public override void FromBytes(byte[] bytes)
		{
			Guard.NotNullOrEmpty(nameof(bytes), bytes);

			base.FromBytes(bytes);

			var expectedMessageType = TotMessageType.Request;
			if (MessageType != expectedMessageType)
			{
				throw new FormatException($"Wrong {nameof(MessageType)}. Expected: {expectedMessageType}. Actual: {MessageType}.");
			}
		}

		#endregion
	}
}
