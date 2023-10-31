using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RimworldTogether.GameServer.Commands;
using RimworldTogether.GameServer.Core;
using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.Misc;

namespace RimworldTogether.GameServer.Managers
{
    public class ServerCommandManager
    {
        public static string[] eventTypes = new string[]
        {
        "Raid",
        "Infestation",
        "MechCluster",
        "ToxicFallout",
        "Manhunter",
        "Wanderer",
        "FarmAnimals",
        "ShipChunks",
        "TraderCaravan"
        };

        public static string[] commandParameters;
        private readonly ILogger<ServerCommandManager> logger;
        private readonly ClientManager clientManager;
        private readonly CommandManager commandManager;
        private readonly SaveManager saveManager;
        private readonly ModManager modManager;
        private readonly WorldManager worldManager;
        private readonly CustomDifficultyManager customDifficultyManager;
        private readonly WhitelistManager whitelistManager;
        private readonly IHostApplicationLifetime hostApplicationLifetime;

        private Task? quitTask;

        public ServerCommandManager(
            ILogger<ServerCommandManager> logger,
            ClientManager clientManager,
            CommandManager commandManager,
            SaveManager saveManager,
            ModManager modManager,
            WorldManager worldManager,
            CustomDifficultyManager customDifficultyManager,
            WhitelistManager whitelistManager,
            IHostApplicationLifetime hostApplicationLifetime)
        {
            this.logger = logger;
            this.clientManager = clientManager;
            this.commandManager = commandManager;
            this.saveManager = saveManager;
            this.modManager = modManager;
            this.worldManager = worldManager;
            this.customDifficultyManager = customDifficultyManager;
            this.whitelistManager = whitelistManager;
            this.hostApplicationLifetime = hostApplicationLifetime;
        }


        public void ParseServerCommands(string parsedString)
        {
            string parsedPrefix = parsedString.Split(' ')[0].ToLower();
            int parsedParameters = parsedString.Split(' ').Count() - 1;
            commandParameters = parsedString.Replace(parsedPrefix + " ", "").Split(" ");

            try
            {
                ServerCommand commandToFetch = ServerCommandStorage.serverCommands.ToList().Find(x => x.prefix == parsedPrefix);
                if (commandToFetch == null) WriteToConsole($"[ERROR] > Command '{parsedPrefix}' was not found", LogMode.Warning);
                else
                {
                    if (commandToFetch.parameters != parsedParameters && commandToFetch.parameters != -1)
                    {
                        WriteToConsole($"[ERROR] > Command '{commandToFetch.prefix}' wanted [{commandToFetch.parameters}] parameters "
                            + $"but was passed [{parsedParameters}]", LogMode.Warning);
                    }
                    else
                    {
                        logger.LogInformation("Server Command: {CommandString}", parsedString);

                        if (commandToFetch.commandAction != null) commandToFetch.commandAction.Invoke(this);

                        else WriteToConsole($"[ERROR] > Command '{commandToFetch.prefix}' didn't have any action built in",
                            LogMode.Warning);
                    }
                }
            }
            catch (Exception e) { WriteToConsole($"[Error] > Couldn't parse command '{parsedPrefix}'. Reason: {e}", LogMode.Error); }
        }

        public async Task ListenForServerCommands(CancellationToken cancellationToken = default)
        {
            bool interactiveConsole;

            WriteToConsole("Type 'help' to get a list of available commands");

            try
            {
                if (Console.In.Peek() != -1) interactiveConsole = true;
                else interactiveConsole = false;
            }

            catch
            {
                interactiveConsole = false;
                WriteToConsole($"[Warning] > Couldn't found interactive console, disabling commands", LogMode.Warning);
            }

            if (!interactiveConsole)
            {
                WriteToConsole($"[Warning] > Couldn't found interactive console, disabling commands", LogMode.Warning);
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await Console.In.ReadLineAsync(cancellationToken);
                    ParseServerCommands(result);
                }
                catch (TaskCanceledException)
                {
                    WriteToConsole("Server Commands Stopped");
                    break;
                }
            }
        }

