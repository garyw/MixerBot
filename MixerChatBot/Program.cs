using System;
using System.Threading.Tasks;
using MixerChatBot.Authentication;
using MixerChatBot.Chat;
using MixerChatBot.Chat.Messages;
using MixerChatBot.Http;

namespace MixerChatBot
{
    class Program
    {
        // This avoids a couple web requests that I don't want to write code for.
        const uint MixerDevShowChannelId = 48121131;

        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            AuthClient authClient = new AuthClient("aaba1641f5fca65a60386df808f7ec23f6a817a68beb869a", "97cbf17468b223623a574023da0ae150af5a964bf64fb0ca82c79164eaeba2bb");
            var oAuthToken = await authClient.RunOauthCodeFlowForConsoleAppAsync("chat:connect chat:chat chat:whisper chat:remove_message");

            HttpClient httpClient = new HttpClient(oAuthToken);
            var chatConnectionInfo = await httpClient.RequestChatAuthKeyAsync(MixerDevShowChannelId);
            var userInfo = await httpClient.GetAuthenticatedUserInfoAsync();

            ChatClient chat = new ChatClient();
            await chat.ConnectAsync(chatConnectionInfo, MixerDevShowChannelId, userInfo.id);

            var chatMessageInfo = await chat.GetNextChatMessageAsync();
            while (chatMessageInfo != null)
            {
                if (chatMessageInfo is ChatMessageEvent)
                {
                    var msg = chatMessageInfo as ChatMessageEvent;
                    Console.WriteLine(msg.data.user_name + ": " + msg.data.message.message[0].text);

                    if (string.Compare(msg.data.message.message[0].text, "y", true) == 0)
                    {
                        await chat.SendDeleteMessageAsync(msg.data.id);
                        await chat.SendWhisperAsync(msg.data.user_name, "Use the left stick or D-Pad to select a letter before hitting A and sending");
                        Console.WriteLine($"Sent whisper to {msg.data.user_name}");
                    }
                }
                else if (chatMessageInfo is ChatDeleteMessageEvent)
                {
                    var delete = chatMessageInfo as ChatDeleteMessageEvent;
                    Console.WriteLine(delete.data.moderator.user_name + ": deleted message " + delete.data.id);
                }

                chatMessageInfo = await chat.GetNextChatMessageAsync();

            }
        }
    }
}
