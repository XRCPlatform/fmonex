using NetMQ;
using System;
using System.Collections.Generic;

namespace FreeMarketOne.DataStructure.Chat
{
    public class ChatMessage
    {
        public string Message { get; set; }

        public string ExtraMessage { get; set; }

        public DateTime DateCreated { get; set; }

        public string Signature { get; set; }

        public ChatMessage()
        {
        }

        public ChatMessage(NetMQMessage rawMessage)
        {
            IEnumerator<NetMQFrame> it = rawMessage.GetEnumerator();

            it.MoveNext();
            Message = it.Current.ConvertToString();

            it.MoveNext();
            ExtraMessage = it.Current.ConvertToString();

            it.MoveNext();
            DateCreated = DateTime.ParseExact(it.Current.ConvertToString(), "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'", null);

            it.MoveNext();
            Signature = it.Current.ConvertToString();
        }

        public NetMQMessage ToNetMQMessage()
        {
            var message = new NetMQMessage();

            message.Append(new NetMQFrame(Message));
            message.Append(new NetMQFrame(ExtraMessage));
            message.Append(new NetMQFrame(DateCreated.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'", null)));
            message.Append(new NetMQFrame(Signature));

            return message;
        }
    }
}
