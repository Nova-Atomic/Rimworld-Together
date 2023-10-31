using RimworldTogether.GameServer.Managers;

namespace RimworldTogether.GameServer.Commands
{
    public class ServerCommand
    {
        public string prefix;

        public string description;

        public int parameters;

        public Action<ServerCommandManager> commandAction;

        public ServerCommand(string prefix, int parameters, string description, Action<ServerCommandManager> commandAction)
        {
            this.prefix = prefix;
            this.parameters = parameters;
            this.description = description;
            this.commandAction = commandAction;
        }
    }
}
