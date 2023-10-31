using Microsoft.Extensions.Logging;
using RimworldTogether.GameServer.Managers.Actions;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Managers
{
    public class ResponseShortcutManager
    {
        private readonly ILogger<ResponseShortcutManager> logger;

        public ResponseShortcutManager(
            ILogger<ResponseShortcutManager> logger)
        {
            this.logger = logger;
        }

        public void SendIllegalPacket(Client client, bool broadcast = true)
        {
            Packet Packet = new Packet("IllegalActionPacket");
            client.SendData(Packet);
            client.disconnectFlag = true;

            if (broadcast) logger.LogError($"[Illegal action] > {client.username} > {client.SavedIP}");
        }

        public void SendUnavailablePacket(Client client)
        {
            Packet packet = new Packet("UserUnavailablePacket");
            client.SendData(packet);
        }

        public void SendBreakPacket(Client client)
        {
            Packet packet = new Packet("BreakPacket");
            client.SendData(packet);
        }

        public void SendNoPowerPacket(Client client, FactionManifestJSON factionManifest)
        {
            factionManifest.manifestMode = ((int)FactionManager.FactionManifestMode.NoPower).ToString();

            string[] contents = new string[] { Serializer.SerializeToString(factionManifest) };
            Packet packet = new Packet("FactionPacket", contents);
            client.SendData(packet);
        }
    }
}