        public void HelpCommandAction()
        {
            WriteToConsole($"List of available commands: [{ServerCommandStorage.serverCommands.Count()}]", LogMode.Title);
            WriteToConsole("----------------------------------------", LogMode.Title);
            foreach (ServerCommand command in ServerCommandStorage.serverCommands)
            {
                WriteToConsole($"{command.prefix} - {command.description}", LogMode.Warning);
            }
            WriteToConsole("----------------------------------------", LogMode.Title);
        }

        public void ListCommandAction()
        {
            WriteToConsole($"Connected players: [{clientManager.Clients.ToArray().Count()}]", LogMode.Title);
            WriteToConsole("----------------------------------------", LogMode.Title);
            foreach (Client client in clientManager.Clients.ToArray())
            {
                WriteToConsole($"{client.username} - {client.SavedIP}", LogMode.Warning);
            }
            WriteToConsole("----------------------------------------", LogMode.Title);
        }

        public void DeepListCommandAction()
        {
            UserFile[] userFiles = UserManager.GetAllUserFiles();

            WriteToConsole($"Server players: [{userFiles.Count()}]", LogMode.Title);
            WriteToConsole("----------------------------------------", LogMode.Title);
            foreach (UserFile user in userFiles)
            {
                WriteToConsole($"{user.username} - {user.SavedIP}", LogMode.Warning);
            }
            WriteToConsole("----------------------------------------", LogMode.Title);
        }

        public void OpCommandAction()
        {
            Client toFind = clientManager.Clients.ToList().Find(x => x.username == ServerCommandManager.commandParameters[0]);
            if (toFind == null) WriteToConsole($"[ERROR] > User '{ServerCommandManager.commandParameters[0]}' was not found",
                LogMode.Warning);

            else
            {
                if (CheckIfIsAlready(toFind)) return;
                else
                {
                    toFind.isAdmin = true;

                    UserFile userFile = UserManager.GetUserFile(toFind);
                    userFile.isAdmin = true;
                    UserManager.SaveUserFile(toFind, userFile);

                    commandManager.SendOpCommand(toFind);

                    WriteToConsole($"User '{ServerCommandManager.commandParameters[0]}' has now admin privileges",
                        LogMode.Warning);
                }
            }

            bool CheckIfIsAlready(Client client)
            {
                if (client.isAdmin)
                {
                    WriteToConsole($"[ERROR] > User '{client.username}' " +
                    $"was already an admin", LogMode.Warning);
                    return true;
                }

                else return false;
            }
        }

        public void DeopCommandAction()
        {
            Client toFind = clientManager.Clients.ToList().Find(x => x.username == ServerCommandManager.commandParameters[0]);
            if (toFind == null) WriteToConsole($"[ERROR] > User '{ServerCommandManager.commandParameters[0]}' was not found",
                LogMode.Warning);

            else
            {
                if (CheckIfIsAlready(toFind)) return;
                else
                {
                    toFind.isAdmin = false;

                    UserFile userFile = UserManager.GetUserFile(toFind);
                    userFile.isAdmin = false;
                    UserManager.SaveUserFile(toFind, userFile);

                    commandManager.SendDeOpCommand(toFind);

                    WriteToConsole($"User '{toFind.username}' is no longer an admin",
                        LogMode.Warning);
                }
            }

            bool CheckIfIsAlready(Client client)
            {
                if (!client.isAdmin)
                {
                    WriteToConsole($"[ERROR] > User '{client.username}' " +
                    $"was not an admin", LogMode.Warning);
                    return true;
                }

                else return false;
            }
        }

