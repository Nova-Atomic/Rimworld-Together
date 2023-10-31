using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON.Actions;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Managers.Actions
{
    public class VisitManager
    {
        private readonly UserManager userManager;
        private readonly ResponseShortcutManager responseShortcutManager;

        public enum VisitStepMode { Request, Accept, Reject, Unavailable, Action, Stop }

        public VisitManager(UserManager userManager, ResponseShortcutManager responseShortcutManager)
        {
            this.userManager = userManager;
            this.responseShortcutManager = responseShortcutManager;
        }

        public void ParseVisitPacket(Client client, Packet packet)
        {
            VisitDetailsJSON visitDetailsJSON = Serializer.SerializeFromString<VisitDetailsJSON>(packet.contents[0]);

            switch (int.Parse(visitDetailsJSON.visitStepMode))
            {
                case (int)VisitStepMode.Request:
                    SendVisitRequest(client, visitDetailsJSON);
                    break;

                case (int)VisitStepMode.Accept:
                    AcceptVisitRequest(client, visitDetailsJSON);
                    break;

                case (int)VisitStepMode.Reject:
                    RejectVisitRequest(client, visitDetailsJSON);
                    break;

                case (int)VisitStepMode.Action:
                    SendVisitActions(client, visitDetailsJSON);
                    break;

                case (int)VisitStepMode.Stop:
                    SendVisitStop(client, visitDetailsJSON);
                    break;
            }
        }

        private void SendVisitRequest(Client client, VisitDetailsJSON visitDetailsJSON)
        {
            SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(visitDetailsJSON.targetTile);
            if (settlementFile == null) responseShortcutManager.SendIllegalPacket(client);
            else
            {
                Client toGet = userManager.GetConnectedClientFromUsername(settlementFile.owner);
                if (toGet == null)
                {
                    visitDetailsJSON.visitStepMode = ((int)VisitStepMode.Unavailable).ToString();
                    string[] contents = new string[] { Serializer.SerializeToString(visitDetailsJSON) };
                    Packet packet = new Packet("VisitPacket", contents);
                    client.SendData(packet);
                }

                else
                {
                    if (toGet.inVisitWith != null)
                    {
                        visitDetailsJSON.visitStepMode = ((int)VisitStepMode.Unavailable).ToString();
                        string[] contents = new string[] { Serializer.SerializeToString(visitDetailsJSON) };
                        Packet packet = new Packet("VisitPacket", contents);
                        client.SendData(packet);
                    }

                    else
                    {
                        visitDetailsJSON.visitorName = client.username;
                        string[] contents = new string[] { Serializer.SerializeToString(visitDetailsJSON) };
                        Packet packet = new Packet("VisitPacket", contents);
                        toGet.SendData(packet);
                    }
                }
            }
        }

        private void AcceptVisitRequest(Client client, VisitDetailsJSON visitDetailsJSON)
        {
            SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(visitDetailsJSON.fromTile);
            if (settlementFile == null) return;
            else
            {
                Client toGet = userManager.GetConnectedClientFromUsername(settlementFile.owner);
                if (toGet == null) return;
                else
                {
                    client.inVisitWith = toGet;
                    toGet.inVisitWith = client;

                    string[] contents = new string[] { Serializer.SerializeToString(visitDetailsJSON) };
                    Packet packet = new Packet("VisitPacket", contents);
                    toGet.SendData(packet);
                }
            }
        }

        private void RejectVisitRequest(Client client, VisitDetailsJSON visitDetailsJSON)
        {
            SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(visitDetailsJSON.fromTile);
            if (settlementFile == null) return;
            else
            {
                Client toGet = userManager.GetConnectedClientFromUsername(settlementFile.owner);
                if (toGet == null) return;
                else
                {
                    string[] contents = new string[] { Serializer.SerializeToString(visitDetailsJSON) };
                    Packet packet = new Packet("VisitPacket", contents);
                    toGet.SendData(packet);
                }
            }
        }

        private void SendVisitActions(Client client, VisitDetailsJSON visitDetailsJSON)
        {
            if (client.inVisitWith == null)
            {
                visitDetailsJSON.visitStepMode = ((int)VisitStepMode.Stop).ToString();
                string[] contents = new string[] { Serializer.SerializeToString(visitDetailsJSON) };
                Packet packet = new Packet("VisitPacket", contents);
                client.SendData(packet);
            }

            else
            {
                string[] contents = new string[] { Serializer.SerializeToString(visitDetailsJSON) };
                Packet packet = new Packet("VisitPacket", contents);
                client.inVisitWith.SendData(packet);
            }
        }

        public void SendVisitStop(Client client, VisitDetailsJSON visitDetailsJSON)
        {
            string[] contents = new string[] { Serializer.SerializeToString(visitDetailsJSON) };
            Packet packet = new Packet("VisitPacket", contents);

            if (client.inVisitWith == null) client.SendData(packet);
            else
            {
                client.SendData(packet);
                client.inVisitWith.SendData(packet);

                client.inVisitWith.inVisitWith = null;
                client.inVisitWith = null;
            }
        }
    }
}
