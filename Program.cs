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

        private readonly string[] smallPpJokes =
        {
            "Your penis is so small that if it were a tree it would be a bonsai tree.",
            "Your penis is so small that you can't simultaneously determine its position and velocity.",
            "Your penis is so small you put a red and white condom on it and let your gf play wheres waldo in your pubes.",
            "Your penis is so small that you always thought you had 2 belly buttons.",
            "Your penis is so small you can use it to open a jammed optical drive.",
            "Your penis is so small you can read a dvd with it.",
            "Your penis is so small you use it to change the SIM card in my iPhone.\nWait, no you don't. It doesn't reach down far enough to extend the tray.",
            "Your penis is so small when you run into a wall with a boner, you break your nose.",
            "Your penis is so big that it goes from A to Z... on the keyboard.",
            "Your penis is 3 inches from the floor... while laying on your belly. ",
            "Your penis might not be long, but it's skinny for sure.",
            "Your penis is so small you have to dig to find it.",
            "Your penis is so small you pee on your nuts.",
            "Your penis is so small having sex with you is like doing quantum physics. I have to deal with the 'Particle in a Box' problem.",
            "Your penis is so small that it is more than three standard deviations below the average pens size.",
            "Your penis is so small you cry yourself to sleep every night.",
            "Your penis is so rmall I've seen more meat in a vegetarian restaurant.",
            "On opposite day, your penis is the biggest on the planet.",
            "It may be short but at least it is not thick!",
            "You used to think you had huge balls, until you realized they only looked that way next to your tiny penis.",
            "Your penis is so small, you thought you was a female until you hit puberty.",
            "Your penis is so small you need a microscope and tweezers to fap.",
            "I could make a joke, but it wouldn't even be as lame as your penis.",
            "When you were younger, you thought you had a pubic hair until you pissed from it.",
            "Your penis is so small that it's harder to find than a G-spot.",
            "You wanted to make your password 'penis', but the admin told you it was too short.",
            "Your penis is so small that it is actually tail.",
            "I think your penis is the joke itself!",
            "Your penis is so small you have to sit down when you pee.",
            "When you get in very cold water, your penis gets so small that it almost retracts in the body.",
            "Your penis is so small, penis enlargement advertisers laugh at it.",
            "It's not cold, it's just small.",
            "There is nothing to joke about.",
            "If your penis was any smaller it would be a clit.",
            "If your penis was only just a little smaller, you could let an amoeba suck you off.",
            "There are two things in the universe that fit the definition of singularity. The inside of a black hole and your penis.",
            "Your penis is not only small but also useless.",
            "Your penis is so small that it acts as both a particle and a wave.",
            "Your penis isn't small, it's fun sized!"
        };

        private readonly string[] megaPpJokes =
        {
            "Your penis is so big, there's still snow on it in the summertime.",
            "Your penis is so big, it won't return Spielberg's calls.",
            "Your penis is so big, it graduated a year ahead of you from high school.",
            "Your penis has an elevator and a lobby.",
            "Your penis is so big, clowns climb out of it when you cum.",
            "Your penis is so big, ships use it to find thier way into the harbor.",
            "Your penis is so big, there was once a movie called Godzilla vs. my dick.",
            "Your penis is so big, it lives next door.",
            "Your penis is so big, you entered it in a big-dick contest and it came in first, second, and third.",
            "Your penis is so big, it votes.",
            "Your penis is so big, it has a three-picture deal.",
            "Your penis is so big that the head of it has only seen your balls in pictures.",
            "Your penis is so big, Henry Aaron used it to hit his 750th home run.",
            "No matter where you go, your dick always gets there first.",
            "Your penis was once ambassador to china.",
            "Your penis is so big, you use the Eiffel Tower as a french tickler.",
            "Your penis is so big, you could wear it as a tie if you wasn't so afraid of getting a hard-on and killing yourself.",
            "Your penis is so big, you have to use an elastic zipper.",
            "Your penis is so big, it has feet.",
            "Your penis is so big, a homeless family lives underneath it.",
            "Your penis is so big, they use the bullet train to test your condoms.",
            "Your penis is so big, it has investors.",
            "Your penis is so big, you use a hula hoop as a cock ring.",
            "Your penis is so big, you use it at parties as a limbo pole.",
            "Your penis is so big, king kong is going to crawl up it in the next remake.",
            "Your penis is so big, it has an opening act.",
            "Your penis is so big, you can f*ck elevator shafts.",
            "Your penis is so big, everytime you get hard, you cause a solar eclipse.",
            "If I cut your dick in two, O can tell how old you are.",
            "Your penis is so big, it needs an airplane warning light.",
            "Your penis is so big, that we're all a part of it, and it's all a part of us.",
            "Your penis is so big, we can't blow you without a ladder.",
            "Your penis is so big, we can ski down it.",
            "Your penis is so big, you have to check it as luggage when you fly.",
            "Your penis is so big, it has a personal trainer.",
            "Your penis is so big, it has stairs up the center like the Statue of Liberty.",
            "Your penis is so big, it's against the law to f*ck you without protective headgear.",
            "Your penis is so big, Stephen Hawking has a theory about it.",
            "Your penis is so big, it has its own gravity.",
            "NASA once launched a space probe to search for the tip of your dick.",
            "Your penis is so big, its impossible to see all of it without a satellite.",
            "Your penis is so big, it has a spine.",
            "Your penis is so big, it has a basement.",
            "Your penis is so big, you painted the foreskin red, white, and blue and used it as a flag.",
            "Your penis is so big, the city was going to build a statue of it, but they ran out of cement.",
            "Your penis is so big, when you get hard, your eyebrows get pulled down to your neck.",
            "Your penis is so big, it plays golf with the president.",
            "Your penis is so big, it charges money for its autograph."
        };

        private IGuildUser lastPpUser;
        private readonly ulong ppChannelId = ulong.Parse(Environment.GetEnvironmentVariable("AutomatedAsterakPpChannelId") ?? throw new ArgumentException("AutomatedAsterakDiscordToken environment variable is not set"));
        private Dictionary<ulong, DateTime> PpUserList { get; set; }
        private TimeSpan PpSizeResetTimeUtc { get; set; }
        private Random Rand { get; set; }

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
            Rand = new Random();

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

            await SendAJoke(msg, ppSize);

            await RemovePpRoles(user);

            await user.AddRoleAsync(user.Guild.Roles.First(guildRole => guildRole.Name.Equals(ppSizeString)));

            foreach (var unicode in GetReactions(ppSize))
            {
                await msg.AddReactionAsync(new Emoji(unicode));
            }

        }

        private async Task SendAJoke(SocketMessage msg, int ppSize)
        {
            switch (ppSize)
            {
                case < 4:
                    await((ITextChannel)msg.Channel).SendMessageAsync(smallPpJokes[Rand.Next(0, smallPpJokes.Length)]);
                    break;
                case 15:
                    await((ITextChannel)msg.Channel).SendMessageAsync(megaPpJokes[Rand.Next(0, megaPpJokes.Length)]);
                    break;
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
