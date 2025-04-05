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

        private const int CheckAcceptanceInterval = 15000;

        private bool useAlternativeLetter;

        private bool IsAccepted;

        public TravelingGamblerFormKindDef form;

        public TravelingGamblerRejectionDef rejection;

        public TravelingGamblerAggressiveDef aggressive;

        public TravelingGamblerAcceptanceDef acceptance;

        public Pawn speaker;

        public Quest quest;

        public string spokeToSignal;

        public int timeoutAt;

        public int joinedTick;

        private int acceptanceTriggersAt;

        private bool triggeredRejection;

        private bool triggeredAggressive;

        private bool triggeredAcceptance;

        private bool hasLeft;

        private bool entryLordEnded;

        private int triggerPrisonerRejectionAt;

        private int triggerPrisonerAggressiveAt;

        private int canTriggerAcceptanceAfter;

        private bool duplicated;

        private BaseTravelingGamblerWorker rejectionWorkerInt;

        private BaseTravelingGamblerWorker aggressiveWorkerInt;

        private BaseTravelingGamblerAcceptanceWorker acceptanceWorkerInt;

        private BaseTravelingGamblerWorker RejectionWorker => GetWorker(rejection.workerType, ref rejectionWorkerInt);

        private BaseTravelingGamblerWorker AggressiveWorker => GetWorker(aggressive.workerType, ref aggressiveWorkerInt);

        private BaseTravelingGamblerAcceptanceWorker AcceptanceWorker => GetWorker(acceptance.workerType, ref acceptanceWorkerInt);

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

        public void AcceptTravleingGambler()
        {
            IsAccepted = true;
        }

        public void SetAlternativeLetter(bool setAlternativeLetter)
        {
            if (setAlternativeLetter)
            {
                useAlternativeLetter = true;
            }
            else
            {
                useAlternativeLetter = false;
            }
        }

        public void Notify_Created()
        {
            ResolveGraphics();
            AggressiveWorker?.OnCreated();
            RejectionWorker?.OnCreated();
            AcceptanceWorker?.OnCreated();

            if (acceptance != null && acceptance.triggerMinDays != FloatRange.Zero && canTriggerAcceptanceAfter == 0)
            {
                canTriggerAcceptanceAfter = GenTicks.TicksGame + (int)(acceptance.triggerMinDays.RandomInRange * 60000f);
            }

            if (acceptance != null && acceptance.triggersAfterDays != FloatRange.Zero && acceptanceTriggersAt == 0)
            {
                acceptanceTriggersAt = GenTicks.TicksGame + (int)(acceptance.triggersAfterDays.RandomInRange * 60000f);
            }
        }

        public void Tick()
        {
            if (IsAccepted)
            {
                AcceptedTick();
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

            if (triggerPrisonerAggressiveAt == 0 && triggerPrisonerRejectionAt == 0 && acceptance.canOccurWhenImprisoned && Pawn.IsPrisonerOfColony && Pawn.IsHashIntervalTick(15000))
            {
                CheckAcceptanceOccurs();
            }
        }

        private void AcceptedTick()
        {
            if(Pawn.IsHashIntervalTick(15000))
            {
                CheckAcceptanceOccurs();
            }
        }

        private void CheckAcceptanceOccurs()
        {
            if (acceptance != null && (acceptance.repeats || !triggeredAcceptance) && (acceptance.canOccurWhenImprisoned || (!Pawn.IsPrisoner && !Pawn.IsSlave)) && (!(acceptance.triggerMinDays != FloatRange.Zero) || GenTicks.TicksGame >= canTriggerAcceptanceAfter) && (acceptance.canOccurWhileDowned || !Pawn.Downed) && Pawn.SpawnedOrAnyParentSpawned && (!acceptance.mustBeConscious || Pawn.health.capacities.CanBeAwake))
            {
                bool num = acceptance.triggerMtbDays != 0f && Rand.MTBEventOccurs(acceptance.triggerMtbDays, 60000f, 15000f);
                bool flag = acceptanceTriggersAt != 0 && GenTicks.TicksGame >= acceptanceTriggersAt;
                if ((num || flag) && (AcceptanceWorker == null || AcceptanceWorker.CanOccur()))
                {
                    DoAcceptance();
                }
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

            if (canTriggerAcceptanceAfter != 0 && !triggeredAcceptance && GenTicks.TicksGame < canTriggerAcceptanceAfter && (acceptance.canOccurWhenImprisoned || !Pawn.IsPrisoner))
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Unlock acceptance trigger",
                    action = delegate
                    {
                        canTriggerAcceptanceAfter = GenTicks.TicksGame;
                    }
                };
            }
            else if ((acceptanceTriggersAt != 0 || acceptance.triggerMtbDays != 0f) && (!triggeredAcceptance || acceptance.repeats) && (acceptance.canOccurWhenImprisoned || !Pawn.IsPrisoner))
            {
                Command_Action command_Action = new Command_Action
                {
                    defaultLabel = "DEV: Trigger timed downside",
                    action = DoAcceptance
                };
                BaseTravelingGamblerAcceptanceWorker acceptanceWorker = AcceptanceWorker;
                if (acceptanceWorker != null && !acceptanceWorker.CanOccur())
                {
                    command_Action.Disable("Worker is blocking this trigger from occuring.");
                }

                yield return command_Action;
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
            if (acceptance != null)
            {
                string text = "DEV Acceptance: " + acceptance?.label;
                if (canTriggerAcceptanceAfter != 0 && !triggeredAcceptance && GenTicks.TicksGame < canTriggerAcceptanceAfter && (acceptance.canOccurWhenImprisoned || !Pawn.IsPrisoner))
                {
                    text = text + " (can after: " + (canTriggerAcceptanceAfter - GenTicks.TicksGame).ToStringTicksToPeriod() + ")";
                }
                else if (acceptanceTriggersAt != 0 && !triggeredAcceptance && (acceptance.canOccurWhenImprisoned || !Pawn.IsPrisoner))
                {
                    text = text + " (triggers: " + (acceptanceTriggersAt - GenTicks.TicksGame).ToStringTicksToPeriod() + ")";
                }
                stringBuilder.AppendLine(text);
            }
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

        public void DoAcceptance()
        {
            if (Disabled)
            {
                return;
            }

            ClearLord();
            triggeredAcceptance = true;
            if (acceptance.triggerMinDays != FloatRange.Zero)
            {
                canTriggerAcceptanceAfter = GenTicks.TicksGame + (int)(acceptance.triggerMinDays.RandomInRange * 60000f);
            }

            List<TargetInfo> list = new List<TargetInfo> { Pawn };
            List<NamedArgument> list2 = new List<NamedArgument> { Pawn.Named("PAWN") };
            //foreach (HediffDef hediff2 in acceptance.hediffs)
            //{
            //    if (Pawn.health.hediffSet.TryGetHediff(hediff2, out var hediff) && hediff.TryGetComp<HediffComp_ReplaceHediff>(out var comp))
            //    {
            //        comp.Trigger();
            //    }
            //}

            AcceptanceWorker?.DoResponse(list, list2);
            if (acceptance.hasLetter)
            {
                TaggedString label = acceptance.letterLabel.Formatted(list2);
                TaggedString text = acceptance.letterDesc.Formatted(list2);
                Find.LetterStack.ReceiveLetter(label, text, acceptance.letterDef, list);
            }
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

                if (aggressive.hasAlternativeLetter && useAlternativeLetter)
                {
                    SetAlternativeLetter(false);
                    TaggedString label = aggressive.letterLabel.Formatted(list2.ToArray());
                    TaggedString text = aggressive.alternativeLetterDesc.Formatted(list2.ToArray());
                    Find.LetterStack.ReceiveLetter(label, text, aggressive.letterDef, list);
                }
                else if (aggressive.hasLetter)
                {
                    TaggedString label = aggressive.letterLabel.Formatted(list2.ToArray());
                    TaggedString text = aggressive.letterDesc.Formatted(list2.ToArray());
                    Find.LetterStack.ReceiveLetter(label, text, aggressive.letterDef, list);
                }
            }
        }

        // Do Functions

        public void DoLeave()
        {
            if (!Disabled && !hasLeft)
            {
                ClearLord();
                TravelingGambler_DoFunctions.DoLeave(Pawn, ref hasLeft);
            }
        }

        public void DoRaid(Faction faction)
        {
            TravelingGambler_DoFunctions.DoRaid(Pawn, faction);
        }

        public void DoFight()
        {
            TravelingGambler_DoFunctions.DoFight(Pawn);
        }

        public int DoTheft(int totalPlayerSilver)
        {
            return TravelingGambler_DoFunctions.DoTheft(Pawn, totalPlayerSilver);
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
            Scribe_Defs.Look(ref acceptance, "acceptance");
            Scribe_Values.Look(ref timeoutAt, "timeoutAt", 0);
            Scribe_Values.Look(ref duplicated, "duplicated", defaultValue: false);
            Scribe_Values.Look(ref joinedTick, "joinedTick", 0);
            Scribe_Values.Look(ref IsAccepted, "IsAccepted", defaultValue: false);
            Scribe_Values.Look(ref useAlternativeLetter, "useAlternativeLetter", defaultValue: false);
            Scribe_Values.Look(ref spokeToSignal, "spokeToSignal");
            Scribe_Values.Look(ref triggeredAggressive, "triggeredAggressive", defaultValue: false);
            Scribe_Values.Look(ref triggeredRejection, "triggeredRejection", defaultValue: false);
            Scribe_Values.Look(ref triggeredAcceptance, "triggeredAcceptance", defaultValue: false);
            Scribe_Values.Look(ref hasLeft, "hasLeft", defaultValue: false);
            Scribe_Values.Look(ref canTriggerAcceptanceAfter, "canTriggerDownsideAfter", 0);
            Scribe_Values.Look(ref acceptanceTriggersAt, "downsideTriggersAt", 0);
            Scribe_Values.Look(ref entryLordEnded, "entryLordEnded", defaultValue: false);
            Scribe_References.Look(ref quest, "quest");
            Scribe_References.Look(ref speaker, "speaker");
        }
    }
}
