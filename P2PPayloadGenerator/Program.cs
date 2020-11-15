using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
using Libplanet.Crypto;
using System;
using System.IO;
using System.Threading;
using Libplanet.Extensions;

namespace P2PPayloadGenerator
{
    class Program
    {
        private const int CommandSubjectArgIndex = 0;
        private const int CommandNumberOfExecutionsArgIndex = 1;
        private const int CommandSleepTimeArgIndex = 2;
        private const int CommandCurrentUserPasswordArgIndex = 3;

        private static object _locked = new object();

        public static FreeMarketOneServer Current { get; private set; }

        private static void Init(string password)
        {
            Current = FreeMarketOneServer.Current;

           
            Current.Initialize(password);

        }

        static void Main(string[] args)
        {
            Init(args[CommandCurrentUserPasswordArgIndex]);

            Console.WriteLine("To generate Users run (ObjectType:UserDataV1, quantity:10, sleeptime:5 password:******) : Users 10 5 password");
            if (args[CommandSubjectArgIndex].Equals("UserDataV1",StringComparison.InvariantCultureIgnoreCase)
                || args[CommandSubjectArgIndex].Equals("user", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine($"Matched generate users command with {args[CommandNumberOfExecutionsArgIndex]} and {args[CommandSleepTimeArgIndex]}");
                GenerateUsers(int.Parse(args[CommandNumberOfExecutionsArgIndex]), int.Parse(args[CommandSleepTimeArgIndex]));
            }
        }

        private static void GenerateUsers(int numberOfExecutions, int sleepTime)
        {
            for (int i = 0; i < numberOfExecutions; i++)
            {
                UserDataV1 user = new UserDataV1()
                {
                    UserName = "TestUser - " + i,
                    CreatedUtc = DateTime.UtcNow,
                    Description = "“The European Union fully supports the development, implementation and use of strong encryption,” the report notes. “However, there are instances where encryption renders analysis of the content of communications in the framework of access to electronic evidence extremely challenging or practically impossible despite the fact that the access to such data would be lawful.”",

                };
                
                string seed = Current.Users.CreateRandomSeed();

                var signedUserData = SignUserData(user, new UserPrivateKey(seed));

                Console.WriteLine(signedUserData.UserName + "" + signedUserData.PublicKey);

                if (Current.BasePoolManager.AcceptActionItem(signedUserData) == null)
                {
                    Current.BasePoolManager.PropagateAllActionItemLocal(true);
                }

                Thread.Sleep(sleepTime);
            }

            Console.ReadKey();
        }

        public static UserDataV1 SignUserData(UserDataV1 userData, UserPrivateKey privateKey)
        {
            lock (_locked)
            {
                userData.BaseSignature = userData.Signature;
                userData.PublicKey = Convert.ToBase64String(privateKey.PublicKey.KeyParam.Q.GetEncoded());

                var bytesToSign = userData.ToByteArrayForSign();

                userData.Signature = Convert.ToBase64String(privateKey.Sign(bytesToSign));

                userData.Hash = userData.GenerateHash();

                return userData;
            }
        }
    }
}