        public void KickCommandAction()
        {
            Client toFind = clientManager.Clients.ToList().Find(x => x.username == ServerCommandManager.commandParameters[0]);
            if (toFind == null) WriteToConsole($"[ERROR] > User '{ServerCommandManager.commandParameters[0]}' was not found",
                LogMode.Warning);

            else
            {
                toFind.disconnectFlag = true;

                WriteToConsole($"User '{ServerCommandManager.commandParameters[0]}' has been kicked from the server",
                    LogMode.Warning);
            }
        }

        public void BanCommandAction()
        {
            Client toFind = clientManager.Clients.ToList().Find(x => x.username == ServerCommandManager.commandParameters[0]);
            if (toFind == null)
            {
                UserFile userFile = UserManager.GetUserFileFromName(ServerCommandManager.commandParameters[0]);
                if (userFile == null) WriteToConsole($"[ERROR] > User '{ServerCommandManager.commandParameters[0]}' was not found",
                    LogMode.Warning);

                else
                {
                    if (CheckIfIsAlready(userFile)) return;
                    else
                    {
                        userFile.isBanned = true;
                        UserManager.SaveUserFileFromName(userFile.username, userFile);

                        WriteToConsole($"User '{ServerCommandManager.commandParameters[0]}' has been banned from the server",
                            LogMode.Warning);
                    }
                }
            }

            else
            {
                commandManager.SendBanCommand(toFind);

                toFind.disconnectFlag = true;

                UserFile userFile = UserManager.GetUserFile(toFind);
                userFile.isBanned = true;
                UserManager.SaveUserFile(toFind, userFile);

                WriteToConsole($"User '{ServerCommandManager.commandParameters[0]}' has been banned from the server",
                    LogMode.Warning);
            }

            bool CheckIfIsAlready(UserFile userFile)
            {
                if (userFile.isBanned)
                {
                    WriteToConsole($"[ERROR] > User '{ServerCommandManager.commandParameters[0]}' " +
                    $"was already banned from the server", LogMode.Warning);
                    return true;
                }

                else return false;
            }
        }

        public void BanListCommandAction()
        {
            List<UserFile> userFiles = UserManager.GetAllUserFiles().ToList().FindAll(x => x.isBanned);

            WriteToConsole($"Banned players: [{userFiles.Count()}]", LogMode.Title);
            WriteToConsole("----------------------------------------", LogMode.Title);
            foreach (UserFile user in userFiles)
            {
                WriteToConsole($"{user.username} - {user.SavedIP}", LogMode.Warning);
            }
            WriteToConsole("----------------------------------------", LogMode.Title);
        }

        public void PardonCommandAction()
        {
            UserFile userFile = UserManager.GetUserFileFromName(ServerCommandManager.commandParameters[0]);
            if (userFile == null) WriteToConsole($"[ERROR] > User '{ServerCommandManager.commandParameters[0]}' was not found",
                LogMode.Warning);

            else
            {
                if (CheckIfIsAlready(userFile)) return;
                else
                {
                    userFile.isBanned = false;
                    UserManager.SaveUserFileFromName(userFile.username, userFile);

                    WriteToConsole($"User '{ServerCommandManager.commandParameters[0]}' is no longer banned from the server",
                        LogMode.Warning);
                }
            }

            bool CheckIfIsAlready(UserFile userFile)
            {
                if (!userFile.isBanned)
                {
                    WriteToConsole($"[ERROR] > User '{ServerCommandManager.commandParameters[0]}' " +
                    $"was not banned from the server", LogMode.Warning);
                    return true;
                }

                else return false;
            }
        }

        public void ReloadCommandAction()
        {
            Program.LoadResources(logger, modManager, clientManager, worldManager, customDifficultyManager, whitelistManager);
        }

