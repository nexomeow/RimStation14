using System.Collections.Generic;
using Verse;

namespace RimStation.BlackMarketShop
{
    public class BlackMarketListing : Def
    {
        public List<string> itemDefNames;
        public string previewDefName;
        public string listingName = string.Empty;
        public string listingDesc = string.Empty;
        public int price;
        public int count = 1;
        public ShopTab category = ShopTab.Misc; 
    }

    public enum ShopTab
    {
        Misc,
        Weapons,
        Gear,
        Implants,
        Sabotage,
        Support,
        Chemicals,
        Resources,
        Trinkets
    }
}
