using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON.Actions;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Managers.Actions
{
    public class TransferManager
    {
        private readonly UserManager userManager;
        private readonly ResponseShortcutManager responseShortcutManager;

        public enum TransferMode { Gift, Trade, Rebound, Pod }

        public enum TransferStepMode { TradeRequest, TradeAccept, TradeReject, TradeReRequest, TradeReAccept, TradeReReject, Recover, Pod }

        public TransferManager(UserManager userManager, ResponseShortcutManager responseShortcutManager)
        {
            this.userManager = userManager;
            this.responseShortcutManager = responseShortcutManager;
        }

        public void ParseTransferPacket(Client client, Packet packet)
        {
            TransferManifestJSON transferManifestJSON = Serializer.SerializeFromString<TransferManifestJSON>(packet.contents[0]);

            switch (int.Parse(transferManifestJSON.transferStepMode))
            {
                case (int)TransferStepMode.TradeRequest:
                    TransferThings(client, transferManifestJSON);
                    break;

                case (int)TransferStepMode.TradeAccept:
                    //Nothing goes here
                    break;

                case (int)TransferStepMode.TradeReject:
                    RejectTransfer(client, packet);
                    break;

                case (int)TransferStepMode.TradeReRequest:
                    TransferThingsRebound(client, packet);
                    break;

                case (int)TransferStepMode.TradeReAccept:
                    AcceptReboundTransfer(client, packet);
                    break;

                case (int)TransferStepMode.TradeReReject:
                    RejectReboundTransfer(client, packet);
                    break;
            }
        }

        public void TransferThings(Client client, TransferManifestJSON transferManifestJSON)
        {
            if (!SettlementManager.CheckIfTileIsInUse(transferManifestJSON.toTile)) responseShortcutManager.SendIllegalPacket(client);
            else
            {
                SettlementFile settlement = SettlementManager.GetSettlementFileFromTile(transferManifestJSON.toTile);

                if (!userManager.CheckIfUserIsConnected(settlement.owner))
                {
                    if (int.Parse(transferManifestJSON.transferMode) == (int)TransferMode.Pod) responseShortcutManager.SendUnavailablePacket(client);
                    else
                    {
                        transferManifestJSON.transferStepMode = ((int)TransferStepMode.Recover).ToString();
                        string[] contents = new string[] { Serializer.SerializeToString(transferManifestJSON) };
                        Packet rPacket = new Packet("TransferPacket", contents);
                        client.SendData(rPacket);
                    }
                }

                else
                {
                    if (int.Parse(transferManifestJSON.transferMode) == (int)TransferMode.Gift)
                    {
                        transferManifestJSON.transferStepMode = ((int)TransferStepMode.TradeAccept).ToString();
                        string[] contents = new string[] { Serializer.SerializeToString(transferManifestJSON) };
                        Packet rPacket = new Packet("TransferPacket", contents);
                        client.SendData(rPacket);
                    }

                    else if (int.Parse(transferManifestJSON.transferMode) == (int)TransferMode.Pod)
                    {
                        transferManifestJSON.transferStepMode = ((int)TransferStepMode.TradeAccept).ToString();
                        string[] contents = new string[] { Serializer.SerializeToString(transferManifestJSON) };
                        Packet rPacket = new Packet("TransferPacket", contents);
                        client.SendData(rPacket);
                    }

                    transferManifestJSON.transferStepMode = ((int)TransferStepMode.TradeRequest).ToString();
                    string[] contents2 = new string[] { Serializer.SerializeToString(transferManifestJSON) };
                    Packet rPacket2 = new Packet("TransferPacket", contents2);
                    userManager.GetConnectedClientFromUsername(settlement.owner).SendData(rPacket2);
                }
            }
        }

        public void RejectTransfer(Client client, Packet packet)
        {
            TransferManifestJSON transferManifestJSON = Serializer.SerializeFromString<TransferManifestJSON>(packet.contents[0]);

            SettlementFile settlement = SettlementManager.GetSettlementFileFromTile(transferManifestJSON.fromTile);
            if (!userManager.CheckIfUserIsConnected(settlement.owner))
            {
                transferManifestJSON.transferStepMode = ((int)TransferStepMode.Recover).ToString();
                string[] contents = new string[] { Serializer.SerializeToString(transferManifestJSON) };
                Packet rPacket = new Packet("TransferPacket", contents);
                client.SendData(rPacket);
            }

            else
            {
                transferManifestJSON.transferStepMode = ((int)TransferStepMode.TradeReject).ToString();
                string[] contents = new string[] { Serializer.SerializeToString(transferManifestJSON) };
                Packet rPacket = new Packet("TransferPacket", contents);
                userManager.GetConnectedClientFromUsername(settlement.owner).SendData(rPacket);
            }
        }

        public void TransferThingsRebound(Client client, Packet packet)
        {
            TransferManifestJSON transferManifestJSON = Serializer.SerializeFromString<TransferManifestJSON>(packet.contents[0]);

            SettlementFile settlement = SettlementManager.GetSettlementFileFromTile(transferManifestJSON.toTile);
            if (!userManager.CheckIfUserIsConnected(settlement.owner))
            {
                transferManifestJSON.transferStepMode = ((int)TransferStepMode.TradeReReject).ToString();
                string[] contents = new string[] { Serializer.SerializeToString(transferManifestJSON) };
                Packet rPacket = new Packet("TransferPacket", contents);
                client.SendData(rPacket);
            }

            else
            {
                transferManifestJSON.transferStepMode = ((int)TransferStepMode.TradeReRequest).ToString();
                string[] contents = new string[] { Serializer.SerializeToString(transferManifestJSON) };
                Packet rPacket = new Packet("TransferPacket", contents);
                userManager.GetConnectedClientFromUsername(settlement.owner).SendData(rPacket);
            }
        }

        public void AcceptReboundTransfer(Client client, Packet packet)
        {
            TransferManifestJSON transferManifestJSON = Serializer.SerializeFromString<TransferManifestJSON>(packet.contents[0]);

            SettlementFile settlement = SettlementManager.GetSettlementFileFromTile(transferManifestJSON.fromTile);
            if (!userManager.CheckIfUserIsConnected(settlement.owner))
            {
                transferManifestJSON.transferStepMode = ((int)TransferStepMode.Recover).ToString();
                string[] contents = new string[] { Serializer.SerializeToString(transferManifestJSON) };
                Packet rPacket = new Packet("TransferPacket", contents);
                client.SendData(rPacket);
            }

            else
            {
                transferManifestJSON.transferStepMode = ((int)TransferStepMode.TradeReAccept).ToString();
                string[] contents = new string[] { Serializer.SerializeToString(transferManifestJSON) };
                Packet rPacket = new Packet("TransferPacket", contents);
                userManager.GetConnectedClientFromUsername(settlement.owner).SendData(rPacket);
            }
        }

        public void RejectReboundTransfer(Client client, Packet packet)
        {
            TransferManifestJSON transferManifestJSON = Serializer.SerializeFromString<TransferManifestJSON>(packet.contents[0]);

            SettlementFile settlement = SettlementManager.GetSettlementFileFromTile(transferManifestJSON.fromTile);
            if (!userManager.CheckIfUserIsConnected(settlement.owner))
            {
                transferManifestJSON.transferStepMode = ((int)TransferStepMode.Recover).ToString();
                string[] contents = new string[] { Serializer.SerializeToString(transferManifestJSON) };
                Packet rPacket = new Packet("TransferPacket", contents);
                client.SendData(rPacket);
            }

            else
            {
                transferManifestJSON.transferStepMode = ((int)TransferStepMode.TradeReReject).ToString();
                string[] contents = new string[] { Serializer.SerializeToString(transferManifestJSON) };
                Packet rPacket = new Packet("TransferPacket", contents);
                userManager.GetConnectedClientFromUsername(settlement.owner).SendData(rPacket);
            }
        }
    }
}
