using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using DingoBot.Entities;

namespace DingoBot
{
    class Program
    {

        private static string configFile = "config.json";
        private static string logFile = "dingo.log";

        static void Main(string[] args)
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                Console.WriteLine("Starting Bot...");
                var task = MainAsync(args, tokenSource.Token);
                var readline = true;

                Console.WriteLine("Input Active");
                do
                {
                    string line = Console.ReadLine();
                    string[] prts = line.Split(' ');
                    for (int i = 0; i < prts.Length; i++)
                    {
                        switch (prts[0])
                        {
                            default:
                                Console.WriteLine("Unkown Command " + prts[0]);
                                break;


                            case "quit":
                            case "exit":
                            case "close":
                            case "stop":
                                Console.WriteLine("Stopping Bot...");
                                tokenSource.Cancel();
                                task.Wait();
                                break;
                        }
                    }
                } while (readline && !task.IsCanceled);

                Console.WriteLine("Bot Terminated...");
            }
        }

        static async Task MainAsync(string[] args, CancellationToken cancellationToken)
        {
            //prepare the config
            bool appendLog = false;
            bool debugRenderer = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-config":
                        configFile = args[++i];
                        break;

                    case "-log":
                        logFile = args[++i];
                        break;

                    case "-appendLog":
                        appendLog = true;
                        break;

                    case "-debugRenderer":
                        debugRenderer = true;
                        break;
                }
            }

            //Prepare the logging
            Logging.OutputLogQueue.Initialize(logFile, appendLog);

            //Load the config
            BotConfig config = new BotConfig();
            if (File.Exists(configFile))
            {
                Console.WriteLine("Loading Configuration: {0}", configFile);
                string json = await File.ReadAllTextAsync(configFile);
                try
                {
                    config = JsonConvert.DeserializeObject<BotConfig>(json);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
            }
            else
            {
                //Save the config 
                Console.WriteLine("Aborting because first time generating the configuration file.");
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                await File.WriteAllTextAsync(configFile, json);

                //Create the token file too if it doesnt exist.
                if (!File.Exists(config.TokenFile))
                    await File.WriteAllTextAsync(config.TokenFile, "<BOT TOKEN HERE>");
                return;
            }

            //var db = new DatabaseClient(config.SQL.Address, config.SQL.Database, config.SQL.Username, config.SQL.Password, "k_", 3306);
            //var success = db.OpenAsync().Result;

            if (debugRenderer)
            {
                Console.WriteLine("Debugging Renderer...");
                var renderer = new WkHtml.WkHtmlRenderer(config.WkHtmlToImage)
                {
                    Width = 540,
                    Height = 450,
                    Cropping = new WkHtml.WkHtmlRenderer.Crop()
                    {
                        X = 0,
                        Y = 0,
                        Width = 540,
                        Height = 450,
                    }
                };

                //Get the profile
                var profile = await Profile.LoadAsync("Ondo08");
                var serializedProfile = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(profile));
                var encodedProfile = System.Convert.ToBase64String(serializedProfile);

                //Store the profile into the cookie
                renderer.SetCookie("profile", encodedProfile);

                //Prepare the paths
                Console.WriteLine("Rendering....");
                string document = Path.Combine(config.Resources, "profile/", "slider.html");
                string absolute = Path.GetFullPath(document);
                await File.WriteAllTextAsync(absolute + ".dat", renderer.GetCookie("profile"));

                //Render
                int exit = await renderer.RenderAsync(absolute, $"{absolute}.png");
                if (exit != 0) {
                    Console.WriteLine("Failed to render the image. Exit Code {0}", exit);
                } else {
                    Console.WriteLine("Succesfully rendered the image");
                }

                Console.WriteLine("Finished Program. End.");
            }
            else
            {
                //Create the instance
                Console.WriteLine("Creating Bot...");
                Dingo bot = new Dingo(config);

                Console.WriteLine("Initializing Bot...");
                await bot.InitAsync();

                Console.WriteLine("Done.");
                await Task.Delay(-1, cancellationToken);

                Console.WriteLine("Deinitializing Bot...");
                await bot.DeinitAsync();
            }

        }
    }
}