        public void ModListCommandAction()
        {
            WriteToConsole($"Required Mods: [{modManager.LoadedRequiredMods.Count()}]", LogMode.Title);
            WriteToConsole("----------------------------------------", LogMode.Title);
            foreach (string str in modManager.LoadedRequiredMods)
            {
                WriteToConsole($"{str}", LogMode.Warning);
            }
            WriteToConsole("----------------------------------------", LogMode.Title);

            WriteToConsole($"Optional Mods: [{modManager.LoadedOptionalMods.Count()}]", LogMode.Title);
            WriteToConsole("----------------------------------------", LogMode.Title);
            foreach (string str in modManager.LoadedOptionalMods)
            {
                WriteToConsole($"{str}", LogMode.Warning);
            }
            WriteToConsole("----------------------------------------", LogMode.Title);

            WriteToConsole($"Forbidden Mods: [{modManager.LoadedForbiddenMods.Count()}]", LogMode.Title);
            WriteToConsole("----------------------------------------", LogMode.Title);
            foreach (string str in modManager.LoadedForbiddenMods)
            {
                WriteToConsole($"{str}", LogMode.Warning);
            }
            WriteToConsole("----------------------------------------", LogMode.Title);
        }

        public void EventCommandAction()
        {
            Client toFind = clientManager.Clients.ToList().Find(x => x.username == ServerCommandManager.commandParameters[0]);
            if (toFind == null) WriteToConsole($"[ERROR] > User '{ServerCommandManager.commandParameters[0]}' was not found",
                LogMode.Warning);

            else
            {
                for (int i = 0; i < ServerCommandManager.eventTypes.Count(); i++)
                {
                    if (ServerCommandManager.eventTypes[i] == ServerCommandManager.commandParameters[1])
                    {
                        commandManager.SendEventCommand(toFind, i);

                        WriteToConsole($"Sent event '{ServerCommandManager.commandParameters[1]}' to {toFind.username}",
                            LogMode.Warning);

                        return;
                    }
                }

                WriteToConsole($"[ERROR] > Event '{ServerCommandManager.commandParameters[1]}' was not found",
                    LogMode.Warning);
            }
        }

        public void EventAllCommandAction()
        {
            for (int i = 0; i < ServerCommandManager.eventTypes.Count(); i++)
            {
                if (ServerCommandManager.eventTypes[i] == ServerCommandManager.commandParameters[0])
                {
                    foreach (Client client in clientManager.Clients.ToArray())
                    {
                        commandManager.SendEventCommand(client, i);
                    }

                    WriteToConsole($"Sent event '{ServerCommandManager.commandParameters[0]}' to every connected player",
                        LogMode.Title);

                    return;
                }
            }

            WriteToConsole($"[ERROR] > Event '{ServerCommandManager.commandParameters[0]}' was not found",
                    LogMode.Warning);
        }

        public void EventListCommandAction()
        {
            WriteToConsole($"Available events: [{ServerCommandManager.eventTypes.Count()}]", LogMode.Title);
            WriteToConsole("----------------------------------------", LogMode.Title);
            foreach (string str in ServerCommandManager.eventTypes)
            {
                WriteToConsole($"{str}", LogMode.Warning);
            }
            WriteToConsole("----------------------------------------", LogMode.Title);
        }

        public void BroadcastCommandAction()
        {
            string fullText = "";
            foreach (string str in ServerCommandManager.commandParameters)
            {
                fullText += $"{str} ";
            }
            fullText = fullText.Remove(fullText.Length - 1, 1);

            commandManager.SendBroadcastCommand(fullText);

            WriteToConsole($"Sent broadcast '{fullText}'", LogMode.Title);
        }

        public void WhitelistCommandAction()
        {
            WriteToConsole($"Whitelisted usernames: [{Program.whitelist.WhitelistedUsers.Count()}]", LogMode.Title);
            WriteToConsole("----------------------------------------", LogMode.Title);
            foreach (string str in Program.whitelist.WhitelistedUsers)
            {
                WriteToConsole($"{str}", LogMode.Warning);
            }
            WriteToConsole("----------------------------------------", LogMode.Title);
        }

