using RimworldTogether.GameServer.Managers;
using RimworldTogether.GameServer.Network;

namespace RimworldTogether.GameServer.Commands
{
    public class ChatCommand
    {
        public string prefix;

        public string description;

        public int parameters;

        public Action<ChatManager, Client> commandAction;

        public ChatCommand(string prefix, int parameters, string description, Action<ChatManager, Client> commandAction)
        {
            this.prefix = prefix;
            this.parameters = parameters;
            this.description = description;
            this.commandAction = commandAction;
        }
    }
}
