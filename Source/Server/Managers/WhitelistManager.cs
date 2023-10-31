using Microsoft.Extensions.Logging;
using RimworldTogether.GameServer.Core;
using RimworldTogether.GameServer.Files;
using RimworldTogether.Shared.Misc;

namespace RimworldTogether.GameServer.Managers
{
    public class WhitelistManager
    {
        private readonly ILogger<WhitelistManager> logger;

        public WhitelistManager(ILogger<WhitelistManager> logger)
        {
            this.logger = logger;
        }

        public void AddUserToWhitelist(string username)
        {
            Program.whitelist.WhitelistedUsers.Add(username);

            SaveWhitelistFile();

            logger.LogWarning($"User '{ServerCommandManager.commandParameters[0]}' has been whitelisted");
        }

        public void RemoveUserFromWhitelist(string username)
        {
            Program.whitelist.WhitelistedUsers.Remove(username);

            SaveWhitelistFile();

            logger.LogWarning($"User '{ServerCommandManager.commandParameters[0]}' is no longer whitelisted");
        }

        public void ToggleWhitelist()
        {
            Program.whitelist.UseWhitelist = !Program.whitelist.UseWhitelist;

            SaveWhitelistFile();

            if (Program.whitelist.UseWhitelist) logger.LogWarning("Whitelist is now ON");
            else logger.LogWarning("Whitelist is now OFF");
        }

        private void SaveWhitelistFile()
        {
            Serializer.SerializeToFile(Path.Combine(Program.corePath, "Whitelist.json"),
                Program.whitelist);
        }

        public void LoadServerWhitelist()
        {
            string path = Path.Combine(Program.corePath, "Whitelist.json");

            if (File.Exists(path)) Program.whitelist = Serializer.SerializeFromFile<WhitelistFile>(path);
            else
            {
                Program.whitelist = new WhitelistFile();
                Serializer.SerializeToFile(path, Program.whitelist);
            }

            logger.LogInformation("Loaded server whitelist");
        }
    }
}
