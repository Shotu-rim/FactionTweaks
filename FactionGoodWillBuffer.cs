
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MissingFactionsButton
{
    public static class FactionGoodwillBuffer
    {
        // Queues
        private static Dictionary<FactionDef, Queue<int>> PlannedGoodwill =
            new Dictionary<FactionDef, Queue<int>>();

        // Set goodwill values to faction
        public static Dictionary<Faction, int> PendingGoodwill =
            new Dictionary<Faction, int>();

        // Goodwill random on "Generate"

        public static void Prepare(List<FactionDef> factions)
        {
            Clear();

            if (factions == null)
                return;

            if (FactionTweaksMod.Settings == null)
                return;

            if (!FactionTweaksMod.Settings.randomizeGoodwill)
                return;

            int prepared = 0;

            foreach (FactionDef def in factions)
            {
                if (def == null)
                    continue;

                // Exclude enemies.
                if (def.permanentEnemy ||
                    def.permanentEnemyToEveryoneExceptPlayer ||
                    def.permanentEnemyToEveryoneExcept != null ||
                    def.naturalEnemy)
                {
                    continue;
                }

                int min =
                    FactionTweaksMod.Settings.GetMinGoodwill(
                        def.defName
                    );

                int max =
                    FactionTweaksMod.Settings.GetMaxGoodwill(
                        def.defName
                    );

                if (min > max)
                {
                    int temp = min;
                    min = max;
                    max = temp;
                }

                int goodwill = Rand.RangeInclusive(min, max);

                Queue<int> queue;

                if (!PlannedGoodwill.TryGetValue(def, out queue))
                {
                    queue = new Queue<int>();
                    PlannedGoodwill.Add(def, queue);
                }

                queue.Enqueue(goodwill);
                prepared++;

                Log.Message(
                    "[Faction Tweaks] Planned Goodwill " +
                    goodwill + " for " + def.defName
                );
            }

            Log.Message(
                "[Faction Tweaks] Prepared " +
                prepared + " Goodwill values."
            );
        }

        // Set goodwill on faction creation

        public static bool TryTake(
            Faction faction,
            out int goodwill)
        {
            goodwill = 0;

            if (faction == null || faction.def == null)
                return false;

            Queue<int> queue;

            if (!PlannedGoodwill.TryGetValue(
                faction.def,
                out queue))
            {
                return false;
            }

            if (queue.Count == 0)
                return false;

            goodwill = queue.Dequeue();

            if (queue.Count == 0)
            {
                PlannedGoodwill.Remove(faction.def);
            }

            if (!faction.HasGoodwill)
            {
                Log.Message(
                    "[Faction Tweaks] Faction has no Goodwill: " +
                    faction.Name
                );

                return true;
            }

            PendingGoodwill[faction] = goodwill;

            Log.Message(
                "[Faction Tweaks] Assigned Goodwill " +
                goodwill + " to " + faction.Name
            );

            return true;
        }

        // Apply goodwill

        public static void Apply()
        {
            if (PendingGoodwill.Count == 0)
            {
                Log.Message(
                    "[Faction Tweaks] No pending Goodwill values."
                );

                PlannedGoodwill.Clear();
                return;
            }

            Faction playerFaction = Faction.OfPlayer;

            if (playerFaction == null)
            {
                Log.Error(
                    "[Faction Tweaks] Player faction does not exist!"
                );

                return;
            }

            int applied = 0;

            foreach (KeyValuePair<Faction, int> entry
                in PendingGoodwill.ToList())
            {
                Faction faction = entry.Key;

                if (faction == null ||
                    faction == playerFaction ||
                    !faction.HasGoodwill)
                {
                    PendingGoodwill.Remove(faction);
                    continue;
                }

                if (faction.RelationWith(playerFaction, true) == null)
                {
                    faction.TryMakeInitialRelationsWith(
                        playerFaction
                    );
                }

                FactionRelation factionRelation =
                    faction.RelationWith(playerFaction, true);

                FactionRelation playerRelation =
                    playerFaction.RelationWith(faction, true);

                if (factionRelation == null ||
                    playerRelation == null)
                {
                    Log.Warning(
                        "[Faction Tweaks] Missing relation for " +
                        faction.Name
                    );

                    continue;
                }

                int goodwill = Mathf.Clamp(
                    entry.Value,
                    -100,
                    100
                );

                int maxGoodwill =
                    Find.GoodwillSituationManager.GetMaxGoodwill(
                        faction
                    );

                goodwill = Mathf.Min(
                    goodwill,
                    maxGoodwill
                );

                factionRelation.baseGoodwill = goodwill;
                playerRelation.baseGoodwill = goodwill;

                bool sentLetter;

                factionRelation.CheckKindThresholds(
                    faction,
                    false,
                    null,
                    default(GlobalTargetInfo),
                    out sentLetter
                );

                playerRelation.CheckKindThresholds(
                    playerFaction,
                    false,
                    null,
                    default(GlobalTargetInfo),
                    out sentLetter
                );

                PendingGoodwill.Remove(faction);

                applied++;

                Log.Message(
                    "[Faction Tweaks] Applied Goodwill " +
                    goodwill + " to " + faction.Name
                );
            }

            Log.Message(
                "[Faction Tweaks] Applied Goodwill to " +
                applied + " factions."
            );

            if (PendingGoodwill.Count == 0)
            {
                PlannedGoodwill.Clear();

                Log.Message(
                    "[Faction Tweaks] Goodwill buffer cleared."
                );
            }
            else
            {
                Log.Warning(
                    "[Faction Tweaks] Remaining pending factions: " +
                    PendingGoodwill.Count
                );
            }
        }

        // Clear buffor 

        public static void Clear()
        {
            PlannedGoodwill.Clear();
            PendingGoodwill.Clear();

            Log.Message(
                "[Faction Tweaks] Goodwill buffer cleared."
            );
        }
    }
}
