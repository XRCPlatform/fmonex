using System.Security.Cryptography;
using System.Text;

namespace Libplanet.Extensions
{
    public class SHAProcessor
    {
        public string GetSHA256(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);

            using SHA256 hashAlgo = SHA256.Create();
            var hashDigest = new HashDigest<SHA256>(hashAlgo.ComputeHash(bytes));

            return hashDigest.ToString();
        }
    }
}
