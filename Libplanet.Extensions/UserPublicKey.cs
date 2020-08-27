using Libplanet.Crypto;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using System;
using System.Collections.Generic;

namespace Libplanet.Extensions
{
    public class UserPublicKey : PublicKey
    {
        public UserPublicKey(ECPublicKeyParameters keyParam) : base(keyParam)
        {

        }

        public UserPublicKey(byte[] publicKey) : base(publicKey)
        {

        }

        public List<byte[]> Recover(byte[] message, byte[] signature)
        {
            var pubKeysResult = new List<byte[]>();

            var h = new Sha256Digest();
            var hashed = new byte[h.GetDigestSize()];
            h.BlockUpdate(message, 0, message.Length);
            h.DoFinal(hashed, 0);
            h.Reset();

            var byteRLength = signature[3];
            var byteR = new byte[byteRLength];
            Buffer.BlockCopy(signature, 4, byteR, 0, byteRLength);
            var r = new BigInteger(byteR);

            var byteSLength = signature[3 + byteRLength + 2];
            var byteS = new byte[byteSLength];
            Buffer.BlockCopy(signature, 3 + byteRLength + 3, byteS, 0, byteSLength);
            var s = new BigInteger(byteS);
            var rs = new BigInteger[] { r, s };

            for (int rec = 0; rec < 4; rec++)
            {
                try
                {
                    ECPoint Q = ECDSA_SIG_recover_key_GFp(rs, hashed, rec, true);
                    //TODO: FOR DEBUG - base pubkey = var basex = this.KeyParam.Q.GetEncoded();
                    var recoveredPubKey = Q.GetEncoded();

                    pubKeysResult.Add(recoveredPubKey);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            return pubKeysResult;
        }

        private ECPoint ECDSA_SIG_recover_key_GFp(BigInteger[] sig, byte[] hash, int recid, bool check)
        {
            X9ECParameters ecParams = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
            int i = recid / 2;

            //Console.WriteLine("r: " + ToHex(sig[0].ToByteArrayUnsigned()));
            //Console.WriteLine("s: " + ToHex(sig[1].ToByteArrayUnsigned()));

            BigInteger order = ecParams.N;
            BigInteger field = (ecParams.Curve as FpCurve).Q;
            BigInteger x = order.Multiply(new BigInteger(i.ToString())).Add(sig[0]);
            if (x.CompareTo(field) >= 0) throw new Exception("X too large");

            //Console.WriteLine("Order: " + ToHex(order.ToByteArrayUnsigned()));
            //Console.WriteLine("Field: " + ToHex(field.ToByteArrayUnsigned()));

            byte[] compressedPoint = new Byte[x.ToByteArrayUnsigned().Length + 1];
            compressedPoint[0] = (byte)(0x02 + (recid % 2));
            Buffer.BlockCopy(x.ToByteArrayUnsigned(), 0, compressedPoint, 1, compressedPoint.Length - 1);
            ECPoint R = ecParams.Curve.DecodePoint(compressedPoint);

            //Console.WriteLine("R: " + ToHex(R.GetEncoded()));

            if (check)
            {
                ECPoint O = R.Multiply(order);
                if (!O.IsInfinity) throw new Exception("Check failed");
            }

            int n = (ecParams.Curve as FpCurve).Q.ToByteArrayUnsigned().Length * 8;
            BigInteger e = new BigInteger(1, hash);
            if (8 * hash.Length > n)
            {
                e = e.ShiftRight(8 - (n & 7));
            }
            e = BigInteger.Zero.Subtract(e).Mod(order);
            BigInteger rr = sig[0].ModInverse(order);
            BigInteger sor = sig[1].Multiply(rr).Mod(order);
            BigInteger eor = e.Multiply(rr).Mod(order);
            ECPoint Q = ecParams.G.Multiply(eor).Add(R.Multiply(sor));

            Console.WriteLine("n: " + n);
            //Console.WriteLine("e: " + ToHex(e.ToByteArrayUnsigned()));
            //Console.WriteLine("rr: " + ToHex(rr.ToByteArrayUnsigned()));
            //Console.WriteLine("sor: " + ToHex(sor.ToByteArrayUnsigned()));
            //Console.WriteLine("eor: " + ToHex(eor.ToByteArrayUnsigned()));
            //Console.WriteLine("Q: " + ToHex(Q.GetEncoded()));

            return Q;
        }
    }
}
