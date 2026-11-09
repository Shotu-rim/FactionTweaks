
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MissingFactionsButton
{
    public class FactionTweaksSettings : ModSettings
    {
        public bool randomizeGoodwill = false;

        public Dictionary<string, int> minGoodwill =
            new Dictionary<string, int>();

        public Dictionary<string, int> maxGoodwill =
            new Dictionary<string, int>();

        public const int DefaultMinGoodwill = -100;
        public const int DefaultMaxGoodwill = 100;

        public int GetMinGoodwill(string defName)
        {
            return minGoodwill.TryGetValue(defName, out int value)
                ? Mathf.Clamp(value, -100, 100)
                : DefaultMinGoodwill;
        }

        public int GetMaxGoodwill(string defName)
        {
            return maxGoodwill.TryGetValue(defName, out int value)
                ? Mathf.Clamp(value, -100, 100)
                : DefaultMaxGoodwill;
        }

        public void SetMinGoodwill(string defName, int value)
        {
            value = Mathf.Clamp(value, -100, 100);

            minGoodwill[defName] = value;

            if (value > GetMaxGoodwill(defName))
            {
                maxGoodwill[defName] = value;
            }
        }

        public void SetMaxGoodwill(string defName, int value)
        {
            value = Mathf.Clamp(value, -100, 100);

            maxGoodwill[defName] = value;

            if (value < GetMinGoodwill(defName))
            {
                minGoodwill[defName] = value;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(
                ref randomizeGoodwill,
                "randomizeGoodwill",
                false
            );

            Scribe_Collections.Look(
                ref minGoodwill,
                "minGoodwill",
                LookMode.Value,
                LookMode.Value
            );

            Scribe_Collections.Look(
                ref maxGoodwill,
                "maxGoodwill",
                LookMode.Value,
                LookMode.Value
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (minGoodwill == null)
                    minGoodwill = new Dictionary<string, int>();

                if (maxGoodwill == null)
                    maxGoodwill = new Dictionary<string, int>();
            }
        }
    }
}
