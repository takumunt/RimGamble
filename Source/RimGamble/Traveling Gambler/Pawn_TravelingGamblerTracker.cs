using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimGamble
{
    public class Pawn_TravelingGamblerTracker : IExposable
    {
        private static readonly IntRange ReleaseRejectionDelayTicks = new IntRange(60, 180);

        public TravelingGamblerFormKindDef form;

        public TravelingGamblerRejectionDef rejection;

        public TravelingGamblerAggressiveDef aggressive;

        public Pawn speaker;

        public Quest quest;

        public string spokeToSignal;

        public int timeoutAt;

        public int joinedTick;

        private bool triggeredRejection;

        private bool triggeredAggressive;

        private bool hasLeft;

        private bool entryLordEnded;

        private int triggerPrisonerRejectionAt;

        private int triggerPrisonerAggressiveAt;

        private bool duplicated;

        private BaseTravelingGamblerWorker rejectionWorkerInt;

        private BaseTravelingGamblerWorker aggressiveWorkerInt;

        private BaseTravelingGamblerWorker RejectionWorker => GetWorker(rejection.workerType, ref rejectionWorkerInt);

        private BaseTravelingGamblerWorker AggressiveWorker => GetWorker(aggressive.workerType, ref aggressiveWorkerInt);

        public bool IsOnEntryLord
        {
            get
            {
                if (entryLordEnded)
                {
                    return false;
                }

                Lord lord = Pawn.GetLord();
                if (lord == null)
                {
                    return false;
                }

                return lord.LordJob is LordJob_CreepJoiner;
            }
        }

        public bool Disabled
        {
            get
            {
                if (!duplicated)
                {
                    return Pawn.everLostEgo;
                }

                return true;
            }
        }

        public bool CanTriggerAggressive
        {
            get
            {
                if (!Disabled)
                {
                    return !triggeredAggressive;
                }

                return false;
            }
        }

        public Pawn Pawn { get; }

        public Pawn_TravelingGamblerTracker()
        {
        }

        public Pawn_TravelingGamblerTracker(Pawn pawn)
        {
            Pawn = pawn;
        }
        public void Notify_Created()
        {
            ResolveGraphics();
            AggressiveWorker?.OnCreated();
            RejectionWorker?.OnCreated();
        }

        public void Tick()
        {
            if (Pawn == null || Disabled)
            {
                return;
            }

            else
            {
                CheckTriggersTick();
            }
        }

        private void CheckTriggersTick()
        {
            if (triggerPrisonerAggressiveAt != 0 && GenTicks.TicksGame >= triggerPrisonerAggressiveAt && !triggeredAggressive)
            {
                DoAggressive();
            }
            else if (triggerPrisonerRejectionAt != 0 && GenTicks.TicksGame >= triggerPrisonerRejectionAt && !triggeredRejection)
            {
                DoRejection();
            }
        }

        public void Notify_ChangedFaction()
        {
            if (Pawn.IsColonist)
            {
                ClearLord();
                joinedTick = GenTicks.TicksGame;
            }
        }

        public void Notify_DuplicatedFrom(Pawn _)
        {
            duplicated = true;
        }

        public void Notify_Arrested(bool succeeded)
        {
            if (!duplicated)
            {
                if (!succeeded)
                {
                    DoAggressive();
                }
                else
                {
                    ClearLord();
                }
            }
        }

        public void Notify_Released()
        {
            if (!duplicated)
            {
                triggerPrisonerRejectionAt = GenTicks.TicksGame + ReleaseRejectionDelayTicks.RandomInRange;
            }
        }

        public void Notify_PrisonBreakout()
        {
            if (!duplicated)
            {
                triggerPrisonerAggressiveAt = GenTicks.TicksGame + ReleaseRejectionDelayTicks.RandomInRange;
            }
        }

        public IEnumerable<Gizmo> GetGizmos()
        {
            if (!DebugSettings.ShowDevGizmos)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "DEV: Do aggressive",
                action = DoAggressive
            };
            yield return new Command_Action
            {
                defaultLabel = "DEV: Do rejection",
                action = DoRejection
            };
        }

        public string GetInspectString()
        {
            if (!DebugSettings.godMode || Disabled)
            {
                return " ";
            }

            StringBuilder stringBuilder = new StringBuilder();
            if (rejection != null)
            {
                stringBuilder.AppendLine("DEV Rejection: " + rejection?.label);
            }
            if (aggressive != null)
            {
                stringBuilder.Append("DEV Aggressive: " + aggressive?.label);
            }
            
            return stringBuilder.ToString().TrimEnd();
        }

        public void Notify_TravelingGamblerSpokenTo(Pawn speaker)
        {
            if (!Disabled)
            {
                this.speaker = speaker;
                Find.SignalManager.SendSignal(new Signal(spokeToSignal));
            }
        }

        public void Notify_TravelingGamblerAttacked(Pawn instigatorPawn)
        {
            if (!Disabled && instigatorPawn != null && instigatorPawn.CurJobDef != JobDefOf.SocialFight)
            {
                DoAggressive();
            }
        }

        public void Notify_TravelingGamblerKilled()
        {
            if (!Disabled && AggressiveWorker.CanOccurOnDeath)
            {
                DoAggressive();
            }
        }

        public void Notify_TravelingGamblerRejected()
        {
            DoRejection();
        }

        public void DoRejection()
        {
            if (!Disabled && !triggeredRejection && !triggeredAggressive && !hasLeft)
            {
                triggeredRejection = true;
                ClearLord();
                List<TargetInfo> list = new List<TargetInfo> { Pawn };
                List<NamedArgument> list2 = new List<NamedArgument> { Pawn.Named("PAWN") };
                RejectionWorker?.DoResponse(list, list2);
                if (rejection.hasLetter)
                {
                    TaggedString label = rejection.letterLabel.Formatted(list2);
                    TaggedString text = rejection.letterDesc.Formatted(list2);
                    Find.LetterStack.ReceiveLetter(label, text, rejection.letterDef, list);
                }
            }
        }

        public void DoAggressive()
        {
            if (CanTriggerAggressive)
            {
                triggeredAggressive = true;
                ClearLord();
                List<TargetInfo> list = new List<TargetInfo> { Pawn };
                List<NamedArgument> list2 = new List<NamedArgument> { Pawn.Named("PAWN") };
                AggressiveWorker?.DoResponse(list, list2);
                if (aggressive.hasMessage)
                {
                    Messages.Message(aggressive.message.Formatted(list2), list, MessageTypeDefOf.NegativeEvent);
                }

                if (aggressive.hasLetter)
                {
                    TaggedString label = aggressive.letterLabel.Formatted(list2);
                    TaggedString text = aggressive.letterDesc.Formatted(list2);
                    Find.LetterStack.ReceiveLetter(label, text, aggressive.letterDef, list);
                }
            }
        }

        public void DoLeave()
        {
            if (!Disabled && !hasLeft)
            {
                ClearLord();
                if (Pawn.Faction != null && Pawn.Faction.IsPlayer)
                {
                    Pawn.SetFaction(null);
                }

                LordMaker.MakeNewLord(Pawn.Faction, new LordJob_ExitMapBest(LocomotionUrgency.Jog), Pawn.Map).AddPawn(Pawn);
                hasLeft = true;
            }
        }

        public void DoRaid(Faction faction)
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null || faction == null)
            {
                Log.Error("DoRaid failed: No valid map or faction.");
                return;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            parms.faction = faction;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            parms.points = StorytellerUtility.DefaultThreatPointsNow(map);

            if (parms.points < faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat))
            {
                parms.points = faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat);
            }

            // Directly trigger the raid without checking CanFireNow()
            if (!IncidentDefOf.RaidEnemy.Worker.TryExecute(parms))
            {
                Log.Warning("Raid incident execution failed!");
            }

            this.DoFight();
        }

        public void DoFight()
        {
            Pawn.guest.Recruitable = false;
            Pawn.GetLord()?.RemovePawn(Pawn);
            Pawn.SetFaction(null);
            Pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
            Pawn.mindState.enemyTarget = Find.AnyPlayerHomeMap.mapPawns.FreeColonists.RandomElement();
            Pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
        }

        public IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            if (!Disabled && IsOnEntryLord)
            {
                yield return (!selPawn.CanReach(Pawn as Pawn, PathEndMode.OnCell, Danger.Deadly)) ? new FloatMenuOption("CannotTalkTo".Translate(Pawn) + ": " + "NoPath".Translate().CapitalizeFirst(), null) : (selPawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking) ? FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TalkTo".Translate(Pawn), delegate
                {
                    Job job = JobMaker.MakeJob(RimGamble_DefOf.RimGamble_TalkTravelingGamblerJoiner, Pawn);
                    job.playerForced = true;
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }), selPawn, Pawn) : new FloatMenuOption("CannotTalkTo".Translate(Pawn) + ": " + "Incapable".Translate().CapitalizeFirst(), null));
            }
        }

        private void ResolveGraphics()
        {
            if (!form.forcedHeadTypes.NullOrEmpty())
            {
                Pawn.story.TryGetRandomHeadFromSet(form.forcedHeadTypes);
            }

            if (form.hairTagFilter != null)
            {
                Pawn.story.hairDef = PawnStyleItemChooser.RandomHairFor(Pawn);
            }

            if (form.beardTagFilter != null)
            {
                Pawn.style.beardDef = PawnStyleItemChooser.RandomBeardFor(Pawn);
            }

            if (form.hairColorOverride.HasValue)
            {
                Pawn.story.HairColor = form.hairColorOverride.Value;
            }

            Pawn.Drawer.renderer.SetAllGraphicsDirty();
        }

        private void ClearLord()
        {
            if (IsOnEntryLord)
            {
                entryLordEnded = true;
            }

            Pawn.GetLord()?.Notify_PawnLost(Pawn, PawnLostCondition.Undefined);
        }

        private T GetWorker<T>(Type type, ref T worker) where T : BaseTravelingGamblerWorker
        {
            if (worker != null)
            {
                return worker;
            }

            if (type == null)
            {
                return null;
            }

            try
            {
                worker = (T)Activator.CreateInstance(type);
                worker.Tracker = this;
            }
            catch (Exception ex)
            {
                Log.Error($"Error creating worker of type {type}: {ex}");
                return null;
            }

            return worker;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref form, "form");
            Scribe_Defs.Look(ref rejection, "rejection");
            Scribe_Defs.Look(ref aggressive, "aggressive");
            Scribe_Values.Look(ref timeoutAt, "timeoutAt", 0);
            Scribe_Values.Look(ref duplicated, "duplicated", defaultValue: false);
            Scribe_Values.Look(ref joinedTick, "joinedTick", 0);
            Scribe_Values.Look(ref spokeToSignal, "spokeToSignal");
            Scribe_Values.Look(ref triggeredAggressive, "triggeredAggressive", defaultValue: false);
            Scribe_Values.Look(ref triggeredRejection, "triggeredRejection", defaultValue: false);
            Scribe_Values.Look(ref hasLeft, "hasLeft", defaultValue: false);
            Scribe_Values.Look(ref entryLordEnded, "entryLordEnded", defaultValue: false);
            Scribe_References.Look(ref quest, "quest");
            Scribe_References.Look(ref speaker, "speaker");
        }
    }
}
