using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;

namespace LateNites
{
    public class ModEntry : Mod
    {
        private List<Vector2> seedShopCounterTiles;
        private List<Vector2> animalShopCounterTiles;
        private List<Vector2> carpentersShopCounterTiles;
        private List<Vector2> fishShopShopCounterTiles;
        private List<Vector2> museumCounterTiles;
        private List<Vector2> blacksmithCounterTiles;
        private Dictionary<String, NPC> npcRefs;
        private Dictionary<String, Vector2> doorLocations;

        private ITranslationHelper i18n;

        private bool setupFinished = false;

        public override void Entry(IModHelper helper)
        {
            // event raised each button press to check if a menu should be opened
            helper.Events.Input.ButtonPressed += OnButtonPressed;

            // initialize counter tiles locations and npc dictionary
            helper.Events.GameLoop.SaveLoaded += OnLoad;
            helper.Events.GameLoop.ReturnedToTitle += this.OnExit;

            // event raised each button press to check if a door has been clicked
            helper.Events.Input.ButtonPressed += this.OnDoorClick;

            #if DEBUG
                // each tick event, log tile coords
                //helper.Events.GameLoop.OneSecondUpdateTicked += this.LogTileLocation;
            #endif

            i18n = helper.Translation;
        }

        private void OnLoad(object Sender, EventArgs e)
        {
            // params reset
            seedShopCounterTiles = new List<Vector2>();
            animalShopCounterTiles = new List<Vector2>();
            carpentersShopCounterTiles = new List<Vector2>();
            fishShopShopCounterTiles = new List<Vector2>();
            museumCounterTiles = new List<Vector2>();
            blacksmithCounterTiles = new List<Vector2>();

            npcRefs = new Dictionary<string, NPC>();
            setupFinished = false;

            // params setup
            seedShopCounterTiles.Add(new Vector2(4f, 19f));
            seedShopCounterTiles.Add(new Vector2(5f, 19f));

            animalShopCounterTiles.Add(new Vector2(12f, 16f));
            animalShopCounterTiles.Add(new Vector2(13f, 16f));

            carpentersShopCounterTiles.Add(new Vector2(8f, 20f));

            fishShopShopCounterTiles.Add(new Vector2(4f, 6f));
            fishShopShopCounterTiles.Add(new Vector2(5f, 6f));
            fishShopShopCounterTiles.Add(new Vector2(6f, 6f));

            museumCounterTiles.Add(new Vector2(3f, 10f));
            museumCounterTiles.Add(new Vector2(3f, 9f));

            blacksmithCounterTiles.Add(new Vector2(3f, 15f));
            blacksmithCounterTiles.Add(new Vector2(4f, 15f));

            // setup special case doors
            InitializeDoorLocations();

            foreach (NPC npc in Utility.getAllCharacters())
            {
                switch (npc.Name)
                {
                    case "Pierre":
                    case "Robin":
                    case "Marnie":
                    case "Willy":
                    case "Gunther":
                    case "Clint":
                        npcRefs[npc.Name] = npc;
                        break;
                }
            }

            #if DEBUG
                foreach (var item in npcRefs)
                {
                    Monitor.Log(item.ToString(), LogLevel.Info);
                }
            #endif

            // done
            this.setupFinished = true;
        }

        private void InitializeDoorLocations()
        {
            doorLocations = new Dictionary<string, Vector2>();

            doorLocations.Add("seedShopDoor1", new Vector2(43, 57));
            doorLocations.Add("seedShopDoor2", new Vector2(44, 57));
        }

        private void OnExit(object Sender, EventArgs e)
        {
            this.setupFinished = false;
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            if (this.setupFinished)
                this.IsMenuOpened(IsDoorButton(e.Button));
        }

        private void OnDoorClick(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            String locationString = Game1.player.currentLocation.Name;
            Vector2 playerPosition = Game1.player.getTileLocation();

            if (locationString.Equals("Town") && doorLocations.ContainsValue(playerPosition))
            {
                // Pierre's Seed Shop Wednesday Edition
                if (Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth).Equals("Wed") && IsDoorButton(e.Button))
                {
                    Monitor.Log($"We are at the seed shop door on weds and button is clicked", LogLevel.Info);
                    Monitor.Log($"Warping farmer...", LogLevel.Info);
                    Rumble.rumble(0.15f, 200f);
                    Game1.player.completelyStopAnimatingOrDoingAction();
                    Game1.playSound("doorClose");
                    Game1.warpFarmer("SeedShop", 6, 29, false);
                }
            }
        }

        private bool IsDoorButton(SButton value)
        {
            return (value.IsActionButton() || value == SButton.MouseRight) ? true : false;
        }

        // log the current tile location each game tick for debugging purposes
        // and identifying future unlockable door tiles
        private void LogTileLocation(object Sender, EventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            if (this.setupFinished)
            {
                String locationString = Game1.player.currentLocation.Name;
                Vector2 playerPosition = Game1.player.getTileLocation();

                Monitor.Log($"Player position is X: {playerPosition.X}, Y: {playerPosition.Y}\n" +
                            $"Currently located within the {locationString}", LogLevel.Info);
            }
        }

