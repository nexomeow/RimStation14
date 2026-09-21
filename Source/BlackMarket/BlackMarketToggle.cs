using Verse;
using RimWorld;

namespace RimStation.BlackMarketShop
{
    public class MainButtonWorker_ToggleShopWindow : MainButtonWorker_ToggleTab
    {
        public override void Activate()
        {
            if (Find.WindowStack.IsOpen(typeof(Window_RimStation_Shop)))
                Find.WindowStack.TryRemove(typeof(Window_RimStation_Shop));
            else
                Find.WindowStack.Add(new Window_RimStation_Shop());
        }
    }
}