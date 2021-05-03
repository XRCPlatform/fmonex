using System.IO;
using Xunit;

namespace FreeMarketOne.ServerCore.Test
{
    public class DataDirTest
    {
        [Fact]
        public void CanGenerate()
        {
            var path = Path.GetFullPath("fmonetest");
            var dataDir = new DataDir(path);
            Assert.True(Directory.Exists(path));

            var appSettingsFile = Path.Combine(path, "appsettings.json");
            Assert.True(File.Exists(appSettingsFile));
            Assert.Equal(File.ReadAllText(appSettingsFile), dataDir.MakeAppSettings());
            Directory.Delete(path, true);
        } 
    }
}
