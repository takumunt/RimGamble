using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimGamble
{
    public interface ITravelingGamblerDef
    {
        float Weight { get; }

        float MinCombatPoints { get; }

        bool CanOccurRandomly { get; }

        List<TravelingGamblerBaseDef> Excludes { get; }

        List<TravelingGamblerBaseDef> Requires { get; }
    }

    public class TravelingGamblerBaseDef : Def, ITravelingGamblerDef
    {
        private float weight = 1f;

        private float minCombatPoints;

        private bool canOccurRandomly = true;

        private List<TravelingGamblerBaseDef> excludes = new List<TravelingGamblerBaseDef>();

        private List<TravelingGamblerBaseDef> requires = new List<TravelingGamblerBaseDef>();

        public float Weight => weight;

        public List<TravelingGamblerBaseDef> Excludes => excludes;
        
        public List<TravelingGamblerBaseDef> Requires => requires;

        public float MinCombatPoints => minCombatPoints;

        public bool CanOccurRandomly => canOccurRandomly;
    }

    public class TravelingGamblerAggressiveDef : TravelingGamblerBaseDef
    {
        public bool hasMessage;

        [MustTranslate]
        public string message;

        public bool hasLetter;

        public bool hasAlternativeLetter;

        [MustTranslate]
        public string alternativeLetterDesc;

        [MustTranslate]
        public string letterLabel;

        [MustTranslate]
        public string letterDesc;

        public LetterDef letterDef;

        public Type workerType;

        [MustTranslate]
        public string raidLetterLabel;

        [MustTranslate]
        public string raidLetterDesc;

        public LetterDef raidLetterDef;

        public int raidDelayTicks;
    }

    public class TravelingGamblerRejectionDef : TravelingGamblerBaseDef
    {
        public bool hasLetter;

        [MustTranslate]
        public string letterLabel;

        [MustTranslate]
        public string letterDesc;

        public LetterDef letterDef;

        public Type workerType;
    }

    public class TravelingGamblerAcceptanceDef : TravelingGamblerBaseDef
    {
        public bool repeats;

        public bool hasLetter;

        [MustTranslate]
        public string letterLabel;

        [MustTranslate]
        public string letterDesc;

        public LetterDef letterDef;

        public List<HediffDef> hediffs = new List<HediffDef>();

        public FloatRange triggersAfterDays = FloatRange.Zero;

        public List<AbilityDef> abilities = new List<AbilityDef>();

        public float triggerMtbDays;

        public FloatRange triggerMinDays = FloatRange.Zero;

        public bool canOccurWhenImprisoned;

        public bool canOccurWhileDowned = true;

        public bool mustBeConscious;

        public Type workerType;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string item in base.ConfigErrors())
            {
                yield return item;
            }

            if (triggerMtbDays != 0f && triggersAfterDays != FloatRange.Zero)
            {
                yield return "cannot use both triggerMtbDays and triggersAfterDays fields, use one or neither.";
            }
        }
    }

    public class TravelingGamblerFormKindDef : PawnKindDef, ITravelingGamblerDef
    {
        private float weight = 1f;

        private float minCombatPoints;

        private bool canOccurRandomly = true;

        [MustTranslate]
        public string letterLabel;

        [MustTranslate]
        public string letterPrompt;

        public List<BodyTypeGraphicData> bodyTypeGraphicPaths = new List<BodyTypeGraphicData>();

        public List<HeadTypeDef> forcedHeadTypes;

        public TagFilter hairTagFilter;

        public TagFilter beardTagFilter;

        public Color? hairColorOverride;

        private List<TravelingGamblerBaseDef> excludes = new List<TravelingGamblerBaseDef>();

        private List<TravelingGamblerBaseDef> requires = new List<TravelingGamblerBaseDef>();

        public float Weight => weight;

        public List<TravelingGamblerBaseDef> Excludes => excludes;

        public List<TravelingGamblerBaseDef> Requires => requires;

        public float MinCombatPoints => minCombatPoints;

        public bool CanOccurRandomly => canOccurRandomly;

        public string GetBodyGraphicPath(Pawn pawn)
        {
            for (int i = 0; i < bodyTypeGraphicPaths.Count; i++)
            {
                if (bodyTypeGraphicPaths[i].bodyType == pawn.story.bodyType)
                {
                    return bodyTypeGraphicPaths[i].texturePath;
                }
            }

            return null;
        }

        public bool StyleItemAllowed(StyleItemDef styleItem)
        {
            if (!ModLister.AnomalyInstalled)
            {
                return true;
            }

            bool flag = styleItem is HairDef;
            bool flag2 = styleItem is BeardDef;
            if (!flag && !flag2)
            {
                return true;
            }

            if (flag)
            {
                if (hairTagFilter != null && !hairTagFilter.Allows(styleItem.styleTags))
                {
                    return false;
                }
            }
            else if (flag2 && beardTagFilter != null && !beardTagFilter.Allows(styleItem.styleTags))
            {
                return false;
            }

            return true;
        }
    }
}
