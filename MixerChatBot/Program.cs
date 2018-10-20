using System;
using System.Threading.Tasks;
using MixerChatBot.Authentication;
using MixerChatBot.Chat;

namespace MixerChatBot
{
    class Program
    {
        // This avoids a couple web requests that I don't want to write code for.
        const uint MixerDevShowChannelId = 48121131;
        const uint GaryWUserId = 18600056;

        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            AuthClient authClient = new AuthClient("aaba1641f5fca65a60386df808f7ec23f6a817a68beb869a", "97cbf17468b223623a574023da0ae150af5a964bf64fb0ca82c79164eaeba2bb");
            var oAuthToken = await authClient.RunOauthCodeFlowForConsoleAppAsync("chat:connect chat:chat chat:whisper chat:remove_message");

            var chatConnectionKey = await authClient.RequestChatAuthKey(oAuthToken, MixerDevShowChannelId);

            ChatClient chat = new ChatClient();
            await chat.ConnectAsync(chatConnectionKey, MixerDevShowChannelId, GaryWUserId);

            var chatMessageInfo = await chat.GetNextChatMessageAsync();
            while (chatMessageInfo != null)
            {
                Console.WriteLine(chatMessageInfo.data.user_name + ": " + chatMessageInfo.data.message.message[0].text);
                chatMessageInfo = await chat.GetNextChatMessageAsync();
            }
        }
    }
}
