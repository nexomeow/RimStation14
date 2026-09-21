using System.Collections.Generic;
using Verse;
using RimWorld;

namespace RimStation.Unlocker
{
    public class CompProperties_RS_BiocodeUnlockerImmune : CompProperties
    {
        public CompProperties_RS_BiocodeUnlockerImmune()
        {
            compClass = typeof(RS_CompBiocodeUnlockerImmune);
        }
    }

    public class RS_CompBiocodeUnlockerImmune : ThingComp
    { }

    public class CompProperties_RS_BiocodeUnlocker : CompProperties
    {
        public float unlockProb = 0.5f;

        public CompProperties_RS_BiocodeUnlocker()
        {
            compClass = typeof(RS_CompBiocodeUnlocker);
        }
    }

    public class RS_CompBiocodeUnlocker : ThingComp
    {
        public CompProperties_RS_BiocodeUnlocker Props => (CompProperties_RS_BiocodeUnlocker)props;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            yield return new Command_Action
            {
                defaultLabel = "Use unlocker",
                defaultDesc = "Try to break biocode. In case of failure item will self-destruct.",
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
                canTargetItems = true,
                canTargetPawns = false,
                canTargetBuildings = false,
                mapObjectTargetsMustBeAutoAttackable = false
            };

            Find.Targeter.BeginTargeting(targetParams, delegate (LocalTargetInfo target)
            {
                var targetThing = target.Thing;

                if (targetThing != null && targetThing.TryGetComp<CompBiocodable>()
                    is CompBiocodable biocodeComp && biocodeComp.Biocoded)
                {
                    if (targetThing.HasComp<RS_CompBiocodeUnlockerImmune>())
                    {
                        Messages.Message("This item is immune to biocode unlocker.",
                            MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    TryUnlock(targetThing, biocodeComp);
                }
                else
                {
                    Messages.Message("This item does not biocoded and cannot be biocoded.",
                        MessageTypeDefOf.RejectInput, false);
                }
            });
        }

        private void TryUnlock(Thing item, CompBiocodable biocodeComp)
        {   
            if (!CompBiocodable.IsBiocoded(item)) return;

            if (Rand.Chance(Props.unlockProb))
            {
                biocodeComp.UnCode();

                Messages.Message($"Biocode was successfully deleted from {item.LabelShort}.",
                    item, MessageTypeDefOf.PositiveEvent, true);
            }
            else
            {
                Messages.Message($"Biocode deleting was failed. {item.LabelShort} was self-destroyed.",
                    item, MessageTypeDefOf.NegativeEvent, true);
                item.Destroy();
            }

            parent.SplitOff(1).Destroy();
        }
    }
}