using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using DiscordJsonBanner.Config;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace DiscordJsonBanner
{
    class App
    {
        public static ConfigurationManager[] Configuration = null;

        public static EncoderParameters encoderParameters;
        public static ImageCodecInfo imageCodecInfo;

        public static short GlobalInterval = 5;
        public static short MaxConfigurationThreads = 2, MaxUploadThreads = 2;

        public static Random random;

        const char COMMANDLINE_ARGS_OPTION_CHAR = '*';

        static bool ContainsArg(string[] args, string arg) =>
            args.Any(a => a.Equals(arg, StringComparison.OrdinalIgnoreCase)) ||
            args.Any(a => a.StartsWith(arg, StringComparison.OrdinalIgnoreCase));

        static bool ContainsArg(string[] args, string arg, out string value)
        {
            static string SubstringUptoSpace(string text)
            {
                int index = text.IndexOf(" ");
                if (index == -1) return text;
                return text.Substring(0, index);
            }

            string response =
                args.FirstOrDefault(a => SubstringUptoSpace(a).Equals(arg, StringComparison.OrdinalIgnoreCase)) ??
                args.FirstOrDefault(a => SubstringUptoSpace(a).StartsWith(arg, StringComparison.OrdinalIgnoreCase));
            if (response != null)
                value = response.Substring(1 + arg.Length);
            else
                value = null;
            return value != null;
        }

        private static void Main(string[] args)
        {
            args = string.Join(' ', args).Split(COMMANDLINE_ARGS_OPTION_CHAR, StringSplitOptions.RemoveEmptyEntries)
                .ToArray();

            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += ProcessExit;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(ContainsArg(args, "debug")
                    ? Serilog.Events.LogEventLevel.Debug
                    : Serilog.Events.LogEventLevel.Information)
                .WriteTo.Console(theme: SystemConsoleTheme.Colored)
                .CreateLogger();

            if (ContainsArg(args, "ginterval", out var GlobalIntervalStringValue) &&
                !short.TryParse(GlobalIntervalStringValue, out GlobalInterval)) GlobalInterval = 5;
            if (ContainsArg(args, "mcthreads", out var MaxConfigurationThreadsStringValue) &&
                !short.TryParse(MaxConfigurationThreadsStringValue, out MaxConfigurationThreads))
                MaxConfigurationThreads = 2;
            if (ContainsArg(args, "mcthreads", out var MaxUploadThreadsStringValue) &&
                !short.TryParse(MaxUploadThreadsStringValue, out MaxUploadThreads)) MaxUploadThreads = 2;
            GlobalIntervalStringValue = MaxConfigurationThreadsStringValue = MaxUploadThreadsStringValue = null;
            if (!ContainsArg(args, "dis_token", out var discordToken))
            {
                Log.Fatal("No discord bot token provided");
                return;
            }

            Log.Debug("Application started with the {flag} flag.", "-debug");
            if (GlobalInterval <= 0)
                Log.Error("Global interval value of {interval} minutes is invalid. Default value: {newinterval}",
                    GlobalInterval, GlobalInterval = 5);
            else if (GlobalInterval != 5)
                Log.Information("Global interval is overriden. Timer interval setted to {interval} minutes.",
                    GlobalInterval);

            if (MaxConfigurationThreads <= 0)
                Log.Error("Value of maximum configuration threads ({count}) is invalid. Default value: {newcount}",
                    MaxConfigurationThreads, MaxConfigurationThreads = 2);
            if (MaxUploadThreads <= 0)
                Log.Error("Value of maximum upload threads ({count}) is invalid. Default value: {newcount}",
                    MaxUploadThreads, MaxUploadThreads = 2);

            random = new Random();

            var SETTINGS_FILE_PATH =
                Path.GetFullPath(Environment.GetEnvironmentVariable("settings file") ?? "config.json",
                    Environment.CurrentDirectory);
            if (!File.Exists(SETTINGS_FILE_PATH))
            {
                Log.Fatal("Configuration file wasn't found at {path}", SETTINGS_FILE_PATH);
                Environment.Exit(1);
                return;
            }

            Log.Debug("Loading configuration file at {path}", SETTINGS_FILE_PATH);
            Configuration = ConfigurationManager.FromJSON(File.ReadAllText(SETTINGS_FILE_PATH));
            SETTINGS_FILE_PATH = null;
            Log.Debug("Configuration file succesfully loaded!");
            Log.Information("Loaded {configurations} configurations with total of {groups} guilds.",
                Configuration.Length, Configuration.Sum(c => c.GuildIds.Length));
            RunAsync(discordToken);
            Task.Delay(-1).Wait();
        }

        static async void RunAsync(string token)
        {
            var discordClient = new DiscordRestClient();
            await discordClient.LoginAsync(TokenType.Bot, token).ConfigureAwait(false);
            while (true)
            {
                // Find all configuration providers that should be updated
                var configurationProviders = GetConfigurationsForUpdate().ToArray();
                // Define a locker
                var locker = new object();
                // Declare a base variable of running threads with value of zero
                var runningThreads = 0;
                // In case the encoder parameters are null, add a new parameter of quality
                (encoderParameters ??= new EncoderParameters(1)).Param[0] ??=
                    new EncoderParameter(Encoder.Quality, 100L);
                // And same for the image codec information
                imageCodecInfo ??= WebHelper.GetEncoder(ImageFormat.Jpeg);

                foreach (var configurationProvider in configurationProviders)
                {
                    // cycle over all the configurations in this group
                    foreach (var config in configurationProvider)
                        // and define the next run for each configuration
                        config.NextRun =
                            DateTime.UtcNow.AddMinutes(config.OnlineProvider?.UpdateInterval ?? GlobalInterval);
                    // if all the threads are busy, wait 2 seconds and check again
                    while (runningThreads >= MaxConfigurationThreads) await Task.Delay(2000).ConfigureAwait(false);
                    lock (locker)
                    {
                        runningThreads++;
                    }

                    var thread = new Thread(async () =>
                    {
                        //! the key is the provider URL, path to blank image and the image render settings
                        try
                        {
                            System.Drawing.Image image;
                            
                            var imagePaths = configurationProvider.Key.BlankImagePath.Where(path =>
                                path.Preconditions == null || path.Preconditions.Length == 0 || path.Preconditions.All(p => p.CheckPrecondition())).Select(path => path.Url).ToArray();
                            var imagePath = imagePaths.Length == 1
                                ? imagePaths[0]
                                : imagePaths[random.Next(0, imagePaths.Length)];
                            
                            if (!string.IsNullOrWhiteSpace(configurationProvider.Key.ProviderURL))
                            {
                                var rawResponse =
                                    await WebHelper.DownloadStringAsync(configurationProvider.Key.ProviderURL);
                                var response = JToken.Parse(rawResponse);
                                var online = response.SelectTokens(configurationProvider.Key.JsonSelector)
                                    .Sum(token => token.ToObject<int>());
                                // render the image with the online number and the settings and save it to a variable
                                image = ImageRenderHelper.RenderImage(imagePath, online,
                                    configurationProvider.Key.RenderSettings);
                            }
                            else
                                image = System.Drawing.Image.FromFile(imagePath);

                            // save this image to a memory stream
                            var imageStream = WebHelper.GetImageStream(image, imageCodecInfo, encoderParameters);
                            imageStream.Position = 0;
                            // declare a variables to store the image size
                            int imageWidth = image.Width, imageHeight = image.Height;
                            // we can dispose the image, we don't need for it anymore
                            image.Dispose();
                            // get the guilds from all the selected providers, store the group ID and the token in a Tuple
                            var groups = configurationProvider.SelectMany(config => config.GuildIds).ToArray();
                            // declare variables:   runningUploadThreads will serve to limit the maximum amount of threads to upload those covers
                            //                      totalGroups will store for us the count of the groups to update with this settings
                            //                      proceedGroups will store the amount of groups where the cover already uploaded
                            int runningUploadThreads = 0, totalGroups = groups.Length, proceedGroups = 0;
                            // this TaskCompletionSource will be called when all the groups successfully proceed
                            var allGroupsUpdated = new TaskCompletionSource<bool>();
                            // declare some lockers: the first will be used to create a thread and manage the amount of running threads.
                            //     proceedGroupsLocker will be used to increase the number of the proceedGroups and if needed to set the result of the completion source
                            object locker = new object(), proceedGroupsLocker = new object();
                            // cycle over all the groups
                            foreach (var guild in groups)
                            {
                                // lock everything to be sure we are increasing the number of the running threads correctly
                                lock (locker)
                                {
                                    // declare and start a new thread
                                    new Thread(async () =>
                                    {
                                        var guildObj = await discordClient.GetGuildAsync(guild).ConfigureAwait(false);
                                        if ((await guildObj.GetCurrentUserAsync().ConfigureAwait(false)).GuildPermissions.ManageGuild)
                                        {
                                            try
                                            {
                                                await guildObj.ModifyAsync(gp => gp.Banner = new Image(imageStream)).ConfigureAwait(false);
                                                Log.Debug("Updated cover for guild with ID {group_id}", guild);
                                            }
                                            catch (Exception e)
                                            {
                                                // Log the error
                                                Log.Error(e, "Unable to upload the cover to guild with ID {group_id}", guild);
                                            }
                                            finally
                                            {
                                                // At the final step we want to lock the proceedGroupsLocker and increase this amount
                                                lock (proceedGroupsLocker)
                                                {
                                                    // By the way we want to check if all the groups proceed. In case of true we want to set the result of the task completion source
                                                    if (++proceedGroups >= totalGroups) allGroupsUpdated.TrySetResult(true);
                                                }
                                            }
                                        }
                                    }).Start();
                                    runningUploadThreads++;
                                }
                            }

                            await allGroupsUpdated.Task;
                            imageStream.Dispose();
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, "Unable to render image with provider {provider} and image {image}",
                                configurationProvider.Key.ProviderURL, configurationProvider.Key.BlankImagePath.Select(bip => bip.Url));
                        }

                        lock (locker)
                        {
                            runningThreads--;
                        }
                    });
                    thread.Start();
                }

                // Wait the global interval
                await Task.Delay(GlobalInterval).ConfigureAwait(false);
            }
        }

        private static
            IEnumerable<IGrouping<(string ProviderURL, BlankImage[] BlankImagePath, string JsonSelector,
                ImageRenderConfiguration RenderSettings), ConfigurationManager>> GetConfigurationsForUpdate()
        {
            if (Configuration == null) return null;
            DateTime current = DateTime.UtcNow;
            var configurationProviders = from config in Configuration
                where config.NextRun <= current
                group config by ((string ProviderURL, BlankImage[] BlankImagePath, string JsonSelector,
                    ImageRenderConfiguration RenderSettings)) (config.OnlineProvider?.Url, config.BlankImagePath,
                    config.OnlineProvider?.OnlineSelector, config.ImageRender);
            return configurationProviders;
        }

        private static void ProcessExit(object sender, EventArgs e)
        {
            Log.CloseAndFlush();
        }

        public static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Fatal(
                $"[{DateTimeOffset.UtcNow.Day}/{DateTimeOffset.UtcNow.Month}/{DateTimeOffset.UtcNow.Year} {DateTimeOffset.UtcNow.Hour}:{DateTimeOffset.UtcNow.Minute}:{DateTimeOffset.UtcNow.Second}] " +
                $"Unhandled exception {e.ExceptionObject.GetType().FullName}:");
            if (e.ExceptionObject is Exception exc)
            {
                Console.WriteLine($"\t{exc.Message}");
                Console.WriteLine("\tStacktrace:");
                Console.WriteLine($"\t\t{exc.StackTrace}");
                Console.WriteLine(
                    $"This exception {(e.IsTerminating ? "terminating" : "does not terminating")} the thread!");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("\nNo information about this exception!");
                Console.WriteLine();
            }
        }
    }
}