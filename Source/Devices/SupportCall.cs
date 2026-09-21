using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using RimStation.Consts;

namespace RimStation14.SupportCall
{
    public class CompProperties_RS_SupportCallChip : CompProperties
    {
        public List<PawnKindDef> pawnKinds = new List<PawnKindDef>();
        public string factionDefName = RimStation_Consts.FactionId;
        public int contractTime = 40000;
        public string brifiengSoundDef = "RS_Briefing_NukeOps";
        public string supportDesc = "unknown";

        public CompProperties_RS_SupportCallChip()
        {
            compClass = typeof(RS_CompSupportCallChip);
        }
    }

    public class RS_CompSupportCallChip : ThingComp
    {
        public CompProperties_RS_SupportCallChip Props => (CompProperties_RS_SupportCallChip)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            yield return new Command_Action
            {
                defaultLabel = "Activate support call",
                defaultDesc = "Call supprot team which will come in drop pods to hel your faction.",
                icon = null,
                action = delegate
                {
                    StartTargeting();
                }
            };
        }

        private void StartTargeting()
        {
            var targetParams = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetItems = false,
                canTargetPawns = false,
                canTargetBuildings = false
            };

            Find.Targeter.BeginTargeting(targetParams, delegate (LocalTargetInfo target)
            {
                var cell = target.Cell;
                var map = parent.Map;

                if (map != null && cell.InBounds(map))
                {
                    TryCallSupport(cell, map);
                }
            });
        }

        private void TryCallSupport(IntVec3 cell, Map map)
        {
            if (Props.pawnKinds.Count == 0)
                return;

            var faction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named(Props.factionDefName));
            
            if (faction == null)
                return;

            var pawnsToDrop = new List<Thing>();

            foreach (var kind in Props.pawnKinds)
            {
                if (kind != null)
                {
                    var supporter = PawnGenerator.GeneratePawn(kind, faction);
                    pawnsToDrop.Add(supporter);

                    var lordJob = new RS_LordJob_SupportAndLeave(Props.contractTime, cell);
                    var lord = LordMaker.MakeNewLord(faction, lordJob, map);

                    lord.AddPawn(supporter); 
                }
            }

            DropPodUtility.DropThingsNear(cell, map, pawnsToDrop, leaveSlag: true);

            Messages.Message("The landing force is about to arrive.", new TargetInfo(cell, map), MessageTypeDefOf.PositiveEvent, true);

            parent.SplitOff(1).Destroy();

            var sound = SoundDef.Named(Props.brifiengSoundDef);
            sound?.PlayOneShotOnCamera();
        }
        public override string CompInspectStringExtra()
        {
            return $"Squad size: {Props.pawnKinds.Count}\nSquad desc: {Props.supportDesc}";
        }
    }

    public class RS_LordJob_SupportAndLeave : LordJob
    {
        private IntVec3 fallback;
        private int leaveTick;
        private int durationTicks = 40000;

        public RS_LordJob_SupportAndLeave()
        { }

        public RS_LordJob_SupportAndLeave(int durationTicks, IntVec3 fallbackNew)
        {
            this.durationTicks = durationTicks;
            leaveTick = Find.TickManager.TicksGame + durationTicks;
            fallback = fallbackNew;
        }

        public override StateGraph CreateGraph()
        {
            var graph = new StateGraph();

            if (leaveTick <= 0)
                leaveTick = Find.TickManager.TicksGame + durationTicks;

            var hunt = new LordToil_HuntEnemies(fallback);
            graph.AddToil(hunt);
            graph.StartingToil = hunt;

            var leave = new LordToil_ExitMap(LocomotionUrgency.Jog, canDig: false);
            graph.AddToil(leave);

            int ticksLeft = leaveTick - Find.TickManager.TicksGame;
            if (ticksLeft < 1) ticksLeft = 1;

            var transition = new Transition(hunt, leave);
            transition.AddTrigger(new Trigger_TicksPassed(ticksLeft));
            transition.AddPreAction(new TransitionAction_Message("The reinforcements you ordered are leaving..."));
            transition.AddPreAction(new TransitionAction_EndAllJobs());
            graph.AddTransition(transition);

            return graph;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref leaveTick, "leaveTick", 0);
            Scribe_Values.Look(ref durationTicks, "durationTicks", 15000);
        }
    }
}