        public void WhitelistAddCommandAction()
        {
            UserFile userFile = UserManager.GetUserFileFromName(ServerCommandManager.commandParameters[0]);
            if (userFile == null) WriteToConsole($"[ERROR] > User '{ServerCommandManager.commandParameters[0]}' was not found",
                LogMode.Warning);

            else
            {
                if (CheckIfIsAlready(userFile)) return;
                else whitelistManager.AddUserToWhitelist(ServerCommandManager.commandParameters[0]);
            }

            bool CheckIfIsAlready(UserFile userFile)
            {
                if (Program.whitelist.WhitelistedUsers.Contains(userFile.username))
                {
                    WriteToConsole($"[ERROR] > User '{ServerCommandManager.commandParameters[0]}' " +
                        $"was already whitelisted", LogMode.Warning);

                    return true;
                }

                else return false;
            }
        }

        public void WhitelistRemoveCommandAction()
        {
            UserFile userFile = UserManager.GetUserFileFromName(ServerCommandManager.commandParameters[0]);
            if (userFile == null) WriteToConsole($"[ERROR] > User '{ServerCommandManager.commandParameters[0]}' was not found",
                LogMode.Warning);

            else
            {
                if (CheckIfIsAlready(userFile)) return;
                else whitelistManager.RemoveUserFromWhitelist(ServerCommandManager.commandParameters[0]);
            }

            bool CheckIfIsAlready(UserFile userFile)
            {
                if (!Program.whitelist.WhitelistedUsers.Contains(userFile.username))
                {
                    WriteToConsole($"[ERROR] > User '{ServerCommandManager.commandParameters[0]}' " +
                        $"was not whitelisted", LogMode.Warning);

                    return true;
                }

                else return false;
            }
        }

        public void WhitelistToggleCommandAction()
        {
            whitelistManager.ToggleWhitelist();
        }

        public void ForceSaveCommandAction()
        {
            Client toFind = clientManager.Clients.ToList().Find(x => x.username == ServerCommandManager.commandParameters[0]);
            if (toFind == null) WriteToConsole($"[ERROR] > User '{ServerCommandManager.commandParameters[0]}' was not found",
                LogMode.Warning);

            else
            {
                commandManager.SendForceSaveCommand(toFind);

                WriteToConsole($"User '{ServerCommandManager.commandParameters[0]}' has been forced to save",
                    LogMode.Warning);
            }
        }

        public void DeletePlayerCommandAction()
        {
            UserFile userFile = UserManager.GetUserFileFromName(ServerCommandManager.commandParameters[0]);
            if (userFile == null) WriteToConsole($"[ERROR] > User '{ServerCommandManager.commandParameters[0]}' was not found",
                LogMode.Warning);

            else saveManager.DeletePlayerDetails(userFile.username);
        }

        public void EnableDifficultyCommandAction()
        {
            if (Program.difficultyValues.UseCustomDifficulty == true)
            {
                WriteToConsole($"[ERROR] > Custom difficulty was already enabled", LogMode.Warning);
            }

            else
            {
                Program.difficultyValues.UseCustomDifficulty = true;
                customDifficultyManager.SaveCustomDifficulty(Program.difficultyValues);

                WriteToConsole($"Custom difficulty is now enabled", LogMode.Warning);
            }
        }

        public void DisableDifficultyCommandAction()
        {
            if (Program.difficultyValues.UseCustomDifficulty == false)
            {
                WriteToConsole($"[ERROR] > Custom difficulty was already disabled", LogMode.Warning);
            }

            else
            {
                Program.difficultyValues.UseCustomDifficulty = false;
                customDifficultyManager.SaveCustomDifficulty(Program.difficultyValues);

                WriteToConsole($"Custom difficulty is now disabled", LogMode.Warning);
            }
        }

