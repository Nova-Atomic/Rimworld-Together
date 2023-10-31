using Microsoft.Extensions.Logging;
using RimworldTogether.GameServer.Files;
using RimworldTogether.GameServer.Network;
using RimworldTogether.Shared.JSON;
using RimworldTogether.Shared.Misc;
using RimworldTogether.Shared.Network;

namespace RimworldTogether.GameServer.Managers
{
    public class CustomDifficultyManager
    {
        private readonly ILogger<CustomDifficultyManager> logger;
        private readonly ResponseShortcutManager responseShortcutManager;

        public CustomDifficultyManager(
            ILogger<CustomDifficultyManager> logger,
            ResponseShortcutManager responseShortcutManager)
        {
            this.logger = logger;
            this.responseShortcutManager = responseShortcutManager;
        }

        public void ParseDifficultyPacket(Client client, Packet packet)
        {
            DifficultyValuesJSON difficultyValuesJSON = Serializer.SerializeFromString<DifficultyValuesJSON>(packet.contents[0]);
            SetCustomDifficulty(client, difficultyValuesJSON);
        }

        public void SetCustomDifficulty(Client client, DifficultyValuesJSON difficultyValuesJSON)
        {
            if (!client.isAdmin) responseShortcutManager.SendIllegalPacket(client);
            else
            {
                DifficultyValuesFile newDifficultyValues = new DifficultyValuesFile();

                newDifficultyValues.ThreatScale = difficultyValuesJSON.ThreatScale;

                newDifficultyValues.AllowBigThreats = difficultyValuesJSON.AllowBigThreats;

                newDifficultyValues.AllowViolentQuests = difficultyValuesJSON.AllowViolentQuests;

                newDifficultyValues.AllowIntroThreats = difficultyValuesJSON.AllowIntroThreats;

                newDifficultyValues.PredatorsHuntHumanlikes = difficultyValuesJSON.PredatorsHuntHumanlikes;

                newDifficultyValues.AllowExtremeWeatherIncidents = difficultyValuesJSON.AllowExtremeWeatherIncidents;

                newDifficultyValues.CropYieldFactor = difficultyValuesJSON.CropYieldFactor;

                newDifficultyValues.MineYieldFactor = difficultyValuesJSON.MineYieldFactor;

                newDifficultyValues.ButcherYieldFactor = difficultyValuesJSON.ButcherYieldFactor;

                newDifficultyValues.ResearchSpeedFactor = difficultyValuesJSON.ResearchSpeedFactor;

                newDifficultyValues.QuestRewardValueFactor = difficultyValuesJSON.QuestRewardValueFactor;

                newDifficultyValues.RaidLootPointsFactor = difficultyValuesJSON.RaidLootPointsFactor;

                newDifficultyValues.TradePriceFactorLoss = difficultyValuesJSON.TradePriceFactorLoss;

                newDifficultyValues.MaintenanceCostFactor = difficultyValuesJSON.MaintenanceCostFactor;

                newDifficultyValues.ScariaRotChance = difficultyValuesJSON.ScariaRotChance;

                newDifficultyValues.EnemyDeathOnDownedChanceFactor = difficultyValuesJSON.EnemyDeathOnDownedChanceFactor;

                newDifficultyValues.ColonistMoodOffset = difficultyValuesJSON.ColonistMoodOffset;

                newDifficultyValues.FoodPoisonChanceFactor = difficultyValuesJSON.FoodPoisonChanceFactor;

                newDifficultyValues.ManhunterChanceOnDamageFactor = difficultyValuesJSON.ManhunterChanceOnDamageFactor;

                newDifficultyValues.PlayerPawnInfectionChanceFactor = difficultyValuesJSON.PlayerPawnInfectionChanceFactor;

                newDifficultyValues.DiseaseIntervalFactor = difficultyValuesJSON.DiseaseIntervalFactor;

                newDifficultyValues.DeepDrillInfestationChanceFactor = difficultyValuesJSON.DeepDrillInfestationChanceFactor;

                newDifficultyValues.FriendlyFireChanceFactor = difficultyValuesJSON.FriendlyFireChanceFactor;

                newDifficultyValues.AllowInstantKillChance = difficultyValuesJSON.AllowInstantKillChance;

                newDifficultyValues.AllowTraps = difficultyValuesJSON.AllowTraps;

                newDifficultyValues.AllowTurrets = difficultyValuesJSON.AllowTurrets;

                newDifficultyValues.AllowMortars = difficultyValuesJSON.AllowMortars;

                newDifficultyValues.AdaptationEffectFactor = difficultyValuesJSON.AdaptationEffectFactor;

                newDifficultyValues.AdaptationGrowthRateFactorOverZero = difficultyValuesJSON.AdaptationGrowthRateFactorOverZero;

                newDifficultyValues.FixedWealthMode = difficultyValuesJSON.FixedWealthMode;

                newDifficultyValues.LowPopConversionBoost = difficultyValuesJSON.LowPopConversionBoost;

                newDifficultyValues.NoBabiesOrChildren = difficultyValuesJSON.NoBabiesOrChildren;

                newDifficultyValues.BabiesAreHealthy = difficultyValuesJSON.BabiesAreHealthy;

                newDifficultyValues.ChildRaidersAllowed = difficultyValuesJSON.ChildRaidersAllowed;

                newDifficultyValues.ChildAgingRate = difficultyValuesJSON.ChildAgingRate;

                newDifficultyValues.AdultAgingRate = difficultyValuesJSON.AdultAgingRate;

                newDifficultyValues.WastepackInfestationChanceFactor = difficultyValuesJSON.WastepackInfestationChanceFactor;

                logger.LogWarning($"[Set difficulty] > {client.username}");

                SaveCustomDifficulty(newDifficultyValues);
            }
        }

        public void SaveCustomDifficulty(DifficultyValuesFile newDifficultyValues)
        {
            string path = Path.Combine(Core.Program.corePath, "DifficultyValues.json");

            Serializer.SerializeToFile(path, newDifficultyValues);

            logger.LogInformation("Saved difficulty values");

            LoadCustomDifficulty();
        }

        public void LoadCustomDifficulty()
        {
            string path = Path.Combine(Core.Program.corePath, "DifficultyValues.json");

            if (File.Exists(path)) Core.Program.difficultyValues = Serializer.SerializeFromFile<DifficultyValuesFile>(path);
            else
            {
                Core.Program.difficultyValues = new DifficultyValuesFile();
                Serializer.SerializeToFile(path, Core.Program.difficultyValues);
            }

            logger.LogInformation("Loaded difficulty values");
        }
    }
}
