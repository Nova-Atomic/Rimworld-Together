using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RimworldTogether.GameServer.Managers;

namespace RimworldTogether.GameServer.Core
{
    public static partial class Program
    {
        internal sealed class ResourceStartupService : BackgroundService
        {
            private readonly ILogger<ResourceStartupService> logger;
            private readonly ModManager modManager;
            private readonly ClientManager clientManager;
            private readonly WorldManager worldManager;
            private readonly CustomDifficultyManager customDifficultyManager;
            private readonly WhitelistManager whitelistManager;

            public ResourceStartupService(
                ILogger<ResourceStartupService> logger,
                ModManager modManager,
                ClientManager clientManager,
                WorldManager worldManager,
                CustomDifficultyManager customDifficultyManager,
                WhitelistManager whitelistManager)
            {
                this.logger = logger;
                this.modManager = modManager;
                this.clientManager = clientManager;
                this.worldManager = worldManager;
                this.customDifficultyManager = customDifficultyManager;
                this.whitelistManager = whitelistManager;
            }

            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
                LoadResources(logger, modManager, clientManager, worldManager, customDifficultyManager, whitelistManager);
                return Task.CompletedTask;
            }
        }
    }
}