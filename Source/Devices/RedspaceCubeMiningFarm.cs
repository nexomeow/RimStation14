using Verse;
using Verse.Sound;
using RimWorld;
using RimStation.Consts;

namespace RimStation.MiningFarm
{
    public class CompProperties_RS_RedspaceMiningFarm : CompProperties
    {
        public int spawnIntervalTicks = 60000;
        public int stackCount = 1;
        public float ambientDurationSeconds = 88f;
        public string ambientDefName = "RS_Ambient_MiningFarm";

        public CompProperties_RS_RedspaceMiningFarm()
        {
            compClass = typeof(RS_CompRedspaceMiningAutomated);
        }
    }

    public class RS_CompRedspaceMiningAutomated : ThingComp
    {
        private int ticksUntilSpawn;
        private int ticksUntilAmbientEnds = 0;
        private int cachedAmbientDurationTicks = 0;

        public CompProperties_RS_RedspaceMiningFarm Props => (CompProperties_RS_RedspaceMiningFarm)props;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            ResetTicks();

            cachedAmbientDurationTicks = (int)(Props.ambientDurationSeconds * GenTicks.TicksPerRealSecond);
        }

        private void ResetTicks()
        {
            ticksUntilSpawn = Props.spawnIntervalTicks;
        }

        public override void CompTick()
        {
            base.CompTick();

            var powerComp = parent.TryGetComp<CompPowerTrader>();
            if (powerComp != null && !powerComp.PowerOn)
                return;

            UpdateAmbient();

            ticksUntilSpawn--;
            if (ticksUntilSpawn <= 0)
            {
                DoSpawn();
                ResetTicks();
            }
        }

        private void UpdateAmbient()
        {
            if (ticksUntilAmbientEnds > 0)
            {
                ticksUntilAmbientEnds--;
            }
            else
            {
                TryAmbient();
            }
        }

        private void DoSpawn()
        {
            if (parent.Spawned && parent.Map != null)
            {
                var thing = ThingMaker.MakeThing(ThingDef.Named(RimStation_Consts.CurrencyId));
                thing.stackCount = Props.stackCount;

                if (GenPlace.TryPlaceThing(thing, parent.Position, parent.Map, ThingPlaceMode.Near))
                {
                    // var sound = SoundDef.Named(""); 
                    // sound?.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
                }
            }
        }

        private void TryAmbient()
        {
            var sound = SoundDef.Named(Props.ambientDefName);
            if (sound == null) return;

            sound.PlayOneShot(new TargetInfo(parent.Position, parent.Map));

            ticksUntilAmbientEnds = cachedAmbientDurationTicks > 0 ? cachedAmbientDurationTicks : 1200;
        }

        public override string CompInspectStringExtra()
        {
            var powerComp = parent.TryGetComp<CompPowerTrader>();
            if (powerComp == null)
            {
                return "No power component so this building isn't work.";
            }

            var text = base.CompInspectStringExtra();
            var timeRemaining = ticksUntilSpawn.ToStringTicksToPeriod(allowSeconds: true);
            var thing = ThingDef.Named(RimStation_Consts.CurrencyId);
            var info = $"Next {thing.LabelCap} generation in {timeRemaining}";
            
            if (!text.NullOrEmpty())
            {
                return text + "\n" + info;
            }
            return info;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksUntilSpawn, "ticksUntilSpawn", 0);
            Scribe_Values.Look(ref ticksUntilAmbientEnds, "ticksUntilAmbientEnds", 0); 
        }
    }
}