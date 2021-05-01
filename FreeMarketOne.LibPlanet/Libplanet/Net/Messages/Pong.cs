using Bencodex;
using Bencodex.Types;

namespace Libplanet.Net.Messages
{
	//FMONECHANGE -  changed message serialization from NetMQMessage to Bencoded message
    public class Pong : IBenEncodeable
    {
        public Pong()
        {
        }

        public Pong(byte[] bytes)
        {

        }

        public object FromBenBytes(byte[] bytes)
        {
            return new Pong();
        }

        public byte[] SerializeToBen()
        {
            return new Codec().Encode(ToBencodex());
        }

        public Dictionary ToBencodex()
        {
            return Dictionary.Empty;
        }
    }
}
