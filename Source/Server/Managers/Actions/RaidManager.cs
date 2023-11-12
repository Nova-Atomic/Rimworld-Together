﻿using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Misc;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON;
using RimworldTogether.Shared.JSON.Actions;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;
using RimworldTogether.Shared.Serializers;

namespace RimworldTogether.GameServer.Managers.Actions
{
    public static class RaidManager
    {
        private enum RaidStepMode { Request, Deny }

        public static void ParseRaidPacket(ServerClient client, Packet packet)
        {
            RaidDetailsJSON raidDetailsJSON = (RaidDetailsJSON)ObjectConverter.ConvertBytesToObject(packet.contents);

            switch (int.Parse(raidDetailsJSON.raidStepMode))
            {
                case (int)RaidStepMode.Request:
                    SendRequestedMap(client, raidDetailsJSON);
                    break;

                case (int)RaidStepMode.Deny:
                    //Do nothing
                    break;
            }
        }

        private static void SendRequestedMap(ServerClient client, RaidDetailsJSON raidDetailsJSON)
        {
            if (!SaveManager.CheckIfMapExists(raidDetailsJSON.targetTile))
            {
                raidDetailsJSON.raidStepMode = ((int)RaidStepMode.Deny).ToString();
                Packet packet = Packet.CreatePacketFromJSON("RaidPacket", raidDetailsJSON);
                client.clientListener.SendData(packet);
            }

            else
            {
                SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(raidDetailsJSON.targetTile);

                if (UserManager.CheckIfUserIsConnected(settlementFile.owner))
                {
                    raidDetailsJSON.raidStepMode = ((int)RaidStepMode.Deny).ToString();
                    Packet packet = Packet.CreatePacketFromJSON("RaidPacket", raidDetailsJSON);
                    client.clientListener.SendData(packet);
                }

                else
                {
                    MapDetailsJSON mapDetails = SaveManager.GetUserMapFromTile(raidDetailsJSON.targetTile);
                    raidDetailsJSON.mapDetails = mapDetails;

                    Packet packet = Packet.CreatePacketFromJSON("RaidPacket", raidDetailsJSON);
                    client.clientListener.SendData(packet);
                }
            }
        }
    }
}
