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
    public class SettlementManager
    {
        private readonly ILogger<SettlementManager> logger;
        private readonly ClientManager clientManager;
        private readonly ResponseShortcutManager responseShortcutManager;

        public enum SettlementStepMode { Add, Remove }

        public SettlementManager(
            ILogger<SettlementManager> logger,
            ClientManager clientManager,
            ResponseShortcutManager responseShortcutManager)
        {
            this.logger = logger;
            this.clientManager = clientManager;
            this.responseShortcutManager = responseShortcutManager;
        }

        public void ParseSettlementPacket(Client client, Packet packet)
        {
            SettlementDetailsJSON settlementDetailsJSON = Serializer.SerializeFromString<SettlementDetailsJSON>(packet.contents[0]);

            switch (int.Parse(settlementDetailsJSON.settlementStepMode))
            {
                case (int)SettlementStepMode.Add:
                    AddSettlement(client, settlementDetailsJSON);
                    break;

                case (int)SettlementStepMode.Remove:
                    RemoveSettlement(client, settlementDetailsJSON);
                    break;
            }
        }

        public static bool CheckIfTileIsInUse(string tileToCheck)
        {
            string[] settlements = Directory.GetFiles(Program.settlementsPath);
            foreach (string settlement in settlements)
            {
                SettlementFile settlementJSON = Serializer.SerializeFromFile<SettlementFile>(settlement);
                if (settlementJSON.tile == tileToCheck) return true;
            }

            return false;
        }

        public static SettlementFile GetSettlementFileFromTile(string tileToGet)
        {
            string[] settlements = Directory.GetFiles(Program.settlementsPath);
            foreach (string settlement in settlements)
            {
                SettlementFile settlementFile = Serializer.SerializeFromFile<SettlementFile>(settlement);
                if (settlementFile.tile == tileToGet) return settlementFile;
            }

            return null;
        }

        public static SettlementFile GetSettlementFileFromUsername(string usernameToGet)
        {
            string[] settlements = Directory.GetFiles(Program.settlementsPath);
            foreach (string settlement in settlements)
            {
                SettlementFile settlementFile = Serializer.SerializeFromFile<SettlementFile>(settlement);
                if (settlementFile.owner == usernameToGet) return settlementFile;
            }

            return null;
        }

        public static SettlementFile[] GetAllSettlements()
        {
            List<SettlementFile> settlementList = new List<SettlementFile>();

            string[] settlements = Directory.GetFiles(Program.settlementsPath);
            foreach (string settlement in settlements)
            {
                settlementList.Add(Serializer.SerializeFromFile<SettlementFile>(settlement));
            }

            return settlementList.ToArray();
        }

        public static SettlementFile[] GetAllSettlementsFromUsername(string usernameToCheck)
        {
            List<SettlementFile> settlementList = new List<SettlementFile>();

            string[] settlements = Directory.GetFiles(Program.settlementsPath);
            foreach (string settlement in settlements)
            {
                SettlementFile settlementFile = Serializer.SerializeFromFile<SettlementFile>(settlement);
                if (settlementFile.owner == usernameToCheck) settlementList.Add(settlementFile);
            }

            return settlementList.ToArray();
        }

        public void AddSettlement(Client client, SettlementDetailsJSON settlementDetailsJSON)
        {
            if (CheckIfTileIsInUse(settlementDetailsJSON.tile)) responseShortcutManager.SendIllegalPacket(client);
            else
            {
                settlementDetailsJSON.owner = client.username;

                SettlementFile settlementFile = new SettlementFile();
                settlementFile.tile = settlementDetailsJSON.tile;
                settlementFile.owner = client.username;
                Serializer.SerializeToFile(Path.Combine(Program.settlementsPath, settlementFile.tile + ".json"), settlementFile);

                settlementDetailsJSON.settlementStepMode = ((int)SettlementStepMode.Add).ToString();
                foreach (Client cClient in clientManager.Clients.ToArray())
                {
                    if (cClient == client) continue;
                    else
                    {
                        settlementDetailsJSON.value = LikelihoodManager.GetSettlementLikelihood(cClient, settlementFile).ToString();

                        string[] contents = new string[] { Serializer.SerializeToString(settlementDetailsJSON) };
                        Packet rPacket = new Packet("SettlementPacket", contents);
                        cClient.SendData(rPacket);
                    }
                }

                logger.LogWarning($"[Added settlement] > {settlementFile.tile} > {client.username}");
            }
        }

        public void RemoveSettlement(Client client, SettlementDetailsJSON settlementDetailsJSON, bool sendRemoval = true)
        {
            if (!CheckIfTileIsInUse(settlementDetailsJSON.tile)) responseShortcutManager.SendIllegalPacket(client);

            SettlementFile settlementFile = GetSettlementFileFromTile(settlementDetailsJSON.tile);

            if (sendRemoval)
            {
                if (settlementFile.owner != client.username) responseShortcutManager.SendIllegalPacket(client);
                else
                {
                    File.Delete(Path.Combine(Program.settlementsPath, settlementFile.tile + ".json"));

                    SendSettlementRemoval(client, settlementDetailsJSON);
                }
            }

            else
            {
                File.Delete(Path.Combine(Program.settlementsPath, settlementFile.tile + ".json"));

                logger.LogWarning($"[Remove settlement] > {settlementFile.tile}");
            }
        }

        private void SendSettlementRemoval(Client client, SettlementDetailsJSON settlementDetailsJSON)
        {
            settlementDetailsJSON.settlementStepMode = ((int)SettlementStepMode.Remove).ToString();
            string[] contents = new string[] { Serializer.SerializeToString(settlementDetailsJSON) };
            Packet rPacket = new Packet("SettlementPacket", contents);
            foreach (Client cClient in clientManager.Clients.ToArray())
            {
                if (cClient == client) continue;
                else cClient.SendData(rPacket);
            }

            logger.LogWarning($"[Remove settlement] > {settlementDetailsJSON.tile} > {client.username}");
        }
    }
}
