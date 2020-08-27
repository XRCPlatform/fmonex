using FreeMarketOne.DataStructure.Objects.MarketItems;
using Libplanet.Extensions;
using NUnit.Framework;
using System;
using System.Linq;

namespace LibPlanet.Extensions.Tests
{
    public class UserPrivPubKeyTest
    {
        [Test]
        public void GenerateItemSignatureVerificationAndRecoverPubKeys()
        {
            var privKey = new UserPrivateKey("4239423dfsdf(+_wo432o5iu34o5iusdflkjsjdf23fd");
            var pubkey = privKey.PublicKey;

            var mv1 = new MarketItemV1();
            mv1.Category = "1";
            mv1.BaseSignature = null;
            mv1.DealType = "1";
            mv1.Description = "1";
            mv1.Photos.Add("sia link");
            mv1.Shipping = "1";
            mv1.Title = "1";

            var bytes = mv1.ToByteArrayForSign();

            mv1.Signature = Convert.ToBase64String(privKey.Sign(bytes));
            mv1.Hash = mv1.GenerateHash();

            var isValid = pubkey.Verify(bytes, Convert.FromBase64String(mv1.Signature));
            Assert.IsTrue(isValid);

            var listOfRecoveredKeysM1 = pubkey.Recover(bytes, Convert.FromBase64String(mv1.Signature));
            var rightPubKey = pubkey.KeyParam.Q.GetEncoded();
            var isIdentical = false;

            foreach (var item in listOfRecoveredKeysM1)
            {
                if (item.SequenceEqual(rightPubKey)) isIdentical = true;
            }

            Assert.IsTrue(isIdentical);

            var mv2 = new MarketItemV1();
            mv2.Category = "2";
            mv2.BaseSignature = null;
            mv2.DealType = "2";
            mv2.Description = "1";
            mv2.Photos.Add("sia link 2");
            mv2.Shipping = "2";
            mv2.Title = "2";

            bytes = mv2.ToByteArrayForSign();

            mv2.Signature = Convert.ToBase64String(privKey.Sign(bytes));
            mv2.Hash = mv2.GenerateHash();

            isValid = pubkey.Verify(bytes, Convert.FromBase64String(mv2.Signature));
            Assert.IsTrue(isValid);

            var listOfRecoveredKeysM2 = pubkey.Recover(bytes, Convert.FromBase64String(mv2.Signature));
            isIdentical = false;

            foreach (var item in listOfRecoveredKeysM2)
            {
                if (item.SequenceEqual(rightPubKey)) isIdentical = true;
            }

            Assert.IsTrue(isIdentical);

            //final check of both signatures
            isIdentical = false;

            foreach (var item in listOfRecoveredKeysM1)
            {
                foreach (var item2 in listOfRecoveredKeysM2)
                {
                    if (item.SequenceEqual(item2)) isIdentical = true;
                }
            }

            Assert.IsTrue(isIdentical);
        }
    }
}