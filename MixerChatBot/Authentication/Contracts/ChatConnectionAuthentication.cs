using Newtonsoft.Json;

namespace MixerChatBot.Authentication.Contracts
{
    [JsonObject]
    public class ChatConnectionAuthentication
    {
        [JsonProperty]
        public string[] roles { get; set; }

        [JsonProperty]
        public string authkey { get; set; }

        [JsonProperty]
        public string[] permissions { get; set; }

        [JsonProperty]
        public string[] endpoints { get; set; }
    }
}
