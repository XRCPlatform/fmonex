using Bencodex;
using Bencodex.Types;

namespace Libplanet.Net.Messages
{
	//FMONECHANGE -  changed message serialization from NetMQMessage to Bencoded message
    internal class Ping : IBenEncodeable
    {
        public Ping()
        {
        }


        public Ping(byte[] bytes)
        {

        }

        public object FromBenBytes(byte[] bytes)
        {
            return new Ping();
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
