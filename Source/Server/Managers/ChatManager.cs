using Microsoft.Extensions.Logging;
using RimworldTogether.GameServer.Commands;
using RimworldTogether.GameServer.Managers.Actions;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON;
using RimworldTogether.Shared.JSON.Actions;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Managers
{
    public class ChatManager
    {
        public enum UserColor { Normal, Admin, Console }

        public enum MessageColor { Normal, Admin, Console }

        public static string[] defaultJoinMessages = new string[]
        {
        "Welcome to the global chat!", "Please be considerate with others and have fun!", "Use '/help' to check available commands"
        };
        private readonly ILogger<ChatManager> logger;
        private readonly ClientManager clientManager;
        private readonly VisitManager visitManager;

        public ChatManager(
            ILogger<ChatManager> logger,
            ClientManager clientManager,
            VisitManager visitManager)
        {
            this.logger = logger;
            this.clientManager = clientManager;
            this.visitManager = visitManager;
        }

        public void ParseClientMessages(Client client, Packet packet)
        {
            ChatMessagesJSON chatMessagesJSON = Serializer.SerializeFromString<ChatMessagesJSON>(packet.contents[0]);

            for (int i = 0; i < chatMessagesJSON.messages.Count(); i++)
            {
                if (chatMessagesJSON.messages[i].StartsWith("/")) ExecuteCommand(client, packet);
                else BroadcastClientMessages(client, packet);
            }
        }

        public void ExecuteCommand(Client client, Packet packet)
        {
            ChatMessagesJSON chatMessagesJSON = Serializer.SerializeFromString<ChatMessagesJSON>(packet.contents[0]);

            ChatCommand toFind = ChatCommandManager.chatCommands.ToList().Find(x => x.prefix == chatMessagesJSON.messages[0]);
            if (toFind == null) SendMessagesToClient(client, new string[] { "Command was not found" });
            else
            {
                toFind.commandAction.Invoke(this, client);
            }

            logger.LogInformation($"[Chat command] > {client.username} > {chatMessagesJSON.messages[0]}");
        }

        public void BroadcastClientMessages(Client client, Packet packet)
        {
            ChatMessagesJSON chatMessagesJSON = Serializer.SerializeFromString<ChatMessagesJSON>(packet.contents[0]);
            for (int i = 0; i < chatMessagesJSON.messages.Count(); i++)
            {
                if (client.isAdmin)
                {
                    chatMessagesJSON.userColors.Add(((int)MessageColor.Admin).ToString());
                    chatMessagesJSON.messageColors.Add(((int)MessageColor.Admin).ToString());
                }

                else
                {
                    chatMessagesJSON.userColors.Add(((int)MessageColor.Normal).ToString());
                    chatMessagesJSON.messageColors.Add(((int)MessageColor.Normal).ToString());
                }
            }

            string[] contents = new string[] { Serializer.SerializeToString(chatMessagesJSON) };
            Packet rPacket = new Packet("ChatPacket", contents);
            foreach (Client cClient in clientManager.Clients.ToArray()) cClient.SendData(rPacket);

            logger.LogInformation($"[Chat] > {client.username} > {chatMessagesJSON.messages[0]}");
        }

        public void BroadcastServerMessages(string messageToSend)
        {
            ChatMessagesJSON chatMessagesJSON = new ChatMessagesJSON();
            chatMessagesJSON.usernames.Add("CONSOLE");
            chatMessagesJSON.messages.Add(messageToSend);
            chatMessagesJSON.userColors.Add(((int)MessageColor.Console).ToString());
            chatMessagesJSON.messageColors.Add(((int)MessageColor.Console).ToString());

            string[] contents = new string[] { Serializer.SerializeToString(chatMessagesJSON) };
            Packet packet = new Packet("ChatPacket", contents);

            foreach (Client client in clientManager.Clients.ToArray())
            {
                client.SendData(packet);
            }

            logger.LogInformation($"[Chat] > {"CONSOLE"} > {"127.0.0.1"} > {chatMessagesJSON.messages[0]}");
        }

        public void SendMessagesToClient(Client client, string[] messagesToSend)
        {
            ChatMessagesJSON chatMessagesJSON = new ChatMessagesJSON();
            for (int i = 0; i < messagesToSend.Count(); i++)
            {
                chatMessagesJSON.usernames.Add("CONSOLE");
                chatMessagesJSON.messages.Add(messagesToSend[i]);
                chatMessagesJSON.userColors.Add(((int)MessageColor.Console).ToString());
                chatMessagesJSON.messageColors.Add(((int)MessageColor.Console).ToString());
            }

            string[] contents = new string[] { Serializer.SerializeToString(chatMessagesJSON) };
            Packet packet = new Packet("ChatPacket", contents);
            client.SendData(packet);
        }


        public void ChatHelpCommandAction(Client invoker)
        {
            List<string> messagesToSend = new List<string>();
            messagesToSend.Add("List of available commands: ");
            foreach (ChatCommand command in ChatCommandManager.chatCommands) messagesToSend.Add($"{command.prefix} - {command.description}");
            SendMessagesToClient(invoker, messagesToSend.ToArray());
        }

        public void ChatPingCommandAction(Client invoker)
        {
            List<string> messagesToSend = new List<string>();
            messagesToSend.Add("Pong!");
            SendMessagesToClient(invoker, messagesToSend.ToArray());
        }

        public void ChatDisconnectCommandAction(Client invoker)
        {
            invoker.disconnectFlag = true;
        }

        public void ChatStopVisitCommandAction(Client invoker)
        {
            VisitDetailsJSON visitDetailsJSON = new VisitDetailsJSON();
            visitDetailsJSON.visitStepMode = ((int)VisitManager.VisitStepMode.Stop).ToString();

            visitManager.SendVisitStop(invoker, visitDetailsJSON);
        }
    }

    public class ChatCommandManager
    {
        public static Client invoker;

        private static ChatCommand helpCommand = new ChatCommand("/help", 0,
            "Shows a list of all available commands",
            (n, c) => n.ChatHelpCommandAction(c));

        private static ChatCommand pingCommand = new ChatCommand("/ping", 0,
            "Checks if the connection to the server is working",
            (n, c) => n.ChatPingCommandAction(c));

        private static ChatCommand disconnectCommand = new ChatCommand("/dc", 0,
            "Forcefully disconnects you from the server",
            (n, c) => n.ChatDisconnectCommandAction(c));

        private static ChatCommand stopVisitCommand = new ChatCommand("/sv", 0,
            "Forcefully disconnects you from a visit",
            (n, c) => n.ChatStopVisitCommandAction(c));

        public static ChatCommand[] chatCommands = new ChatCommand[]
        {
        helpCommand,
        pingCommand,
        disconnectCommand,
        stopVisitCommand
        };
    }
}
