using Microsoft.Extensions.Logging;
using RimworldTogether.GameServer.Core;
using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Managers.Actions;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Managers
{
    public class SaveManager
    {
        private readonly ILogger<SaveManager> logger;
        private readonly SettlementManager settlementManager;
        private readonly CommandManager commandManager;
        private readonly ResponseShortcutManager responseShortcutManager;
        private readonly SiteManager siteManager;
        private readonly UserManager userManager;

        public enum SaveMode { Disconnect, Quit, Autosave, Transfer, Event }

        public enum MapMode { Save, Load }

        public SaveManager(
            ILogger<SaveManager> logger,
            SettlementManager settlementManager,
            CommandManager commandManager,
            ResponseShortcutManager responseShortcutManager,
            SiteManager siteManager,
            UserManager userManager)
        {
            this.logger = logger;
            this.settlementManager = settlementManager;
            this.commandManager = commandManager;
            this.responseShortcutManager = responseShortcutManager;
            this.siteManager = siteManager;
            this.userManager = userManager;
        }

        public static bool CheckIfUserHasSave(Client client)
        {
            string[] saves = Directory.GetFiles(Program.savesPath);
            foreach (string save in saves) if (Path.GetFileNameWithoutExtension(save) == client.username) return true;
            return false;
        }

        public static bool CheckIfMapExists(string mapTileToCheck)
        {
            string[] maps = Directory.GetFiles(Program.mapsPath);
            foreach (string str in maps)
            {
                MapFile mapFile = Serializer.SerializeFromFile<MapFile>(str);
                if (mapFile.mapTile == mapTileToCheck) return true;
            }

            return false;
        }

        public static MapFile[] GetAllMapFiles()
        {
            List<MapFile> mapFiles = new List<MapFile>();
            string[] maps = Directory.GetFiles(Program.mapsPath);
            foreach (string str in maps) mapFiles.Add(Serializer.SerializeFromFile<MapFile>(str));
            return mapFiles.ToArray();
        }

        public static byte[] GetUserSaveFromUsername(string username)
        {
            string[] saves = Directory.GetFiles(Program.savesPath);
            foreach (string save in saves)
            {
                if (Path.GetFileNameWithoutExtension(save) == username)
                {
                    return File.ReadAllBytes(save);
                }
            }

            return null;
        }

        public void SaveUserGame(Client client, Packet packet)
        {
            SaveFileJSON saveFileJSON = Serializer.SerializeFromString<SaveFileJSON>(packet.contents[0]);
            File.WriteAllBytes(Path.Combine(Program.savesPath, client.username + ".mpsave"), Convert.FromBase64String(saveFileJSON.saveData));

            if (saveFileJSON.saveMode == ((int)SaveMode.Disconnect).ToString())
            {
                commandManager.SendDisconnectCommand(client);
                client.disconnectFlag = true;

                logger.LogInformation($"[Save game] > {client.username} > To menu");
            }

            else if (saveFileJSON.saveMode == ((int)SaveMode.Quit).ToString())
            {
                commandManager.SendQuitCommand(client);

                client.disconnectFlag = true;

                logger.LogInformation($"[Save game] > {client.username} > Quiting");
            }

            else if (saveFileJSON.saveMode == ((int)SaveMode.Transfer).ToString())
            {
                logger.LogInformation($"[Save game] > {client.username} > Item transfer");
            }

            else logger.LogInformation($"[Save game] > {client.username} > Autosave");
        }

        public void LoadUserGame(Client client)
        {
            string[] contents = new string[] { Convert.ToBase64String(File.ReadAllBytes(Path.Combine(Program.savesPath, client.username + ".mpsave"))) };
            Packet packet = new Packet("LoadFilePacket", contents);
            client.SendData(packet);

            //if (network.usingNewNetworking) logger.LogInformation($"[Load game] > {client.username} {contents.GetHashCode()}");
            //else
            logger.LogInformation($"[Load game] > {client.username}");
        }

        public void SaveUserMap(Client client, Packet packet)
        {
            MapDetailsJSON mapDetailsJSON = Serializer.SerializeFromString<MapDetailsJSON>(packet.contents[0]);

            MapFile mapFile = new MapFile();
            mapFile.mapTile = mapDetailsJSON.mapTile;
            mapFile.mapOwner = client.username;
            mapFile.deflatedMapData = mapDetailsJSON.deflatedMapData;

            Serializer.SerializeToFile(Path.Combine(Program.mapsPath, mapFile.mapTile + ".json"), mapFile);
            logger.LogInformation($"[Save map] > {client.username} > {mapFile.mapTile}");
        }

        public void DeleteMap(MapFile mapFile)
        {
            if (mapFile == null) return;

            File.Delete(Path.Combine(Program.mapsPath, mapFile.mapTile + ".json"));

            logger.LogWarning($"[Remove map] > {mapFile.mapTile}");
        }

        public static MapFile[] GetAllMapsFromUsername(string username)
        {
            List<MapFile> userMaps = new List<MapFile>();

            SettlementFile[] userSettlements = SettlementManager.GetAllSettlementsFromUsername(username);
            foreach (SettlementFile settlementFile in userSettlements)
            {
                MapFile mapFile = GetUserMapFromTile(settlementFile.tile);
                userMaps.Add(mapFile);
            }

            return userMaps.ToArray();
        }

        public static MapFile GetUserMapFromTile(string mapTileToGet)
        {
            MapFile[] mapFiles = GetAllMapFiles();

            foreach (MapFile mapFile in mapFiles)
            {
                if (mapFile.mapTile == mapTileToGet) return mapFile;
            }

            return null;
        }

        public void ResetClientSave(Client client)
        {
            if (!CheckIfUserHasSave(client)) responseShortcutManager.SendIllegalPacket(client);
            else
            {
                client.disconnectFlag = true;

                string[] saves = Directory.GetFiles(Program.savesPath);

                string toDelete = saves.ToList().Find(x => Path.GetFileNameWithoutExtension(x) == client.username);
                if (!string.IsNullOrWhiteSpace(toDelete)) File.Delete(toDelete);

                logger.LogWarning($"[Delete save] > {client.username}");

                MapFile[] userMaps = GetAllMapsFromUsername(client.username);
                foreach (MapFile map in userMaps) DeleteMap(map);

                SiteFile[] playerSites = SiteManager.GetAllSitesFromUsername(client.username);
                foreach (SiteFile site in playerSites) siteManager.DestroySiteFromFile(site);

                SettlementFile[] playerSettlements = SettlementManager.GetAllSettlementsFromUsername(client.username);
                foreach (SettlementFile settlementFile in playerSettlements)
                {
                    SettlementDetailsJSON settlementDetailsJSON = new SettlementDetailsJSON();
                    settlementDetailsJSON.tile = settlementFile.tile;
                    settlementDetailsJSON.owner = settlementFile.owner;

                    settlementManager.RemoveSettlement(client, settlementDetailsJSON);
                }
            }
        }

        public void DeletePlayerDetails(string username)
        {
            Client connectedUser = userManager.GetConnectedClientFromUsername(username);
            if (connectedUser != null) connectedUser.disconnectFlag = true;

            string[] saves = Directory.GetFiles(Program.savesPath);
            string toDelete = saves.ToList().Find(x => Path.GetFileNameWithoutExtension(x) == username);
            if (!string.IsNullOrWhiteSpace(toDelete)) File.Delete(toDelete);

            MapFile[] userMaps = GetAllMapsFromUsername(username);
            foreach (MapFile map in userMaps) DeleteMap(map);

            SiteFile[] playerSites = SiteManager.GetAllSitesFromUsername(username);
            foreach (SiteFile site in playerSites) siteManager.DestroySiteFromFile(site);

            SettlementFile[] playerSettlements = SettlementManager.GetAllSettlementsFromUsername(username);
            foreach (SettlementFile settlementFile in playerSettlements)
            {
                SettlementDetailsJSON settlementDetailsJSON = new SettlementDetailsJSON();
                settlementDetailsJSON.tile = settlementFile.tile;
                settlementDetailsJSON.owner = settlementFile.owner;

                settlementManager.RemoveSettlement(null, settlementDetailsJSON, false);
            }

            logger.LogWarning($"[Deleted player details] > {username}");
        }
    }
}
