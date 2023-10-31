using Microsoft.Extensions.Logging;
using RimworldTogether.GameServer.Core;
using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Managers
{
    public class WorldManager
    {
        public enum WorldStepMode { Required, Existing, Saved }

        private static string worldFileName = "WorldValues.json";

        private static string worldFilePath = Path.Combine(Program.corePath, worldFileName);
        private readonly ILogger<WorldManager> logger;

        public WorldManager(
            ILogger<WorldManager> logger)
        {
            this.logger = logger;
        }

        public void ParseWorldPacket(Client client, Packet packet)
        {
            WorldDetailsJSON worldDetailsJSON = Serializer.SerializeFromString<WorldDetailsJSON>(packet.contents[0]);

            switch (int.Parse(worldDetailsJSON.worldStepMode))
            {
                case (int)WorldStepMode.Required:
                    SaveWorldPrefab(client, worldDetailsJSON);
                    break;

                case (int)WorldStepMode.Existing:
                    //Do nothing
                    break;

                case (int)WorldStepMode.Saved:
                    //Do nothing
                    break;
            }
        }

        public static bool CheckIfWorldExists() { return File.Exists(worldFilePath); }

        public void SaveWorldPrefab(Client client, WorldDetailsJSON worldDetailsJSON)
        {
            WorldValuesFile worldValues = new WorldValuesFile();
            worldValues.SeedString = worldDetailsJSON.SeedString;
            worldValues.PlanetCoverage = worldDetailsJSON.PlanetCoverage;
            worldValues.Rainfall = worldDetailsJSON.Rainfall;
            worldValues.Temperature = worldDetailsJSON.Temperature;
            worldValues.Population = worldDetailsJSON.Population;
            worldValues.Pollution = worldDetailsJSON.Pollution;
            worldValues.Factions = worldDetailsJSON.Factions;

            Serializer.SerializeToFile(worldFilePath, worldValues);
            logger.LogInformation($"[Save world] > {client.username}");

            Program.worldValues = worldValues;

            worldDetailsJSON.worldStepMode = ((int)WorldStepMode.Saved).ToString();
            string[] contents = new string[] { Serializer.SerializeToString(worldDetailsJSON) };
            Packet packet = new Packet("WorldPacket", contents);
            client.SendData(packet);
        }

        public void RequireWorldFile(Client client)
        {
            WorldDetailsJSON worldDetailsJSON = new WorldDetailsJSON();
            worldDetailsJSON.worldStepMode = ((int)WorldStepMode.Required).ToString();

            string[] contents = new string[] { Serializer.SerializeToString(worldDetailsJSON) };
            Packet packet = new Packet("WorldPacket", contents);
            client.SendData(packet);
        }

        public void SendWorldFile(Client client)
        {
            WorldValuesFile worldValues = Program.worldValues;

            WorldDetailsJSON worldDetailsJSON = new WorldDetailsJSON();
            worldDetailsJSON.worldStepMode = ((int)WorldStepMode.Existing).ToString();
            worldDetailsJSON.SeedString = worldValues.SeedString;
            worldDetailsJSON.PlanetCoverage = worldValues.PlanetCoverage;
            worldDetailsJSON.Rainfall = worldValues.Rainfall;
            worldDetailsJSON.Temperature = worldValues.Temperature;
            worldDetailsJSON.Population = worldValues.Population;
            worldDetailsJSON.Pollution = worldValues.Pollution;
            worldDetailsJSON.Factions = worldValues.Factions;

            string[] contents = new string[] { Serializer.SerializeToString(worldDetailsJSON) };
            Packet packet = new Packet("WorldPacket", contents);
            client.SendData(packet);
        }

        public void LoadWorldFile()
        {
            if (File.Exists(worldFilePath))
            {
                Program.worldValues = Serializer.SerializeFromFile<WorldValuesFile>(worldFilePath);

                logger.LogInformation("Loaded world values");
            }

            else logger.LogWarning("[Warning] > World is missing. Join server to create it");
        }
    }
}
