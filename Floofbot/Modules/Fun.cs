﻿using Discord;
using Discord.Commands;
using Floofbot.Modules.Helpers;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Floofbot.Modules
{
    [Summary("Fun commands")]
    [Discord.Commands.Name("Fun")]
    public class Fun : ModuleBase<SocketCommandContext>
    {
        private static readonly Discord.Color EMBED_COLOR = Color.DarkOrange;
        private static Random rand = new Random();

        [Command("8ball")]
        [Summary("Ask the Magic 8-Ball a question")]
        public async Task AskEightBall([Summary("question")][Remainder] string question)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "Magic 8 Ball";
            builder.AddField("Question", question);
            builder.AddField("Answer", EightBall.GetRandomResponse());
            builder.Color = EMBED_COLOR;
            await SendEmbed(builder.Build());
        }

        [Command("xkcd")]
        [Summary("Get an xkcd comic by ID. If no ID given, get the latest one.")]
        public async Task XKCD([Summary("Comic ID")] string comicId = "")
        {
            int parsedComicId;
            if (!int.TryParse(comicId, out parsedComicId) || parsedComicId <= 0)
            {
                await Context.Channel.SendMessageAsync("Comic ID must be a positive integer less than or equal to Int32.MaxValue.");
                return;
            }

            string json = await ApiFetcher.RequestSiteContentAsString($"https://xkcd.com/{comicId}/info.0.json");
            if (string.IsNullOrEmpty(json))
            {
                await Context.Channel.SendMessageAsync("404 Not Found");
                return;
            }

            string imgLink;
            string imgHoverText;
            string comicTitle;
            using (JsonDocument parsedJson = JsonDocument.Parse(json))
            {
                imgLink = parsedJson.RootElement.GetProperty("img").ToString();
                imgHoverText = parsedJson.RootElement.GetProperty("alt").ToString();
                comicTitle = parsedJson.RootElement.GetProperty("safe_title").ToString();
            }

            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = comicTitle;
            builder.WithImageUrl(imgLink);
            builder.WithFooter(imgHoverText);
            builder.Color = EMBED_COLOR;
            await SendEmbed(builder.Build());
        }

        [Command("roll")]
        [Summary("Roll some dice e.g. 1d20")]
        public async Task RollDice([Summary("dice")] string diceStr = "")
        {
            try
            {
                Dice dice = Dice.FromString(diceStr);
                await Context.Channel.SendMessageAsync(string.Join(" ", dice.GenerateRolls()));
            }
            catch (ArgumentException e)
            {
                // This exception occurs when parsing the dice string,
                // and is meant to be displayed to the user
                // there is no need to log it
                await Context.Channel.SendMessageAsync(e.Message);
            }
        }

        [Command("catfact")]
        [Summary("Responds with a random cat fact")]
        public async Task RequestCatFact()
        {
            string fact = await ApiFetcher.RequestStringFromApi("https://catfact.ninja/fact", "fact");
            if (!string.IsNullOrEmpty(fact))
            {
                await Context.Channel.SendMessageAsync(fact);
            }
            else
            {
                await Context.Channel.SendMessageAsync("The catfact command is currently unavailable.");
            }
        }

        [Command("foxfact")]
        [Summary("Responds with a random fox fact")]
        public async Task RequestFoxFact()
        {
            string fact = await ApiFetcher.RequestStringFromApi("https://some-random-api.ml/facts/fox", "fact");
            if (!string.IsNullOrEmpty(fact))
            {
                await Context.Channel.SendMessageAsync(fact);
            }
            else
            {
                await Context.Channel.SendMessageAsync("The foxfact command is currently unavailable.");
            }
        }

        [Command("cat")]
        [Summary("Responds with a random cat")]
        public async Task RequestCat()
        {
            string fileUrl = await ApiFetcher.RequestEmbeddableUrlFromApi("https://aws.random.cat/meow", "file");
            if (!string.IsNullOrEmpty(fileUrl))
            {
                await SendAnimalEmbed(":cat:", fileUrl);
            }
            else
            {
                await Context.Channel.SendMessageAsync("The cat command is currently unavailable.");
            }
        }

        [Command("dog")]
        [Summary("Responds with a random dog")]
        public async Task RequestDog()
        {
            string fileUrl = await ApiFetcher.RequestEmbeddableUrlFromApi("https://random.dog/woof.json", "url");
            if (!string.IsNullOrEmpty(fileUrl))
            {
                await SendAnimalEmbed(":dog:", fileUrl);
            }
            else
            {
                await Context.Channel.SendMessageAsync("The dog command is currently unavailable.");
            }
        }

        [Command("fox")]
        [Summary("Responds with a random fox")]
        public async Task RequestFox()
        {
            string fileUrl = await ApiFetcher.RequestEmbeddableUrlFromApi("https://wohlsoft.ru/images/foxybot/randomfox.php", "file");
            if (!string.IsNullOrEmpty(fileUrl))
            {
                await SendAnimalEmbed(":fox:", fileUrl);
            }
            else
            {
                await Context.Channel.SendMessageAsync("The fox command is currently unavailable.");
            }
        }

        [Command("birb")]
        [Summary("Responds with a random birb")]
        public async Task RequestBirb()
        {
            string fileUrl = await ApiFetcher.RequestEmbeddableUrlFromApi("https://random.birb.pw/tweet.json", "file");
            if (!string.IsNullOrEmpty(fileUrl))
            {
                fileUrl = "https://random.birb.pw/img/" + fileUrl;
                await SendAnimalEmbed(":bird:", fileUrl);
            }
            else
            {
                await Context.Channel.SendMessageAsync("The birb command is currently unavailable.");
            }
        }

        [Command("choice")]
        [Summary("Chooses one item from a list of possible options")]
        public async Task Choice([Summary("the options, delimited by ';'")][Remainder]string choices = "")
        {
            if (!string.IsNullOrEmpty(choices))
            {
                string[] splitChoices = choices.Split(";")
                    .Select(choice => choice.Trim())
                    .Where(choice => !string.IsNullOrEmpty(choice)).ToArray();
                if (splitChoices.Length != 0)
                {
                    await Context.Channel.SendMessageAsync(splitChoices[rand.Next(splitChoices.Length)]);
                    return;
                }
            }
            string usageString = "Not enough options were provided, or all options were whitespace.\n" +
                "Example usage: `.choice choiceA; choiceB; choiceC`";
            EmbedBuilder builder = new EmbedBuilder {
                Description = usageString,
                Color = EMBED_COLOR
            };
            await SendEmbed(builder.Build());
        }

        [Command("minesweeper")]
        [Summary("Minesweeper minigame")]
        public async Task Minesweeper([Summary("width")]int width, [Summary("height")]int height, [Summary("bomb count")]int bombs)
        {
            if (width < 1 || height < 1 || bombs < 0)
            {
                await Context.Channel.SendMessageAsync("Invalid grid size or bomb count");
            }
            else if (width > 10 || height > 10)
            {
                await Context.Channel.SendMessageAsync("Max Grid Size: 10 x 10");
            }
            else if (bombs >= height * width)
            {
                await Context.Channel.SendMessageAsync("Too many bombs!");
            }
            else
            {
                MinesweeperBoard game = new MinesweeperBoard(height, width, bombs);
                EmbedBuilder builder = new EmbedBuilder();
                builder.Title = ":bomb: Minesweeper";
                builder.Color = EMBED_COLOR;
                builder.Description = game.ToString();
                await SendEmbed(builder.Build());
            }
        }

        [Command("enlarge")]
        [Summary("Enlarges the provided emoji")]
        public async Task enlarge([Summary("emoji ID")] string emojiId = "")
        {


            /* TODO
                1) use original regex to find all emoji matches
                2) count the number of matches. Too many is an error too little is an error
                3) try Parse that result. If it parses it's a custom emoji, if it doesn't it's a normal emoji
            */

            //TODO actually enlarging the emoji

            //       await Context.Channel.SendMessageAsync("The emoji is:");
            //       await Context.Channel.SendMessageAsync(emojiId);

            EmbedBuilder builder = new EmbedBuilder();
            builder.Color = EMBED_COLOR;

            var regex = "((<a?:[\\w\\d]+:\\d*>)|(\u00a9|\u00ae|[\u2000-\u200c]|[\u200e-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff]))";

            //       var matchEmoji = Regex.Match(emojiId, regex);
            //    if (matchEmoji.Success)

            var matchEmoji = Regex.Matches(emojiId, regex);
            int nr = matchEmoji.Count;

         //   await Context.Channel.SendMessageAsync($"nr of matches: {nr}");

            if (!Emote.TryParse(emojiId, out var parsedEmoteiId)) {
                if (nr == 1) {
                    builder.Title = $"Enlarged \\{emojiId}";
                    builder.WithDescription($"{emojiId}");
                }
                else {
                    if (nr == 0) {
                        await Context.Channel.SendMessageAsync("ERROR: `Please provide a valid emoji.`");
                        return;
                    }
                    else {
                        await Context.Channel.SendMessageAsync("ERROR: `The input text has too many parameters.`");
                        return;
                    }
                }
            }
            else
            {
                if (nr != 1)
                {
                    await Context.Channel.SendMessageAsync("ERROR: `The input text has too many parameters.`");
                    return;
                }

                builder.Title = $"Enlarged {emojiId}";
                string str = parsedEmoteiId.Id.ToString();

                if (parsedEmoteiId.Animated)
                {
                    builder.WithImageUrl($"https://cdn.discordapp.com/emojis/{str}.gif");
                }
                else
                {
                    builder.WithImageUrl($"https://cdn.discordapp.com/emojis/{str}.png");
                }
            }

            await SendEmbed(builder.Build());
        }

        private async Task SendAnimalEmbed(string title, string fileUrl)
        {
            EmbedBuilder builder = new EmbedBuilder()
                .WithTitle(title)
                .WithColor(EMBED_COLOR)
                .WithImageUrl(fileUrl);
            await SendEmbed(builder.Build());
        }

        private async Task SendEmbed(Embed embed)
        {
            await Context.Channel.SendMessageAsync("", false, embed);
        }
    }
}
