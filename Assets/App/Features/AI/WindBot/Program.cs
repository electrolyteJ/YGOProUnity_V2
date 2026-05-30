using UnityEngine;
using System;
using System.IO;
// using System.Net; // removed for Unity compatibility (HttpListener)
// using System.Web; // removed for Unity compatibility (HttpUtility)
using System.Diagnostics;
using System.Threading;
using WindBot.Game;
using WindBot.Game.AI;
using YGOSharp.OCGWrapper2;

namespace WindBot
{
    public class Program
    {
        internal static System.Random Rand;
        internal static bool ServerMode;

        internal static void Main(string[] args)
        {
            // Unity process mode: driven by WindBotRunner, not command-line entry
            Logger.WriteLine("WindBot initialized (Unity in-process mode).");
        }

        public static void InitDatas(string databasePath)
        {
            Rand = new System.Random();
            DecksManager.Init();
            string absolutePath = Path.GetFullPath(databasePath);
            if (!File.Exists(absolutePath) && !Path.IsPathRooted(databasePath))
                absolutePath = Path.GetFullPath("../" + databasePath);
            if (!File.Exists(absolutePath) && !Path.IsPathRooted(databasePath))
                absolutePath = Path.GetFullPath("../cdb/" + databasePath);
            if (!File.Exists(absolutePath))
            {
                Logger.WriteErrorLine("Cannot find cards database file.");
                Logger.WriteErrorLine("Please place cards.cdb next to WindBot.exe or Bot.exe .");
                // Press any key to quit... (skipped in Unity)
                // Console.ReadKey(); // not available in Unity
                throw new System.Exception("Cannot find cards database file: " + absolutePath);
            }
            NamedCardsManager.Init(absolutePath);
        }

        private static void RunFromArgs()
        {
            WindBotInfo Info = new WindBotInfo();
            Info.Name = Config.GetString("Name", Info.Name);
            Info.Deck = Config.GetString("Deck", Info.Deck);
            Info.DeckFile = Config.GetString("DeckFile", Info.DeckFile);
            Info.Dialog = Config.GetString("Dialog", Info.Dialog);
            Info.Host = Config.GetString("Host", Info.Host);
            Info.Port = Config.GetInt("Port", Info.Port);
            Info.HostInfo = Config.GetString("HostInfo", Info.HostInfo);
            Info.Version = Config.GetInt("Version", Info.Version);
            Info.Hand = Config.GetInt("Hand", Info.Hand);
            Info.Debug = Config.GetBool("Debug", Info.Debug);
            Info.Chat = Config.GetBool("Chat", Info.Chat);
            Run(Info);
        }

        private static void Run(object o)
        {
            // All errors should be caught instead of causing the program to crash.
            try
            {
                WindBotInfo Info = (WindBotInfo)o;
                GameClient client = new GameClient(Info);
                client.Start();
                Logger.DebugWriteLine(client.Username + " started.");
                while (client.Connection.IsConnected)
                {
                    try
                    {
                        client.Tick();
                        #if DEBUG
                            Thread.Sleep(1);
                        #else
                            Thread.Sleep(30);
                        #endif
                    }
                    catch (Exception ex)
                    {
                        if (Debugger.IsAttached)
                            throw;
                        Logger.WriteErrorLine("Tick Error: " + ex);
                    }
                }
                Logger.DebugWriteLine(client.Username + " end.");
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                    throw;
                Logger.WriteErrorLine("Run Error: " + ex);
            }
        }

        public static System.IO.FileStream ReadFile(string directory, string filename, string extension)
        {
            string tryfilename = filename + "." + extension;
            string fullpath = System.IO.Path.Combine(directory, tryfilename);
            // Try multiple paths for different runtime environments
            string[] searchPaths = new string[] {
                fullpath,
                filename,
                System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "WindBot", directory, tryfilename),
                System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "WindBot", directory, filename),
                System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "WindBot", tryfilename),
                System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "WindBot", filename),
                System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "WindBot/Decks", tryfilename),
                System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, "WindBot/Decks", filename),
                System.IO.Path.Combine("../", filename),
                System.IO.Path.Combine("../deck/", filename),
                System.IO.Path.Combine("../", tryfilename),
                System.IO.Path.Combine("../deck/", tryfilename)
            };
            foreach (string path in searchPaths)
            {
                if (System.IO.File.Exists(path))
                    return new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            }
            throw new System.IO.FileNotFoundException("Cannot find file: " + filename + " in directory " + directory);
        }
    }
}
