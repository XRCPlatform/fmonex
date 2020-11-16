using FreeMarketOne.DataStructure;
using FreeMarketOne.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.ServerCore.Test
{
    [TestClass]
    public class UserManagerTest
    {

        [TestMethod]
        public void TestShortPasswordToKeyDeriviation()
        {
            var userManager = new UserManager(null);
            string key = userManager.GenerateKey("shortPassword");
            Assert.AreEqual(32, key.Length);
            Assert.AreEqual("shortPassword5469523d905da5930da", key);
        }

        [TestMethod]
        public void TestLongPasswordToKeyDeriviation()
        {
            var userManager = new UserManager(null);
            string key = userManager.GenerateKey("thisPasswordVeeerryyyyLooooooongthisPasswordVeeerryyyyLooooooongthisPasswordVeeerryyyyLooooooong");
            Assert.AreEqual(32, key.Length);
            Assert.AreEqual("thisPasswordVeeerryyyyLooooooong", key);
        }
    }
}
