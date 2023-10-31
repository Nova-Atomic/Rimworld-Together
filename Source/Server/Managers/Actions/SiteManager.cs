using Microsoft.Extensions.Logging;
using RimworldTogether.GameServer.Core;
using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Managers.Actions
{
    public class SiteManager
    {
        private readonly ILogger<SiteManager> logger;
        private readonly ClientManager clientManager;
        private readonly ResponseShortcutManager responseShortcutManager;

        public enum SiteStepMode { Accept, Build, Destroy, Info, Deposit, Retrieve, Reward }

        private enum PersonalSiteType { Farmland, Quarry, Sawmill, Storage }

        private enum FactionSiteType { Bank, Silo }

        public SiteManager(
            ILogger<SiteManager> logger,
            ClientManager clientManager,
            ResponseShortcutManager responseShortcutManager)
        {
            this.logger = logger;
            this.clientManager = clientManager;
            this.responseShortcutManager = responseShortcutManager;
        }

        public void ParseSitePacket(Client client, Packet packet)
        {
            SiteDetailsJSON siteDetailsJSON = Serializer.SerializeFromString<SiteDetailsJSON>(packet.contents[0]);

            switch (int.Parse(siteDetailsJSON.siteStep))
            {
                case (int)SiteStepMode.Build:
                    AddNewSite(client, siteDetailsJSON);
                    break;

                case (int)SiteStepMode.Destroy:
                    DestroySite(client, siteDetailsJSON);
                    break;

                case (int)SiteStepMode.Info:
                    GetSiteInfo(client, siteDetailsJSON);
                    break;

                case (int)SiteStepMode.Deposit:
                    DepositWorkerToSite(client, siteDetailsJSON);
                    break;

                case (int)SiteStepMode.Retrieve:
                    RetrieveWorkerFromSite(client, siteDetailsJSON);
                    break;
            }
        }

        public static bool CheckIfTileIsInUse(string tileToCheck)
        {
            string[] sites = Directory.GetFiles(Program.sitesPath);
            foreach (string site in sites)
            {
                SiteFile siteFile = Serializer.SerializeFromFile<SiteFile>(site);
                if (siteFile.tile == tileToCheck) return true;
            }

            return false;
        }

        public void ConfirmNewSite(Client client, SiteFile siteFile)
        {
            SaveSite(siteFile);

            SiteDetailsJSON siteDetailsJSON = new SiteDetailsJSON();
            siteDetailsJSON.siteStep = ((int)SiteStepMode.Build).ToString();
            siteDetailsJSON.tile = siteFile.tile;
            siteDetailsJSON.owner = client.username;
            siteDetailsJSON.type = siteFile.type;
            siteDetailsJSON.isFromFaction = siteFile.isFromFaction;

            foreach (Client cClient in clientManager.Clients.ToArray())
            {
                siteDetailsJSON.likelihood = LikelihoodManager.GetSiteLikelihood(cClient, siteFile).ToString();
                string[] contents = new string[] { Serializer.SerializeToString(siteDetailsJSON) };
                Packet packet = new Packet("SitePacket", contents);

                cClient.SendData(packet);
            }

            siteDetailsJSON.siteStep = ((int)SiteStepMode.Accept).ToString();
            string[] contents2 = new string[] { Serializer.SerializeToString(siteDetailsJSON) };
            Packet rPacket = new Packet("SitePacket", contents2);
            client.SendData(rPacket);

            logger.LogWarning($"[Created site] > {client.username}");
        }

        public static void SaveSite(SiteFile siteFile)
        {
            Serializer.SerializeToFile(Path.Combine(Program.sitesPath, siteFile.tile + ".json"), siteFile);
        }

        public static SiteFile[] GetAllSites()
        {
            List<SiteFile> sitesList = new List<SiteFile>();

            string[] sites = Directory.GetFiles(Program.sitesPath);
            foreach (string site in sites)
            {
                sitesList.Add(Serializer.SerializeFromFile<SiteFile>(site));
            }

            return sitesList.ToArray();
        }

        public static SiteFile[] GetAllSitesFromUsername(string username)
        {
            List<SiteFile> sitesList = new List<SiteFile>();

            string[] sites = Directory.GetFiles(Program.sitesPath);
            foreach (string site in sites)
            {
                SiteFile siteFile = Serializer.SerializeFromFile<SiteFile>(site);
                if (!siteFile.isFromFaction && siteFile.owner == username)
                {
                    sitesList.Add(siteFile);
                }
            }

            return sitesList.ToArray();
        }

        public static SiteFile GetSiteFileFromTile(string tileToGet)
        {
            string[] sites = Directory.GetFiles(Program.sitesPath);
            foreach (string site in sites)
            {
                SiteFile siteFile = Serializer.SerializeFromFile<SiteFile>(site);
                if (siteFile.tile == tileToGet) return siteFile;
            }

            return null;
        }

        private void AddNewSite(Client client, SiteDetailsJSON siteDetailsJSON)
        {
            if (SettlementManager.CheckIfTileIsInUse(siteDetailsJSON.tile)) responseShortcutManager.SendIllegalPacket(client);
            else if (CheckIfTileIsInUse(siteDetailsJSON.tile)) responseShortcutManager.SendIllegalPacket(client);
            else
            {
                SiteFile siteFile = null;

                if (siteDetailsJSON.isFromFaction)
                {
                    FactionFile factionFile = FactionManager.GetFactionFromClient(client);

                    if (FactionManager.GetMemberRank(factionFile, client.username) == FactionManager.FactionRanks.Member)
                    {
                        responseShortcutManager.SendNoPowerPacket(client, new FactionManifestJSON());
                        return;
                    }

                    else
                    {
                        siteFile = new SiteFile();
                        siteFile.tile = siteDetailsJSON.tile;
                        siteFile.owner = client.username;
                        siteFile.type = siteDetailsJSON.type;
                        siteFile.isFromFaction = true;
                        siteFile.factionName = client.factionName;
                    }
                }

                else
                {
                    siteFile = new SiteFile();
                    siteFile.tile = siteDetailsJSON.tile;
                    siteFile.owner = client.username;
                    siteFile.type = siteDetailsJSON.type;
                    siteFile.isFromFaction = false;
                }

                ConfirmNewSite(client, siteFile);
            }
        }

        private void DestroySite(Client client, SiteDetailsJSON siteDetailsJSON)
        {
            SiteFile siteFile = GetSiteFileFromTile(siteDetailsJSON.tile);

            if (siteFile.isFromFaction)
            {
                if (siteFile.factionName != client.factionName) responseShortcutManager.SendIllegalPacket(client);
                else
                {
                    FactionFile factionFile = FactionManager.GetFactionFromClient(client);

                    if (FactionManager.GetMemberRank(factionFile, client.username) !=
                        FactionManager.FactionRanks.Member) DestroySiteFromFile(siteFile);

                    else responseShortcutManager.SendNoPowerPacket(client, new FactionManifestJSON());
                }
            }

            else
            {
                if (siteFile.owner != client.username) responseShortcutManager.SendIllegalPacket(client);
                else DestroySiteFromFile(siteFile);
            }
        }

        public void DestroySiteFromFile(SiteFile siteFile)
        {
            SiteDetailsJSON siteDetailsJSON = new SiteDetailsJSON();
            siteDetailsJSON.siteStep = ((int)SiteStepMode.Destroy).ToString();
            siteDetailsJSON.tile = siteFile.tile;

            string[] contents = new string[] { Serializer.SerializeToString(siteDetailsJSON) };
            Packet packet = new Packet("SitePacket", contents);
            foreach (Client client in clientManager.Clients.ToArray()) client.SendData(packet);

            File.Delete(Path.Combine(Program.sitesPath, siteFile.tile + ".json"));
            logger.LogWarning($"[Destroyed site] > {siteFile.tile}");
        }

        private void GetSiteInfo(Client client, SiteDetailsJSON siteDetailsJSON)
        {
            SiteFile siteFile = GetSiteFileFromTile(siteDetailsJSON.tile);

            siteDetailsJSON.type = siteFile.type;
            siteDetailsJSON.workerData = siteFile.workerData;
            siteDetailsJSON.isFromFaction = siteFile.isFromFaction;

            string[] contents = new string[] { Serializer.SerializeToString(siteDetailsJSON) };
            Packet packet = new Packet("SitePacket", contents);
            client.SendData(packet);
        }

        private void DepositWorkerToSite(Client client, SiteDetailsJSON siteDetailsJSON)
        {
            SiteFile siteFile = GetSiteFileFromTile(siteDetailsJSON.tile);

            if (siteFile.owner != client.username &&
                FactionManager.GetFactionFromClient(client).factionMembers.Contains(siteFile.owner))
            {
                responseShortcutManager.SendIllegalPacket(client);
            }

            else
            {
                siteFile.workerData = siteDetailsJSON.workerData;
                SaveSite(siteFile);
            }
        }

        private void RetrieveWorkerFromSite(Client client, SiteDetailsJSON siteDetailsJSON)
        {
            SiteFile siteFile = GetSiteFileFromTile(siteDetailsJSON.tile);

            if (siteFile.owner != client.username &&
                FactionManager.GetFactionFromClient(client).factionMembers.Contains(siteFile.owner))
            {
                responseShortcutManager.SendIllegalPacket(client);
            }

            else
            {
                siteDetailsJSON.workerData = siteFile.workerData;
                siteFile.workerData = "";

                SaveSite(siteFile);

                string[] contents = new string[] { Serializer.SerializeToString(siteDetailsJSON) };
                Packet packet = new Packet("SitePacket", contents);
                client.SendData(packet);
            }
        }

        public async Task StartSiteTicker(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1800000, cancellationToken);
                SiteRewardTick();
            }
        }

        private void SiteRewardTick()
        {
            SiteFile[] sites = GetAllSites();

            SiteDetailsJSON siteDetailsJSON = new SiteDetailsJSON();
            siteDetailsJSON.siteStep = ((int)SiteStepMode.Reward).ToString();

            foreach (Client client in clientManager.Clients.ToArray())
            {
                siteDetailsJSON.sitesWithRewards.Clear();

                List<SiteFile> playerSites = sites.ToList().FindAll(x => x.owner == client.username);
                foreach (SiteFile site in playerSites)
                {
                    if (!string.IsNullOrWhiteSpace(site.workerData) && !site.isFromFaction)
                    {
                        siteDetailsJSON.sitesWithRewards.Add(site.tile);
                        continue;
                    }
                }

                if (client.hasFaction)
                {
                    List<SiteFile> factionSites = sites.ToList().FindAll(x => x.factionName == client.factionName);
                    foreach (SiteFile site in factionSites)
                    {
                        if (site.isFromFaction) siteDetailsJSON.sitesWithRewards.Add(site.tile);
                    }
                }

                if (siteDetailsJSON.sitesWithRewards.Count() > 0)
                {
                    string[] contents = new string[] { Serializer.SerializeToString(siteDetailsJSON) };
                    Packet packet = new Packet("SitePacket", contents);
                    client.SendData(packet);
                }
            }

            logger.LogInformation($"[Site tick]");
        }
    }
}
