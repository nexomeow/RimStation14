using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using RimStation.Consts;

namespace RimStation.BlackMarketShop
{
    public class Window_RimStation_Shop : Window
    {
        public override Vector2 InitialSize => new Vector2(750f, 600f);

        private Vector2 scrollPosition = Vector2.zero;
        private ShopTab currentTab = ShopTab.Weapons;

        private struct ShopItem
        {
            public List<ThingDef> thingsDef;
            public int price;

            public ShopItem(List<string> defNames, int price)
            {
                var list = new List<ThingDef>();
                foreach (string defName in defNames)
                {
                    list.Add(ThingDef.Named(defName));
                }

                thingsDef = list;
                this.price = price;
            }
        }

        public Window_RimStation_Shop()
        {
            forcePause = false;
            doCloseX = true;
            closeOnAccept = false;
            closeOnCancel = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, 200f, 35f), "Uplink");
            Text.Font = GameFont.Small;

            var targetFaction = Find.FactionManager.AllFactions
                .FirstOrDefault(f => f.def.defName == RimStation_Consts.FactionId);

            if (targetFaction == null)
            {
                var textWidth = 600f;
                var textHeight = 250f;
                var centerRect = new Rect((inRect.width - textWidth) / 2f, (inRect.height - textHeight) / 2f, textWidth, textHeight);

                Text.Anchor = TextAnchor.UpperCenter;
                Text.Font = GameFont.Medium; 
                Widgets.Label(centerRect, "This rim world does not have access to syndicate black market.\n\nP.S. Recreate your world if you wanna play with syndie faction and blackmarket.");

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            var isHostile = targetFaction.HostileTo(Faction.OfPlayer);
            if (isHostile)
            {
                var textWidth = 600f;
                var textHeight = 250f;
                var centerRect = new Rect((inRect.width - textWidth) / 2f, (inRect.height - textHeight) / 2f, textWidth, textHeight);

                Text.Anchor = TextAnchor.UpperCenter;
                Text.Font = GameFont.Medium; 
                Widgets.Label(centerRect, "You cannot purchase anything here, as your faction's relationship with the Syndicate has become hostile.");

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            var tech = ResearchProjectDef.Named(RimStation_Consts.TechId);
            if (!tech.IsFinished)
            {
                var textWidth = 600f;
                var textHeight = 250f;
                var centerRect = new Rect((inRect.width - textWidth) / 2f, (inRect.height - textHeight) / 2f, textWidth, textHeight);

                Text.Anchor = TextAnchor.UpperCenter;
                Text.Font = GameFont.Medium; 
                Widgets.Label(centerRect, "You do not have access to the Syndicate black market. To start trading within this network, you need to acquire a special Redspace Cube and then research the corresponding technology. Once you have done so, the Syndicate liaison officer will notify you that access has been granted.");

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            var playerCurrencyBalance = CountPlayerCurrency();
            var balanceText = $"Balance: {playerCurrencyBalance} redspace cubes.";
            var balanceWidth = Text.CalcSize(balanceText).x;
            
            Widgets.Label(new Rect(inRect.width - balanceWidth - 35f, 5f, balanceWidth, 30f), balanceText);

            Widgets.DrawLineHorizontal(0f, 40f, inRect.width);

            var tabsList = new List<TabRecord>();
            foreach (ShopTab tabType in Enum.GetValues(typeof(ShopTab)))
            {
                ShopTab targetTab = tabType;
                tabsList.Add(new TabRecord(
                    GetTabLabel(targetTab),
                    () => { currentTab = targetTab; scrollPosition = Vector2.zero; },
                    currentTab == targetTab
                ));
            }

            var tabsRect = new Rect(0f, 75f, inRect.width, inRect.height - 135f);
            TabDrawer.DrawTabs(tabsRect, tabsList);
            Widgets.DrawMenuSection(tabsRect);

            var allShopItems = DefDatabase<BlackMarketListing>.AllDefsListForReading;
            var filteredItems = new List<BlackMarketListing>();
            
            foreach (var item in allShopItems)
            {
                if (item.category == currentTab)
                {
                    filteredItems.Add(item);
                }
            }

            var outRect = new Rect(tabsRect.x + 10f, tabsRect.y + 15f, tabsRect.width - 20f, tabsRect.height - 30f);
            var viewRect = new Rect(0f, 0f, outRect.width - 16f, filteredItems.Count * 45f);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            var currentY = 0f;
            foreach (var item in filteredItems)
            {
                var list = new List<ThingDef>();
                foreach (var def in item.itemDefNames)
                {
                    var realThingDef = ThingDef.Named(def);
                    if (realThingDef == null) continue;
                }

                var previewThingDef = ThingDef.Named(item.previewDefName);
                if (previewThingDef == null) continue;

                var rowRect = new Rect(0f, currentY, viewRect.width, 40f);
                Widgets.DrawHighlightIfMouseover(rowRect);

                if (!string.IsNullOrEmpty(item.listingDesc))
                {
                    TooltipHandler.TipRegion(rowRect, item.listingDesc.ToString());
                }

                var iconRect = new Rect(5f, currentY + 2f, 36f, 36f);
                Widgets.ThingIcon(iconRect, previewThingDef);

                var labelRect = new Rect(50f, currentY + 10f, 250f, 25f);
                var displayLabel = item.listingName == string.Empty ? $"{previewThingDef.label}" : $"{item.listingName}";
                Widgets.Label(labelRect, displayLabel);

                var priceRect = new Rect(outRect.width - 220f, currentY + 10f, 100f, 25f);
                Widgets.Label(priceRect, $"{item.price}");

                var buttonRect = new Rect(viewRect.width - 105f, currentY + 5f, 100f, 30f);
                
                var isNotPaused = !Find.TickManager.Paused;
                
                var buyButtonText = isNotPaused ? "Buy" : "Pause!";

                if (Widgets.ButtonText(buttonRect, buyButtonText, active: true))
                {
                    if (!isNotPaused)
                    {
                        Messages.Message("You cannot buy anything while pause is active.", MessageTypeDefOf.RejectInput, false);
                        continue; 
                    }

                    if (playerCurrencyBalance < item.price)
                    {
                        Messages.Message("Not enough redspace cubes.", MessageTypeDefOf.RejectInput, false);
                        continue; 
                    }

                    var legacyItem = new ShopItem(item.itemDefNames, item.price);
                    TryPurchaseItem(legacyItem);
                }

                currentY += 45f;
            }

            Widgets.EndScrollView();

            var closeButtonRect = new Rect(inRect.width / 2f - 60f, inRect.height - 40f, 120f, 30f);
            if (Widgets.ButtonText(closeButtonRect, "Close"))
            {
                Close();
            }
        }

        private int CountPlayerCurrency()
        {
            var map = Find.CurrentMap;
            if (map == null) return 0;

            var currencyDef = ThingDef.Named(RimStation_Consts.CurrencyId);
            return map.resourceCounter.GetCount(currencyDef);
        }

        private void TryPurchaseItem(ShopItem item)
        {
            var map = Find.CurrentMap;
            var dropCell = DropCellFinder.TradeDropSpot(map);

            var currencyDef = ThingDef.Named(RimStation_Consts.CurrencyId);

            var amountToTake = item.price;
            var currencies = map.listerThings.ThingsOfDef(currencyDef);
            
            for (var i = currencies.Count - 1; i >= 0; i--)
            {
                var coinStack = currencies[i];
                if (coinStack.Position.Fogged(map)) continue;

                if (coinStack.stackCount >= amountToTake)
                {
                    coinStack.SplitOff(amountToTake).Destroy();
                    break;
                }
                else
                {
                    amountToTake -= coinStack.stackCount;
                    coinStack.Destroy();
                }
            }

            var transporterInfo = new ActiveTransporterInfo
            {
                leaveSlag = false
            };

            foreach (var thing in item.thingsDef)
            {
                var purchasedThing = ThingMaker.MakeThing(thing);
                transporterInfo.innerContainer.TryAdd(purchasedThing, true);
            }

            DropPodUtility.MakeDropPodAt(dropCell, map, transporterInfo);

            Messages.Message($"Succesfuly buyed. Order was sended in drop pod.", MessageTypeDefOf.PositiveEvent);
        }

        private string GetTabLabel(ShopTab tab)
        {
            switch (tab)
            {
                case ShopTab.Weapons: return "Weapons";
                case ShopTab.Gear: return "Equipment";
                case ShopTab.Implants: return "Implants";
                case ShopTab.Sabotage: return "Sabotage";
                case ShopTab.Support: return "Support";
                case ShopTab.Chemicals: return "Chemicals";
                case ShopTab.Resources: return "Resources";
                case ShopTab.Trinkets: return "Trinkets";
                case ShopTab.Misc: return "Misc";
                default: return tab.ToString();
            }
        }
    }
}
