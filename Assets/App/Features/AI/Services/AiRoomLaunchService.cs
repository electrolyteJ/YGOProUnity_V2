using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace App.Features.AI.Services
{
    public sealed class AiRoomLaunchService
    {
        private const string RandomPattern = "Random=(\\w+)";
        private const string SelectDeckFileFlag = "SELECT_DECKFILE";

        public AiRoomBotDefinition[] ParseBots(IList<string> lines)
        {
            if (lines == null)
            {
                throw new ArgumentNullException("lines");
            }

            List<AiRoomBotDefinition> bots = new List<AiRoomBotDefinition>();
            for (int index = 0; index < lines.Count; index++)
            {
                string line = (lines[index] ?? string.Empty).Trim();
                if (line.Length == 0 || line[0] != '!')
                {
                    continue;
                }

                AiRoomBotDefinition bot = new AiRoomBotDefinition();
                bot.Name = line.TrimStart('!');
                bot.Command = lines[index + 1].Trim();
                bot.Description = lines[index + 2].Trim();
                bot.Flags = lines[index + 3].Trim().Split(' ');
                if (Array.IndexOf(bot.Flags, SelectDeckFileFlag) < 0)
                {
                    bots.Add(bot);
                }

                index += 3;
            }

            return bots.ToArray();
        }

        public string ResolveCommand(string command, IList<AiRoomBotDefinition> bots)
        {
            string resolvedCommand = command;
            Match match = Regex.Match(resolvedCommand, RandomPattern);
            if (!match.Success)
            {
                return resolvedCommand;
            }

            string randomFlag = match.Groups[1].Value;
            string randomBotCommand = GetRandomBotCommand(randomFlag, bots);
            if (randomBotCommand != string.Empty)
            {
                resolvedCommand = randomBotCommand;
            }

            return resolvedCommand;
        }

        /// <summary>
        /// 准备好进程内启动配置。
        /// 替代原来启动外部进程 (AI.Server.exe + WindBot.exe) 的方式。
        /// </summary>
        public AiRoomLaunchPreparation PrepareLaunch(AiRoomLaunchRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            AiRoomLaunchPreparation preparation = new AiRoomLaunchPreparation();

            // 不再限制平台 — 进程内模式支持所有平台
            preparation.IsPlatformSupported = true;

            // 解析命令参数
            string preparedCommand = PrepareCommand(request.Command, request.LockHand);
            preparation.PreparedCommand = preparedCommand;

            // 解析 bot 参数
            var botParams = ParseBotParams(preparedCommand);
            preparation.BotDeckFile = botParams.DeckFile;
            preparation.BotDeck = botParams.Deck;
            preparation.BotDialog = botParams.Dialog;
            preparation.BotHand = botParams.Hand;
            preparation.BotName = botParams.Name;

            // 对决设置
            preparation.NoCheck = request.NoCheck;
            preparation.NoShuffle = request.NoShuffle;
            preparation.LockHand = request.LockHand;

            return preparation;
        }

        private static string PrepareCommand(string command, bool lockHand)
        {
            string preparedCommand = command.Replace("'", "\"");
            if (lockHand)
            {
                preparedCommand += " Hand=1";
            }

            return preparedCommand;
        }

        private static BotParams ParseBotParams(string command)
        {
            var p = new BotParams();

            // 解析 key=value 参数 (WindBot 格式)
            // 例如: "Name=Bot1 Deck=BlueEyes DeckFile=Decks/BlueEyes.ydk Dialog=dialog_dir"
            var parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                int eq = part.IndexOf('=');
                if (eq < 0) continue;

                string key = part.Substring(0, eq).Trim();
                string value = part.Substring(eq + 1).Trim();

                switch (key.ToUpperInvariant())
                {
                    case "DECKFILE":
                        p.DeckFile = value;
                        break;
                    case "DECK":
                        p.Deck = value;
                        break;
                    case "DIALOG":
                        p.Dialog = value;
                        break;
                    case "NAME":
                        p.Name = value;
                        break;
                    case "HAND":
                        if (int.TryParse(value, out int h))
                            p.Hand = h;
                        break;
                    case "HOST":
                        p.Host = value;
                        break;
                    case "PORT":
                        if (int.TryParse(value, out int port))
                            p.Port = port;
                        break;
                }
            }

            return p;
        }

        private struct BotParams
        {
            public string DeckFile;
            public string Deck;
            public string Dialog;
            public string Name;
            public int Hand;
            public string Host;
            public int Port;
        }

        private static string GetRandomBotCommand(string flag, IList<AiRoomBotDefinition> bots)
        {
            List<AiRoomBotDefinition> foundBots = new List<AiRoomBotDefinition>();
            for (int index = 0; index < bots.Count; index++)
            {
                AiRoomBotDefinition bot = bots[index];
                if (bot != null && bot.Flags != null && Array.IndexOf(bot.Flags, flag) >= 0)
                {
                    foundBots.Add(bot);
                }
            }

            if (foundBots.Count == 0)
            {
                return string.Empty;
            }

            System.Random random = new System.Random();
            AiRoomBotDefinition selectedBot = foundBots[random.Next(foundBots.Count)];
            return selectedBot.Command;
        }
    }
}
