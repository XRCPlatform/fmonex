using System.Text;

namespace Libplanet.Extensions
{
    public class SHAProcessor
    {
        public string GetSHA256(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashDigest = Hashcash.Hash(bytes);

            return hashDigest.ToString();
        }
    }
}
