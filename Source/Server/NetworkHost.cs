using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RimworldTogether.GameServer.Managers;

namespace RimworldTogether.GameServer
{
    internal sealed class NetworkHost : BackgroundService
    {
        private readonly ILogger<NetworkHost> logger;
        private readonly Network.Network network;
        private readonly ServerCommandManager serverCommandManager;

        public NetworkHost(
            ILogger<NetworkHost> logger,
            Network.Network network,
            ServerCommandManager serverCommandManager)
        {
            this.logger = logger;
            this.network = network;
            this.serverCommandManager = serverCommandManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Network Host Starting");

            var networkTask = network.ReadyServer(stoppingToken);

            // TODO - the console read async doesn't play nice with async as one would hope.
            await Task.WhenAny(Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken), Task.Run(async () => await serverCommandManager.ListenForServerCommands(stoppingToken)));
            logger.LogInformation("Network Host Stopped");
        }
    }
}