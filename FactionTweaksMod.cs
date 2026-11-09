
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace MissingFactionsButton
{
    public class FactionTweaksMod : Mod
    {
        public static FactionTweaksSettings Settings;

        private Vector2 scrollPosition = Vector2.zero;

        public FactionTweaksMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<FactionTweaksSettings>();
        }

        public override string SettingsCategory()
        {
            return "Faction Tweaks";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (Settings == null)
                return;

            Rect checkboxRect = new Rect(
                inRect.x,
                inRect.y,
                inRect.width,
                30f
            );

            Widgets.CheckboxLabeled(
                checkboxRect,
                "Randomize Goodwill",
                ref Settings.randomizeGoodwill
            );

            TooltipHandler.TipRegion(
                checkboxRect,
                "This option will randomize Goodwill of configurable factions (natural hostile not included)"
            );

            // Show faction settings only when randomization is enabled.

            if (!Settings.randomizeGoodwill)
            {
                return;
            }

            Rect listRect = new Rect(
                inRect.x,
                inRect.y + 40f,
                inRect.width,
                inRect.height - 40f
            );

            List<FactionDef> factions =
                FactionGenerator.ConfigurableFactions
                    .Where(f => f != null)
                    .Where(f => f.displayInFactionSelection)
                    .Where(f => !f.permanentEnemy)
                    .Where(f => !f.permanentEnemyToEveryoneExceptPlayer)
                    .Where(f => f.permanentEnemyToEveryoneExcept == null)
                    .Where(f => !f.naturalEnemy)
                    .Distinct()
                    .OrderBy(f => f.label)
                    .ToList();

            float rowHeight = 76f;

            Rect viewRect = new Rect(
                0f,
                0f,
                listRect.width - 16f,
                factions.Count * rowHeight + 10f
            );

            Widgets.BeginScrollView(
                listRect,
                ref scrollPosition,
                viewRect
            );

            float y = 0f;

            foreach (FactionDef faction in factions)
            {
                string defName = faction.defName;

                int min = Settings.GetMinGoodwill(defName);
                int max = Settings.GetMaxGoodwill(defName);

                Widgets.Label(
                    new Rect(0f, y, viewRect.width, 22f),
                    faction.LabelCap + " (" + defName + ")"
                );

                y += 23f;

                Widgets.Label(
                    new Rect(0f, y, 145f, 22f),
                    "Min Goodwill: " + min
                );

                float newMin = Widgets.HorizontalSlider(
                    new Rect(150f, y + 2f, viewRect.width - 155f, 20f),
                    min,
                    -100f,
                    100f
                );

                int roundedMin = Mathf.RoundToInt(newMin);

                if (roundedMin != min)
                {
                    Settings.SetMinGoodwill(defName, roundedMin);
                    WriteSettings();
                }

                y += 23f;

                Widgets.Label(
                    new Rect(0f, y, 145f, 22f),
                    "Max Goodwill: " + max
                );

                float newMax = Widgets.HorizontalSlider(
                    new Rect(150f, y + 2f, viewRect.width - 155f, 20f),
                    max,
                    -100f,
                    100f
                );

                int roundedMax = Mathf.RoundToInt(newMax);

                if (roundedMax != max)
                {
                    Settings.SetMaxGoodwill(defName, roundedMax);
                    WriteSettings();
                }

                y += 30f;

                Widgets.DrawLineHorizontal(
                    0f,
                    y,
                    viewRect.width
                );

                y += 5f;
            }

            Widgets.EndScrollView();

            WriteSettings();
        }
    }
}
