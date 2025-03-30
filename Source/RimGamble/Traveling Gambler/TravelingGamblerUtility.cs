using System;
using System.Collections.Generic;
using RimWorld;
using System.Linq;
using Verse.AI.Group;
using Verse;

namespace RimGamble
{
    public static class TravelingGamblerUtility
    {
        private static readonly List<TravelingGamblerBaseDef> requires = new List<TravelingGamblerBaseDef>();

        private static readonly List<TravelingGamblerBaseDef> exclude = new List<TravelingGamblerBaseDef>();

        private static readonly List<ITravelingGamblerDef> temp = new List<ITravelingGamblerDef>();

        public static void GetTravelingGamblerSpecifics(Map map, ref TravelingGamblerFormKindDef form, ref TravelingGamblerAggressiveDef aggressive, ref TravelingGamblerRejectionDef rejection, ref TravelingGamblerAcceptanceDef acceptance)
        {
            float combatPoints = StorytellerUtility.DefaultThreatPointsNow(map);

            if (form == null)
            {
                form = DefDatabase<TravelingGamblerFormKindDef>.AllDefsListForReading.RandomElementByWeight((TravelingGamblerFormKindDef x) => x.Weight);
            }

            requires.AddRange(form.Requires);
            exclude.AddRange(form.Excludes);
            if (aggressive == null)
            {
                aggressive = GetRandom(DefDatabase<TravelingGamblerAggressiveDef>.AllDefsListForReading, combatPoints, requires, exclude);
            }

            if (rejection == null)
            {
                rejection = GetRandom(DefDatabase<TravelingGamblerRejectionDef>.AllDefsListForReading, combatPoints, requires, exclude);
            }

            if (acceptance == null)
            {
                acceptance = GetRandom(DefDatabase<TravelingGamblerAcceptanceDef>.AllDefsListForReading, combatPoints, requires, exclude);
            }

            exclude.Clear();
            requires.Clear();
        }

        public static Pawn GenerateAndSpawn(Map map, float combatPoints)
        {
            TravelingGamblerFormKindDef travelingGamblerFormKindDef = DefDatabase<TravelingGamblerFormKindDef>.AllDefsListForReading.RandomElementByWeight((TravelingGamblerFormKindDef x) => x.Weight);
            requires.AddRange(travelingGamblerFormKindDef.Requires);
            exclude.AddRange(travelingGamblerFormKindDef.Excludes);
            TravelingGamblerAggressiveDef aggressive = GetRandom(DefDatabase<TravelingGamblerAggressiveDef>.AllDefsListForReading, combatPoints, requires, exclude);
            TravelingGamblerRejectionDef rejection = GetRandom(DefDatabase<TravelingGamblerRejectionDef>.AllDefsListForReading, combatPoints, requires, exclude);
            TravelingGamblerAcceptanceDef acceptance = GetRandom(DefDatabase<TravelingGamblerAcceptanceDef>.AllDefsListForReading, combatPoints, requires, exclude);
            exclude.Clear();
            requires.Clear();
            return GenerateAndSpawn(travelingGamblerFormKindDef, aggressive, rejection, acceptance, map);
        }

        public static Pawn GenerateAndSpawn(TravelingGamblerFormKindDef form, TravelingGamblerAggressiveDef aggressive, TravelingGamblerRejectionDef rejection, TravelingGamblerAcceptanceDef acceptance, Map map)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(form, null, PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: true, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, DevelopmentalStage.Adult, null, null, null, forceRecruitable: true);
            request.AllowedDevelopmentalStages = DevelopmentalStage.Adult;
            request.ForceGenerateNewPawn = true;
            request.AllowFood = true;
            request.DontGiveWeapon = true;
            request.OnlyUseForcedBackstories = form.fixedAdultBackstories.Any();
            request.MaximumAgeTraits = 1;
            request.MinimumAgeTraits = 1;
            request.ForceNoIdeoGear = true;
            request.MustBeCapableOfViolence = true;
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            Pawn_TravelingGamblerTracker travelinggambler = pawn.GetTravelingGamblerTracker();
            travelinggambler.form = form;
            travelinggambler.aggressive = aggressive;
            travelinggambler.rejection = rejection;
            travelinggambler.acceptance = acceptance;
            pawn.guest.Recruitable = false;

