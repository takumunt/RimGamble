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

        /// <summary>
        /// Filters behavior definitions based on current mod settings
        /// </summary>
        private static List<T> FilterDefsBySettings<T>(List<T> defs) where T : TravelingGamblerBaseDef
        {
            var filtered = new List<T>();
            
            foreach (var def in defs)
            {
                bool shouldInclude = true;
                
                // Check if this is an acceptance behavior and if acceptance behaviors are enabled
                if (def is TravelingGamblerAcceptanceDef acceptance)
                {
                    if (!Options.RimGamble_Settings.enableAcceptanceBehaviors)
                    {
                        shouldInclude = false;
                    }
                    else
                    {
                        // Check specific acceptance behavior settings
                        switch (acceptance.defName)
                        {
                            case "RimGamble_Leaves":
                                shouldInclude = Options.RimGamble_Settings.allowPeacefulDeparture;
                                break;
                            case "RimGamble_HumanBomb":
                                shouldInclude = Options.RimGamble_Settings.allowHumanBomb;
                                break;
                            case "RimGamble_Sabotage":
                                shouldInclude = Options.RimGamble_Settings.allowSabotage;
                                break;
                            case "RimGamble_LifeOfTheParty":
                                shouldInclude = Options.RimGamble_Settings.allowPartyMood;
                                break;
                            case "RimGamble_RumorSpread":
                                shouldInclude = Options.RimGamble_Settings.allowRumorSpread;
                                break;
                            case "RimGamble_TheftAccept":
                                shouldInclude = Options.RimGamble_Settings.allowTheftAccept;
                                break;
                            case "RimGamble_TradeCaravan":
                                shouldInclude = Options.RimGamble_Settings.allowTradeCaravan;
                                break;
                            case "RimGamble_DropPod":
                                shouldInclude = Options.RimGamble_Settings.allowDropPod;
                                break;
                            case "RimGamble_TeachSkill":
                                shouldInclude = Options.RimGamble_Settings.allowTeachSkill;
                                break;
                            case "RimGamble_GiveInspiration":
                                shouldInclude = Options.RimGamble_Settings.allowGiveInspiration;
                                break;
                            case "RimGamble_AskJoin":
                                shouldInclude = Options.RimGamble_Settings.allowAskJoin;
                                break;
                            default:
                                shouldInclude = true; // Allow unknown behaviors by default
                                break;
                        }
                    }
                }
                // Check aggressive behaviors
                else if (def is TravelingGamblerAggressiveDef aggressive)
                {
                    if (!Options.RimGamble_Settings.enableAggressiveBehaviors)
                    {
                        shouldInclude = false;
                    }
                    else
                    {
                        switch (aggressive.defName)
                        {
                            case "RimGamble_Assault":
                                shouldInclude = Options.RimGamble_Settings.allowAssault;
                                break;
                            case "RimGamble_Raid":
                                shouldInclude = Options.RimGamble_Settings.allowRaid;
                                break;
                            case "RimGamble_DelayedRaid":
                                shouldInclude = Options.RimGamble_Settings.allowDelayedRaid;
                                break;
                            case "RimGamble_Theft":
                                shouldInclude = Options.RimGamble_Settings.allowTheft;
                                break;
                            default:
                                shouldInclude = true;
                                break;
                        }
                    }
                }
                // Check rejection behaviors  
                else if (def is TravelingGamblerRejectionDef rejection)
                {
                    if (!Options.RimGamble_Settings.enableRejectionBehaviors)
                    {
                        shouldInclude = false;
                    }
                    else
                    {
                        switch (rejection.defName)
                        {
                            case "RimGamble_Departure":
                                shouldInclude = Options.RimGamble_Settings.allowPeacefulRejection;
                                break;
                            case "RimGamble_AggressiveRejection":
                                shouldInclude = Options.RimGamble_Settings.allowAggressiveRejection;
                                break;
                            default:
                                shouldInclude = true;
                                break;
                        }
                    }
                }
                // Check form types
                else if (def is TravelingGamblerFormKindDef form)
                {
                    switch (form.defName)
                    {
                        case "LuckyDrifter":
                            shouldInclude = Options.RimGamble_Settings.allowLuckyDrifter;
                            break;
                        case "RiskyCardshark":
                            shouldInclude = Options.RimGamble_Settings.allowRiskyCardshark;
                            break;
                        case "WashedUpHustler":
                            shouldInclude = Options.RimGamble_Settings.allowWashedUpHustler;
                            break;
                        case "Trickster":
                            shouldInclude = Options.RimGamble_Settings.allowTrickster;
                            break;
                        default:
                            shouldInclude = true; // Allow unknown forms by default
                            break;
                    }
                }
                // For other behavior types, include all for now
                
                if (shouldInclude)
                {
                    filtered.Add(def);
                }
            }
            
            return filtered;
        }

        public static void GetTravelingGamblerSpecifics(Map map, ref TravelingGamblerFormKindDef form, ref TravelingGamblerAggressiveDef aggressive, ref TravelingGamblerRejectionDef rejection, ref TravelingGamblerAcceptanceDef acceptance)
        {
            float combatPoints = StorytellerUtility.DefaultThreatPointsNow(map);

            if (form == null)
            {
                var allForms = DefDatabase<TravelingGamblerFormKindDef>.AllDefsListForReading;
                var availableForms = allForms.Where(f => IsFormEnabled(f)).ToList();
                
                if (availableForms.Count == 0)
                {
                    // Fallback: re-enable all forms if none are available
                    availableForms = allForms.ToList();
                }
                form = availableForms.RandomElementByWeight((TravelingGamblerFormKindDef x) => x.Weight);
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
            var allForms = DefDatabase<TravelingGamblerFormKindDef>.AllDefsListForReading;
            var availableForms = allForms.Where(f => IsFormEnabled(f)).ToList();
            
            if (availableForms.Count == 0)
            {
                // Fallback: re-enable all forms if none are available
                availableForms = allForms.ToList();
            }
            
            TravelingGamblerFormKindDef travelingGamblerFormKindDef = availableForms.RandomElementByWeight((TravelingGamblerFormKindDef x) => x.Weight);
            requires.AddRange(travelingGamblerFormKindDef.Requires);
            exclude.AddRange(travelingGamblerFormKindDef.Excludes);
            TravelingGamblerAggressiveDef aggressive = GetRandom(DefDatabase<TravelingGamblerAggressiveDef>.AllDefsListForReading, combatPoints, requires, exclude);
            TravelingGamblerRejectionDef rejection = GetRandom(DefDatabase<TravelingGamblerRejectionDef>.AllDefsListForReading, combatPoints, requires, exclude);
            TravelingGamblerAcceptanceDef acceptance = GetRandom(DefDatabase<TravelingGamblerAcceptanceDef>.AllDefsListForReading, combatPoints, requires, exclude);
            if (acceptance == null)
            {
                Log.Warning("[RimGamble] Failed to find valid acceptance behavior. Defaulting to first available.");
                 acceptance = DefDatabase<TravelingGamblerAcceptanceDef>.AllDefsListForReading.RandomElement();
            }
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

            if (!RCellFinder.TryFindRandomPawnEntryCell(out var result, map, CellFinder.EdgeRoadChance_Friendly, allowFogged: false, (IntVec3 cell) => map.reachability.CanReachMapEdge(cell, TraverseParms.For(TraverseMode.PassDoors))))
            {
                return null;
            }

            GenSpawn.Spawn(pawn, result, map);
            
            if (!RCellFinder.TryFindRandomSpotJustOutsideColony(pawn, out var result2))
            {
                return null;
            }

            Lord lord = LordMaker.MakeNewLord(pawn.Faction, new LordJob_CreepJoiner(result2, pawn), map);
            lord.AddPawn(pawn);
            
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

            // Filter defs based on mod settings
            var filteredDefs = FilterDefsBySettings(defs);
            if (filteredDefs.Count == 0)
            {
                // Fallback: allow all behaviors if none are available
                filteredDefs = defs;
            }

            T val;
            if (requires.Empty() && exclude.Empty())
            {
                val = filteredDefs.Where((T x) => combatPoints >= x.MinCombatPoints && x.CanOccurRandomly).RandomElementByWeight((T x) => x.Weight);
            }
            else
            {
                bool flag = false;
                foreach (T def in filteredDefs)
                {
                    if (!(combatPoints < def.MinCombatPoints) && def.CanOccurRandomly && requires.Contains(def))
                    {
                        flag = true;
                        break;
                    }
                }

                if (flag)
                {
                    foreach (T def2 in filteredDefs)
                    {
                        if (!(combatPoints < def2.MinCombatPoints) && def2.CanOccurRandomly && requires.Contains(def2))
                        {
                            temp.Add(def2);
                        }
                    }
                }
                else
                {
                    foreach (T def3 in filteredDefs)
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
                        temp.RemoveAt(num);
                    }
                }

                if (temp.Empty())
                {
                    string text = filteredDefs.Select((T x) => x.label).ToCommaList();
                    string text2 = requires.Select((TravelingGamblerBaseDef x) => x.label).ToCommaList();
                    string text3 = exclude.Select((TravelingGamblerBaseDef x) => x.label).ToCommaList();
                    Log.Error($"Attempted to create travelinggambler but blacklist removed all possible whitelist; combatPoints = {combatPoints}, defs = ({text}), whitelist = ({text2}), blacklist = ({text3})");
                    val = filteredDefs.RandomElementByWeight((T x) => x.Weight);
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
        
        private static bool IsFormEnabled(TravelingGamblerFormKindDef form)
        {
            switch (form.defName)
            {
                case "LuckyDrifter":
                    return Options.RimGamble_Settings.allowLuckyDrifter;
                case "RiskyCardshark":
                    return Options.RimGamble_Settings.allowRiskyCardshark;
                case "WashedUpHustler":
                    return Options.RimGamble_Settings.allowWashedUpHustler;
                case "Trickster":
                    return Options.RimGamble_Settings.allowTrickster;
                default:
                    return true; // Allow unknown forms by default
            }
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
