using Libplanet.Extensions;
using NUnit.Framework;

namespace LibPlanet.Extensions.Tests
{
    public class SHATest
    {

        [Test]
        public void GenerateHashForString()
        {
            var testString = "this is string for test";        

            var generator = new SHAProcessor();
            var hash = generator.GetSHA256(testString);

            Assert.AreEqual(hash, "972f2200bbc515376cc5e1a00927563b2f9f87af4ca215c653198e1d62391fc3");
        }
    }
}