            if (!RCellFinder.TryFindRandomPawnEntryCell(out var result, map, CellFinder.EdgeRoadChance_Friendly, allowFogged: false, (IntVec3 cell) => map.reachability.CanReachMapEdge(cell, TraverseParms.For(TraverseMode.PassDoors))))
            {
                return null;
            }

            GenSpawn.Spawn(pawn, result, map);
            if (!RCellFinder.TryFindRandomSpotJustOutsideColony(pawn, out var result2))
            {
                return null;
            }

            LordMaker.MakeNewLord(pawn.Faction, new LordJob_CreepJoiner(result2, pawn), map).AddPawn(pawn);
            travelinggambler.Notify_Created();
            return pawn;
        }

        public static T GetRandom<T>(List<T> defs, float combatPoints, List<TravelingGamblerBaseDef> requires, List<TravelingGamblerBaseDef> exclude) where T : TravelingGamblerBaseDef
        {
            if (defs == null || defs.Count == 0)
            {
                Log.Error("GetRandom<T> failed: defs list is null or empty!");
                return null;
            }

            if (requires == null)
            {
                requires = new List<TravelingGamblerBaseDef>();
            }

            if (exclude == null)
            {
                exclude = new List<TravelingGamblerBaseDef>();
            }

            T val;
            if (requires.Empty() && exclude.Empty())
            {
                val = defs.Where((T x) => combatPoints >= x.MinCombatPoints && x.CanOccurRandomly).RandomElementByWeight((T x) => x.Weight);
            }
            else
            {
                bool flag = false;
                foreach (T def in defs)
                {
                    if (!(combatPoints < def.MinCombatPoints) && def.CanOccurRandomly && requires.Contains(def))
                    {
                        flag = true;
                        break;
                    }
                }

                if (flag)
                {
                    foreach (T def2 in defs)
                    {
                        if (!(combatPoints < def2.MinCombatPoints) && def2.CanOccurRandomly && requires.Contains(def2))
                        {
                            temp.Add(def2);
                        }
                    }
                }
                else
                {
                    foreach (T def3 in defs)
                    {
                        if (combatPoints >= def3.MinCombatPoints && def3.CanOccurRandomly)
                        {
                            temp.Add(def3);
                        }
                    }
                }

                for (int num = temp.Count - 1; num >= 0; num--)
                {
                    if (exclude.Contains(temp[num]))
                    {
                        defs.RemoveAt(num);
                    }
                }

                if (temp.Empty())
                {
                    string text = defs.Select((T x) => x.label).ToCommaList();
                    string text2 = requires.Select((TravelingGamblerBaseDef x) => x.label).ToCommaList();
                    string text3 = exclude.Select((TravelingGamblerBaseDef x) => x.label).ToCommaList();
                    Log.Error($"Attempted to create travelinggambler but blacklist removed all possible whitelist; combatPoints = {combatPoints}, defs = ({text}), whitelist = ({text2}), blacklist = ({text3})");
                    val = defs.RandomElementByWeight((T x) => x.Weight);
                }
                else
                {
                    val = temp.RandomElementByWeight((ITravelingGamblerDef x) => x.Weight) as T;
                }

                temp.Clear();
            }

            exclude.AddRange(val.Excludes);
            requires.AddRange(val.Requires);
            return val;
        }
    }

    public static class PawnExtensions
    {
        public static Pawn_TravelingGamblerTracker GetTravelingGamblerTracker(this Pawn pawn)
        {
            return TravelingGamblerTrackerManager.GetTracker(pawn);
        }
    }
}