        public void LockSaveCommandAction()
        {
            // TODO - Compression is different for client and server, causing saves to become useless after executing this
            return;

            byte[] saveFile = SaveManager.GetUserSaveFromUsername(ServerCommandManager.commandParameters[0]);

            if (saveFile == null)
            {
                WriteToConsole($"[ERROR] > Save {ServerCommandManager.commandParameters[0]} was not found", LogMode.Warning);
            }

            else
            {
                byte[] lockedBytes = GZip.CompressDefault(saveFile);

                File.WriteAllBytes(Path.Combine(Program.savesPath, ServerCommandManager.commandParameters[0] + ".mpsave"), lockedBytes);

                WriteToConsole($"Save {ServerCommandManager.commandParameters[0]} has been locked");
            }
        }

        public void UnlockSaveCommandAction()
        {
            // TODO - Compression is different for client and server, causing saves to become useless after executing this
            return;

            byte[] saveFile = SaveManager.GetUserSaveFromUsername(ServerCommandManager.commandParameters[0]);

            if (saveFile == null)
            {
                WriteToConsole($"[ERROR] > Save {ServerCommandManager.commandParameters[0]} was not found", LogMode.Warning);
            }

            else
            {
                byte[] unlockedBytes = GZip.DecompressDefault(saveFile);

                File.WriteAllBytes(Path.Combine(Program.savesPath, ServerCommandManager.commandParameters[0] + ".mpsave"), unlockedBytes);

                WriteToConsole($"Save {ServerCommandManager.commandParameters[0]} has been unlocked");
            }
        }

        public void QuitCommandAction()
        {
            Program.isClosing = true;

            WriteToConsole($"Waiting for all saves to quit", LogMode.Warning);

            foreach (Client client in clientManager.Clients.ToArray())
            {
                commandManager.SendForceSaveCommand(client);
            }

            while (clientManager.ClientCount > 0)
            {
                Thread.Sleep(1);
            }

            hostApplicationLifetime.StopApplication();
        }

        public void ForceQuitCommandAction() { Environment.Exit(0); }

        public void ClearCommandAction()
        {
            Console.Clear();

            WriteToConsole("[Cleared console]", LogMode.Title);
        }

        public enum LogMode { Normal, Warning, Error, Title }

