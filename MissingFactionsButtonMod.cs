
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MissingFactionsButton
{
    [StaticConstructorOnStartup]
    public static class MissingFactionsButtonMod
    {
        static MissingFactionsButtonMod()
        {
            Harmony harmony = new Harmony("shotu.factiontweaks");
            harmony.PatchAll();

            Log.Message("[Faction Tweaks] Mod initialized successfully.");
        }
    }

    // UI change

    [HarmonyPatch(typeof(WorldFactionsUIUtility), "DoWindowContents")]
    public static class Patch_WorldFactionsUIUtility
    {
        public static List<FactionDef> PendingFactions;

        public static bool InsideFactionUtility;

        public static float WarningHeightAtStart;

        private static readonly FieldInfo WarningHeightField =
            typeof(WorldFactionsUIUtility).GetField(
                "warningHeight",
                BindingFlags.Static | BindingFlags.NonPublic
            );


        // Prefix

        [HarmonyPrefix]
        public static void Prefix()
        {
            InsideFactionUtility = true;

            if (WarningHeightField != null)
            {
                WarningHeightAtStart =
                    (float)WarningHeightField.GetValue(null);
            }
        }


        // Postfix

        [HarmonyPostfix]
        public static void Postfix(
            Rect rect,
            List<FactionDef> factions,
            bool isDefaultFactionCounts)
        {
            float num = rect.width * 0.0500000119f;
            float num2 = Text.LineHeight + 4f;

            // Use the warning height 

            Rect rect3 = new Rect(
                rect.x + num,
                rect.y + num2,
                rect.width * 0.9f,
                rect.height - num2 - Text.LineHeight - 28f
                    - WarningHeightAtStart
            );

            Rect outRect = rect3.ContractedBy(4f);

            float listingHeight = 0f;

            foreach (FactionDef faction in factions)
            {
                if (faction.displayInFactionSelection)
                {
                    listingHeight += 32f;
                }
            }

            // ADD MISSING FACTIONS button

            float originalAddY = Mathf.Min(
                rect3.yMax,
                outRect.y + listingHeight + 4f
            );

            Rect addButtonRect = new Rect(
                outRect.x,
                originalAddY + 32f,
                outRect.width,
                28f
            );

            if (Widgets.ButtonText(
                addButtonRect,
                "Add missing factions"))
            {
                AddMissingFactions(factions);
            }

            // reserve space for button "just in case"

            if (WarningHeightField != null)
            {
                float currentWarningHeight =
                    (float)WarningHeightField.GetValue(null);

                WarningHeightField.SetValue(
                    null,
                    currentWarningHeight + 32f
                );
            }

            // Factions in bottom

            PendingFactions = factions;

            InsideFactionUtility = false;
        }

        // Final build

        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception)
        {
            InsideFactionUtility = false;

            return __exception;
        }

        // Add missing factions method

        private static void AddMissingFactions(
            List<FactionDef> factions)
        {
            List<FactionDef> missingFactions =
                FactionGenerator.ConfigurableFactions
                    .Where(f => f.displayInFactionSelection)
                    .Where(f => !factions.Contains(f))
                    .OrderBy(f => Guid.NewGuid())
                    .ToList();

            int added = 0;

            foreach (FactionDef faction in missingFactions)
            {
                if (faction.maxConfigurableAtWorldCreation < 1)
                {
                    continue;
                }

                factions.Add(faction);
                added++;
            }

            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();

            Messages.Message(
                "Added missing factions: " + added,
                MessageTypeDefOf.PositiveEvent,
                false
            );

            Log.Message(
                "[Faction Tweaks] Added " + added +
                " missing factions."
            );
        }

        // Random factions method

        public static void RandomizeFactions(
            List<FactionDef> factions)
        {
            // Remove removable factions. Mandatory stay as-is

            for (int i = factions.Count - 1; i >= 0; i--)
            {
                FactionDef faction = factions[i];

                if (faction.displayInFactionSelection &&
                    !IsFactionMandatory(faction))
                {
                    factions.RemoveAt(i);
                }
            }

            // List available factions.

            List<FactionDef> factionPool =
                FactionGenerator.ConfigurableFactions
                    .Where(f => f.displayInFactionSelection)
                    .Where(f => !IsFactionMandatory(f))
                    .Where(f => f.maxConfigurableAtWorldCreation > 0)
                    .Distinct()
                    .ToList();

            int maxFactions = factionPool.Count;

            if (maxFactions == 0)
            {
                Log.Warning(
                    "[Faction Tweaks] No factions available for randomization."
                );

                return;
            }

            int minTarget = Math.Min(6, maxFactions);

            int target = Rand.RangeInclusive(
                minTarget,
                maxFactions
            );

            int added = 0;

            while (added < target)
            {
                List<FactionDef> availableFactions =
                    factionPool
                        .Where(f =>
                            factions.Count(x => x == f) <
                            f.maxConfigurableAtWorldCreation)
                        .ToList();

                if (availableFactions.Count == 0)
                {
                    break;
                }

                int randomIndex = Rand.Range(
                    0,
                    availableFactions.Count
                );

                FactionDef selectedFaction =
                    availableFactions[randomIndex];

                factions.Add(selectedFaction);
                added++;
            }

            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();

            Messages.Message(
                "Randomized factions: " + added,
                MessageTypeDefOf.PositiveEvent,
                false
            );

            Log.Message(
                "[Faction Tweaks] Randomized factions: " +
                added
            );
        }

        // Check mandatory factions "just in case"

        private static bool IsFactionMandatory(FactionDef faction)
        {
            FieldInfo partsField = typeof(Scenario).GetField(
                "parts",
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public
            );

            if (partsField == null)
            {
                Log.Error(
                    "[Faction Tweaks] Scenario.parts NOT FOUND!"
                );

                return true;
            }

            List<ScenPart> parts =
                partsField.GetValue(Current.Game.Scenario)
                as List<ScenPart>;

            if (parts == null)
            {
                Log.Error(
                    "[Faction Tweaks] Scenario.parts is NULL!"
                );

                return true;
            }

            foreach (ScenPart part in parts)
            {
                if (part.def.preventRemovalOfFaction == faction)
                {
                    return true;
                }
            }

            return false;
        }
    }

    // Warnings position

    [HarmonyPatch(
        typeof(Widgets),
        nameof(Widgets.Label),
        new Type[] { typeof(Rect), typeof(string) }
    )]
    public static class Patch_Widgets_Label
    {
        [HarmonyPrefix]
        public static void Prefix(ref Rect __0)
        {
            // Modify inside faction pool

            if (!Patch_WorldFactionsUIUtility.InsideFactionUtility)
            {
                return;
            }

            // Modify yellow warnings

            if (GUI.color != Color.yellow ||
                Text.Font != GameFont.Tiny)
            {
                return;
            }

            // Move warnings below Add missing factions.

            __0.y += 32f;

            __0.height = Mathf.Max(
                0f,
                __0.height - 32f
            );
        }
    }

    // Random factions setting

    [HarmonyPatch(typeof(Page), "DoBottomButtons")]
    public static class Patch_Page_DoBottomButtons
    {
        [HarmonyPostfix]
        public static void Postfix(Rect __0)
        {
            List<FactionDef> factions =
                Patch_WorldFactionsUIUtility.PendingFactions;

            if (factions == null)
            {
                return;
            }

            Patch_WorldFactionsUIUtility.PendingFactions = null;

            FieldInfo sizeField = typeof(Page).GetField(
                "BottomButSize",
                BindingFlags.Static |
                BindingFlags.NonPublic
            );

            if (sizeField == null)
            {
                Log.Error(
                    "[Faction Tweaks] BottomButSize field not found!"
                );

                return;
            }

            Vector2 buttonSize =
                (Vector2)sizeField.GetValue(null);

            float buttonY =
                __0.y + __0.height - 38f;

            float generateX =
                __0.x + __0.width - buttonSize.x;

            Rect randomButtonRect = new Rect(
                generateX - buttonSize.x - 8.5f,
                buttonY,
                buttonSize.x,
                buttonSize.y
            );

            if (Widgets.ButtonText(
                randomButtonRect,
                "Random factions"))
            {
                Patch_WorldFactionsUIUtility.RandomizeFactions(
                    factions
                );
            }
        }
    }
}
