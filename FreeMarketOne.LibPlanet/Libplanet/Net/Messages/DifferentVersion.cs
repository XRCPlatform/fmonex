using Bencodex;
using Bencodex.Types;

namespace Libplanet.Net.Messages
{
    public class DifferentVersion : IBenEncodeable
    {
        public DifferentVersion()
        {
        }

        public DifferentVersion(byte[] bytes)
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
