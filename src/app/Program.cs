// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading;
using Dapr.Client;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Ngsa.Middleware;

namespace Ngsa.Application
{
    /// <summary>
    /// Main application class
    /// </summary>
    public sealed partial class App
    {
        // App configuration values
        public static Config Config { get; } = new Config();
        public static DaprClient DaprClient { get; } = new DaprClientBuilder().Build();

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">command line args</param>
        /// <returns>0 == success</returns>
        public static int Main(string[] args)
        {
            DisplayAsciiArt(args);

            if (args.Contains("--dapr"))
            {
                WaitForDapr();
            }

            // build the System.CommandLine.RootCommand
            RootCommand root = BuildRootCommand();
            root.Handler = CommandHandler.Create<Config>(RunApp);

            // run the app
            return root.Invoke(args);
        }

        // wait for dapr sidecar to start
        private static void WaitForDapr()
        {
            const string daprUrl = "http://127.0.0.1:3500/v1.0/healthz";

            System.Net.Http.HttpClient client = new ();
            int i = 0;

            System.Net.Http.HttpResponseMessage resp = null;

            // wait for dapr to start
            while (true)
            {
                try
                {
                    resp = client.GetAsync(daprUrl).Result;

                    if ((int)resp.StatusCode >= 500 || (int)resp.StatusCode < 300)
                    {
                        // dapr is ready
                        Console.WriteLine($"{i}\t{(int)resp.StatusCode} dapr sidecar ready");
                        resp.Dispose();
                        return;
                    }

                    if (i > 10)
                    {
                        return;
                    }
                }
                catch
                {
                }

                // log and sleep
                i++;
                Console.WriteLine($"{i}\t{(int)resp.StatusCode} waiting for dapr sidecar");
                Thread.Sleep(1000);
            }
        }

        // load secrets from volume
        private static void LoadSecrets()
        {
            // TODO - move this - testing dapr access
            if (Config.Dapr)
            {
                while (true)
                {
                    try
                    {
                        var data = DaprClient.GetSecretAsync("kubernetes", "ngsa-secrets").Result;
                        Console.WriteLine($"CosmosCollection: {data["CosmosCollection"]}");
                        Console.WriteLine($"CosmosDatabase: {data["CosmosDatabase"]}");
                        Console.WriteLine($"CosmosKey: length {data["CosmosKey"].Length}");
                        Console.WriteLine($"CosmosUrl: {data["CosmosUrl"]}");
                        break;
                    }
                    catch
                    {
                        Console.WriteLine("retrying dapr");
                        Thread.Sleep(1000);
                    }
                }
            }

            if (Config.InMemory)
            {
                Config.Secrets = new Secrets
                {
                    CosmosCollection = "movies",
                    CosmosDatabase = "imdb",
                    CosmosKey = "in-memory",
                    CosmosServer = "in-memory",
                };

                Config.CosmosName = "in-memory";
            }
            else
            {
                Config.Secrets = Secrets.GetSecretsFromVolume(Config.SecretsVolume);

                // set the Cosmos server name for logging
                Config.CosmosName = Config.Secrets.CosmosServer.Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase);

                int ndx = Config.CosmosName.IndexOf('.', StringComparison.OrdinalIgnoreCase);

                if (ndx > 0)
                {
                    Config.CosmosName = Config.CosmosName.Remove(ndx);
                }
            }
        }

        // display Ascii Art
        private static void DisplayAsciiArt(string[] args)
        {
            if (args != null)
            {
                ReadOnlySpan<string> cmd = new (args);

                if (!cmd.Contains("--version") &&
                    (cmd.Contains("-h") ||
                    cmd.Contains("--help") ||
                    cmd.Contains("--dry-run")))
                {
                    const string file = "Core/ascii-art.txt";

                    try
                    {
                        if (File.Exists(file))
                        {
                            string txt = File.ReadAllText(file);

                            if (!string.IsNullOrWhiteSpace(txt))
                            {
                                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                                Console.WriteLine(txt);
                                Console.ResetColor();
                            }
                        }
                    }
                    catch
                    {
                        // ignore any errors
                    }
                }
            }
        }

        // Create a CancellationTokenSource that cancels on ctl-c or sigterm
        private static CancellationTokenSource SetupSigTermHandler(IWebHost host, NgsaLog logger)
        {
            CancellationTokenSource ctCancel = new ();

            Console.CancelKeyPress += async (sender, e) =>
            {
                e.Cancel = true;
                ctCancel.Cancel();

                logger.LogInformation("Shutdown", "Shutting Down ...");

                // trigger graceful shutdown for the webhost
                // force shutdown after timeout, defined in UseShutdownTimeout within BuildHost() method
                await host.StopAsync().ConfigureAwait(false);

                // end the app
                Environment.Exit(0);
            };

            return ctCancel;
        }

        // Log startup messages
        private static void LogStartup(NgsaLog logger)
        {
            logger.LogInformation($"Ngsa.Dapr Started", VersionExtension.Version);
        }

        // Build the web host
        private static IWebHost BuildHost()
        {
            // configure the web host builder
            IWebHostBuilder builder = WebHost.CreateDefaultBuilder()
                .UseUrls($"http://*:{Config.Port}/")
                .UseContentRoot("wwwroot")
                .UseStartup<Startup>()
                .UseShutdownTimeout(TimeSpan.FromSeconds(10))
                .ConfigureLogging(logger =>
                {
                    // log to XML
                    // this can be replaced when the dotnet XML logger is available
                    logger.ClearProviders();
                    logger.AddNgsaLogger(config => { config.LogLevel = Config.LogLevel; });

                    // if you specify the --log-level option, it will override the appsettings.json options
                    // remove any or all of the code below that you don't want to override
                    if (Config.IsLogLevelSet)
                    {
                        logger.AddFilter("Microsoft", Config.LogLevel)
                        .AddFilter("System", Config.LogLevel)
                        .AddFilter("Default", Config.LogLevel)
                        .AddFilter("Ngsa.Application", Config.LogLevel);
                    }
                });

            // build the host
            return builder.Build();
        }
    }
}
