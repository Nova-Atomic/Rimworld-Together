﻿using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimworldTogether.GameClient.Managers.Actions;
using RimworldTogether.Shared.JSON.Actions;

namespace RimworldTogether.GameClient.Values
{
    public static class ClientValues
    {
        public static bool needsToGenerateWorld;

        public static bool isLoadingPrefabWorld;

        public static bool isSaving;

        public static bool isDisconnecting;

        public static bool isQuiting;

        public static bool isReadyToPlay;

        //Do not change manually
        public static bool autoDenyTransfers;

        //Do not change manually
        public static bool autoRejectSiteRewards;

        public static bool isInTransfer;

        public static bool isInVisit;

        public static Settlement chosenSettlement;
        public static Caravan chosenCaravan;
        public static Site chosenSite;
        public static CompLaunchable chosendPods;

        public static TransferManifestJSON outgoingManifest = new TransferManifestJSON();
        public static TransferManifestJSON incomingManifest = new TransferManifestJSON();
        public static List<Tradeable> listToShowInTradesMenu = new List<Tradeable>();

        public static int autosaveDays = 1;
        public static float autosaveCurrentTicks;
        public static float autosaveInternalTicks = autosaveDays * 60000f;
        public static string versionCode = "1.0.6";

        public static string[] serverBrowserContainer = new string[]
        {
            "127.0.0.1|25555",
            "173.212.193.68|25555",
            "173.212.193.68|25556",
            "173.212.193.68|25557",
        };

        public static void ToggleGenerateWorld(bool mode) { needsToGenerateWorld = mode; }

        public static void ToggleLoadingPrefabWorld(bool mode) { isLoadingPrefabWorld = mode; }

        public static void ToggleSaving(bool mode) { isSaving = mode; }

        public static void ToggleDisconnecting(bool mode) { isDisconnecting = mode; }

        public static void ToggleQuiting(bool mode) { isQuiting = mode; }

        public static void ToggleReadyToPlay(bool mode) { isReadyToPlay = mode; }

        public static void ToggleTransfer(bool mode) { isInTransfer = mode; }

        public static void ToggleVisit(bool mode) { isInVisit = mode; }

        public static void ToggleChatScroll(bool mode) { ChatManager.shouldScrollChat = mode; }

        public static void CleanValues()
        {
            needsToGenerateWorld = false;
            isLoadingPrefabWorld = false;
            isSaving = false;
            isDisconnecting = false;
            isQuiting = false;
            isReadyToPlay = false;
            isInTransfer = false;
            isInVisit = false;

            chosenSettlement = null;
            chosenCaravan = null;
            chosenSite = null;

            outgoingManifest = new TransferManifestJSON();
            incomingManifest = new TransferManifestJSON();
            listToShowInTradesMenu = new List<Tradeable>();
        }
    }
}