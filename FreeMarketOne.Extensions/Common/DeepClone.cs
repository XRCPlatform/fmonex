using Newtonsoft.Json;

namespace FreeMarketOne.Extensions.Common
{
    public static class ExtensionMethods
    {
        public static T Clone<T>(this T a)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(a));
        }
    }
}
