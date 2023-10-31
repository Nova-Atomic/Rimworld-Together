using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON.Actions;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Managers.Actions
{
    public class SpyManager
    {
        private readonly UserManager userManager;

        private enum SpyStepMode { Request, Deny }

        public SpyManager(UserManager userManager)
        {
            this.userManager = userManager;
        }

        public void ParseSpyPacket(Client client, Packet packet)
        {
            SpyDetailsJSON spyDetailsJSON = Serializer.SerializeFromString<SpyDetailsJSON>(packet.contents[0]);

            switch (int.Parse(spyDetailsJSON.spyStepMode))
            {
                case (int)SpyStepMode.Request:
                    SendRequestedMap(client, spyDetailsJSON);
                    break;

                case (int)SpyStepMode.Deny:
                    //Nothing goes here
                    break;
            }
        }

        private void SendRequestedMap(Client client, SpyDetailsJSON spyDetailsJSON)
        {
            if (!SaveManager.CheckIfMapExists(spyDetailsJSON.spyData))
            {
                spyDetailsJSON.spyStepMode = ((int)SpyStepMode.Deny).ToString();
                string[] contents = new string[] { Serializer.SerializeToString(spyDetailsJSON) };
                Packet packet = new Packet("SpyPacket", contents);
                client.SendData(packet);
            }

            else
            {
                SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(spyDetailsJSON.spyData);

                if (userManager.CheckIfUserIsConnected(settlementFile.owner))
                {
                    spyDetailsJSON.spyStepMode = ((int)SpyStepMode.Deny).ToString();
                    string[] contents = new string[] { Serializer.SerializeToString(spyDetailsJSON) };
                    Packet packet = new Packet("SpyPacket", contents);
                    client.SendData(packet);
                }

                else
                {
                    MapFile mapFile = SaveManager.GetUserMapFromTile(spyDetailsJSON.spyData);
                    spyDetailsJSON.spyData = Serializer.SerializeToString(mapFile);

                    string[] contents = new string[] { Serializer.SerializeToString(spyDetailsJSON) };
                    Packet packet = new Packet("SpyPacket", contents);
                    client.SendData(packet);
                }
            }
        }
    }
}
