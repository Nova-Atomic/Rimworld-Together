using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON.Actions;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Managers.Actions
{
    public class OfflineVisitManager
    {
        private readonly UserManager userManager;

        private enum OfflineVisitStepMode { Request, Deny }

        public OfflineVisitManager(UserManager userManager)
        {
            this.userManager = userManager;
        }

        public void ParseOfflineVisitPacket(Client client, Packet packet)
        {
            OfflineVisitDetailsJSON offlineVisitDetails = Serializer.SerializeFromString<OfflineVisitDetailsJSON>(packet.contents[0]);

            switch (int.Parse(offlineVisitDetails.offlineVisitStepMode))
            {
                case (int)OfflineVisitStepMode.Request:
                    SendRequestedMap(client, offlineVisitDetails);
                    break;

                case (int)OfflineVisitStepMode.Deny:
                    //Nothing goes here
                    break;
            }
        }

        private void SendRequestedMap(Client client, OfflineVisitDetailsJSON offlineVisitDetails)
        {
            if (!SaveManager.CheckIfMapExists(offlineVisitDetails.offlineVisitData))
            {
                offlineVisitDetails.offlineVisitStepMode = ((int)OfflineVisitStepMode.Deny).ToString();
                string[] contents = new string[] { Serializer.SerializeToString(offlineVisitDetails) };
                Packet packet = new Packet("OfflineVisitPacket", contents);
                client.SendData(packet);
            }

            else
            {
                SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(offlineVisitDetails.offlineVisitData);

                if (userManager.CheckIfUserIsConnected(settlementFile.owner))
                {
                    offlineVisitDetails.offlineVisitStepMode = ((int)OfflineVisitStepMode.Deny).ToString();
                    string[] contents = new string[] { Serializer.SerializeToString(offlineVisitDetails) };
                    Packet packet = new Packet("OfflineVisitPacket", contents);
                    client.SendData(packet);
                }

                else
                {
                    MapFile mapFile = SaveManager.GetUserMapFromTile(offlineVisitDetails.offlineVisitData);
                    offlineVisitDetails.offlineVisitData = Serializer.SerializeToString(mapFile);

                    string[] contents = new string[] { Serializer.SerializeToString(offlineVisitDetails) };
                    Packet packet = new Packet("OfflineVisitPacket", contents);
                    client.SendData(packet);
                }
            }
        }
    }
}
