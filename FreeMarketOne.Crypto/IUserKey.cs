using Org.BouncyCastle.Crypto;

namespace FreeMarketOne.Crypto
{
    public interface IUserKey
    {
         AsymmetricKeyParameter PrivateKey
         {
             get;
         }

        AsymmetricKeyParameter PublicKey
        {
            get;
        }

        byte[] Sign(byte[] msg);
        bool Verify(byte[] msg, byte[] signature);
    }
}