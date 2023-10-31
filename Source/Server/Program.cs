using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Managers;
using RimworldTogether.GameServer.Managers.Actions;
using RimworldTogether.GameServer.Misc;
using RimworldTogether.GameServer.Network;
using RimworldTogether.GameServer.Users;
using RimworldTogether.Shared.Misc;
using Serilog;

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace RimworldTogether.GameServer.Core
{
    public static partial class Program
    {
        public static string mainPath;
        public static string corePath;
        public static string usersPath;
        public static string settlementsPath;
        public static string savesPath;
        public static string mapsPath;
        public static string logsPath;
        public static string sitesPath;
        public static string factionsPath;

        public static string modsPath;
        public static string requiredModsPath;
        public static string optionalModsPath;
        public static string forbiddenModsPath;

        public static ServerConfigFile serverConfig;
        public static ServerValuesFile serverValues;
        public static WorldValuesFile worldValues;
        public static DifficultyValuesFile difficultyValues;
        public static EventValuesFile eventValues;
        public static SiteValuesFile siteValues;
        public static ActionValuesFile actionValues;
        public static WhitelistFile whitelist;

        public static string serverVersion = "1.0.9";

        public static bool isClosing;

        public static async Task Main(string[] args)
        {
            QuickEdit.DisableQuickEdit();

            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddSingleton<ClientManager>();
            builder.Services.AddSingleton<Network.Network>();
            builder.Services.AddSingleton<CommandManager>();
            builder.Services.AddSingleton<ResponseShortcutManager>();
            builder.Services.AddSingleton<SettlementManager>();
            builder.Services.AddSingleton<SiteManager>();
            builder.Services.AddSingleton<UserManager_Joinings>();
            builder.Services.AddSingleton<UserManager>();
            builder.Services.AddSingleton<SaveManager>();
            builder.Services.AddSingleton<ModManager>();
            builder.Services.AddSingleton<EventManager>();
            builder.Services.AddSingleton<LikelihoodManager>();
            builder.Services.AddSingleton<FactionManager>();
            builder.Services.AddSingleton<VisitManager>();
            builder.Services.AddSingleton<ChatManager>();
            builder.Services.AddSingleton<SpyManager>();
            builder.Services.AddSingleton<ServerOverallManager>();
            builder.Services.AddSingleton<WorldManager>();
            builder.Services.AddSingleton<UserLogin>();
            builder.Services.AddSingleton<TransferManager>();
            builder.Services.AddSingleton<OfflineVisitManager>();
            builder.Services.AddSingleton<RaidManager>();
            builder.Services.AddSingleton<CustomDifficultyManager>();
            builder.Services.AddSingleton<UserRegister>();
            builder.Services.AddSingleton<PacketHandler>();
            builder.Services.AddSingleton<ServerCommandManager>();
            builder.Services.AddSingleton<XmlParser>();
            builder.Services.AddSingleton<CustomDifficultyManager>();
            builder.Services.AddSingleton<WhitelistManager>();

            builder.Services.AddHostedService<ResourceStartupService>();
            builder.Services.AddHostedService<NetworkHost>();

            builder.Logging.AddSimpleConsole();

            Log.Logger = new Serilog.LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.RollingFile("logs/{Date}.txt")
                .CreateLogger();

            builder.Logging.AddSerilog(dispose: true);

            SetPaths();
            LoadServerConfig();

            var host = builder.Build();
            host.Run();
        }

        public static void LoadResources(ILogger logger, ModManager modManager, ClientManager clientManager, WorldManager worldManager, CustomDifficultyManager customDifficultyManager, WhitelistManager whitelistManager)
        {
            logger.LogInformation($"Loading all necessary resources");
            logger.LogInformation($"----------------------------------------");

            SetCulture(logger);
            customDifficultyManager.LoadCustomDifficulty();
            // LoadServerConfig();
            LoadServerValues(logger);
            LoadEventValues(logger);
            LoadSiteValues(logger);
            worldManager.LoadWorldFile();
            LoadActionValues(logger);
            whitelistManager.LoadServerWhitelist();
            modManager.LoadMods();

            Titler.ChangeTitle(clientManager.ClientCount, int.Parse(serverConfig.MaxPlayers));

            logger.LogInformation($"----------------------------------------");
        }

        private static void SetCulture(ILogger logger)
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
            CultureInfo.CurrentUICulture = new CultureInfo("en-US", false);
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US", false);
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US", false);

            logger.LogInformation($"Loaded server culture > [{CultureInfo.CurrentCulture}]");
        }

        private static void SetPaths()
        {
            mainPath = Directory.GetCurrentDirectory();
            corePath = Path.Combine(mainPath, "Core");
            usersPath = Path.Combine(mainPath, "Users");
            settlementsPath = Path.Combine(mainPath, "Settlements");
            savesPath = Path.Combine(mainPath, "Saves");
            mapsPath = Path.Combine(mainPath, "Maps");
            logsPath = Path.Combine(mainPath, "Logs");
            sitesPath = Path.Combine(mainPath, "Sites");
            factionsPath = Path.Combine(mainPath, "Factions");

            modsPath = Path.Combine(mainPath, "Mods");
            requiredModsPath = Path.Combine(modsPath, "Required");
            optionalModsPath = Path.Combine(modsPath, "Optional");
            forbiddenModsPath = Path.Combine(modsPath, "Forbidden");

            if (!Directory.Exists(corePath)) Directory.CreateDirectory(corePath);
            if (!Directory.Exists(usersPath)) Directory.CreateDirectory(usersPath);
            if (!Directory.Exists(settlementsPath)) Directory.CreateDirectory(settlementsPath);
            if (!Directory.Exists(savesPath)) Directory.CreateDirectory(savesPath);
            if (!Directory.Exists(mapsPath)) Directory.CreateDirectory(mapsPath);
            if (!Directory.Exists(logsPath)) Directory.CreateDirectory(logsPath);
            if (!Directory.Exists(sitesPath)) Directory.CreateDirectory(sitesPath);
            if (!Directory.Exists(factionsPath)) Directory.CreateDirectory(factionsPath);

            if (!Directory.Exists(modsPath)) Directory.CreateDirectory(modsPath);
            if (!Directory.Exists(requiredModsPath)) Directory.CreateDirectory(requiredModsPath);
            if (!Directory.Exists(optionalModsPath)) Directory.CreateDirectory(optionalModsPath);
            if (!Directory.Exists(forbiddenModsPath)) Directory.CreateDirectory(forbiddenModsPath);
        }

        private static void LoadServerConfig()
        {
            string path = Path.Combine(corePath, "ServerConfig.json");

            if (File.Exists(path)) serverConfig = Serializer.SerializeFromFile<ServerConfigFile>(path);
            else
            {
                serverConfig = new ServerConfigFile();
                Serializer.SerializeToFile(path, serverConfig);
            }

            // TODO Logger.WriteToConsole("Loaded server configs");
        }

        private static void LoadServerValues(ILogger logger)
        {
            string path = Path.Combine(corePath, "ServerValues.json");

            if (File.Exists(path)) serverValues = Serializer.SerializeFromFile<ServerValuesFile>(path);
            else
            {
                serverValues = new ServerValuesFile();
                Serializer.SerializeToFile(path, serverValues);
            }

            logger.LogInformation("Loaded server values");
        }

        private static void LoadEventValues(ILogger logger)
        {
            string path = Path.Combine(corePath, "EventValues.json");

            if (File.Exists(path)) eventValues = Serializer.SerializeFromFile<EventValuesFile>(path);
            else
            {
                eventValues = new EventValuesFile();
                Serializer.SerializeToFile(path, eventValues);
            }

            logger.LogInformation("Loaded event values");
        }

        private static void LoadSiteValues(ILogger logger)
        {
            string path = Path.Combine(corePath, "SiteValues.json");

            if (File.Exists(path)) siteValues = Serializer.SerializeFromFile<SiteValuesFile>(path);
            else
            {
                siteValues = new SiteValuesFile();
                Serializer.SerializeToFile(path, siteValues);
            }

            logger.LogInformation("Loaded site values");
        }

        private static void LoadActionValues(ILogger logger)
        {
            string path = Path.Combine(corePath, "ActionValues.json");

            if (File.Exists(path)) actionValues = Serializer.SerializeFromFile<ActionValuesFile>(path);
            else
            {
                actionValues = new ActionValuesFile();
                Serializer.SerializeToFile(path, actionValues);
            }

            logger.LogInformation("Loaded action values");
        }
    }
}