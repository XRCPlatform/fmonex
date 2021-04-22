using FreeMarketOne.DataStructure.Objects.BaseItems;
using Newtonsoft.Json;

namespace FreeMarketOne.WebApi.Models
{
    public class UserInfoModel
    {
        [JsonProperty("username")]
        public string UserName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("photoUrl")]
        public string Photo { get; set; }

        [JsonProperty("baseSignature")]
        public string BaseSignature { get; set; }

        [JsonProperty("publicKey")]
        public string PublicKey { get; set; }

        /**
         * <summary>
         * Creates a UserInfoModel from a <see cref="UserData"/> value.
         * </summary>
         *
         * <param name="userData">A <see cref="UserData"/> object.</param>
         */
        public static UserInfoModel FromUserData(UserData userData)
        {
            return new()
            {
                UserName = userData.UserName,
                Description = userData.Description,
                Photo = userData.Photo,
                BaseSignature = userData.BaseSignature,
                PublicKey = userData.PublicKey
            };
        }
    }
}