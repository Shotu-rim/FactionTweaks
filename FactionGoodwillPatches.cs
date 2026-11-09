
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MissingFactionsButton
{

    // Prepare goodwill before world generation

    [HarmonyPatch]
    public static class Patch_WorldGenerator_Goodwill
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(WorldGenerator),
                "GenerateWorld",
                new Type[]
                {
                    typeof(float),
                    typeof(string),
                    typeof(OverallRainfall),
                    typeof(OverallTemperature),
                    typeof(OverallPopulation),
                    typeof(LandmarkDensity),
                    typeof(List<FactionDef>),
                    typeof(float)
                }
            );
        }

        [HarmonyPrefix]
        public static void Prefix(List<FactionDef> __6)
        {
            if (FactionTweaksMod.Settings == null)
                return;

            if (!FactionTweaksMod.Settings.randomizeGoodwill)
            {
                FactionGoodwillBuffer.Clear();
                return;
            }

            Log.Message(
                "[Faction Tweaks] Preparing Goodwill " +
                "before world generation."
            );

            if (__6 == null)
            {
                Log.Error(
                    "[Faction Tweaks] World generation faction list is null."
                );

                FactionGoodwillBuffer.Clear();
                return;
            }

            Log.Message(
                "[Faction Tweaks] World generation received " +
                __6.Count + " faction definitions."
            );

            FactionGoodwillBuffer.Prepare(__6);
        }
    }


    // Set goodwill to faction

    [HarmonyPatch(
        typeof(FactionGenerator),
        "NewGeneratedFaction",
        new Type[]
        {
            typeof(PlanetLayer),
            typeof(FactionGeneratorParms)
        }
    )]
    public static class Patch_FactionGenerator_Goodwill
    {
        [HarmonyPostfix]
        public static void Postfix(Faction __result)
        {
            if (__result == null)
                return;

            if (FactionTweaksMod.Settings == null)
                return;

            if (!FactionTweaksMod.Settings.randomizeGoodwill)
                return;

            int goodwill;

            FactionGoodwillBuffer.TryTake(
                __result,
                out goodwill
            );
        }
    }



    // Display goodwill on map and "Faction" button


    [HarmonyPatch(
        typeof(Faction),
        "get_PlayerGoodwill"
    )]
    public static class Patch_Faction_PlayerGoodwill
    {
        [HarmonyPrefix]
        public static bool Prefix(
            Faction __instance,
            ref int __result)
        {
            if (FactionTweaksMod.Settings == null ||
                !FactionTweaksMod.Settings.randomizeGoodwill)
            {
                return true;
            }

            if (__instance == null || __instance.IsPlayer)
                return true;

            int bufferedGoodwill;

            if (FactionGoodwillBuffer.PendingGoodwill.TryGetValue(
                __instance,
                out bufferedGoodwill))
            {
                __result = bufferedGoodwill;
                return false;
            }

            return true;
        }
    }



    // Display relation kind 


    [HarmonyPatch(
        typeof(Faction),
        "get_PlayerRelationKind"
    )]
    public static class Patch_Faction_PlayerRelationKind
    {
        [HarmonyPrefix]
        public static bool Prefix(
            Faction __instance,
            ref FactionRelationKind __result)
        {
            if (FactionTweaksMod.Settings == null ||
                !FactionTweaksMod.Settings.randomizeGoodwill)
            {
                return true;
            }

            if (__instance == null || __instance.IsPlayer)
                return true;

            int bufferedGoodwill;

            if (!FactionGoodwillBuffer.PendingGoodwill.TryGetValue(
                __instance,
                out bufferedGoodwill))
            {
                return true;
            }

            if (bufferedGoodwill <= -75)
            {
                __result = FactionRelationKind.Hostile;
            }
            else if (bufferedGoodwill >= 75)
            {
                __result = FactionRelationKind.Ally;
            }
            else
            {
                __result = FactionRelationKind.Neutral;
            }

            return false;
        }
    }


    // Goodwill applied to faction on game init

    [HarmonyPatch(
        typeof(Game),
        nameof(Game.InitNewGame)
    )]
    public static class Patch_Game_InitNewGame_Goodwill
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (FactionTweaksMod.Settings == null)
                return;

            if (!FactionTweaksMod.Settings.randomizeGoodwill)
                return;

            Log.Message(
                "[Faction Tweaks] Scheduling Goodwill application."
            );

            LongEventHandler.ExecuteWhenFinished(
                delegate
                {
                    FactionGoodwillBuffer.Apply();
                }
            );
        }
    }
}
