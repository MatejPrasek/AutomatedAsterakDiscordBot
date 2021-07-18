using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace AutomatedAsterakDiscordBot
{
    class Program
    {
        private DiscordSocketClient client;

        private readonly string[] ppSizeRoleNames = {"micro pp", "Smol pp Gang", "Average pp size", "Huge pp Gang", "MONSTER PP GANG", "MEGA PP"};

        private IGuildUser lastPpUser;
        private readonly ulong ppChannelId = 865864730040205342;

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            client = new DiscordSocketClient();

            client.Log += Log;
            
            var token = Environment.GetEnvironmentVariable("AutomatedAsterakDiscordToken");

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            client.MessageReceived += MessageReceived;

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }


        private Task Log(string msg)
        {
            return Log(new LogMessage(LogSeverity.Info, string.Empty, $"{DateTime.Now} - {msg}"));
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage msg)
        {

            if (msg.Author.Id.Equals(client.CurrentUser.Id))
            {
                return;
            }

            switch (GetMessagePurpose(msg))
            {
                case MessagePurpose.DankPpSizeResponse:
                    DankPpSizeResponse(msg);
                    break;
                case MessagePurpose.DankPpSizeRequest:
                    await DankPpSizeRequest(msg);
                    break;
                default:
                    return;
            }

            //var channel = client.GetChannel(msg.Channel.Id) as ITextChannel;
        }

        private Task DankPpSizeRequest(SocketMessage msg)
        {
            if (msg.Channel.Id.Equals(ppChannelId))
            {
                lastPpUser = msg.Author as IGuildUser;
            }

            return Task.CompletedTask;
        }

        private async Task DankPpSizeResponse(SocketMessage msg)
        {
            if (!msg.Channel.Id.Equals(ppChannelId))
            {
                return;
            }

            var user = lastPpUser;
            lastPpUser = null;

            if (user == null)
            {
                Log("Dank pp size response - No information about user");
                return;
            }

            var pp = msg.Embeds.First().Description.Split('\n', StringSplitOptions.RemoveEmptyEntries).Last();
            var ppSize = pp.Substring(1, pp.Length - 2).Length;
            var ppSizeString = DeterminePpSize(ppSize);

            var roles = user.Guild.Roles.Where(guildRole => ppSizeRoleNames.Contains(guildRole.Name));

            roles = roles.Where(role => ppSizeRoleNames.Any(name => name.Equals(role.Name)));

            await user.RemoveRolesAsync(roles);

            await user.AddRoleAsync(user.Guild.Roles.First(guildRole => guildRole.Name.Equals(ppSizeString)));
            
            await msg.AddReactionAsync(new Emoji("\U0001F346"));

            Console.WriteLine($"user {user.Username}, size {ppSizeString}");

            //var channel = client.GetChannel(msg.Channel.Id) as ITextChannel;
        }

        private string DeterminePpSize(int size)
        {
            return size switch
            {
                < 4 => ppSizeRoleNames[1],
                < 7 => ppSizeRoleNames[2],
                < 10 => ppSizeRoleNames[3],
                < 15 => ppSizeRoleNames[4],
                _ => ppSizeRoleNames[5]
            };
        }

        private MessagePurpose GetMessagePurpose(SocketMessage msg)
        {
            return msg.Author.IsBot ? GetBotMessagePurpose(msg) : GetUserMessagePurpose(msg);
        }

        private bool IsPpRequest(string msg)
        {
            string[] aliases = {"penis", "howbig", "peepee", "pickle", "pp"};

            return aliases.Any(alias => msg.Equals($"pls {alias}", StringComparison.InvariantCultureIgnoreCase));
        }
        private MessagePurpose GetUserMessagePurpose(SocketMessage msg)
        {
            if (IsPpRequest(msg.ToString()))
            {
                return MessagePurpose.DankPpSizeRequest;
            }

            return MessagePurpose.Other;
        }

        private MessagePurpose GetBotMessagePurpose(SocketMessage msg)
        {
            if (msg.Embeds.Any() && msg.Embeds.First().Title.Equals("peepee size machine"))
            {
                return MessagePurpose.DankPpSizeResponse;
            }

            return MessagePurpose.Other;
        }
    }

    internal enum MessagePurpose
    {
        DankPpSizeRequest,
        DankPpSizeResponse,
        Other
    }
}
