using Assistant;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace RazorEnhanced
{
    /// <summary>
    /// The Item class represent a single in-game Item object. Examples of Item are: Swords, bags, bandages, reagents, clothing.
    /// While the Item.Serial is unique for each Item, Item.ItemID is the unique for the Item apparence, or image. Sometimes is also called ID or Graphics ID.
    /// Item can also be house foriture as well as decorative items on the ground, like lamp post and banches.
    /// However, for Item on the ground that cannot be picked up, they might be part of the world map, see Statics class.
    /// </summary>
    public class Item : UOEntity
    {
        private readonly Assistant.Item m_AssistantItem;

        internal Item(Assistant.Item item)
            : base(item.Serial)
        {
            base.TypeID = item.TypeID;
            base.Hue = item.Hue;
            base.Position = item.Position;
            m_AssistantItem = item;
        }

        public int Serial { get { return (int)base.Serial.Value; } }
        public ushort Hue { get { return Color; } }
        public ushort Color { get { return base.Hue; } }
        public ushort Graphics { get { return base.TypeID.Value; } }

        /// <summary>
        /// Represents the type of Item, usually unique for the Item image.  Sometime called ID or Graphics ID.
        /// </summary>
        public int ItemID { get { return base.TypeID.Value; } }

        //public int ItemID
        //{
        //    get
        //    {
        //        if (m_AssistantItem != null)
        //            return m_AssistantItem.TypeID.Value;
        //        else
        //            return 0;
        //    }
        //}

        /// <summary>
        /// Check if the Item already have been updated with all the properties. (need better documentation) 
        /// </summary>
        public bool Updated { get { return m_AssistantItem.Updated; } }

        /// <summary>
        /// True when the container was opened
        /// </summary>
        public bool ContainerOpened => m_AssistantItem.ContainerOpened;


        internal Assistant.Item AsAssistant { get { return m_AssistantItem; } }
        /// <summary>
        /// Read amount from item type object.
        /// </summary>
        public int Amount { get { return m_AssistantItem.Amount; } }

        /// <summary>
        /// Item direction. 
        /// </summary>
        public string Direction { get { return m_AssistantItem.Direction.ToString(); } }

        /// <summary>
        /// Item is Visible
        /// </summary>
        public bool Visible { get { return m_AssistantItem.Visible; } }

        /// <summary>
        /// Item is movable
        /// </summary>
        public bool Movable { get { return m_AssistantItem.Movable; } }

        /// <summary>
        /// Item name
        /// </summary>
        public string Name { get { return m_AssistantItem.Name; } }

        /// <summary>
        /// Gets the Layer, for werable items only. (need better documentation) 
        /// </summary>
        public string Layer { get { return m_AssistantItem.Layer.ToString(); } }

        /// <summary>
        /// Item light's direction (e.g. will affect corpse's facing direction)
        /// </summary>
        public byte Light { get { return m_AssistantItem.Light; } }

        /// <summary>
        /// Serial of the container which contains the object.
        /// </summary>
        public int Container
        {
            get
            {
                if (m_AssistantItem.Container is Assistant.Item)
                    return (m_AssistantItem.Container as Assistant.Item).Serial;
                else if (m_AssistantItem.Container is Assistant.Mobile)
                    return (m_AssistantItem.Container as Assistant.Mobile).Serial;
                else
                    return 0;
            }
        }

        /// <summary>
        /// Get serial of root container of item.
        /// </summary>
        public int RootContainer
        {
            get
            {
                if (m_AssistantItem.RootContainer is Assistant.Item)
                    return (m_AssistantItem.RootContainer as Assistant.Item).Serial;
                else if (m_AssistantItem.RootContainer is Assistant.Mobile)
                    return (m_AssistantItem.RootContainer as Assistant.Mobile).Serial;
                else
                    return 0;
            }
        }

        /// <summary>
        /// True: if Properties are updated - False: otherwise.
        /// </summary>
        public bool PropsUpdated { get { return m_AssistantItem.PropsUpdated; } }

        /// <summary>
        /// Check if an Item is contained in a container. Can be a Item or a Mobile (wear by).
        /// </summary>
        /// <param name="container">Item as container.</param>
        /// <returns>True: if is contained - False: otherwise.</returns>
        public bool IsChildOf(Item container, int maxDepth = 100)
        {
            return m_AssistantItem.IsChildOf(container.m_AssistantItem, maxDepth);
        }

        /// <param name="container">Mobile as container.</param>
        public bool IsChildOf(Mobile container, int maxDepth = 100)
        {
            return m_AssistantItem.IsChildOf(container, maxDepth);
        }

        /// <summary>
        /// Return the distance in number of tiles, from Item to Mobile.
        /// </summary>
        /// <param name="mob">Target as Mobile</param>
        /// <returns>Distance in number of tiles.</returns>
        public int DistanceTo(Mobile mob)
        {
            return Utility.Distance(Position.X, Position.Y, mob.Position.X, mob.Position.Y);
        }
        /// <param name="itm">Target as Item</param>
        public int DistanceTo(Item itm)
        {
            return Utility.Distance(Position.X, Position.Y, itm.Position.X, itm.Position.Y);
        }

        /// <summary>
        /// Contains the list of Item inside a container.
        /// </summary>
        public List<Item> Contains
        {
            get
            {
                List<Item> items = new();
                for (int i = 0; i < m_AssistantItem.Contains.Count; i++)
                {
                    RazorEnhanced.Item enhancedItem = new(m_AssistantItem.Contains[i]);
                    items.Add(enhancedItem);
                }
                return items;
            }
        }

        // possibly 4 bit x/y - 16x16?
        /// <summary>
        /// Returns the GridNum of the item. (need better documentation) 
        /// </summary>
        public byte GridNum { get { return m_AssistantItem.GridNum; } }

        /// <summary>
        /// True: if the item is on the ground - False: otherwise.
        /// </summary>
        public bool OnGround { get { return m_AssistantItem.OnGround; } }

        /// <summary>
        /// True: if the item is a container - False: otherwise.
        /// </summary>
        public bool IsContainer { get { return m_AssistantItem.IsContainer; } }

        /// <summary>
        /// True: if the item is a bag of sending - False: otherwise.
        /// </summary>
        public bool IsBagOfSending { get { return m_AssistantItem.IsBagOfSending; } }

        /// <summary>
        /// True: if the item is in the Player's backpack - False: otherwise.
        /// </summary>
        internal bool IsInBackpack
        { get { return m_AssistantItem.IsInBackpack; } }

        /// <summary>
        /// True: if the item is lootable - False: otherwise.
        /// </summary>
        internal bool IsLootableTarget
        { get { return m_AssistantItem.IsLootableTarget; } }

        /// <summary>
        /// True: if the item is in the Player's bank - False: otherwise.
        /// </summary>
        public bool IsInBank { get { return m_AssistantItem.IsInBank; } }

        /// <summary>
        /// True: if the item is a pouch - False: otherwise.
        /// </summary>

        public bool IsSearchable { get { return m_AssistantItem.IsSearchable; } }

        /// <summary>
        /// True: if the item is a corpse - False: otherwise.
        /// </summary>
        public bool IsCorpse { get { return m_AssistantItem.IsCorpse; } }

        /// <summary>
        /// -1 until corpse is checked, then # items in corpse. Used by looter to ignore empty corpses
        /// </summary>
        public int CorpseNumberItems
        {
            get { return m_AssistantItem.CorpseNumberItems; }
            set { m_AssistantItem.CorpseNumberItems = value; }
        }

        /// <summary>
        /// True: if the item is a door - False: otherwise.
        /// </summary>
        public bool IsDoor { get { return m_AssistantItem.IsDoor; } }

        /// <summary>
        /// True: For regualar items - False: for hair, beards, etc.
        /// </summary>
        public bool IsLootable { get { return m_AssistantItem.IsLootable; } }

        /// <summary>
        /// True: if the item is a resource (ore, sand, wood, stone, fish) - False: otherwise
        /// </summary>
        public bool IsResource { get { return m_AssistantItem.IsResource; } }

        /// <summary>
        /// True: if the item is a potion - False: otherwise.
        /// </summary>
        public bool IsPotion { get { return m_AssistantItem.IsPotion; } }


        /// <summary>
        /// True: if the item is a virtue shield - False: otherwise.
        /// </summary>
        public bool IsVirtueShield { get { return m_AssistantItem.IsVirtueShield; } }

        /// <summary>
        /// True: if the item is a 2-handed weapon - False: otherwise.
        /// </summary>
        public bool IsTwoHanded { get { return m_AssistantItem.IsTwoHanded; } }

        public override string ToString()
        {
            return m_AssistantItem.ToString();
        }

        /// <summary>
        /// @nodoc
        /// @experimental
        /// Price of a recently purchased item. (see Vendor class )
        /// </summary>
        public int Price { get { return m_AssistantItem.Price; } set { m_AssistantItem.Price = value; } }

        /// <summary>
        /// @nodoc
        /// @experimental
        /// Descrition of a recently purchased item. (see Vendor class )
        /// </summary>
        public string BuyDesc { get { return m_AssistantItem.BuyDesc; } }

        public Point3D GetWorldPosition()
        {
            Assistant.Point3D assistantPoint = m_AssistantItem.GetWorldPosition();
            RazorEnhanced.Point3D enhancedPoint = new(assistantPoint);
            return enhancedPoint;
        }

        internal Assistant.Layer AssistantLayer { get { return m_AssistantItem.Layer; } }

        /// <summary>
        /// Get the list of Properties of an Item.
        /// </summary>
        public List<Property> Properties
        {
            get
            {
                List<Property> properties = new();
                foreach (Assistant.ObjectPropertyList.OPLEntry entry in m_AssistantItem.ObjPropList.Content)
                {
                    Property property = new(entry);
                    properties.Add(property);
                }
                return properties;
            }
        }

        /// <summary>
        /// Get the weight of a item. (0: no weight)
        /// </summary>
        public int Weight
        {
            get
            {
                List<Property> properties = Properties;
                foreach (Property property in properties)
                {
                    int number = property.Number;
                    string args = property.Args;
                    switch (number)
                    {
                        case 1072788:
                            return 1;       // Peso 1 se cliloc � 1072788
                        case 1072789:
                            try
                            {
                                return Convert.ToInt32(args);  // Ritorna valore peso
                            }
                            catch
                            {
                                return 1;  // errore di conversione torna peso  1
                            }
                    }
                }
                return 0;  // item senza peso
            }
        }

        /// <summary>
        /// Get the current durability of an Item. (0: no durability)
        /// </summary>
        public int Durability
        {
            get
            {
                List<Property> properties = Properties;
                foreach (Property property in properties)
                {
                    int number = property.Number;
                    if (number != 1060639)
                        continue;

                    string Text = property.Args;
                    int step = 0;
                    string Durability = String.Empty;

                    for (int i = 0; i <= Text.Length - 1; i++)
                    {
                        if (step == 0)
                            if (Char.IsNumber(Text[i]))
                            {
                                Durability += Text[i];
                                step = 1;
                                i++;
                            }
                        if (step == 1)
                            if (Char.IsNumber(Text[i]))
                            {
                                Durability += Text[i];
                            }
                            else
                                step = 2;
                    }

                    try
                    {
                        return Convert.ToInt32(Durability);  // Ritorna valore Dur
                    }
                    catch
                    {
                        return 0;  // errore di conversione torna 0
                    }
                }
                return 0; // item senza Dur
            }
        }

        /// <summary>
        /// Get the maximum durability of an Item. (0: no durability)
        /// </summary>
        public int MaxDurability
        {
            get
            {
                List<Property> properties = Properties;
                foreach (Property property in properties)
                {
                    int number = property.Number;
                    if (number != 1060639)
                        continue;

                    string Text = property.Args;
                    string TempMaxDurability = String.Empty;
                    int step = 0;
                    string MaxDurability = String.Empty;
                    for (int y = Text.Length - 1; y != 0; y--)
                    {
                        if (step == 0)
                            if (Char.IsNumber(Text[y]))
                            {
                                TempMaxDurability += Text[y];
                                step = 1;
                                y--;
                            }
                        if (step == 1)
                            if (Char.IsNumber(Text[y]))
                            {
                                TempMaxDurability += Text[y];
                            }
                            else
                                step = 2;
                    }
                    for (int i = TempMaxDurability.Length - 1; i > -1; i--)
                    {
                        MaxDurability += TempMaxDurability[i];
                    }
                    try
                    {
                        return Convert.ToInt32(MaxDurability);  // Ritorna valore maxdur
                    }
                    catch
                    {
                        return 0;  // errore di conversione torna 0
                    }
                }
                return 0; // item senza maxdur
            }
        }

        /// <summary>
        /// Get the in-game image on an Item as Bitmap object.
        /// See MSDN: https://docs.microsoft.com/dotnet/api/system.drawing.bitmap
        /// </summary>
        public System.Drawing.Bitmap Image
        {
            get
            {
                return Items.GetImage(m_AssistantItem.TypeID, m_AssistantItem.Hue);
            }
        }
    }

    /// <summary>
    /// The Items class provides a wide range of functions to search and interact with Items.
    /// </summary>
    public class Items
    {

        /// <summary>
        /// Open a container at a specific location on the screen
        /// </summary>
        /// <param name="bag">Container as Item object.</param>
        /// <param name="x">x location to open at</param>
        /// <param name="y">y location to open at</param>
        public static void OpenContainerAt(Item bag, int x, int y)
        {
            if (bag == null || (!bag.IsCorpse && !bag.IsContainer))
                return;
            if (Client.IsOSI)
            {
                Misc.NextContPosition(x, y);
                RazorEnhanced.Items.UseItem(bag);
            }
            else
            {
                CUO.OpenContainerAt(bag, x, y);
            }
        }


        internal static HashSet<ushort> IgnoreIDs = new();
        /// <summary>
        /// Used to ignore specific types. Be careful as you wont see things you ignore, 
        /// and could result in a mobile being able to kill you without you seeing it
        /// </summary>
        public static void IgnoreTypes(IronPython.Runtime.PythonList itemIdList)
        {
            IgnoreIDs.Clear();
            foreach (object itemid in itemIdList)
            {
                ushort itemID = Convert.ToUInt16(itemid);
                IgnoreIDs.Add(itemID);
            }
        }

        /// <summary>
        /// NOTE: This is from an internal razor table and can be changed based on your server!
        /// 
        /// Returns a pair of string values (Primary Ability, Secondary Ability) 
        /// for the supplied item ID. 
        /// "Invalid", "Invalid" for items not in the internal table
        /// </summary>
        public static (string, string) GetWeaponAbility(int itemId)
        {
            var primary = SpecialMoves.GetPrimaryAbility(itemId);
            var secondary = SpecialMoves.GetSecondaryAbility(itemId);

            return (primary.ToString(), secondary.ToString());
        }


        /// <summary>
        /// Open a container an wait for the Items to load, for a maximum amount of time.
        /// </summary>
        /// <param name="bag">Container as Item object.</param>
        /// <param name="delay">Maximum wait, in milliseconds.</param>
        public static bool WaitForContents(Item bag, int delay) // Delay in MS
        {
            if (bag == null || (!bag.IsCorpse && !bag.IsContainer))
                return false;

            RazorEnhanced.Items.UseItem(bag);

            if (bag.Updated)
                return true;

            int subdelay = delay;
            while (!bag.Updated)
            {
                Thread.Sleep(2);
                subdelay -= 2;
                if (subdelay <= 0)
                    return false;
            }

            return true;
        }

        /// <param name="bag_serial">Container as Item serial.</param>
        /// <param name="delay">max time to wait for contents</param>
        public static bool WaitForContents(int bag_serial, int delay) // Delay in MS
        {
            Item bag = FindBySerial(bag_serial);
            if (bag != null)
                return WaitForContents(bag, delay);

            return false;
        }

        private static readonly Dictionary<uint, int> m_HuedItems = new();

        internal static int Hued(uint serial)
        {
            if (m_HuedItems.ContainsKey(serial))
                return m_HuedItems[serial];
            return -1;
        }


        /// <summary>
        /// Change/override the Color of an Item, the change affects only Player client. The change is not persistent.
        /// If the color is -1 or unspecified, the color of the item is restored.
        /// </summary>
        /// <param name="serial">Serial of the Item.</param>
        /// <param name="color">Color as number. (default: -1, reset original color)</param>
        ///
        public static void SetColor(int serial, int color = -1)
        {
            //Reset original color
            if (color == -1)
            {
                try
                {
                    m_HuedItems.Remove((uint)serial);
                }
                catch (Exception) { }

                return;
            }

            // store the setting even if item is not exist yet
            m_HuedItems[(uint)serial] = color;
            RazorEnhanced.Item i = RazorEnhanced.Items.FindBySerial(serial);
            Assistant.Item assistantItem = Assistant.World.FindItem((Assistant.Serial)((uint)serial));
            if (assistantItem == null)
                return;

            if (i.Container == World.Player.Serial)
            {
                Assistant.Client.Instance.SendToClient(new EquipmentItem(assistantItem, (ushort)color, World.Player.Serial));
                return;
            }

            // if not worn Apply color for valid flag
            assistantItem.Hue = (ushort)color;
            if (i.Container == 0)
            {
                if (Engine.UsePostSAChanges)
                    Assistant.Client.Instance.SendToClient(new SAWorldItem(assistantItem));
                else
                    Assistant.Client.Instance.SendToClient(new WorldItem(assistantItem));
            }
            else
                Assistant.Client.Instance.SendToClient(new ContainerItem(assistantItem, true));
        }

        /// <summary>
        /// @nodoc: Method ranamed to SetColor, to be removed.
        /// </summary>
        public static void Color(int serial, int color = -1)
        {
            SetColor(serial, color);
        }

        /// <summary>
        /// Use the Dyes on a Dyeing Tub and select the color via color picker, using dedicated packets. 
        /// Need to specify the dyes, the dye tube and the color to use.
        /// </summary>
        /// <param name="dyes">Dyes as Item object.</param>
        /// <param name="dyeingTub">Dyeing Tub as Item object.</param>
        /// <param name="color">Color to choose.</param>
        public static void ChangeDyeingTubColor(Item dyes, Item dyeingTub, int color)
        {
            Items.UseItem(dyes);
            if (Target.WaitForTarget(1000))
            {
                var cb = new HueEntry.HueEntryCallback(
                    (serial, iid, hue) =>
                    {
                        HueEntry.Callback = null;
                        Assistant.Client.Instance.SendToServer(new HuePicker(serial, iid, (ushort)color));
                    }
                );
                HueEntry.Callback = cb;
                Target.TargetExecute(dyeingTub);

                // Dalamar: Callback AutoCleanup
                // If the selected object doesn't prompt for a Color Picker (any other object, including some cusom dyeing tub) the callback would remain pending, waiting for a packet which will never arrive.
                // Eventually, the first time a player try to dye something, the callback finally fire and apply the last color tried.
                // As the packet response usually arrives very fast, we will safely assume the following:
                //    if after X seconds the original callback is still waiting for a packet, probably something went wrong and the callback gets removed ( probably the packet will never arrive )
                var cleanup = new Thread(() =>
                {
                    Thread.Sleep(2000);
                    if (HueEntry.Callback == cb) { HueEntry.Callback = null; }
                });
                cleanup.Start();

            }
        }

        /// <summary>
        /// The Items.Filter class is used to store options to filter the global Item list.
        /// Often used in combination with Items.ApplyFilter.
        /// </summary>
        public class Filter
        {
            /// <summary>
            /// True: The filter is used - False: Return all Item. ( default: True, active )
            /// </summary>
            public bool Enabled = true;

            /// <summary>
            /// Limit the search to a list of Serials of Item to find. (ex: 0x0406EFCA )
            /// Supports .Add() and .AddRange()
            /// </summary>
            public List<int> Serials = new();

            /// <summary>
            /// Limit the search to a list of Grapichs ID (see: Item.ItemID ) 
            /// Supports .Add() and .AddRange()
            /// </summary>
            public List<int> Graphics = new();

            /// <summary>
            /// Limit the search by name of the Item.
            /// </summary>
            public string Name = String.Empty;

            /// <summary>
            /// Limit the search to a list of Colors.
            /// Supports .Add() and .AddRange()
            /// </summary>
            public List<int> Hues = new();

            /// <summary>
            /// Limit the search by distance, to Items which are at least RangeMin tiles away from the Player. ( default: -1, any Item )
            /// </summary>
            public double RangeMin = -1;
            /// <summary>
            /// Limit the search by distance, to Items which are at most RangeMax tiles away from the Player. ( default: -1, any Item )
            /// </summary>
            public double RangeMax = -1;
            /// <summary>
            /// Limit the search by height, to Items which are at least ZRangeMin coordinates away from the Player. ( default: -1, any Item )
            /// </summary>
            public double ZRangeMin = -1;
            /// <summary>
            /// Limit the search by height, to Items which are at most ZRangeMax coordinates away from the Player. ( default: -1, any Item )
            /// </summary>
            public double ZRangeMax = -1;

            /// <summary>
            /// Limit the search to only Movable Items. ( default: -1, any Item )
            /// </summary>
            public int Movable = -1;
            /// <summary>
            /// Limit the search to only Multi Items. ( default: -1, any Item )
            /// </summary>
            public int Multi = -1;
            /// <summary>
            /// Exclude from the search Items which are currently on the global Ignore List. ( default: False, any Item )
            /// </summary>
            public bool CheckIgnoreObject = false;
            /// <summary>
            /// Limit the search to the wearable Items by Layer.
            /// Supports .Add() and .AddRange()
            /// 
            /// Layers:
            ///     RightHand
            ///     LeftHand
            ///     Shoes
            ///     Pants
            ///     Shirt
            ///     Head
            ///     Gloves
            ///     Ring
            ///     Neck
            ///     Waist
            ///     InnerTorso
            ///     Bracelet
            ///     MiddleTorso
            ///     Earrings
            ///     Arms
            ///     Cloak
            ///     OuterTorso
            ///     OuterLegs
            ///     InnerLegs
            ///     Talisman
            /// </summary>
            public List<string> Layers = new();
            /// <summary>
            /// Limit the search to the Items on the ground. (default: -1, any Item)
            /// </summary>
            public int OnGround = -1;
            /// <summary>
            /// Limit the search to the corpses on the ground. (default: -1, any Item)
            /// </summary>
            public int IsCorpse = -1;
            /// <summary>
            /// Limit the search to the Items which are also containers. (default: -1: any Item)
            /// </summary>
            public int IsContainer = -1;
            /// <summary>
            /// Limit the search to the doors. (default: -1: any Item)
            /// </summary>
            public int IsDoor = -1;

            public Filter()
            {
            }

            internal virtual bool InFilteredItems(Assistant.Item item)
            {
                if (Name != String.Empty)
                {
                    Regex rgx = new(Name, RegexOptions.IgnoreCase);
                    // should probably use prop[0]
                    if (!rgx.IsMatch(item.Name))
                        return false;
                }

                if (Graphics.Count > 0)
                {
                    if (!Graphics.Contains(item.TypeID.Value))
                        return false;
                }

                if (Hues.Count > 0)
                {
                    if (!Hues.Contains(item.Hue))
                        return false;
                }

                if (RangeMin > -1)
                {
                    if (Utility.Distance(World.Player.Position.X, World.Player.Position.Y, item.Position.X, item.Position.Y) < RangeMin)
                        return false;
                }

                if (RangeMax > -1)
                {
                    if (Utility.Distance(World.Player.Position.X, World.Player.Position.Y, item.Position.X, item.Position.Y) > RangeMax)
                        return false;
                }
                if (ZRangeMin > -1)
                {
                    if (Math.Abs(Math.Abs(item.Position.Z) - Math.Abs(World.Player.Position.Z)) < ZRangeMin)
                        return false;
                }

                if (ZRangeMax > -1)
                {
                    if (Math.Abs(Math.Abs(item.Position.Z) - Math.Abs(World.Player.Position.Z)) > ZRangeMax)
                        return false;
                }


                if (Movable >= 0)
                {
                    if (!item.Movable == (Movable > 0))
                        return false;
                }

                if (Multi >= 0)
                {
                    if (!item.IsMulti == (Multi > 0))
                        return false;
                }


                if (Layers.Count > 0)
                {
                    List<Assistant.Layer> list = new();

                    foreach (string text in Layers)
                    {
                        Enum.TryParse<Layer>(text, out Layer l);
                        if (l != Assistant.Layer.Invalid)
                        {
                            list.Add(l);
                        }
                    }
                    if (!list.Contains(item.Layer))
                        return false;
                }

                if (OnGround != -1)
                {
                    if (!item.OnGround == Convert.ToBoolean(OnGround))
                        return false;
                }

                if (IsContainer != -1)
                {
                    if (!item.IsContainer == Convert.ToBoolean(IsContainer))
                        return false;
                }

                if (IsCorpse != -1)
                {
                    if (!item.IsCorpse == Convert.ToBoolean(IsCorpse))
                        return false;
                }

                if (IsDoor != -1)
                {
                    if (!item.IsDoor == Convert.ToBoolean(IsDoor))
                        return false;
                }

                if (CheckIgnoreObject)
                {
                    if (Misc.CheckIgnoreObject(item.Serial) == true)
                        return false;
                }


                return true;
            }
        }
        public class AutoLootFilter : Filter
        {
            internal override bool InFilteredItems(Assistant.Item item)
            {
                // only autoloot corpses
                if (item.IsCorpse == false)
                    return false;
                // dont look in empty corpses
                if (item.CorpseNumberItems == 0)
                    return false;
                // corpses dont move 
                if (item.Movable == true)
                    return false;
                // corpses are on ground
                if (!item.OnGround)
                    return false;
                // loot distance is limited
                return (Utility.Distance(World.Player.Position.X, World.Player.Position.Y, item.Position.X, item.Position.Y) <= RangeMax);
            }
        }

        /// <summary>
        /// Filter the global list of Items according to the options specified by the filter ( see: Items.Filter ).
        /// </summary>
        /// <param name="filter">A filter object.</param>
        /// <returns>the list of Items respectinf the filter criteria.</returns>
        public static List<Item> ApplyFilter(Filter filter)
        {

            List<RazorEnhanced.Item> result = new();
            try
            {
                foreach (var entry in World.Items)
                {
                    if (filter.InFilteredItems(entry.Value))
                        result.Add(new RazorEnhanced.Item(entry.Value));
                }
            }
            catch { }

            return result;
        }

        /// <summary>
        /// Select a single Item from a list by some criteria: Distance, Amount, Durability or Randomly
        /// </summary>
        /// <param name="items">List of Item.</param>
        /// <param name="selector">
        ///     Nearest 
        ///     Farthest 
        ///     Less 
        ///     Most 
        ///     Weakest 
        ///     Strongest
        ///     Random
        /// </param>
        /// <returns>The selected item.</returns>
        public static Item Select(List<Item> items, string selector)
        {
            Item result = null;

            if (items.Count <= 0)
                return null;

            switch (selector)
            {
                case "Random":
                    result = items[Utility.Random(items.Count)];
                    break;

                case "Nearest":
                    Item nearest = items[0];
                    if (nearest != null)
                    {
                        double minDist = Utility.Distance(Player.Position.X, Player.Position.Y, nearest.Position.X, nearest.Position.Y);

                        foreach (Item t in items)
                        {
                            if (t == null)
                                continue;

                            double dist = Utility.Distance(Player.Position.X, Player.Position.Y, t.Position.X, t.Position.Y);

                            if (!(dist < minDist))
                                continue;

                            nearest = t;
                            minDist = dist;
                        }
                        result = nearest;
                    }
                    break;

                case "Farthest":
                    Item farthest = items[0];
                    if (farthest != null)
                    {
                        double maxDist = Utility.Distance(Player.Position.X, Player.Position.Y, farthest.Position.X, farthest.Position.Y);

                        foreach (Item t in items)
                        {
                            if (t == null)
                                continue;

                            double dist = Utility.Distance(Player.Position.X, Player.Position.Y, t.Position.X, t.Position.Y);
                            if (dist > maxDist)
                            {
                                farthest = t;
                                maxDist = dist;
                            }
                        }
                        result = farthest;
                    }
                    break;

                case "Less":
                    Item least = items[0];
                    if (least != null)
                    {
                        int minAmount = least.Amount;
                        foreach (Item t in items)
                        {
                            if (t == null)
                                continue;

                            int amount = t.Amount;
                            if (amount < minAmount)
                            {
                                least = t;
                                minAmount = amount;
                            }
                        }
                        result = least;
                    }
                    break;

                case "Most":
                    Item most = items[0];
                    if (most != null)
                    {
                        int maxAmount = most.Amount;
                        foreach (Item t in items)
                        {
                            if (t == null)
                                continue;

                            int amount = t.Amount;

                            if (amount <= maxAmount)
                                continue;

                            most = t;
                            maxAmount = amount;
                        }
                        result = most;
                    }
                    break;

                case "Weakest":
                    Item weakest = items[0];
                    if (weakest != null)
                    {
                        int minDur = weakest.Durability;
                        foreach (Item t in items)
                        {
                            if (t == null)
                                continue;

                            int dur = t.Durability;

                            if (dur >= minDur)
                                continue;

                            weakest = t;
                            minDur = dur;
                        }
                        result = weakest;
                    }
                    break;

                case "Strongest":
                    Item strongest = items[0];
                    if (strongest != null)
                    {
                        int maxDur = strongest.Durability;
                        foreach (Item t in items)
                        {
                            if (t == null)
                                continue;

                            int dur = t.Durability;

                            if (dur <= maxDur)
                                continue;
                            strongest = t;
                            maxDur = dur;
                        }
                        result = strongest;
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// Search for a specific Item by using it Serial
        /// </summary>
        /// <param name="serial">Serial of the Item.</param>
        /// <returns>Item object if found, or null if not found.</returns>
        public static Item FindBySerial(int serial)
        {
            Assistant.Item assistantItem = Assistant.World.FindItem((Assistant.Serial)((uint)serial));
            if (assistantItem == null)
                return null;
            else
            {
                RazorEnhanced.Item enhancedItem = new(assistantItem);
                return enhancedItem;
            }
        }

        /// <summary>
        /// Lift an Item and hold it in-hand. ( see: Items.DropFromHand )
        /// </summary>
        /// <param name="item">Item object to Lift.</param>
        /// <param name="amount">Amount to lift. (0: the whole stack)</param>
        public static void Lift(Item item, int amount)
        {
            if (item == null)
            {
                Scripts.SendMessageScriptError("Script Error: Move: Source Item  not found");
                return;
            }
            if (amount == 0)
            {
                Assistant.Client.Instance.SendToServerWait(new LiftRequest(item.Serial, item.Amount));
            }
            else
            {
                if (item.Amount < amount)
                {
                    amount = item.Amount;
                }
                Assistant.Client.Instance.SendToServerWait(new LiftRequest(item.Serial, amount));
            }
        }

        /// <summary>
        /// Drop into a bag an Item currently held in-hand. ( see: Items.Lift )
        /// </summary>
        /// <param name="item">Item object to drop.</param>
        /// <param name="container">Target container.</param>
        public static void DropFromHand(Item item, Item container)
        {
            if (item == null)
            {
                Scripts.SendMessageScriptError("Script Error: Move: Source Item  not found");
                return;
            }
            if (container == null)
            {
                Scripts.SendMessageScriptError("Script Error: Move: Destination Item not found");
                return;
            }
            if (!container.IsContainer)
            {
                Scripts.SendMessageScriptError("Script Error: Move: Destination Item is not a container");
                return;
            }
            Assistant.Client.Instance.SendToServerWait(new DropRequest(item.Serial, Assistant.Point3D.MinusOne, container.Serial));
        }

        /// <summary>
        /// Move an Item to a destination, which can be an Item or a Mobile.
        /// </summary>
        /// <param name="source">Serial or Item of the Item to move.</param>
        /// <param name="destination">Serial, Mobile or Item as destination.</param>
        /// <param name="amount">Amount to move (-1: the whole stack)</param>
        /// <param name="x">Optional: X coordinate inside the container.</param>
        /// <param name="y">Optional: Y coordinate inside the container.</param>
        public static void Move(int source, int destination, int amount, int x, int y)
        {
            Assistant.Item bag = Assistant.World.FindItem(destination);
            Assistant.Item item = Assistant.World.FindItem(source);
            Assistant.Mobile mbag = null;

            int serialdestination = 0;
            bool isMobile = false;
            bool onLocation = false;
            if (item == null)
            {
                Scripts.SendMessageScriptError("Script Error: Move: Source Item  not found");
                return;
            }

            if (bag != null)
            {
                serialdestination = bag.Serial;
            }
            else
            {
                mbag = Assistant.World.FindMobile(destination);
                if (mbag != null)
                {
                    isMobile = true;
                    serialdestination = mbag.Serial;
                }
            }

            if (serialdestination == 0)
            {
                Scripts.SendMessageScriptError("Script Error: Move: " + item.Name + " to Destination not found 0x" + destination.ToString("X4"));
                return;
            }

            Assistant.Point3D loc = Assistant.Point3D.MinusOne;
            if (x != -1 && y != -1)
            {
                onLocation = true;
                loc = new Assistant.Point3D(x, y, 0);
            }

            int newamount;
            // calcolo amount
            if (amount == 0)
            {
                if (item.Amount == 0)
                    newamount = 1;
                newamount = item.Amount;
            }
            else
            {
                if (item.Amount < amount)
                    newamount = item.Amount;
                else
                    newamount = amount;
            }

            if (isMobile)
                Assistant.DragDropManager.DragDrop(item, newamount, mbag.Serial);
            else if (onLocation)
                Assistant.DragDropManager.DragDrop(item, newamount, bag, loc);
            else
                Assistant.DragDropManager.DragDrop(item, newamount, bag);
        }

        public static void Move(Item source, Mobile destination, int amount)
        {
            Move(source.Serial, destination.Serial, amount, -1, -1);
        }

        public static void Move(int source, Mobile destination, int amount)
        {
            Move(source, destination.Serial, amount, -1, -1);
        }

        public static void Move(Item source, int destination, int amount)
        {
            Move(source.Serial, destination, amount, -1, -1);
        }

        public static void Move(int source, Item destination, int amount)
        {
            Move(source, destination.Serial, amount, -1, -1);
        }

        public static void Move(Item source, Item destination, int amount)
        {
            Move(source.Serial, destination.Serial, amount, -1, -1);
        }

        public static void Move(int source, int destination, int amount)
        {
            Move(source, destination, amount, -1, -1);
        }

        public static void Move(Item source, Mobile destination, int amount, int x, int y)
        {
            Move(source.Serial, destination.Serial, amount, x, y);
        }

        public static void Move(int source, Mobile destination, int amount, int x, int y)
        {
            Move(source, destination.Serial, amount, x, y);
        }

        public static void Move(Item source, int destination, int amount, int x, int y)
        {
            Move(source.Serial, destination, amount, x, y);
        }

        public static void Move(int source, Item destination, int amount, int x, int y)
        {
            Move(source, destination.Serial, amount, x, y);
        }

        public static void Move(Item source, Item destination, int amount, int x, int y)
        {
            Move(source.Serial, destination.Serial, amount, x, y);
        }




        /// <summary>
        /// Move an Item on the ground to a specific location.
        /// </summary>
        /// <param name="source">Serial or Item to move.</param>
        /// <param name="amount">Amount of Items to move (0: the whole stack )</param>
        /// <param name="x">X world coordinates.</param>
        /// <param name="y">Y world coordinates.</param>
        /// <param name="z">Z world coordinates.</param>
        public static void MoveOnGround(int source, int amount, int x, int y, int z)
        {
            Assistant.Item item = Assistant.World.FindItem(source);

            if (item == null)
            {
                Scripts.SendMessageScriptError("Script Error: MoveOnGround: Source Item  not found");
                return;
            }

            Assistant.Point3D loc = new(x, y, z);

            int amounttodrop = amount;
            if ((item.Amount < amount) || (amount == 0))
                amounttodrop = item.Amount;

            Assistant.DragDropManager.DragDrop(item, loc, amounttodrop);
            //Assistant.Client.Instance.SendToServerWait(new LiftRequest(item.Serial, amounttodrop));
            //Assistant.Client.Instance.SendToServerWait(new DropRequest(item.Serial, loc, Assistant.Serial.MinusOne));
        }


        public static void MoveOnGround(Item source, int amount, int x, int y, int z)
        {
            MoveOnGround(source.Serial, amount, x, y, z);
        }

        /// <summary>
        /// Drop an Item on the ground, at the current Player position.
        /// NOTE: On some server is not allowed to drop Items on tiles occupied by Mobiles and the Player.
        /// </summary>
        /// <param name="item">Item object to drop.</param>
        /// <param name="amount">Amount to move. (default: 0, the whole stack)</param>
        public static void DropItemGroundSelf(Item item, int amount = 0)
        {
            if (item == null)
            {
                Scripts.SendMessageScriptError("Script Error: DropItemGroundSelf: Item not found");
                return;
            }

            int amounttodrop = amount;
            if ((item.Amount < amount) || (amount == 0))
                amounttodrop = item.Amount;

            MoveOnGround(item.Serial, amounttodrop, Player.Position.X, Player.Position.Y, Player.Position.Z);
        }

        /// <summary>
        /// This function seldom works because the servers dont allow drop where your standing
        /// </summary>
        /// <param name="serialitem"></param>
        /// <param name="amount"></param>
        public static void DropItemGroundSelf(int serialitem, int amount = 0)
        {
            Item i = FindBySerial(serialitem);
            DropItemGroundSelf(i, amount);
        }

        /// <summary>
        /// Use an Item, optionally is possible to specify a Item or Mobile target.
        /// NOTE: The optional target may not work on some free shards. Use Target.Execute instead.
        /// </summary>
        /// <param name="itemSerial">Serial or Item to use.</param>
        /// <param name="targetSerial">Optional: Serial of the Item or Mobile target.</param>
        /// <param name="wait">Optional: Wait for confirmation by the server. (default: True)</param>
        public static void UseItem(int itemSerial, int targetSerial, bool wait)
        {
            Assistant.Item item = Assistant.World.FindItem(itemSerial);
            if (item == null)
            {
                Scripts.SendMessageScriptError("Script Error: UseItem: Invalid Use Serial");
                return;
            }

            Assistant.Item itemTarget = Assistant.World.FindItem(targetSerial);
            Assistant.Mobile mobileTarget = Assistant.World.FindMobile(targetSerial);
            if (itemTarget == null && mobileTarget == null)
            {
                Scripts.SendMessageScriptError("Script Error: UseItem: Invalid Target Serial");
                return;
            }

            if (!item.Serial.IsItem)
            {
                Scripts.SendMessageScriptError("Script Error: UseItem: (" + item.Serial.ToString() + ") is not an item");
                return;
            }
            if (itemTarget == null && !mobileTarget.Serial.IsMobile)
            {
                Scripts.SendMessageScriptError("Script Error: UseItem: (" + targetSerial.ToString() + ") is not a mobile");
                return;
            }
            if (mobileTarget == null && !itemTarget.Serial.IsItem)
            {
                Scripts.SendMessageScriptError("Script Error: UseItem: (" + targetSerial.ToString() + ") is not an item");
                return;
            }

            if (wait)
                Assistant.Client.Instance.SendToServerWait(new UseTargetedItem((uint)itemSerial, (uint)targetSerial));
            else
                Assistant.Client.Instance.SendToServer(new UseTargetedItem((uint)itemSerial, (uint)targetSerial));
        }

        public static void UseItem(int itemserial)
        {
            Assistant.Item item = Assistant.World.FindItem(itemserial);
            if (item == null)
            {
                Scripts.SendMessageScriptError("Script Error: UseItem: Invalid Serial");
                return;
            }

            if (item.Serial.IsItem)
            {
                Assistant.Client.Instance.SendToServerWait(new DoubleClick(item.Serial));
            }
            else
            {
                Scripts.SendMessageScriptError("Script Error: UseItem: (" + item.Serial.ToString() + ") is not a item");
            }
        }

        public static void UseItem(Item item)
        {
            if (item != null)
                UseItem(item.Serial);
        }

        public static void UseItem(Item item, UOEntity target)
        {
            if (item == null || target == null)
                return;

            UseItem(item.Serial, target.Serial, true);
        }
        public static void UseItem(int item, UOEntity target)
        {
            if (target == null)
                return;

            UseItem(item, target.Serial, true);
        }
        public static void UseItem(Item item, int target)
        {
            if (item == null)
                return;

            UseItem(item.Serial, target, true);
        }

        public static void UseItem(int itemSerial, int targetSerial)
        {
            UseItem(itemSerial, targetSerial, true);
        }


        /// <summary>
        /// Use any item of a specific type, matching Item.ItemID. Optionally also of a specific color, matching Item.Hue.
        /// </summary>
        /// <param name="itemid">ItemID to be used.</param>
        /// <param name="color">Color to be used. (default: -1, any)</param>
        /// <returns></returns>
        public static bool UseItemByID(int itemid, int color = -1)
        {
            // Genero filtro item
            Items.Filter itemFilter = new()
            {
                Enabled = true
            };
            itemFilter.Graphics.Add(itemid);

            if (color != -1)
                itemFilter.Hues.Add(color);

            List<Item> containeritem = RazorEnhanced.Items.ApplyFilter(itemFilter);

            foreach (Item found in containeritem)
            {
                if (found.IsInBackpack)
                {
                    RazorEnhanced.Items.UseItem(found);
                    return true;
                }
            }

            return false;
        }

        // Find item by id
        /// <summary>
        /// Find a single Item matching specific ItemID, Color and Container. 
        /// Optionally can search in all subcontaners or to a maximum depth in subcontainers.
        /// Can use -1 on color for no chose color, can use -1 on container for search in all item in memory. The depth defaults to only the top but can search for # of sub containers.
        /// </summary>
        /// <param name="itemid">ItemID filter.</param>
        /// <param name="color">Color filter. (-1: any, 0: natural )</param>
        /// <param name="container">Serial of the container to search. (-1: any Item)</param>
        /// <param name="recursive">
        /// Search subcontainers. 
        ///     True: all subcontainers
        ///     False: only main
        ///     1,2,n: Maximum subcontainer depth
        /// </param>
        /// <param name="considerIgnoreList">True: Ignore Items are excluded - False: any Item.</param>
        /// <returns>The Item matching the criteria.</returns>
        public static Item FindByID(int itemid, int color, int container, bool recursive = false, bool considerIgnoreList = true)
        {
            if (container != -1)  // search in specific container
            {
                Item cont = FindBySerial(container);
                if (cont == null) // not valid serial or container not found
                {
                    Scripts.SendMessageScriptError("Script Error: FindByID: Container serial not found");
                    return null;
                }
                foreach (Item i in cont.Contains)
                {
                    if (considerIgnoreList && Misc.CheckIgnoreObject(i.Serial))
                        continue;
                    if (i.Amount == 0)
                        continue;

                    if (i.TypeID == itemid) // check item id
                    {
                        if (color != -1) // color -1 ALL
                        {
                            if (i.Hue == color)
                                return i;
                        }
                        else
                        {
                            return i;
                        }
                    }
                    else if (recursive && i.IsContainer)
                    {
                        Item recursItem = FindByID(itemid, color, i.Serial, recursive, considerIgnoreList); // recall for sub container
                        if (recursItem != null)
                            return recursItem;
                    }
                }
                return null; // Return null if no item found
            }
            else  // Search in world
            {
                Items.Filter itemFilter = new()
                {
                    Enabled = true
                };
                itemFilter.Graphics.Add(itemid);
                itemFilter.CheckIgnoreObject = considerIgnoreList;

                if (color != -1)
                    itemFilter.Hues.Add(color);

                List<Item> containeritem = RazorEnhanced.Items.ApplyFilter(itemFilter);

                foreach (Item found in containeritem)  // Return frist one found
                    return found;

                return null;
            }
        }

        public static Item FindByID(int itemid, int color, int container, int range, bool considerIgnoreList = true)
        {
            var itemids = new List<int>();
            itemids.Add(itemid);
            return FindByID(itemids, color, container, range, considerIgnoreList);
        }


        // Find item matchind any of a list of ids
        /// <summary>
        /// Find a single Item matching specific list of ItemID, Color and Container. 
        /// Optionally can search in all subcontaners or to a maximum depth in subcontainers.
        /// Can use -1 on color for no chose color, can use -1 on container for search in all item in memory. The depth defaults to only the top but can search for # of sub containers.
        /// </summary>
        /// <param name="itemid"> List of ItemID filter.</param>
        /// <param name="color">Color filter. (-1: any, 0: natural )</param>
        /// <param name="container">Serial of the container to search. (-1: any Item)</param>
        /// <param name="range">In containers means the number of sub-containers to search. In World items means distance in squares (10: any Item)</param>
        /// <param name="recursive">
        /// Search subcontainers. 
        ///     True: all subcontainers
        ///     False: only main
        ///     1,2,n: Maximum subcontainer depth
        /// </param>
        /// <param name="considerIgnoreList">True: Ignore Items are excluded - False: any Item.</param>
        /// <returns>The Item matching the criteria.</returns>
        public static Item FindByID(List<int> itemids, int color = -1, int container = -1, int range = 10, bool considerIgnoreList = true)
        {
            if (container != -1)  // search in specific container
            {
                // range should be # of packs deep to search .. but not implemented
                Item cont = FindBySerial(container);
                if (cont == null) // not valid serial or container not found
                {
                    Scripts.SendMessageScriptError("Script Error: FindByID: Container serial not found");
                    return null;
                }
                foreach (Item i in cont.Contains)
                {
                    if (considerIgnoreList && Misc.CheckIgnoreObject(i.Serial))
                        continue;
                    if (i.Amount == 0)
                        continue;
                    if (itemids.Contains(i.TypeID.Value)) // check item id
                    {
                        if (color != -1) // color -1 ALL
                        {
                            if (i.Hue == color)
                                return i;
                        }
                        else
                        {
                            return i;
                        }
                    }
                    else if (i.IsContainer && range != 0)
                    {
                        Item recursItem = FindByID(itemids, color, i.Serial, range - 1, considerIgnoreList); // recall for sub container
                        if (recursItem != null)
                            return recursItem;
                    }
                }
                return null; // Return null if no item found
            }
            else  // Search in world
            {
                Items.Filter itemFilter = new()
                {
                    Enabled = true
                };
                itemFilter.Graphics.AddRange(itemids);
                itemFilter.RangeMax = range;
                itemFilter.CheckIgnoreObject = considerIgnoreList;

                if (color != -1)
                    itemFilter.Hues.Add(color);

                List<Item> containeritem = RazorEnhanced.Items.ApplyFilter(itemFilter);

                foreach (Item found in containeritem)  // Return first one found
                    return found;

                return null;
            }
        }

        // Find all items matching any of a list of ids
        /// <summary>
        /// Find a List of Items matching specific list of ItemID, Color and Container. 
        /// Optionally can search in all subcontaners or to a maximum depth in subcontainers.
        /// Can use -1 on color for no chose color, can use -1 on container for search in all item in memory.
        /// The depth defaults to only the top but can search for # of sub containers.
        /// </summary>
        /// <param name="itemid"> List of ItemID filter.</param>
        /// <param name="color">Color filter. (-1: any, 0: natural )</param>
        /// <param name="container">Serial of the container to search. (-1: any Item)</param>
        /// <param name="recursive">
        /// Search subcontainers. 
        ///     True: all subcontainers
        ///     False: only main
        ///     1,2,n: Maximum subcontainer depth
        /// </param>
        /// <param name="considerIgnoreList">True: Ignore Items are excluded - False: any Item.</param>
        /// <returns>The Item matching the criteria.</returns>
        public static IronPython.Runtime.PythonList FindAllByID(IronPython.Runtime.PythonList itemids, int color, int container, int range, bool considerIgnoreList = true)
        {
            var returnList = new IronPython.Runtime.PythonList();
            var internalList = new List<int>();
            foreach (var item in itemids)
                internalList.Add((int)item);
            var foundList = FindAllByID(internalList, color, container, range, considerIgnoreList);
            if (foundList != null)
            {
                foreach (var item in foundList)
                    returnList.Add(item);
            }
            return returnList;


        }
        // Find all items matching an id
        public static IronPython.Runtime.PythonList FindAllByID(int itemid, int color, int container, int range, bool considerIgnoreList = true)
        {
            var returnList = new IronPython.Runtime.PythonList();
            List<int> internalList = new();
            internalList.Add(itemid);
            ;
            var foundList = FindAllByID(internalList, color, container, range, considerIgnoreList);
            if (foundList != null)
            {
                foreach (var item in foundList)
                    returnList.Add(item);
            }
            return returnList;
        }

        // Find all items matching an C# list[ids]
        public static List<Item> FindAllByID(List<int> itemids, int color, int container, int range, bool considerIgnoreList = true)
        {
            var returnList = new List<Item>();
            if (container == -1)  // search everywhere
            {
                Items.Filter itemFilter = new()
                {
                    Enabled = true
                };
                foreach (var itemID in itemids)
                    itemFilter.Graphics.Add(itemID);
                itemFilter.RangeMax = range;
                itemFilter.CheckIgnoreObject = considerIgnoreList;
                if (color != -1)
                    itemFilter.Hues.Add(color);

                foreach (Item item in RazorEnhanced.Items.ApplyFilter(itemFilter))
                {
                    returnList.Add(item);
                }
                return returnList;
            }

            // else just search in container
            Item cont = FindBySerial(container);
            if (cont == null) // not valid serial or container not found
            {
                Scripts.SendMessageScriptError("Script Error: FindAllByID: Container serial not found");
                return null;
            }
            foreach (Item i in cont.Contains)
            {
                if (considerIgnoreList && Misc.CheckIgnoreObject(i.Serial))
                    continue;

                if (itemids.Contains(i.TypeID.Value)) // check item id
                {
                    if (color != -1) // color -1 ALL
                    {
                        if (i.Hue == color)
                            returnList.Add(i);
                    }
                    else
                    {
                        returnList.Add(i);
                    }
                }
                else if (i.IsContainer && range != 0)
                {
                    var recursItems = FindAllByID(itemids, color, i.Serial, range - 1, considerIgnoreList); // recall for sub container
                    if (recursItems != null)
                        returnList.AddRange(recursItems);
                }
            }
            return returnList; // Return null if no item found
        }



        // Find item by Name
        /// <summary>
        /// Find a single Item matching specific Name, Color and Container. 
        /// Optionally can search in all subcontaners or to a maximum depth in subcontainers.
        /// Can use -1 on color for no chose color, can use -1 on container for search in all item in memory. The depth defaults to only the top but can search for # of sub containers.
        /// </summary>
        /// <param name="itemName">Item Name filter.</param>
        /// <param name="color">Color filter. (-1: any, 0: natural )</param>
        /// <param name="container">Serial of the container to search. (-1: any Item)</param>
        /// <param name="range">
        /// Search subcontainers. 
        ///     1,2,n: Maximum subcontainer depth
        /// </param>
        /// <param name="considerIgnoreList">True: Ignore Items are excluded - False: any Item.</param>
        /// <returns>The Item matching the criteria.</returns>

        public static Item FindByName(string itemName, int color, int container, int range, bool considerIgnoreList = true)
        {
            if (container != -1)  // search in specific container
            {
                // range should be # of packs deep to search .. but not implemented
                Item cont = FindBySerial(container);
                if (cont == null) // not valid serial or container not found
                {
                    Scripts.SendMessageScriptError("Script Error: FindByName: Container serial not found");
                    return null;
                }
                foreach (Item i in cont.Contains)
                {
                    if (considerIgnoreList && Misc.CheckIgnoreObject(i.Serial))
                        continue;

                    if (i.Name == itemName) // check item id
                    {
                        if (color != -1) // color -1 ALL
                        {
                            if (i.Hue == color)
                                return i;
                        }
                        else
                        {
                            return i;
                        }
                    }
                    else if (i.IsContainer && range != 0)
                    {
                        Item recursItem = FindByName(itemName, color, i.Serial, range - 1, considerIgnoreList); // recall for sub container
                        if (recursItem != null)
                            return recursItem;
                    }
                }
                return null; // Return null if no item found
            }
            else  // Search in world
            {
                Items.Filter itemFilter = new()
                {
                    Enabled = true
                };
                itemFilter.Name = itemName;
                itemFilter.RangeMax = range;
                itemFilter.CheckIgnoreObject = considerIgnoreList;

                if (color != -1)
                    itemFilter.Hues.Add(color);

                List<Item> containeritem = RazorEnhanced.Items.ApplyFilter(itemFilter);

                foreach (Item found in containeritem)  // Return frist one found
                    return found;

                return null;
            }
        }



        // Single Click
        /// <summary>
        /// Send a single click network event to the server.
        /// </summary>
        /// <param name="item">Serial or Item to click</param>
        public static void SingleClick(Item item)
        {
            Assistant.Client.Instance.SendToServerWait(new SingleClick(item.Serial));
        }

        public static void SingleClick(int itemserial)
        {
            Assistant.Item item = Assistant.World.FindItem(itemserial);
            if (item == null)
            {
                Scripts.SendMessageScriptError("Script Error: SingleClick: Invalid Serial");
                return;
            }
            Assistant.Client.Instance.SendToServerWait(new SingleClick(itemserial));
        }

        // Props

        /// <summary>
        /// Request to get immediatly the Properties of an Item, and wait for a specified amount of time.
        /// This only returns properties and does not attempt to update the object.
        /// Used in this way, properties for object not yet seen can be retrieved
        /// </summary>
        /// <param name="itemserial">Serial or Item read.</param>
        /// <param name="delay">Maximum waiting time, in milliseconds.</param>
        public static List<Property> GetProperties(int itemserial, int delay) // Delay in MS
        {
            List<Property> properties = new();

            RazorEnhanced.Item i = FindBySerial(itemserial);
            if (i != null)
            {
                WaitForProps(itemserial, delay);
                return i.Properties;
            }

            // Item not found then just get properties
            var fakeItem = Assistant.Item.Factory(itemserial, 1);
            var callback = new PacketViewerCallback((PacketReader p, PacketHandlerEventArgs args) =>
            {
                ushort id = p.ReadUInt16();
                if (id == 1)
                {
                    int s = p.ReadInt32();
                    if (s == itemserial)
                    {
                        fakeItem.ReadPropertyList(p);
                    }
                }
            });
            PacketHandler.RegisterServerToClientViewer(0xD6, callback);//0xD6 "encoded" packets
            Assistant.Client.Instance.SendToServerWait(new QueryProperties(itemserial));
            Utility.DelayUntil(() => { return fakeItem.PropsUpdated == true; }, delay);
            PacketHandler.RemoveServerToClientViewer(0xD6, callback);

            foreach (Assistant.ObjectPropertyList.OPLEntry entry in fakeItem.ObjPropList.Content)
            {
                Property property = new(entry);
                properties.Add(property);
            }
            return properties;
        }




        /// <summary>
        /// If not updated, request to the Properties of an Item, and wait for a maximum amount of time. 
        /// </summary>
        /// <param name="itemserial">Serial or Item read.</param>
        /// <param name="delay">Maximum waiting time, in milliseconds.</param>
        public static void WaitForProps(int itemserial, int delay) // Delay in MS
        {
            if (World.Player != null && World.Player.Expansion <= 3) //  Expansion <= 3. Non esistono le props
            {
                return;
            }

            Assistant.Item i = Assistant.World.FindItem(itemserial);
            if (i == null)
                return;

            if (!i.PropsUpdated)
            {
                Assistant.Client.Instance.SendToServerWait(new QueryProperties(i.Serial));
                Utility.DelayUntil(() => { return i.PropsUpdated == true; }, delay);
            }
        }

        public static void WaitForProps(Item i, int delay)
        {
            WaitForProps(i.Serial, delay);
        }



        /// <summary>
        /// Get string list of all Properties of an item, if item no props list is empty.
        /// </summary>
        /// <param name="serial">Serial or Item to read.</param>
        /// <returns>List of strings.</returns>
        public static List<string> GetPropStringList(int serial)
        {
            List<string> propstringlist = new();
            Assistant.Item assistantItem = Assistant.World.FindItem((uint)serial);

            if (assistantItem == null)
                return propstringlist;

            List<Assistant.ObjectPropertyList.OPLEntry> props = assistantItem.ObjPropList.Content;
            foreach (Assistant.ObjectPropertyList.OPLEntry prop in props)
            {
                propstringlist.Add(prop.ToString());
            }
            return propstringlist;
        }

        public static List<string> GetPropStringList(Item item)
        {
            return GetPropStringList(item.Serial);
        }



        /// <summary>
        /// Get a Property line, by index. if not found returns and empty string.
        /// </summary>
        /// <param name="serial">Serial or Item to read.</param>
        /// <param name="index">Number of the Property line.</param>
        /// <returns>A property line as a string.</returns>
        public static string GetPropStringByIndex(int serial, int index)
        {
            string propstring = String.Empty;
            Assistant.Item assistantItem = Assistant.World.FindItem((uint)serial);

            if (assistantItem == null)
                return propstring;

            List<Assistant.ObjectPropertyList.OPLEntry> props = assistantItem.ObjPropList.Content;
            if (props.Count > index)
                propstring = props[index].ToString();
            return propstring;
        }

        /// <summary>
        /// Get a Property line, by name. if not found returns and empty string.
        /// </summary>
        /// <param name="serial">Serial or Item to read.</param>
        /// <param name="name">Number of the Property line.</param>
        /// <returns>A property value as a string.</returns>
        public static string GetPropValueString(int serial, string name)
        {
            string propString = String.Empty;
            Assistant.Item assistantItem = World.FindItem((uint)serial);
            try
            {
                if (assistantItem != null && assistantItem.ObjPropList != null && assistantItem.ObjPropList.Content != null)
                {
                    var content = assistantItem.ObjPropList.Content;
                    if (content != null)
                    {
                        for (int i = 0; i < content.Count; i++)
                        {
                            if (!content[i].ToString().ToLower().StartsWith(name.ToLower())) // Props Name not match
                                continue;

                            try
                            {
                                //p = Items.GetPropValueString(0x401DD82A, "weight")
                                //print(p)
                                propString = content[i].ToString();
                                string pattern = @"\s*(\b[^:]*)(:)?\s*([^\s].*)?";
                                Regex rx = new(pattern,
                                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
                                Match m = rx.Match(propString);
                                if (m.Groups.Count > 3)
                                {
                                    if (m.Groups[2].Value == ":")
                                    {
                                        return m.Groups[3].Value;
                                    }
                                    return m.Groups[0].Value;
                                }

                                return propString;
                            }
                            catch
                            {
                                // fall thru
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // do nothing because sometimes the content[i] no longer existed so fall through
                // and return 0
            }
            return propString;


        }
        /*
            string pattern = @"\((\d+)\s*,\s*(\d+)\s*\)";
            Regex rx = new Regex(pattern,
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            //p = Items.GetPropValue(0x401DD82A, "location")
            //print(p)
            Match m = rx.Match(propString);
            if (m.Groups.Count > 2)
            {
                return m.Groups[0].Value;
            }
        */
        public static string GetPropStringByIndex(Item item, int index)
        {
            return GetPropStringByIndex(item.Serial, index);
        }


        /// <summary>
        /// Read the value of a Property.
        /// </summary>
        /// <param name="serial">Serial or Item to read.</param>
        /// <param name="name">Name of the Propery.</param>
        /// <returns></returns>

        public static float GetPropValue(int serial, string name)
        {
            if (name.ToLower().Contains("total") && name.ToLower().Contains("resist"))
                return GetTotalResistProp(serial);

            Assistant.Item assistantItem = World.FindItem((uint)serial);
            try
            {
                if (assistantItem != null && assistantItem.ObjPropList != null && assistantItem.ObjPropList.Content != null)
                {
                    var content = assistantItem.ObjPropList.Content;
                    if (content != null)
                    {
                        for (int i = 0; i < content.Count; i++)
                        {
                            if (!content[i].ToString().ToLower().StartsWith(name.ToLower())) // Props Name not match
                                continue;

                            if (content[i].Args == null)  // Props exist but not have value
                                return 1;

                            Regex regex = new Regex(@"\d+$"); // Matches one or more digits (\d) at the end of the string ($)
                            Match match = regex.Match(content[i].Args);
                            if (match.Success)
                            {
                                int number = int.Parse(match.Value);
                                Utility.Logger.Debug($"Extracted number: {number}");
                                return number;
                            }
                            else
                            {
                                return 1; // conversion error
                            }

                        }
                    }
                }
            }
            catch (Exception)
            {
                // do nothing because sometimes the content[i] no longer existed so fall through
                // and return 0
            }
            return 0;  // Item not exist or props not exist
        }

        public static float GetPropValue(Item item, string name)
        {
            if (item == null)
                return 0;

            return GetPropValue(item.Serial, name);
        }

        // GetPropValue: Special case "Total Resist" so that items can be collected based on total resist
        /// <summary>
        /// Get the total resistence of an Item by reading the Propery.
        /// </summary>
        /// <param name="serial">Serial or Item to read.</param>
        /// <returns>The value of the property as float a number.</returns>
        static float GetTotalResistProp(int serial)
        {
            Assistant.Item assistantItem = World.FindItem((uint)serial);

            float totalResist = 0;
            if (assistantItem != null)
            {
                try
                {
                    if (assistantItem != null && assistantItem.ObjPropList != null && assistantItem.ObjPropList.Content != null)
                    {
                        for (int i = 0; i < assistantItem.ObjPropList.Content.Count; i++)
                        {
                            if (assistantItem.ObjPropList.Content[i].ToString().ToLower().Contains("resist"))
                            {
                                if (assistantItem.ObjPropList.Content[i].Args != null)
                                {
                                    float addIt = 0;
                                    try
                                    {
                                        addIt = Convert.ToSingle(Language.ParsePropsCliloc(assistantItem.ObjPropList.Content[i].Args), CultureInfo.InvariantCulture);
                                    }
                                    catch
                                    {
                                        addIt = 1;  // Conversion error
                                    }
                                    totalResist += addIt;
                                }
                            }
                        }
                    }
                }
                catch (System.ArgumentOutOfRangeException)
                {
                    // Do nothing. This occurs when looting or claiming a corpse while processing is still going on
                }
            }
            return totalResist;
        }

        // Message
        /// <summary>
        /// Display an in-game message on top of an Item, visibile only for the Player.
        /// </summary>
        /// <param name="item">Serial or Item to display text on.</param>
        /// <param name="hue">Color of the message.</param>
        /// <param name="message">Message as </param>
        public static void Message(Item item, int hue, string message)
        {
            // Prevent spamm message on left bottom screen
            if (World.Player == null || Utility.Distance(World.Player.Position.X, World.Player.Position.Y, item.Position.X, item.Position.Y) > 11)
                return;

            Assistant.Client.Instance.SendToClientWait(new UnicodeMessage(item.Serial, item.TypeID, MessageType.Regular, hue, 3, Language.CliLocName, item.Name, message));
        }

        public static void Message(int serial, int hue, string message)
        {
            Item item = FindBySerial(serial);

            if (item == null) //Intem
                return;

            // Prevent spamm message on left bottom screen
            if (World.Player == null || Utility.Distance(World.Player.Position.X, World.Player.Position.Y, item.Position.X, item.Position.Y) > 11)
                return;

            Assistant.Client.Instance.SendToClientWait(new UnicodeMessage(item.Serial, item.TypeID, MessageType.Regular, hue, 3, Language.CliLocName, item.Name, message));
        }

        // Count

        /// <summary>
        /// Count items inside a container, summing also the amount in stacks.
        /// </summary>
        /// <param name="container">Serial or Item to search into.</param>
        /// <param name="itemid">ItemID of the item to search.</param>
        /// <param name="color">Color to match. (default: -1, any color)</param>
        /// <param name="recursive">Search also in already open subcontainers.</param>
        /// <returns></returns>
        public static int ContainerCount(Item container, int itemid, int color = -1, bool recursive = false)
        {
            int count = 0;
            if (container != null && container.IsContainer)
            {
                foreach (RazorEnhanced.Item itemToCount in container.Contains)
                {
                    if (color == -1)
                    {
                        if (itemToCount.TypeID == itemid)
                            count += itemToCount.Amount;
                        if (recursive && itemToCount.IsContainer)
                        {
                            int recurseCount = ContainerCount(itemToCount, itemid, color); // recall for sub container
                            count += recurseCount;
                        }
                    }
                    else
                    {
                        if (itemToCount.TypeID == itemid && itemToCount.Hue == color)
                            count += itemToCount.Amount;
                        if (recursive && itemToCount.IsContainer)
                        {
                            int recurseCount = ContainerCount(itemToCount, itemid, color); // recall for sub container
                            count += recurseCount;
                        }

                    }
                }
            }
            else
            {
                Scripts.SendMessageScriptError("Script Error: ContainerCount: Invalid container");
            }
            return count;
        }

        public static int ContainerCount(int serial, int itemid, int color = -1, bool recursive = false)
        {
            Item container = FindBySerial(serial);
            if (container != null)
                return ContainerCount(container, itemid, color, recursive);
            else
            {
                Scripts.SendMessageScriptError("Script Error: ContainerCount: Invalid container");
                return 0;
            }
        }


        /// <summary>
        /// Hied an Item, affects only the player.
        /// </summary>
        /// <param name="serial">Serial or Item to hide.</param>
        /// 
        public static void Hide(int serial)
        {
            Assistant.Item item = World.FindItem(serial);
            if (item != null)
            {
                item.Visible = false;
                Assistant.Client.Instance.SendToClientWait(new RemoveObject(serial));
            }
        }
        public static void Hide(Item item)
        {
            Hide(item.Serial);
        }

        /// <summary>
        /// Close opened container window. 
        /// On OSI, to close opened corpse window, you need to close the corpse's root container 
        /// Currently corpse's root container can be found by using item filter. 
        /// </summary>
        /// <param name="serial">Serial or Item to hide.</param>
        ///
        public static void Close(int serial)
        {
            if (Client.IsOSI)
            {
                Assistant.Item item = World.FindItem(serial);
                if (item != null)
                {
                    Assistant.Client.Instance.SendToClientWait(new CloseContainer(serial));
                }
            }
            else
            {
                CUO.CloseGump((uint)serial);
            }
        }

        public static void Close(Item item)
        {
            Close(item.Serial);
        }

        /// <summary>
        /// Open a container at a specific location
        /// </summary>
        /// <param name="serial">Serial or Item to hide.</param>
        /// <param name="x"> x location to open at
        /// <param name="y"> y location to open at
        ///
        public static void OpenAt(int serial, int x, int y)
        {
            if (Client.IsOSI)
            {
                Assistant.Item item = World.FindItem(serial);
                if (item != null)
                {
                    Misc.NextContPosition(x, y);
                    UseItem(serial);
                }
            }
            else
            {
                CUO.OpenContainerAt((uint)serial, x, y);
            }
        }

        public static void OpenAt(Item item, int x, int y)
        {
            OpenAt(item.Serial, x, y);
        }



        /// <summary>
        /// Count items in Player Backpack.
        /// </summary>
        /// <param name="itemid">ItemID to search.</param>
        /// <param name="color">Color to search. (default -1: any color)</param>
        /// <returns></returns>
        public static int BackpackCount(int itemid, int color = -1)
        {
            List<Assistant.Item> items = new(World.Items.Values.ToList());
            if (color == -1)
                items = items.Where((i) => i.IsInBackpack && i.TypeID == itemid).ToList();
            else
                items = items.Where((i) => i.IsInBackpack && i.TypeID == itemid && i.Hue == color).ToList();

            int amount = 0;
            foreach (Assistant.Item i in items)
                amount += i.Amount;

            return amount;
        }

        // Context

        /// <summary>
        /// Check if Context Menu entry exists for an Item.
        /// </summary>
        /// <param name="serial">Serial or Item to check.</param>
        /// <param name="name">Name of the Context Manu entry</param>
        /// <returns></returns>
        public static int ContextExist(int serial, string name)
        {
            Assistant.Item item = World.FindItem(serial);
            if (item == null) // Se item non valido
                return -1;

            Misc.WaitForContext(serial, 1500);

            foreach (KeyValuePair<ushort, int> entry in item.ContextMenu)
            {
                string menuname = Language.GetCliloc(entry.Value);
                if (menuname.ToLower() == name.ToLower())
                {
                    return entry.Key;
                }
            }

            return -1; // Se non trovata
        }

        public static int ContextExist(Item i, string name)
        {
            return ContextExist(i.Serial, name);
        }



        /// <summary>
        /// Get the Image on an Item by specifing the ItemID. Optinally is possible to apply a color.
        /// </summary>
        /// <param name="itemID">ItemID to use.</param>
        /// <param name="hue">Optional: Color to apply. (Default 0, natural)</param>
        /// <returns></returns>
        public static System.Drawing.Bitmap GetImage(int itemID, int hue = 0)
        {
            System.Drawing.Bitmap bitmapImage = null;

            try
            {
                // Get original cached Bitmap Static
                System.Drawing.Bitmap bitmapOriginal = Ultima.Art.GetStatic(itemID);
                {
                    if (bitmapOriginal != null)
                    {
                        bitmapImage = bitmapOriginal;

                        if (hue > 0)
                        {
                            // Create clone to preserve original cached bitmap!
                            System.Drawing.Bitmap bitmapDeepCopy = new(bitmapOriginal);

                            bool onlyHueGrayPixels = (hue & 0x8000) != 0;
                            hue = (hue & 0x3FFF) - 1;
                            Ultima.Hue m_hue = Ultima.Hues.GetHue(hue);
                            m_hue.ApplyTo(bitmapDeepCopy, onlyHueGrayPixels);

                            bitmapImage = bitmapDeepCopy;
                        }
                    }
                }
            }
            catch { Misc.SendMessage("Items.GetBitmapImage() exception => itemID: " + itemID + ", hue: " + hue); }

            return bitmapImage;
        }
    }
}
