using Microsoft.Extensions.Logging;
using RimworldTogether.GameServer.Core;
using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Managers.Actions
{
    public class FactionManager
    {
        private readonly ILogger<FactionManager> logger;
        private readonly ClientManager clientManager;
        private readonly LikelihoodManager likelihoodManager;
        private readonly ResponseShortcutManager responseShortcutManager;
        private readonly SiteManager siteManager;
        private readonly UserManager userManager;

        public enum FactionManifestMode
        {
            Create,
            Delete,
            NameInUse,
            NoPower,
            AddMember,
            RemoveMember,
            AcceptInvite,
            Promote,
            Demote,
            AdminProtection,
            MemberList
        }

        public enum FactionRanks { Member, Moderator, Admin }

        public FactionManager(
            ILogger<FactionManager> logger,
            ClientManager clientManager,
            LikelihoodManager likelihoodManager,
            ResponseShortcutManager responseShortcutManager,
            SiteManager siteManager,
            UserManager userManager)
        {
            this.logger = logger;
            this.clientManager = clientManager;
            this.likelihoodManager = likelihoodManager;
            this.responseShortcutManager = responseShortcutManager;
            this.siteManager = siteManager;
            this.userManager = userManager;
        }

        public void ParseFactionPacket(Client client, Packet packet)
        {
            FactionManifestJSON factionManifest = Serializer.SerializeFromString<FactionManifestJSON>(packet.contents[0]);

            switch (int.Parse(factionManifest.manifestMode))
            {
                case (int)FactionManifestMode.Create:
                    CreateFaction(client, factionManifest);
                    break;

                case (int)FactionManifestMode.Delete:
                    DeleteFaction(client, factionManifest);
                    break;

                case (int)FactionManifestMode.AddMember:
                    AddMemberToFaction(client, factionManifest);
                    break;

                case (int)FactionManifestMode.RemoveMember:
                    RemoveMemberFromFaction(client, factionManifest);
                    break;

                case (int)FactionManifestMode.AcceptInvite:
                    ConfirmAddMemberToFaction(client, factionManifest);
                    break;

                case (int)FactionManifestMode.Promote:
                    PromoteMember(client, factionManifest);
                    break;

                case (int)FactionManifestMode.Demote:
                    DemoteMember(client, factionManifest);
                    break;

                case (int)FactionManifestMode.MemberList:
                    SendFactionMemberList(client, factionManifest);
                    break;
            }
        }

        private static FactionFile[] GetAllFactions()
        {
            List<FactionFile> factionFiles = new List<FactionFile>();

            string[] factions = Directory.GetFiles(Program.factionsPath);
            foreach (string faction in factions)
            {
                factionFiles.Add(Serializer.SerializeFromFile<FactionFile>(faction));
            }

            return factionFiles.ToArray();
        }

        public static FactionFile GetFactionFromClient(Client client)
        {
            string[] factions = Directory.GetFiles(Program.factionsPath);
            foreach (string faction in factions)
            {
                FactionFile factionFile = Serializer.SerializeFromFile<FactionFile>(faction);
                if (factionFile.factionName == client.factionName) return factionFile;
            }

            return null;
        }

        public static FactionFile GetFactionFromFactionName(string factionName)
        {
            string[] factions = Directory.GetFiles(Program.factionsPath);
            foreach (string faction in factions)
            {
                FactionFile factionFile = Serializer.SerializeFromFile<FactionFile>(faction);
                if (factionFile.factionName == factionName) return factionFile;
            }

            return null;
        }

        public static bool CheckIfUserIsInFaction(FactionFile factionFile, string usernameToCheck)
        {
            foreach (string str in factionFile.factionMembers)
            {
                if (str == usernameToCheck) return true;
            }

            return false;
        }

        public static FactionRanks GetMemberRank(FactionFile factionFile, string usernameToCheck)
        {
            for (int i = 0; i < factionFile.factionMembers.Count(); i++)
            {
                if (factionFile.factionMembers[i] == usernameToCheck)
                {
                    return (FactionRanks)int.Parse(factionFile.factionMemberRanks[i]);
                }
            }

            return FactionRanks.Member;
        }

        public static void SaveFactionFile(FactionFile factionFile)
        {
            string savePath = Path.Combine(Program.factionsPath, factionFile.factionName + ".json");
            Serializer.SerializeToFile(savePath, factionFile);
        }

        private static bool CheckIfFactionExistsByName(string nameToCheck)
        {
            FactionFile[] factions = GetAllFactions();
            foreach (FactionFile faction in factions)
            {
                if (faction.factionName == nameToCheck) return true;
            }

            return false;
        }

        private void CreateFaction(Client client, FactionManifestJSON factionManifest)
        {
            if (CheckIfFactionExistsByName(factionManifest.manifestDetails))
            {
                factionManifest.manifestMode = ((int)FactionManifestMode.NameInUse).ToString();

                string[] contents = new string[] { Serializer.SerializeToString(factionManifest) };
                Packet packet = new Packet("FactionPacket", contents);
                client.SendData(packet);
            }

            else
            {
                factionManifest.manifestMode = ((int)FactionManifestMode.Create).ToString();

                FactionFile factionFile = new FactionFile();
                factionFile.factionName = factionManifest.manifestDetails;
                factionFile.factionMembers.Add(client.username);
                factionFile.factionMemberRanks.Add(((int)FactionRanks.Admin).ToString());
                SaveFactionFile(factionFile);

                client.hasFaction = true;
                client.factionName = factionFile.factionName;

                UserFile userFile = UserManager.GetUserFile(client);
                userFile.hasFaction = true;
                userFile.factionName = factionFile.factionName;
                UserManager.SaveUserFile(client, userFile);

                string[] contents = new string[] { Serializer.SerializeToString(factionManifest) };
                Packet packet = new Packet("FactionPacket", contents);
                client.SendData(packet);

                logger.LogWarning($"[Created faction] > {client.username} > {factionFile.factionName}");
            }
        }

        private void DeleteFaction(Client client, FactionManifestJSON factionManifest)
        {
            if (!CheckIfFactionExistsByName(client.factionName)) return;
            else
            {
                FactionFile factionFile = GetFactionFromClient(client);

                if (GetMemberRank(factionFile, client.username) != FactionRanks.Admin)
                {
                    responseShortcutManager.SendNoPowerPacket(client, factionManifest);
                }

                else
                {
                    factionManifest.manifestMode = ((int)FactionManifestMode.Delete).ToString();

                    UserFile[] userFiles = UserManager.GetAllUserFiles();
                    foreach (UserFile userFile in userFiles)
                    {
                        if (userFile.factionName == client.factionName)
                        {
                            userFile.hasFaction = false;
                            userFile.factionName = "";

                            UserManager.SaveUserFileFromName(userFile.username, userFile);
                        }
                    }

                    string[] contents = new string[] { Serializer.SerializeToString(factionManifest) };
                    Packet packet = new Packet("FactionPacket", contents);
                    foreach (string str in factionFile.factionMembers)
                    {
                        Client cClient = clientManager.Clients.ToList().Find(x => x.username == str);
                        if (cClient != null)
                        {
                            cClient.hasFaction = false;
                            cClient.factionName = "";
                            cClient.SendData(packet);

                            likelihoodManager.UpdateClientLikelihoods(cClient);
                        }
                    }

                    SiteFile[] factionSites = GetFactionSites(factionFile);
                    foreach (SiteFile site in factionSites) siteManager.DestroySiteFromFile(site);

                    File.Delete(Path.Combine(Program.factionsPath, factionFile.factionName + ".json"));
                    logger.LogWarning($"[Deleted Faction] > {client.username} > {factionFile.factionName}");
                }
            }
        }

        private void AddMemberToFaction(Client client, FactionManifestJSON factionManifest)
        {
            FactionFile factionFile = GetFactionFromClient(client);
            SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(factionManifest.manifestDetails);
            Client toAdd = userManager.GetConnectedClientFromUsername(settlementFile.owner);

            if (factionFile == null) return;
            else
            {
                if (GetMemberRank(factionFile, client.username) == FactionRanks.Member)
                {
                    responseShortcutManager.SendNoPowerPacket(client, factionManifest);
                }

                else
                {
                    if (toAdd == null) return;
                    else
                    {
                        if (toAdd.hasFaction) return;
                        else
                        {
                            if (factionFile.factionMembers.Contains(toAdd.username)) return;
                            else
                            {
                                factionManifest.manifestDetails = factionFile.factionName;
                                string[] contents = new string[] { Serializer.SerializeToString(factionManifest) };
                                Packet packet = new Packet("FactionPacket", contents);
                                toAdd.SendData(packet);
                            }
                        }
                    }
                }
            }
        }

        private void ConfirmAddMemberToFaction(Client client, FactionManifestJSON factionManifest)
        {
            FactionFile factionFile = GetFactionFromFactionName(factionManifest.manifestDetails);

            if (factionFile == null) return;
            else
            {
                if (!factionFile.factionMembers.Contains(client.username))
                {
                    factionFile.factionMembers.Add(client.username);
                    factionFile.factionMemberRanks.Add(((int)FactionRanks.Member).ToString());
                    SaveFactionFile(factionFile);

                    client.hasFaction = true;
                    client.factionName = factionFile.factionName;

                    UserFile userFile = UserManager.GetUserFile(client);
                    userFile.hasFaction = true;
                    userFile.factionName = factionFile.factionName;
                    UserManager.SaveUserFile(client, userFile);

                    likelihoodManager.ClearAllFactionMemberLikelihoods(factionFile);

                    Client[] members = GetAllConnectedFactionMembers(factionFile);
                    foreach (Client member in members) likelihoodManager.UpdateClientLikelihoods(member);
                }
            }
        }

        private void RemoveMemberFromFaction(Client client, FactionManifestJSON factionManifest)
        {
            FactionFile factionFile = GetFactionFromClient(client);
            SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(factionManifest.manifestDetails);
            UserFile toRemoveLocal = UserManager.GetUserFileFromName(settlementFile.owner);
            Client toRemove = userManager.GetConnectedClientFromUsername(settlementFile.owner);

            if (GetMemberRank(factionFile, client.username) == FactionRanks.Member)
            {
                if (settlementFile.owner == client.username) RemoveFromFaction();
                else responseShortcutManager.SendNoPowerPacket(client, factionManifest);
            }

            else if (GetMemberRank(factionFile, client.username) == FactionRanks.Moderator)
            {
                if (settlementFile.owner == client.username) RemoveFromFaction();
                else
                {
                    if (GetMemberRank(factionFile, settlementFile.owner) != FactionRanks.Member)
                        responseShortcutManager.SendNoPowerPacket(client, factionManifest);

                    else RemoveFromFaction();
                }
            }

            else if (GetMemberRank(factionFile, client.username) == FactionRanks.Admin)
            {
                if (settlementFile.owner == client.username)
                {
                    factionManifest.manifestMode = ((int)FactionManifestMode.AdminProtection).ToString();
                    string[] contents = new string[] { Serializer.SerializeToString(factionManifest) };
                    Packet packet = new Packet("FactionPacket", contents);
                    client.SendData(packet);
                }
                else RemoveFromFaction();
            }

            void RemoveFromFaction()
            {
                if (!factionFile.factionMembers.Contains(settlementFile.owner)) return;
                else
                {
                    if (toRemove != null)
                    {
                        toRemove.hasFaction = false;
                        toRemove.factionName = "";

                        string[] contents = new string[] { Serializer.SerializeToString(factionManifest) };
                        Packet packet = new Packet("FactionPacket", contents);
                        toRemove.SendData(packet);

                        likelihoodManager.UpdateClientLikelihoods(toRemove);
                    }

                    if (toRemoveLocal == null) return;
                    else
                    {
                        toRemoveLocal.hasFaction = false;
                        toRemoveLocal.factionName = "";
                        UserManager.SaveUserFileFromName(toRemoveLocal.username, toRemoveLocal);

                        for (int i = 0; i < factionFile.factionMembers.Count(); i++)
                        {
                            if (factionFile.factionMembers[i] == toRemoveLocal.username)
                            {
                                factionFile.factionMembers.RemoveAt(i);
                                factionFile.factionMemberRanks.RemoveAt(i);
                                SaveFactionFile(factionFile);
                                break;
                            }
                        }
                    }

                    Client[] members = GetAllConnectedFactionMembers(factionFile);
                    foreach (Client member in members) likelihoodManager.UpdateClientLikelihoods(member);
                }
            }
        }

        private void PromoteMember(Client client, FactionManifestJSON factionManifest)
        {
            SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(factionManifest.manifestDetails);
            UserFile userFile = UserManager.GetUserFileFromName(settlementFile.owner);
            FactionFile factionFile = GetFactionFromClient(client);

            if (GetMemberRank(factionFile, client.username) == FactionRanks.Member)
            {
                responseShortcutManager.SendNoPowerPacket(client, factionManifest);
            }

            else
            {
                if (!factionFile.factionMembers.Contains(userFile.username)) return;
                else
                {
                    if (GetMemberRank(factionFile, settlementFile.owner) == FactionRanks.Admin)
                    {
                        responseShortcutManager.SendNoPowerPacket(client, factionManifest);
                    }

                    else
                    {
                        for (int i = 0; i < factionFile.factionMembers.Count(); i++)
                        {
                            if (factionFile.factionMembers[i] == userFile.username)
                            {
                                factionFile.factionMemberRanks[i] = "1";
                                SaveFactionFile(factionFile);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void DemoteMember(Client client, FactionManifestJSON factionManifest)
        {
            SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(factionManifest.manifestDetails);
            UserFile userFile = UserManager.GetUserFileFromName(settlementFile.owner);
            FactionFile factionFile = GetFactionFromClient(client);

            if (GetMemberRank(factionFile, client.username) != FactionRanks.Admin)
            {
                responseShortcutManager.SendNoPowerPacket(client, factionManifest);
            }

            else
            {
                if (!factionFile.factionMembers.Contains(userFile.username)) return;
                else
                {
                    for (int i = 0; i < factionFile.factionMembers.Count(); i++)
                    {
                        if (factionFile.factionMembers[i] == userFile.username)
                        {
                            factionFile.factionMemberRanks[i] = "0";
                            SaveFactionFile(factionFile);
                            break;
                        }
                    }
                }
            }
        }

        private static SiteFile[] GetFactionSites(FactionFile factionFile)
        {
            SiteFile[] sites = SiteManager.GetAllSites();
            List<SiteFile> factionSites = new List<SiteFile>();
            foreach (SiteFile site in sites)
            {
                if (site.isFromFaction && site.factionName == factionFile.factionName)
                {
                    factionSites.Add(site);
                }
            }

            return factionSites.ToArray();
        }

        private Client[] GetAllConnectedFactionMembers(FactionFile factionFile)
        {
            List<Client> connectedFactionMembers = new List<Client>();
            foreach (Client client in clientManager.Clients.ToArray())
            {
                if (factionFile.factionMembers.Contains(client.username))
                {
                    connectedFactionMembers.Add(client);
                }
            }

            return connectedFactionMembers.ToArray();
        }

        private void SendFactionMemberList(Client client, FactionManifestJSON factionManifest)
        {
            FactionFile factionFile = GetFactionFromClient(client);

            foreach (string str in factionFile.factionMembers)
            {
                factionManifest.manifestComplexDetails.Add(str);
                factionManifest.manifestSecondaryComplexDetails.Add(((int)GetMemberRank(factionFile, str)).ToString());
            }

            string[] contents = new string[] { Serializer.SerializeToString(factionManifest) };
            Packet packet = new Packet("FactionPacket", contents);
            client.SendData(packet);
        }
    }
}
