using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RimworldTogether.GameServer.Core;
using RimworldTogether.GameServer.Managers;
using RimworldTogether.GameServer.Managers.Actions;
using RimworldTogether.GameServer.Misc;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Network
{
    public class Network
    {
        private TcpListener server;
        private IPAddress localAddress = IPAddress.Parse(Program.serverConfig.IP);
        private int port = int.Parse(Program.serverConfig.Port);

        public bool isServerOpen;
        public bool usingNewNetworking;
        private readonly ILogger<Network> logger;
        private readonly ClientManager clientManager;
        private readonly SiteManager siteManager;
        private readonly UserManager userManager;
        private readonly PacketHandler packetHandler;
        private readonly UserManager_Joinings userManager_Joinings;
        private readonly ResponseShortcutManager responseShortcutManager;

        public Network(ILogger<Network> logger,
            ClientManager clientManager,
            SiteManager siteManager,
            UserManager userManager,
            PacketHandler packetHandler,
            UserManager_Joinings userManager_Joinings,
            ResponseShortcutManager responseShortcutManager)
        {
            this.logger = logger;
            this.clientManager = clientManager;
            this.siteManager = siteManager;
            this.userManager = userManager;
            this.packetHandler = packetHandler;
            this.userManager_Joinings = userManager_Joinings;
            this.responseShortcutManager = responseShortcutManager;
        }

        public async Task ReadyServer(CancellationToken cancellationToken = default)
        {
            server = new TcpListener(localAddress, port);
            server.Start();
            isServerOpen = true;

            _ = HeartbeatClients(cancellationToken);
            _ = siteManager.StartSiteTicker(cancellationToken);

            logger.LogInformation($"Listening for users at {localAddress}:{port}");
            logger.LogInformation("Server launched");

            Titler.ChangeTitle(clientManager.ClientCount, int.Parse(Program.serverConfig.MaxPlayers));

            while (!cancellationToken.IsCancellationRequested)
            {
                await ListenForIncomingUsers(cancellationToken);
            }
        }

        private async Task ListenForIncomingUsers(CancellationToken cancellationToken = default)
        {
            var tcpClient = await server.AcceptTcpClientAsync(cancellationToken);

            Client newServerClient = new Client(tcpClient);

            if (Program.isClosing) newServerClient.disconnectFlag = true;
            else
            {
                if (clientManager.Clients.ToArray().Count() >= int.Parse(Program.serverConfig.MaxPlayers))
                {
                    userManager_Joinings.SendLoginResponse(newServerClient, UserManager_Joinings.LoginResponse.ServerFull);
                    logger.LogWarning($"[Warning] > Server Full");
                }

                else
                {
                    clientManager.AddClient(newServerClient);
                    Titler.ChangeTitle(clientManager.ClientCount, int.Parse(Program.serverConfig.MaxPlayers));

                    newServerClient.DataTask = ListenToClient(newServerClient, cancellationToken);
                    logger.LogInformation($"[Connect] > {newServerClient.username} | {newServerClient.SavedIP}");
                }
            }
        }

        private async Task ListenToClient(Client client, CancellationToken cancellationToken = default)
        {
            try
            {
                while (!client.disconnectFlag)
                {
                    string data = client.streamReader.ReadLine();
                    Packet receivedPacket = Serializer.SerializeToPacket(data);

                    try { packetHandler.HandlePacket(client, receivedPacket); }
                    catch { responseShortcutManager.SendIllegalPacket(client, true); }
                }
            }
            catch { client.disconnectFlag = true; }
        }

        public void KickClient(Client client)
        {
            try
            {
                clientManager.RemoveClient(client);
                client.tcp.Dispose();

                userManager.SendPlayerRecount();

                Titler.ChangeTitle(clientManager.ClientCount, int.Parse(Program.serverConfig.MaxPlayers));

                logger.LogInformation($"[Disconnect] > {client.username} | {client.SavedIP}");
            }

            catch
            {
                logger.LogWarning($"Error disconnecting user {client.username}, this will cause memory overhead");
            }
        }

        private async Task HeartbeatClients(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);

                Client[] actualClients = clientManager.Clients.ToArray();
                foreach (Client client in actualClients)
                {
                    try
                    {
                        if (client.disconnectFlag || !CheckIfConnected(client))
                        {
                            KickClient(client);
                        }
                    }
                    catch { KickClient(client); }
                }
            }
        }

        private bool CheckIfConnected(Client client)
        {
            if (!client.tcp.Connected) return false;
            else
            {
                if (client.tcp.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buff = new byte[1];
                    if (client.tcp.Client.Receive(buff, SocketFlags.Peek) == 0) return false;
                    else return true;
                }

                else return true;
            }
        }
    }
}