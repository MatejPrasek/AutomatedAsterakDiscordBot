using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace AutomatedAsterakDiscordBot
{
    class Program
    {
        private DiscordSocketClient client;

        private readonly string[] ppSizeRoleNames = {"micro pp", "Smol pp Gang", "Average pp size", "Huge pp Gang", "MONSTER PP GANG", "MEGA PP"};

        private IGuildUser lastPpUser;
        private readonly ulong ppChannelId = ulong.Parse(Environment.GetEnvironmentVariable("AutomatedAsterakPpChannelId") ?? throw new ArgumentException("AutomatedAsterakDiscordToken environment variable is not set"));
        private Dictionary<ulong, DateTime> PpUserList { get; set; }
        private TimeSpan PpSizeResetTimeUtc { get; set; }

        static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            client = new DiscordSocketClient();

            client.Log += ClientLog;
            
            var token = Environment.GetEnvironmentVariable("AutomatedAsterakDiscordToken");

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            client.MessageReceived += MessageReceived;
            PpUserList = await GetPpUserList();
            PpSizeResetTimeUtc = new TimeSpan(16, 0, 0);

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private async Task<Dictionary<ulong, DateTime>> GetPpUserList()
        {
            if (!File.Exists("PpUserList.json"))
            {
                return new Dictionary<ulong, DateTime>();
            }

            var ppUserList = await File.ReadAllTextAsync("PpUserList.json");
            return JsonConvert.DeserializeObject<Dictionary<ulong, DateTime>>(ppUserList);
        }


        private Task Log(string msg)
        {
            File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "log.txt"), $"{DateTime.Now} - {msg}{Environment.NewLine}");
            return Task.CompletedTask;
        }

        private Task ClientLog(LogMessage msg)
        {
            File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "ClientLog.txt"), msg.ToString() + Environment.NewLine);
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
                    _ = DankPpSizeReminder(msg);
                    break;
            }
            return Task.CompletedTask;
        }

        private async Task DankPpSizeReminder(SocketMessage msg)
        {
            PpSizeResetTimeUtc = msg.CreatedAt.UtcDateTime.TimeOfDay;

            foreach (var (key, lastMeasured) in PpUserList)
            {
                if (DateTime.UtcNow - lastMeasured <= TimeSpan.FromDays(2)) continue;

                var user = msg.Channel.GetUserAsync(key).Result as IGuildUser;
                if(user == null) continue;

                await ((ITextChannel) msg.Channel).SendMessageAsync(
                    $"{user.Mention} we are not sure about your pp size. Please allow us to measure it again. (you lost your pp size role due to inactivity");

                await RemovePpRoles(user);

                PpUserList.Remove(key);
                _ = SavePpUserListAsync();
            }
            
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

            if (user == null)
            {
                _ = Log("Dank pp size response - No information about user");
                return;
            }

            if (!PpUserList.TryGetValue(user.Id, out var lastMeasure))
            {
                lastMeasure = DateTime.MinValue;
            }

            if (!IsPpRequestInLimit(lastMeasure, msg.CreatedAt.UtcDateTime))
            {
                await AssignMicroPp(user, msg);
                return;
            }

            PpUserList[user.Id] = msg.CreatedAt.UtcDateTime;
            _ = SavePpUserListAsync();

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

        private async Task SavePpUserListAsync()
        {
            var serialized = JsonConvert.SerializeObject(PpUserList);
            await File.WriteAllTextAsync("PpUserList.json", serialized);
        }

        private bool IsPpRequestInLimit(DateTime lastMeasure, DateTime requestTime)
        {
            var resetDateTime = DateTime.Today + PpSizeResetTimeUtc;

            if (requestTime.TimeOfDay < PpSizeResetTimeUtc)
            {
                resetDateTime -= TimeSpan.FromDays(1);
            }

            return requestTime - resetDateTime < requestTime - lastMeasure;
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
