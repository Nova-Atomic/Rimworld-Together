﻿using RimworldTogether.GameServer.Core;
using RimworldTogether.GameServer.Misc;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON;

namespace RimworldTogether.GameServer.Managers
{
    public static class ModManager
    {
        private static List<string> loadedRequiredMods = new List<string>();
        private static List<string> loadedOptionalMods = new List<string>();
        private static List<string> loadedForbiddenMods = new List<string>();

        public static IReadOnlyCollection<string> LoadedRequiredMods => loadedRequiredMods;
        public static IReadOnlyCollection<string> LoadedOptionalMods => loadedOptionalMods;
        public static IReadOnlyCollection<string> LoadedForbiddenMods => loadedForbiddenMods;

        public static void LoadMods()
        {
            loadedRequiredMods.Clear();
            string[] requiredModsToLoad = Directory.GetDirectories(Program.requiredModsPath);
            foreach (string modPath in requiredModsToLoad)
            {
                try
                {
                    string aboutFile = Directory.GetFiles(modPath, "About.xml", SearchOption.AllDirectories)[0];
                    foreach (string str in XmlParser.ParseDataFromXML(aboutFile, "packageId"))
                    {
                        if (!loadedRequiredMods.Contains(str.ToLower())) loadedRequiredMods.Add(str.ToLower());
                    }
                }
                catch { Logger.WriteToConsole($"[Error] > Failed to load About.xml of mod at '{modPath}'", Logger.LogMode.Error); }
            }

            Logger.WriteToConsole($"Loaded required mods [{loadedRequiredMods.Count()}]");

            loadedOptionalMods.Clear();
            string[] optionalModsToLoad = Directory.GetDirectories(Program.optionalModsPath);
            foreach (string modPath in optionalModsToLoad)
            {
                try
                {
                    string aboutFile = Directory.GetFiles(modPath, "About.xml", SearchOption.AllDirectories)[0];
                    foreach (string str in XmlParser.ParseDataFromXML(aboutFile, "packageId"))
                    {
                        if (!loadedOptionalMods.Contains(str.ToLower())) loadedOptionalMods.Add(str.ToLower());
                    }
                }
                catch { Logger.WriteToConsole($"[Error] > Failed to load About.xml of mod at '{modPath}'", Logger.LogMode.Error); }
            }

            Logger.WriteToConsole($"Loaded optional mods [{loadedOptionalMods.Count()}]");

            loadedForbiddenMods.Clear();
            string[] forbiddenModsToLoad = Directory.GetDirectories(Program.forbiddenModsPath);
            foreach (string modPath in forbiddenModsToLoad)
            {
                try
                {
                    string aboutFile = Directory.GetFiles(modPath, "About.xml", SearchOption.AllDirectories)[0];
                    foreach (string str in XmlParser.ParseDataFromXML(aboutFile, "packageId"))
                    {
                        if (!loadedForbiddenMods.Contains(str.ToLower())) loadedForbiddenMods.Add(str.ToLower());
                    }
                }
                catch { Logger.WriteToConsole($"[Error] > Failed to load About.xml of mod at '{modPath}'", Logger.LogMode.Error); }
            }

            Logger.WriteToConsole($"Loaded forbidden mods [{loadedForbiddenMods.Count()}]");
        }

        public static bool CheckIfModConflict(Client client, LoginDetailsJSON loginDetailsJSON)
        {
            List<string> conflictingMods = new List<string>();

            if (loadedRequiredMods.Count() > 0)
            {
                foreach (string mod in loadedRequiredMods)
                {
                    if (!loginDetailsJSON.runningMods.Contains(mod))
                    {
                        conflictingMods.Add($"[Required] > {mod}");
                        continue;
                    }
                }

                foreach (string mod in loginDetailsJSON.runningMods)
                {
                    if (!loadedRequiredMods.Contains(mod) && !loadedOptionalMods.Contains(mod))
                    {
                        conflictingMods.Add($"[Disallowed] > {mod}");
                        continue;
                    }
                }
            }

            if (loadedForbiddenMods.Count() > 0)
            {
                foreach (string mod in loadedForbiddenMods)
                {
                    if (loginDetailsJSON.runningMods.Contains(mod))
                    {
                        conflictingMods.Add($"[Forbidden] > {mod}");
                    }
                }
            }

            if (conflictingMods.Count == 0)
            {
                client.runningMods = loginDetailsJSON.runningMods;
                return false;
            }

            else
            {
                if (client.isAdmin)
                {
                    Logger.WriteToConsole($"[Mod bypass] > {client.username}", Logger.LogMode.Warning);
                    client.runningMods = loginDetailsJSON.runningMods;
                    return false;
                }

                else
                {
                    UserManager_Joinings.SendLoginResponse(client, UserManager_Joinings.LoginResponse.WrongMods, conflictingMods);
                    return true;
                }
            }
        }
    }
}