        private bool IsMenuOpened(bool isActionKey)
        {
            // returns true if menu is opened, otherwise false

            String locationString = Game1.player.currentLocation.Name;
            Vector2 playerPosition = Game1.player.getTileLocation();
            int faceDirection = Game1.player.getFacingDirection();

            bool result = false; // default

            if (ShouldOpen(isActionKey, Game1.player.getFacingDirection(), locationString, playerPosition))
            {
                result = true;
                switch (locationString)
                {
                    case "SeedShop":
                        Game1.player.currentLocation.createQuestionDialogue(
                            i18n.Get("SeedShop_Menu"),
                            new Response[2]
                            {
                                new Response("Shop", i18n.Get("SeedShopMenu_Shop")),
                                new Response("Leave", i18n.Get("SeedShopMenu_Leave"))
                            },
                            delegate (Farmer who, string whichAnswer)
                            {
                                switch (whichAnswer)
                                {
                                    case "Shop":
                                        List<ISalable> shopMenuContent = new List<ISalable>();
                                        foreach (Item item in Utility.getShopStock(true))
                                            shopMenuContent.Add((ISalable)item);

                                        Game1.activeClickableMenu = (IClickableMenu)new ShopMenu(shopMenuContent, 0, "Pierre");
                                        break;
                                    case "Leave":
                                        // do nothing
                                        break;
                                    default:
                                        Monitor.Log($"invalid dialogue answer: {whichAnswer}", LogLevel.Info);
                                        break;
                                }
                            }
                        );
                        break;
                    case "AnimalShop":
                        Game1.player.currentLocation.createQuestionDialogue(
                            i18n.Get("AnimalShop_Menu"),
                            new Response[3]
                            {
                                new Response("Supplies", Game1.content.LoadString("Strings\\Locations:AnimalShop_Marnie_Supplies")),
                                new Response("Purchase", Game1.content.LoadString("Strings\\Locations:AnimalShop_Marnie_Animals")),
                                new Response("Leave", Game1.content.LoadString("Strings\\Locations:AnimalShop_Marnie_Leave"))
                            },
                            "Marnie"
                        );
                        break;
                    case "ScienceHouse":
                        if (Game1.player.daysUntilHouseUpgrade.Value < 0 && !Game1.getFarm().isThereABuildingUnderConstruction())
                        {
                            Response[] answerChoices;
                            if (Game1.player.HouseUpgradeLevel < 3)
                                answerChoices = new Response[4]
                                {
                                    new Response("Shop", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Shop")),
                                    new Response("Upgrade", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_UpgradeHouse")),
                                    new Response("Construct", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Construct")),
                                    new Response("Leave", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Leave"))
                                };
                            else
                                answerChoices = new Response[3]
                                {
                                    new Response("Shop", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Shop")),
                                    new Response("Construct", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Construct")),
                                    new Response("Leave", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Leave"))
                                };

                            Game1.player.currentLocation.createQuestionDialogue(i18n.Get("ScienceHouse_CarpenterMenu"), answerChoices, "carpenter");
                        }
                        else
                        {
                            Game1.activeClickableMenu = (IClickableMenu)new ShopMenu(Utility.getCarpenterStock(), 0, "Robin");
                        }
                        break;
                    case "FishShop":
                        Game1.player.currentLocation.createQuestionDialogue(
                            (SDate.Now() != new SDate(9, "spring")) ? i18n.Get("FishShop_Menu") : i18n.Get("FishShop_Menu_DocVisit"),
                            new Response[2]
                            {
                                new Response("Shop", i18n.Get("FishShopMenu_Shop")),
                                new Response("Leave", i18n.Get("FishShopMenu_Leave"))
                            },
                            delegate (Farmer who, string whichAnswer)
                            {
                                switch (whichAnswer)
                                {
                                    case "Shop":
                                        Game1.activeClickableMenu = (IClickableMenu)new ShopMenu(Utility.getFishShopStock(Game1.player), 0, "Willy");
                                        break;
                                    case "Leave":
                                        // do nothing
                                        break;
                                    default:
                                        Monitor.Log($"invalid dialogue answer: {whichAnswer}", LogLevel.Info);
                                        break;
                                }
                            }
                        );
                        break;
                    case "ArchaeologyHouse":
                        // does Gunther ever move from his post? b/c it's hard to test the self-service since he doesn't move
                        /*
                            Game1.player.currentLocation.createQuestionDialogue(i18n.Get("ArchaeologyHouse_Menu"),
                            new Response[3]
                            {
                                new Response("Donate", Game1.content.LoadString("Strings\\Locations:ArchaeologyHouse_Menu_Donate")),
                                new Response("Collect Rewards", Game1.content.LoadString("Strings\\Locations:ArchaeologyHouse_Menu_CollectRewards")),
                                new Response("Leave", Game1.content.LoadString("Strings\\Locations:ArchaeologyHouse_Menu_Leave"))
                            },
                            "Gunther"
                        );
                        */
                        break;
                    case "Blacksmith":
                        if (Game1.player.toolBeingUpgraded.Value == null)
                        {
                            Response[] answerChoices;
                            if (IsGeodeInInventory())
                            {
                                answerChoices = new Response[4]
                                {
                                new Response("Shop", Game1.content.LoadString("Strings\\Locations:Blacksmith_Clint_Shop")),
                                new Response("Upgrade", Game1.content.LoadString("Strings\\Locations:Blacksmith_Clint_Upgrade")),
                                new Response("Process", Game1.content.LoadString("Strings\\Locations:Blacksmith_Clint_Geodes")),
                                new Response("Leave", Game1.content.LoadString("Strings\\Locations:Blacksmith_Clint_Leave"))
                                };
                            }
                            else
                            {
                                answerChoices = new Response[3]
                                {
                                new Response("Shop", Game1.content.LoadString("Strings\\Locations:Blacksmith_Clint_Shop")),
                                new Response("Upgrade", Game1.content.LoadString("Strings\\Locations:Blacksmith_Clint_Upgrade")),
                                new Response("Leave", Game1.content.LoadString("Strings\\Locations:Blacksmith_Clint_Leave"))
                                };
                            }
                            Game1.currentLocation.createQuestionDialogue("", answerChoices, "Blacksmith");
                        }
                        else
                        {
                            GiveToolIfDone();
                        }

                        break;
                    default:
                        Monitor.Log($"invalid location: {locationString}", LogLevel.Info);
                        break;
                }
            }

            return result;

        }

        // checks if the player has a geode in their inventory, returns true if they do
        private bool IsGeodeInInventory()
        {
            return (Game1.player.hasItemInInventory(535, 1) 
                || Game1.player.hasItemInInventory(536, 1) 
                || Game1.player.hasItemInInventory(537, 1) 
                || Game1.player.hasItemInInventory(749, 1));
        }

        // gives player upgraded tool if Clint has finished it, returns true on success
        private bool GiveToolIfDone()
        {
            NPC clint = npcRefs["Clint"];

            if (Game1.player.daysLeftForToolUpgrade.Value <= 0)
            {
                if (Game1.player.freeSpotsInInventory() > 0)
                {
                    // inventory has space, give player upgraded tool
                    Game1.player.holdUpItemThenMessage(Game1.player.toolBeingUpgraded.Value);
                    Game1.player.addItemToInventoryBool(Game1.player.toolBeingUpgraded.Value);
                    Game1.player.toolBeingUpgraded.Value = null;
                    return true;
                }

                // no space to put tool, so return false
                Game1.drawDialogue(clint, Game1.content.LoadString("Data\\ExtraDialogue:Clint_NoInventorySpace"));
                return false;
            }

            // NPC Clint is still working on tool
            Game1.drawDialogue(clint, Game1.content.LoadString("Data\\ExtraDialogue:Clint_StillWorking", 
                Game1.player.toolBeingUpgraded.Value.DisplayName));
            return false;
        }

        private bool ShouldOpen(bool isActionKey, int facingDirection, String locationString, Vector2 playerLocation)
        {
            bool result = false;
            if (Game1.activeClickableMenu == null && isActionKey && facingDirection == 3) // somehow SMAPI doesn't provide enum for facing directions?
            {
                // TODO: refactor this part to avoid hard coded tile locations
                switch (locationString)
                {
                    case "SeedShop":
                        result = this.seedShopCounterTiles.Contains(playerLocation) && (npcRefs["Pierre"].currentLocation.Name != locationString || !npcRefs["Pierre"].getTileLocation().Equals(new Vector2(4f, 17f)));
                        break;
                    case "AnimalShop":
                        result = this.animalShopCounterTiles.Contains(playerLocation) && (npcRefs["Marnie"].currentLocation.Name != locationString || !npcRefs["Marnie"].getTileLocation().Equals(new Vector2(12f, 14f)));
                        break;
                    case "ScienceHouse":
                        result = this.carpentersShopCounterTiles.Contains(playerLocation) && (npcRefs["Robin"].currentLocation.Name != locationString || !npcRefs["Robin"].getTileLocation().Equals(new Vector2(8f, 18f)));
                        break;
                    case "FishShop":
                        result = this.fishShopShopCounterTiles.Contains(playerLocation) && (npcRefs["Willy"].currentLocation.Name != locationString || !(npcRefs["Willy"].getTileLocation().Y < 6f));
                        break;
                    case "ArchaeologyHouse":
                        result = this.museumCounterTiles.Contains(playerLocation) && (npcRefs["Gunther"].currentLocation.Name != locationString || !npcRefs["Gunther"].getTileLocation().Equals(new Vector2()));
                        break;
                    case "Blacksmith":
                        result = this.blacksmithCounterTiles.Contains(playerLocation) && (npcRefs["Clint"].currentLocation.Name != locationString || !npcRefs["Clint"].getTileLocation().Equals(new Vector2(3f, 13f)));
                        break;
                    default:
                        break;
                }
            }

            return result;
        }
    }
}
