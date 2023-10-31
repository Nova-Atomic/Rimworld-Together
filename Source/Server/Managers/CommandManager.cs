using RimworldTogether.GameServer.Managers.Actions;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON;
using RimworldTogether.Shared.JSON.Actions;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Managers
{
    public class CommandManager
    {
        private readonly ClientManager clientManager;

        public enum CommandType { Op, Deop, Ban, Disconnect, Quit, Broadcast, ForceSave }

        public CommandManager(ClientManager clientManager)
        {
            this.clientManager = clientManager;
        }

        public void ParseCommand(Packet packet)
        {
            CommandDetailsJSON commandDetailsJSON = Serializer.SerializeFromString<CommandDetailsJSON>(packet.contents[0]);

            switch (int.Parse(commandDetailsJSON.commandType))
            {
                case (int)CommandType.Op:
                    //Do nothing
                    break;

                case (int)CommandType.Deop:
                    //Do nothing
                    break;

                case (int)CommandType.Ban:
                    //Do nothing
                    break;

                case (int)CommandType.Disconnect:
                    //Do nothing
                    break;

                case (int)CommandType.Quit:
                    //Do nothing
                    break;

                case (int)CommandType.Broadcast:
                    //Do nothing
                    break;
            }
        }

        public void SendOpCommand(Client client)
        {
            CommandDetailsJSON commandDetailsJSON = new CommandDetailsJSON();
            commandDetailsJSON.commandType = ((int)CommandType.Op).ToString();

            string[] contents = new string[] { Serializer.SerializeToString(commandDetailsJSON) };
            Packet packet = new Packet("CommandPacket", contents);
            client.SendData(packet);
        }

        public void SendDeOpCommand(Client client)
        {
            CommandDetailsJSON commandDetailsJSON = new CommandDetailsJSON();
            commandDetailsJSON.commandType = ((int)CommandType.Deop).ToString();

            string[] contents = new string[] { Serializer.SerializeToString(commandDetailsJSON) };
            Packet packet = new Packet("CommandPacket", contents);
            client.SendData(packet);

        }

        public void SendBanCommand(Client client)
        {
            CommandDetailsJSON commandDetailsJSON = new CommandDetailsJSON();
            commandDetailsJSON.commandType = ((int)CommandType.Ban).ToString();

            string[] contents = new string[] { Serializer.SerializeToString(commandDetailsJSON) };
            Packet packet = new Packet("CommandPacket", contents);
            client.SendData(packet);

        }

        public void SendDisconnectCommand(Client client)
        {
            CommandDetailsJSON commandDetailsJSON = new CommandDetailsJSON();
            commandDetailsJSON.commandType = ((int)CommandType.Disconnect).ToString();

            string[] contents = new string[] { Serializer.SerializeToString(commandDetailsJSON) };
            Packet packet = new Packet("CommandPacket", contents);
            client.SendData(packet);

        }

        public void SendQuitCommand(Client client)
        {
            CommandDetailsJSON commandDetailsJSON = new CommandDetailsJSON();
            commandDetailsJSON.commandType = ((int)CommandType.Quit).ToString();

            string[] contents = new string[] { Serializer.SerializeToString(commandDetailsJSON) };
            Packet packet = new Packet("CommandPacket", contents);
            client.SendData(packet);
        }

        public void SendEventCommand(Client client, int eventID)
        {
            EventDetailsJSON eventDetailsJSON = new EventDetailsJSON();
            eventDetailsJSON.eventStepMode = ((int)EventManager.EventStepMode.Receive).ToString();
            eventDetailsJSON.eventID = eventID.ToString();

            string[] contents = new string[] { Serializer.SerializeToString(eventDetailsJSON) };
            Packet packet = new Packet("EventPacket", contents);
            client.SendData(packet);
        }

        public void SendBroadcastCommand(string str)
        {
            CommandDetailsJSON commandDetailsJSON = new CommandDetailsJSON();
            commandDetailsJSON.commandType = ((int)CommandType.Broadcast).ToString();
            commandDetailsJSON.commandDetails = str;

            string[] contents = new string[] { Serializer.SerializeToString(commandDetailsJSON) };
            Packet packet = new Packet("CommandPacket", contents);
            foreach (Client client in clientManager.Clients.ToArray())
            {
                client.SendData(packet);
            }
        }

        public void SendForceSaveCommand(Client client)
        {
            CommandDetailsJSON commandDetailsJSON = new CommandDetailsJSON();
            commandDetailsJSON.commandType = ((int)CommandType.ForceSave).ToString();

            string[] contents = new string[] { Serializer.SerializeToString(commandDetailsJSON) };
            Packet packet = new Packet("CommandPacket", contents);
            client.SendData(packet);
        }
    }
}
