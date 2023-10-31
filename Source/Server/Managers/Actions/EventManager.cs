using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON.Actions;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Managers.Actions
{
    public class EventManager
    {
        private readonly ResponseShortcutManager responseShortcutManager;
        private readonly UserManager userManager;

        public enum EventStepMode { Send, Receive, Recover }

        public EventManager(
            ResponseShortcutManager responseShortcutManager,
            UserManager userManager)
        {
            this.responseShortcutManager = responseShortcutManager;
            this.userManager = userManager;
        }

        public void ParseEventPacket(Client client, Packet packet)
        {
            EventDetailsJSON eventDetailsJSON = Serializer.SerializeFromString<EventDetailsJSON>(packet.contents[0]);

            switch (int.Parse(eventDetailsJSON.eventStepMode))
            {
                case (int)EventStepMode.Send:
                    SendEvent(client, eventDetailsJSON);
                    break;

                case (int)EventStepMode.Receive:
                    //Nothing goes here
                    break;

                case (int)EventStepMode.Recover:
                    //Nothing goes here
                    break;
            }
        }

        public void SendEvent(Client client, EventDetailsJSON eventDetailsJSON)
        {
            if (!SettlementManager.CheckIfTileIsInUse(eventDetailsJSON.toTile)) responseShortcutManager.SendIllegalPacket(client);
            else
            {
                SettlementFile settlement = SettlementManager.GetSettlementFileFromTile(eventDetailsJSON.toTile);
                if (!userManager.CheckIfUserIsConnected(settlement.owner))
                {
                    eventDetailsJSON.eventStepMode = ((int)EventStepMode.Recover).ToString();
                    string[] contents = new string[] { Serializer.SerializeToString(eventDetailsJSON) };
                    Packet packet = new Packet("EventPacket", contents);
                    client.SendData(packet);
                }

                else
                {
                    Client target = userManager.GetConnectedClientFromUsername(settlement.owner);
                    if (target.inSafeZone)
                    {
                        eventDetailsJSON.eventStepMode = ((int)EventStepMode.Recover).ToString();
                        string[] contents = new string[] { Serializer.SerializeToString(eventDetailsJSON) };
                        Packet packet = new Packet("EventPacket", contents);
                        client.SendData(packet);
                    }

                    else
                    {
                        target.inSafeZone = true;

                        string[] contents = new string[] { Serializer.SerializeToString(eventDetailsJSON) };
                        Packet packet = new Packet("EventPacket", contents);
                        client.SendData(packet);

                        eventDetailsJSON.eventStepMode = ((int)EventStepMode.Receive).ToString();
                        contents = new string[] { Serializer.SerializeToString(eventDetailsJSON) };
                        Packet rPacket = new Packet("EventPacket", contents);
                        target.SendData(rPacket);
                    }
                }
            }
        }
    }
}
