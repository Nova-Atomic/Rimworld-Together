using Newtonsoft.Json;
using RimworldTogether.GameServer.Core;
using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Misc;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Serializers;
using Shared.Misc;
using System.Diagnostics;
using System.IO.Compression;

namespace RimworldTogether.GameServer.Managers
{
    public static class ModManager
    {
        public static List<ModFile> requiredMods = new List<ModFile>();
        public static List<ModFile> optionalMods = new List<ModFile>();
        public static List<ModFile> forbiddenMods = new List<ModFile>();

        public enum ModType { Required, Optional, Forbidden }

        public static void LoadMods()
        {
            CheckModFolder(ModType.Required);
            CheckModFolder(ModType.Optional);
            CheckModFolder(ModType.Forbidden);
        }

        private static void CheckModFolder(ModType modType)
        {
            string[] toLoad = null;
            string modCategory = "";

            switch (modType)
            {
                case ModType.Required:
                    requiredMods = new List<ModFile>();
                    toLoad = Directory.GetDirectories(Program.requiredModsPath);
                    modCategory = "required";
                    break;

                case ModType.Optional:
                    optionalMods = new List<ModFile>();
                    toLoad = Directory.GetDirectories(Program.optionalModsPath);
                    modCategory = "optional";
                    break;

                case ModType.Forbidden:
                    forbiddenMods = new List<ModFile>();
                    toLoad = Directory.GetDirectories(Program.forbiddenModsPath);
                    modCategory = "forbidden";
                    break;
            }

            string[] modsToConvert = toLoad.Where(x => Path.GetExtension(x) != ".mpmod").ToArray();
            Logger.WriteToConsole($"[Converting {modCategory} mods]", Logger.LogMode.Title);
            foreach (string modPath in modsToConvert) { ConvertModSingle(modPath); }
            Logger.WriteToConsole($"[converted {modCategory} mods]", Logger.LogMode.Title);

            //FIXME
            //FIX ETERNAL LOOP BECAUSE NOT DELETING FILES AT THE MOMENT
            //if (modsToConvert.Length > 0) CheckModFolder(modType);

            string[] modsToLoad = toLoad.Where(x => Path.GetExtension(x) == ".mpmod").ToArray();
            Logger.WriteToConsole($"[Loading {modCategory} mods]", Logger.LogMode.Title);
            foreach (string modPath in modsToLoad) { LoadModSingle(modPath, modType); }
            Logger.WriteToConsole($"[Loaded {modCategory} mods]", Logger.LogMode.Title);
        }

        private static void LoadModSingle(string path, ModType modType)
        {
            ModFile modFile = Serializer.SerializeFromFile<ModFile>(path);

            switch (modType)
            {
                case ModType.Required:
                    requiredMods.Add(modFile);
                    break;

                case ModType.Optional:
                    optionalMods.Add(modFile);
                    break;

                case ModType.Forbidden:
                    forbiddenMods.Add(modFile);
                    break;
            }

            Logger.WriteToConsole($"Loaded mod '{modFile.name}'");
        }

        private static void ConvertModSingle(string path)
        {
            ModFile modFile = new ModFile();

            modFile = GetModDetails(path, modFile);

            modFile = ZipMod(path, modFile);

            ConvertMod(path, modFile);
        }

        private static ModFile GetModDetails(string path, ModFile modFile)
        {
            string aboutFile = Directory.GetFiles(path, "About.xml", SearchOption.AllDirectories)[0];

            string[] xmlValues = XmlParser.ParseDataFromXML(aboutFile, "name");
            modFile.name = xmlValues[0];

            xmlValues = XmlParser.ParseDataFromXML(aboutFile, "packageId");
            modFile.packageID = xmlValues[0];

            return modFile;
        }

        private static ModFile ZipMod(string path, ModFile modFile)
        {
            string modDirectory = Path.GetDirectoryName(path);
            string modName = Path.GetFileNameWithoutExtension(path);
            string zipPath = Path.Combine(modDirectory, modName + ".zip");

            ZipFile.CreateFromDirectory(path, zipPath);

            modFile.data = File.ReadAllBytes(zipPath);
            modFile.hash = Hasher.GetHash(modFile.data);

            //TODO
            //READD THIS WHEN DONE WITH MOD SYNC
            //File.Delete(path);

            File.Delete(zipPath);

            return modFile;
        }

        private static void ConvertMod(string path, ModFile modFile)
        {
            string modDirectory = Path.GetDirectoryName(path);
            string modName = Path.GetFileNameWithoutExtension(path);
            string finalPath = Path.Combine(modDirectory, modName + ".mpmod");

            Serializer.SerializeToFile(finalPath, modFile);

            Logger.WriteToConsole($"Converted mod '{modFile.name}'");
        }

        public static bool CheckIfModConflict(ServerClient client, JoinDetailsJSON loginDetailsJSON)
        {
            //TODO
            //REDO ALL OF THIS WITH NEW SYSTEM
            return false;

            //List<string> conflictingMods = new List<string>();

            //if (requiredMods.Count() > 0)
            //{
            //    foreach (ModFile modFile in requiredMods)
            //    {
            //        if (!loginDetailsJSON.runningMods.Contains(modFile.packageID))
            //        {
            //            conflictingMods.Add($"[Required] > {mod}");
            //            continue;
            //        }
            //    }

            //    foreach (string mod in loginDetailsJSON.runningMods)
            //    {
            //        if (!requiredMods.Contains(mod) && !optionalMods.Contains(mod))
            //        {
            //            conflictingMods.Add($"[Disallowed] > {mod}");
            //            continue;
            //        }
            //    }
            //}

            //if (forbiddenMods.Count() > 0)
            //{
            //    foreach (ModFile modFile in forbiddenMods)
            //    {
            //        if (loginDetailsJSON.runningMods.Contains(mod))
            //        {
            //            conflictingMods.Add($"[Forbidden] > {mod}");
            //        }
            //    }
            //}

            //if (conflictingMods.Count == 0)
            //{
            //    client.runningMods = loginDetailsJSON.runningMods;
            //    return false;
            //}

            //else
            //{
            //    if(client.isAdmin)
            //    {
            //        Logger.WriteToConsole($"[Mod bypass] > {client.username}", Logger.LogMode.Warning);
            //        client.runningMods = loginDetailsJSON.runningMods;
            //        return false;
            //    }

            //    else
            //    {
            //        UserManager_Joinings.SendLoginResponse(client, CommonEnumerators.LoginResponse.WrongMods, conflictingMods);
            //        return true;
            //    }
            //}
        }
    }
}
