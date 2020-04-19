using System;
using Xunit;
using Org.BouncyCastle.Security;
using System.Linq;
using Org.BouncyCastle.Crypto.Parameters;

/* We are testing if the wrapper is OK, so we will use this from RFC 8032 (test 2, page 23):

   ALGORITHM: Ed25519
   SECRET KEY: 4CCD089B28FF96DA9DB6C346EC114E0F5B8A319F35ABA624DA8CF6ED4FB8A6FB
   PUBLIC KEY: 3D4017C3E843895A92B70AA74D1B7EBC9C982CCF2EC4968CC0CD55F12AF4660C
   MESSAGE (length 1 byte): 72
   SIGNATURE: 92A009A9F0D4CAB8720E820B5F642540A2B27B5416503F8FB3762223EBDB69DA085AC1E43E15996E458F3613D0F11D8C387B2EAEB4302AEEB00D291612BB0C00

*/

namespace FreeMarketOne.Crypto.Tests
{
    public class UserKeyTest
    {

        private string privKeyString = "4CCD089B28FF96DA9DB6C346EC114E0F5B8A319F35ABA624DA8CF6ED4FB8A6FB";
        private string pubKeyString = "3D4017C3E843895A92B70AA74D1B7EBC9C982CCF2EC4968CC0CD55F12AF4660C";
        private string expectedSig = "92A009A9F0D4CAB8720E820B5F642540A2B27B5416503F8FB3762223EBDB69DA085AC1E43E15996E458F3613D0F11D8C387B2EAEB4302AEEB00D291612BB0C00";

        [Fact]
        public void ShouldGenerateExpectedKeyPair()
        {
            // TODO: I am not sure if this test is doing anything meaningful.
            var seedString = System.Text.Encoding.UTF8.GetBytes("4239423dfsdf(+_wo432o5iu34o5iusdflkjsjdf23fd");
            var expectedPriv = "343233393432336466736466282B5F776F3433326F35697533346F3569757364";
            var expectedPub = "811A033CCD086AA8A0A7E4F005440F6107232987F4A0DC1F6E7F89D4BD7B1E24";

            var key = new UserKey(seedString, false);

            var privHex = BitConverter.ToString((key.PrivateKey as Ed25519PrivateKeyParameters).GetEncoded()).Replace("-", "");
            var pubHex = BitConverter.ToString((key.PublicKey as Ed25519PublicKeyParameters).GetEncoded()).Replace("-", "");
            Assert.Equal(expectedPriv, privHex);
            Assert.Equal(expectedPub, pubHex);
        }

        [Fact]
        public void ShouldGenerateWithSecureRandom()
        {
            var random = new SecureRandom();
            var key = new UserKey(random);
            Assert.NotNull(key.PrivateKey);
            Assert.NotNull(key.PublicKey);
        }

        [Fact]
        public void CreatesCorrectPubKeyFromPrivKey()
        {
            var privKeyBytes = Enumerable.Range(0, privKeyString.Length)
                                         .Where(x => x % 2 == 0)
                                         .Select(x => Convert.ToByte(privKeyString.Substring(x, 2), 16))
                                         .ToArray();

            var key = new UserKey(privKeyBytes, false);

            Assert.Equal(
                BitConverter.ToString((key.PublicKey as Ed25519PublicKeyParameters).GetEncoded()).Replace("-", ""),
                pubKeyString);

        }

        [Fact]
        public void ShouldCreateCorrectSignatureAndVerify()
        {
            var privKeyBytes = Enumerable.Range(0, privKeyString.Length)
                                         .Where(x => x % 2 == 0)
                                         .Select(x => Convert.ToByte(privKeyString.Substring(x, 2), 16))
                                         .ToArray();

            var key = new UserKey(privKeyBytes, false);
            var msg = new byte[] { 0x72 };
            var result = key.Sign(msg);
            Assert.Equal(
                BitConverter.ToString(result).Replace("-", ""),
                expectedSig);

            Assert.True(key.Verify(msg, result));
        }


    }
}
