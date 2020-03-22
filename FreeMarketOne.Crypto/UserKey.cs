using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto;
using Newtonsoft.Json;

namespace FreeMarketOne.Crypto
{
    /// <summary>
    /// Wrapper around Ed25519 for key generation, signing and verifying
    /// signatures.
    /// </summary>
    public class UserKey : IUserKey
    {
        /// <summary>
        /// Priavte key ; optional
        /// </summary>
        /// <value></value>
        [JsonProperty("private")]
        public AsymmetricKeyParameter PrivateKey
        {
            get;
        }

        /// <summary>
        /// Public key, used particularly for verification.
        /// </summary>
        /// <value></value>
        [JsonProperty("public")]
        public AsymmetricKeyParameter PublicKey
        {
            get;
        }

        /// <summary>
        /// Create a private/public key pair.
        /// </summary>
        /// <param name="random"></param>
        public UserKey(SecureRandom random)
        {
            PrivateKey = new Ed25519PrivateKeyParameters(random);
            PublicKey = (PrivateKey as Ed25519PrivateKeyParameters).GeneratePublicKey();
        }

        /// <summary>
        /// Set existing keys directly.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="isPublic">If true, just set public. Otherwise set private
        /// and derive public.</param>
        public UserKey(byte[] bytes, bool isPublic)
        {
            if (isPublic)
            {
                PublicKey = new Ed25519PublicKeyParameters(bytes, 0);
            }
            else
            {
                PrivateKey = new Ed25519PrivateKeyParameters(bytes, 0);
                PublicKey = (PrivateKey as Ed25519PrivateKeyParameters).GeneratePublicKey();
            }

        }

        public byte[] Sign(byte[] msg)
        {
            var signer = new Ed25519Signer();
            signer.Init(true, PrivateKey);
            signer.BlockUpdate(msg, 0, msg.Length);
            byte[] signature = signer.GenerateSignature();
            return signature;
        }

        public bool Verify(byte[] msg, byte[] signature)
        {
            var signer = new Ed25519Signer();
            signer.Init(false, PublicKey);
            signer.BlockUpdate(msg, 0, msg.Length);
            return signer.VerifySignature(signature);
        }

    }
}