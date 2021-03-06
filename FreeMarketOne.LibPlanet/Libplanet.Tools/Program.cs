using System;
using System.IO;
using System.Threading.Tasks;
using Cocona;
using Libplanet.Extensions.Cocona.Commands;
using Libplanet.Extensions.Cocona.Extensions;

namespace Libplanet.Tools
{
    [HasSubCommands(typeof(ApvCommand), "apv", Description = "App protocol version utilities.")]
    [HasSubCommands(typeof(KeyCommand), "key", Description = "Manage private keys.")]
    [HasSubCommands(typeof(MptCommand), "mpt", Description = "Merkle Patricia Trie utilities.")]
    public class Program
    {
        private static readonly string FileConfigurationServiceRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "planetarium",
            "cli.json");

        public static Task Main(string[] args) =>
            CoconaLiteApp.Create()
                .ConfigureServices(services =>
                {
                    services.AddJsonConfigurationService(FileConfigurationServiceRoot);
                })
                .RunAsync<Program>(args, options =>
                {
                    options.TreatPublicMethodsAsCommands = false;
                });

        [PrimaryCommand]
        public Task Help() =>
            /* FIXME: I believe there is a better way... */
            Main(new[] { "--help" });
    }
}