        public static Dictionary<LogMode, ConsoleColor> colorDictionary = new Dictionary<LogMode, ConsoleColor>
        {
            { LogMode.Normal, ConsoleColor.White },
            { LogMode.Warning, ConsoleColor.Yellow },
            { LogMode.Error, ConsoleColor.Red },
            { LogMode.Title, ConsoleColor.Green }
        };
        public static void WriteToConsole(string text, LogMode mode = LogMode.Normal)
        {
            Console.ForegroundColor = colorDictionary[mode];
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    public static class ServerCommandStorage
    {
        private static ServerCommand helpCommand = new ServerCommand("help", 0,
            "Shows a list of all available commands to use",
            n => n.HelpCommandAction());

        private static ServerCommand listCommand = new ServerCommand("list", 0,
            "Shows all connected players",
            n => n.ListCommandAction());

        private static ServerCommand opCommand = new ServerCommand("op", 1,
            "Gives admin privileges to the selected player",
            n => n.OpCommandAction());

        private static ServerCommand deopCommand = new ServerCommand("deop", 1,
            "Removes admin privileges from the selected player",
            n => n.DeopCommandAction());

        private static ServerCommand kickCommand = new ServerCommand("kick", 1,
            "Kicks the selected player from the server",
            n => n.KickCommandAction());

        private static ServerCommand banCommand = new ServerCommand("ban", 1,
            "Bans the selected player from the server",
            n => n.BanCommandAction());

        private static ServerCommand pardonCommand = new ServerCommand("pardon", 1,
            "Pardons the selected player from the server",
            n => n.PardonCommandAction());

        private static ServerCommand deepListCommand = new ServerCommand("deeplist", 0,
            "Shows a list of all server players",
            n => n.DeepListCommandAction());

        private static ServerCommand banListCommand = new ServerCommand("banlist", 0,
            "Shows a list of all banned server players",
            n => n.BanListCommandAction());

        private static ServerCommand reloadCommand = new ServerCommand("reload", 0,
            "Reloads all server resources",
            n => n.ReloadCommandAction());

        private static ServerCommand modListCommand = new ServerCommand("modlist", 0,
            "Shows all currently loaded mods",
            n => n.ModListCommandAction());

        private static ServerCommand eventCommand = new ServerCommand("event", 2,
            "Sends a command to the selecter players",
            n => n.EventCommandAction());

        private static ServerCommand eventAllCommand = new ServerCommand("eventall", 1,
            "Sends a command to all connected players",
            n => n.EventAllCommandAction());

        private static ServerCommand eventListCommand = new ServerCommand("eventlist", 0,
            "Shows a list of all available events to use",
            n => n.EventListCommandAction());

        private static ServerCommand broadcastCommand = new ServerCommand("broadcast", -1,
            "Broadcast a message to all connected players",
            n => n.BroadcastCommandAction());

        private static ServerCommand clearCommand = new ServerCommand("clear", 0,
            "Clears the console output",
            n => n.ClearCommandAction());

        private static ServerCommand whitelistCommand = new ServerCommand("whitelist", 0,
            "Shows all whitelisted players",
            n => n.WhitelistCommandAction());

        private static ServerCommand whitelistAddCommand = new ServerCommand("whitelistadd", 1,
            "Adds a player to the whitelist",
            n => n.WhitelistAddCommandAction());

        private static ServerCommand whitelistRemoveCommand = new ServerCommand("whitelistremove", 1,
            "Removes a player from the whitelist",
            n => n.WhitelistRemoveCommandAction());

        private static ServerCommand whitelistToggleCommand = new ServerCommand("togglewhitelist", 0,
            "Toggles the whitelist ON or OFF",
            n => n.WhitelistToggleCommandAction());

        private static ServerCommand forceSaveCommand = new ServerCommand("forcesave", 1,
            "Forces a player to sync their save",
            n => n.ForceSaveCommandAction());

        private static ServerCommand deletePlayerCommand = new ServerCommand("deleteplayer", 1,
            "Deletes all data of a player",
            n => n.DeletePlayerCommandAction());

        private static ServerCommand enableDifficultyCommand = new ServerCommand("enabledifficulty", 0,
            "Locks an editable save for use [WIP]",
            n => n.EnableDifficultyCommandAction());

        private static ServerCommand disableDifficultyCommand = new ServerCommand("disabledifficulty", 0,
            "Locks an editable save for use [WIP]",
            n => n.DisableDifficultyCommandAction());

        private static ServerCommand lockSaveCommand = new ServerCommand("locksave", 1,
            "Locks an editable save for use [WIP]",
            n => n.LockSaveCommandAction());

        private static ServerCommand unlockSaveCommand = new ServerCommand("unlocksave", 1,
            "Unlocks a save file for editing [WIP]",
            n => n.UnlockSaveCommandAction());

        private static ServerCommand quitCommand = new ServerCommand("quit", 0,
            "Saves all player details and then closes the server",
            n => n.QuitCommandAction());

        private static ServerCommand forceQuitCommand = new ServerCommand("forcequit", 0,
            "Closes the server without saving player details",
            n => n.ForceQuitCommandAction());

        public static ServerCommand[] serverCommands = new ServerCommand[]
        {
        helpCommand,
        listCommand,
        deepListCommand,
        opCommand,
        deopCommand,
        kickCommand,
        banCommand,
        banListCommand,
        pardonCommand,
        reloadCommand,
        modListCommand,
        eventCommand,
        eventAllCommand,
        eventListCommand,
        broadcastCommand,
        whitelistCommand,
        whitelistAddCommand,
        whitelistRemoveCommand,
        whitelistToggleCommand,
        clearCommand,
        forceSaveCommand,
        deletePlayerCommand,
        enableDifficultyCommand,
        disableDifficultyCommand,
        //lockSaveCommand,
        //unlockSaveCommand,
        quitCommand,
        forceQuitCommand
        };
    }
}
