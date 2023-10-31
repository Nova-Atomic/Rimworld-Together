using System.Diagnostics;
using RimworldTogether.GameServer.Managers;
using RimworldTogether.GameServer.Managers.Actions;
using RimworldTogether.GameServer.Users;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Network
{
    public class PacketHandler
    {
        private readonly SettlementManager settlementManager;
        private readonly EventManager eventManager;
        private readonly FactionManager factionManager;
        private readonly LikelihoodManager likelihoodManager;
        private readonly ChatManager chatManager;
        private readonly SpyManager spyManager;
        private readonly UserLogin userLogin;
        private readonly SaveManager saveManager;
        private readonly TransferManager transferManager;
        private readonly SiteManager siteManager;
        private readonly VisitManager visitManager;
        private readonly OfflineVisitManager offlineVisitManager;
        private readonly RaidManager raidManager;
        private readonly WorldManager worldManager;
        private readonly CustomDifficultyManager customDifficultyManager;
        private readonly UserRegister userRegister;

        public PacketHandler(
            SettlementManager settlementManager,
            EventManager eventManager,
            FactionManager factionManager,
            LikelihoodManager likelihoodManager,
            ChatManager chatManager,
            SpyManager spyManager,
            UserLogin userLogin,
            SaveManager saveManager,
            TransferManager transferManager,
            SiteManager siteManager,
            VisitManager visitManager,
            OfflineVisitManager offlineVisitManager,
            RaidManager raidManager,
            WorldManager worldManager,
            CustomDifficultyManager customDifficultyManager,
            UserRegister userRegister
            )
        {
            this.settlementManager = settlementManager;
            this.eventManager = eventManager;
            this.factionManager = factionManager;
            this.likelihoodManager = likelihoodManager;
            this.chatManager = chatManager;
            this.spyManager = spyManager;
            this.userLogin = userLogin;
            this.saveManager = saveManager;
            this.transferManager = transferManager;
            this.siteManager = siteManager;
            this.visitManager = visitManager;
            this.offlineVisitManager = offlineVisitManager;
            this.raidManager = raidManager;
            this.worldManager = worldManager;
            this.customDifficultyManager = customDifficultyManager;
            this.userRegister = userRegister;
        }

        public void HandlePacket(Client client, Packet packet)
        {
#if DEBUG
            Debug.WriteLine(packet.header);
#endif

            switch (packet.header)
            {
                case "LoginClientPacket":
                    userLogin.TryLoginUser(client, packet);
                    break;

                case "RegisterClientPacket":
                    userRegister.TryRegisterUser(client, packet);
                    break;

                case "SaveFilePacket":
                    saveManager.SaveUserGame(client, packet);
                    break;

                case "LikelihoodPacket":
                    likelihoodManager.ChangeUserLikelihoods(client, packet);
                    break;

                case "TransferPacket":
                    transferManager.ParseTransferPacket(client, packet);
                    break;

                case "SitePacket":
                    siteManager.ParseSitePacket(client, packet);
                    break;

                case "VisitPacket":
                    visitManager.ParseVisitPacket(client, packet);
                    break;

                case "OfflineVisitPacket":
                    offlineVisitManager.ParseOfflineVisitPacket(client, packet);
                    break;

                case "ChatPacket":
                    chatManager.ParseClientMessages(client, packet);
                    break;

                case "FactionPacket":
                    factionManager.ParseFactionPacket(client, packet);
                    break;

                case "MapPacket":
                    saveManager.SaveUserMap(client, packet);
                    break;

                case "RaidPacket":
                    raidManager.ParseRaidPacket(client, packet);
                    break;

                case "SpyPacket":
                    spyManager.ParseSpyPacket(client, packet);
                    break;

                case "SettlementPacket":
                    settlementManager.ParseSettlementPacket(client, packet);
                    break;

                case "EventPacket":
                    eventManager.ParseEventPacket(client, packet);
                    break;

                case "WorldPacket":
                    worldManager.ParseWorldPacket(client, packet);
                    break;

                case "CustomDifficultyPacket":
                    customDifficultyManager.ParseDifficultyPacket(client, packet);
                    break;

                case "ResetSavePacket":
                    saveManager.ResetClientSave(client);
                    break;
            }
        }
    }
}
