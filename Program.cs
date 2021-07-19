using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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
        private readonly ulong ppChannelId = 822339454794334238; // production
        //private readonly ulong ppChannelId = 865864730040205342; // test
        private List<ulong> PpRequestUserIds { get; set; }

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            client = new DiscordSocketClient();

            client.Log += Log;
            
            var token = Environment.GetEnvironmentVariable("AutomatedAsterakDiscordToken");

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            client.MessageReceived += MessageReceived;
            PpRequestUserIds = new List<ulong>();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }


        private Task Log(string msg)
        {
            return Log(new LogMessage(LogSeverity.Info, string.Empty, msg));
        }

        private Task Log(LogMessage msg)
        {
            File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "log.txt"), msg.ToString() + Environment.NewLine);
            return Task.CompletedTask;
        }

        private Task MessageReceived(SocketMessage msg)
        {
            if (msg.Author.Id.Equals(client.CurrentUser.Id))
            {
                return Task.CompletedTask;
            }

            switch (GetMessagePurpose(msg))
            {
                case MessagePurpose.DankPpSizeResponse:
                    _ = Log("Received pp size response message type");
                    _ = DankPpSizeResponse(msg);
                    break;
                case MessagePurpose.DankPpSizeRequest:
                    _ = Log("Received pp size request message type");
                    _ = DankPpSizeRequest(msg);
                    break;
                case MessagePurpose.DankPpReminder:
                    _ = Log("Received pp reminder message type");
                    _ = ClearPpRequests();
                    break;
            }
            return Task.CompletedTask;
        }

        private Task ClearPpRequests()
        {
            PpRequestUserIds = new List<ulong>();
            return Task.CompletedTask;
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
                _ = Log($"Incorrect channel. Expected {ppChannelId}, got {msg.Channel.Id}");
                return;
            }

            var user = lastPpUser;
            lastPpUser = null;

            user = msg.Channel.GetUserAsync(user.Id).Result as IGuildUser;

            if (user == null)
            {
                _ = Log("Dank pp size response - No information about user");
                return;
            }

            if (PpRequestUserIds.Contains(user.Id))
            {
                await AssignMicroPp(user, msg);
                return;
            }

            PpRequestUserIds.Add(user.Id);

            var pp = msg.Embeds.First().Description.Split('\n', StringSplitOptions.RemoveEmptyEntries).Last();
            var ppSize = pp.Substring(1, pp.Length - 2).Length;
            var ppSizeString = DeterminePpSize(ppSize);

            _ = Log($"Adding {ppSizeString} role to {user.Nickname} because of size {ppSize}");

            await RemovePpRoles(user);

            await user.AddRoleAsync(user.Guild.Roles.First(guildRole => guildRole.Name.Equals(ppSizeString)));

            foreach (var unicode in GetReactions(ppSize))
            {
                await msg.AddReactionAsync(new Emoji(unicode));
            }

        }

        private IEnumerable<string> GetReactions(int number)
        {
            if (number > 9)
            {
                yield return "\U0001F1EE";
            }
            
            switch (number%10)
            {
                case 0:
                    yield return "\U00000030\U0000FE0F\U000020E3";
                    break;
                case 1:
                    yield return "\U00000031\U0000FE0F\U000020E3";
                    break;
                case 2:
                    yield return "\U00000032\U0000FE0F\U000020E3";
                    break;
                case 3:
                    yield return "\U00000033\U0000FE0F\U000020E3";
                    break;
                case 4:
                    yield return "\U00000034\U0000FE0F\U000020E3";
                    break;
                case 5:
                    yield return "\U00000035\U0000FE0F\U000020E3";
                    break;
                case 6:
                    yield return "\U00000036\U0000FE0F\U000020E3";
                    break;
                case 7:
                    yield return "\U00000037\U0000FE0F\U000020E3";
                    break;
                case 8:
                    yield return "\U00000038\U0000FE0F\U000020E3";
                    break;
                case 9:
                    yield return "\U00000039\U0000FE0F\U000020E3";
                    break;
            }
        }

        private async Task RemovePpRoles(IGuildUser user)
        {
            var roles = user.Guild.Roles.Where(guildRole => ppSizeRoleNames.Contains(guildRole.Name));
            
            roles = roles.Where(role => user.RoleIds.Any(userRole => userRole.Equals(role.Id)));
            
            await user.RemoveRolesAsync(roles);
        }

        private async Task AssignMicroPp(IGuildUser user, SocketMessage msg)
        {
            _ = Log($"Adding  micro pp role to {user.Nickname}");
            await RemovePpRoles(user);
            await user.AddRoleAsync(user.Guild.Roles.First(guildRole => guildRole.Name.Equals(ppSizeRoleNames[0])));
            await msg.AddReactionAsync(new Emoji("\U0000274C"));
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
            if (msg.Embeds.Any() && "peepee size machine".Equals(msg.Embeds.First().Title))
            {
                return MessagePurpose.DankPpSizeResponse;
            }

            if(msg.Channel is SocketGuildChannel channel && msg.MentionedRoles.Contains(channel.Guild.Roles.FirstOrDefault(role => role.Name.Equals("pp reminder"))))
            {
                return MessagePurpose.DankPpReminder;
            }

            return MessagePurpose.Other;
        }
    }

    internal enum MessagePurpose
    {
        DankPpSizeRequest,
        DankPpSizeResponse,
        Other,
        DankPpReminder
    }
}
