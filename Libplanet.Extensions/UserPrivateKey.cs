using Libplanet.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using System.Diagnostics.Contracts;
using System.Text;

namespace Libplanet.Extensions
{
    public class UserPrivateKey : PrivateKey
    {
        public UserPrivateKey(string seed) 
            : base(GenerateKeyParam(seed))
        {
            
        }

        [Pure]
        [IgnoreDuringEquals]
        public new UserPublicKey PublicKey
        {
            get
            {
                ECDomainParameters ecParams = GetECParameters();
                ECPoint q = ecParams.G.Multiply(this.keyParam.D);
                var kp = new ECPublicKeyParameters("ECDSA", q, ecParams);
                return new UserPublicKey(kp);
            }
        }

        public static ECPrivateKeyParameters GenerateKeyParam(string seed)
        {
            var gen = new ECKeyPairGenerator();

            SecureRandom sr = SecureRandom.GetInstance("SHA256PRNG", false);
            sr.SetSeed(Encoding.UTF8.GetBytes(seed));

            ECDomainParameters ecParams = GetECParameters();
            var keyGenParam = new ECKeyGenerationParameters(ecParams, sr);
            gen.Init(keyGenParam);

            return gen.GenerateKeyPair().Private as ECPrivateKeyParameters;
        }
    }
}
