using RimworldTogether.GameServer.Core;

namespace RimworldTogether.GameServer.Misc
{
    public static class Titler
    {
        public static void ChangeTitle(int connectedCount, int maxPlayers)
        {
            Console.Title = $"Rimworld Together {Program.serverVersion} - " +
                $"Players [{connectedCount}/{maxPlayers}]";
        }
    }
}
