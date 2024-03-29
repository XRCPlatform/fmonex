﻿using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
using Libplanet.Crypto;
using System;
using System.IO;
using System.Threading;
using Libplanet.Extensions;
using Newtonsoft.Json;
using System.Linq;
using FreeMarketOne.Extensions.Common;
using System.Threading.Tasks;
using System.Collections.Generic;
using FreeMarketOne.Pools;

namespace P2PPayloadGenerator
{
    class Program
    {
        private const int CommandSubjectArgIndex = 0;
        private const int CommandNumberOfExecutionsArgIndex = 1;
        private const int CommandSleepTimeArgIndex = 2;
        private const int CommandCurrentUserPasswordArgIndex = 3;

        private static object _locked = new object();

        private static int counter = 0;
        private static int counter1 = 0;
        private static StreamWriter stream;

        public static FreeMarketOneServer Current { get; private set; }

        private static void Init(string password)
        {
            stream = File.CreateText($"run_{Guid.NewGuid()}.csv");
            Current = FreeMarketOneServer.Current;

            var task = Task.Factory.StartNew(()=> Current.Initialize(password));
            try
            {
                task.Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
           

            //let events and service start up
            Thread.Sleep(10000);

            //should load eventually but only wait limited time 100 seconds if it did not start it won't 
            while (Current.BasePoolManager == null && counter1 < 100)
            {
                Interlocked.Increment(ref counter1);
                Thread.Sleep(1000);
            }
            

            //should load eventually but only wait limited time 100 seconds if it did not start it won't 
            while (Current.MarketPoolManager == null && counter < 100)
            {
                Interlocked.Increment(ref counter);
                Thread.Sleep(1000);
            }

            while (Current.MarketBlockChainManager.SwarmServer.Peers.Count() < 1 || Current.BaseBlockChainManager.SwarmServer.Peers.Count() < 1 && counter < 1000)
            {
                if (counter == 0)
                {
                    Current.OnionSeedsManager.MarketSwarm = Current.MarketBlockChainManager.SwarmServer;
                    Current.OnionSeedsManager.BaseSwarm = Current.BaseBlockChainManager.SwarmServer;
                    var task1 = Task.Factory.StartNew(() =>
                        Current.OnionSeedsManager.Start()
                    );
                    try
                    {
                        task1.Wait();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    //Current.OnionSeedsManager.MarketSwarm.BootstrapAsync(Current.OnionSeedsManager.OnionSeedPeers.,1000000, 10000, 1, null).ConfigureAwait(false).GetAwaiter().GetResult();


                }

                Console.WriteLine($"Waiting for peers base {Current.BaseBlockChainManager.SwarmServer.Peers.Count()} market {Current.MarketBlockChainManager.SwarmServer.Peers.Count()}");
                Interlocked.Increment(ref counter);
                Thread.Sleep(10000);
            }

            foreach (var boundPeer in Current.BaseBlockChainManager.SwarmServer.Peers)
            {
                Console.WriteLine(boundPeer.ToString());
            }

            foreach (var boundPeer in Current.MarketBlockChainManager.SwarmServer.Peers)
            {
                Console.WriteLine(boundPeer.ToString());
            }
            List<Task> tasks = new List<Task>();
            tasks.Add(Task.Factory.StartNew(async () => await Current.BaseBlockChainManager.PullRemoteChainDifferences()));
            tasks.Add(Task.Factory.StartNew(async () => await Current.MarketBlockChainManager.PullRemoteChainDifferences()));
            Task.WaitAll(tasks.ToArray());

            while (Current.MarketBlockChainManager.BlockChain.Tip.Index <= 2 & Current.BaseBlockChainManager.BlockChain.Tip.Index <= 2)
            {
                Thread.Sleep(10000);
            }            
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

            stream.Close();
            stream.Dispose();
        }


        private static void GenerateOffers(int numberOfExecutions, int sleepTime, UserPrivateKey privateKey, UserDataV1 user)
        {
            string json = File.ReadAllText("../../../data/gold.json");
            
            for (int i = 1; i < numberOfExecutions; i++)
            {
                //deliberately create new object eevry time
                MarketItemV1 template = JsonConvert.DeserializeObject<MarketItemV1>(json);
                template.Title = template.Title + " from " + user.UserName  + " [" + i +"/"+ (numberOfExecutions-1)  + "]";
                template.Manufacturer = user.UserName;
                var _offer = Current.MarketManager.SignMarketData(template, privateKey);            

                var errors = Current.MarketPoolManager.AcceptActionItem(_offer);
                if (errors.HasValue)
                {
                    Console.WriteLine(errors.ToString());
                    if (errors.Value.Equals(PoolManagerStates.Errors.NoMinimalPeer))
                    {
                        Current.OnionSeedsManager.MarketSwarm = Current.MarketBlockChainManager.SwarmServer;
                        Current.OnionSeedsManager.BaseSwarm = Current.BaseBlockChainManager.SwarmServer;
                        var task1 = Task.Factory.StartNew(() =>
                            Current.OnionSeedsManager.Start()
                        );
                        try
                        {
                            task1.Wait();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
                

                Thread.Sleep(sleepTime);
            }

        }


        private static void GenerateUsers(int numberOfExecutions, int sleepTime)
        {
            for (int i = 0; i < numberOfExecutions; i++)
            {
                UserDataV1 user = new UserDataV1()
                {
                    UserName = "TestUser - C[" + i +"]",
                    CreatedUtc = DateTime.UtcNow,
                    Description = "The axis of rotation of the Solar System makes a large angle of about 60 degrees relative to the axis of rotation of the Milky Way. " +
                    "That seems unusual - for example, most of the bodies within the Solar Sytem are better behaved than that. Do most stars or planetary systems " +
                    " in the Milky Way rotate in close accord with the galactic rotation ? Or is there a large scatter, so that, in fact, our Sun is not atypical ? "

                };
                
                string seed = Current.UserManager.CreateRandomSeed();
                var privateKey = new UserPrivateKey(seed);
                
                var signedUserData = SignUserData(user, privateKey);

                string data = signedUserData.UserName + "," + signedUserData.PublicKey;
                stream.WriteLine(data);
                stream.Flush();

                Console.WriteLine(data);

                var result = Current.BasePoolManager.AcceptActionItem(signedUserData);
                if (result != null)
                {
                    Console.WriteLine("result:" + result.ToString());
                }
                Thread.Sleep(sleepTime);
                GenerateOffers(6, sleepTime, privateKey, user);
            }
        }

        public static UserDataV1 SignUserData(UserDataV1 userData, UserPrivateKey privateKey)
        {
            lock (_locked)
            {
                
                var clone = userData.Clone<UserDataV1>();
                clone.BaseSignature = clone.Signature;
                clone.PublicKey = Convert.ToBase64String(privateKey.PublicKey.KeyParam.Q.GetEncoded());
                var bytesToSign = clone.ToByteArrayForSign();
                clone.Signature = Convert.ToBase64String(privateKey.Sign(bytesToSign));
                clone.Hash = clone.GenerateHash();
                return clone;
            }
        }
    }
}
