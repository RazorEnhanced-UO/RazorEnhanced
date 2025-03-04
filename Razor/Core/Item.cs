using ConcurrentCollections;
using Newtonsoft.Json;
using RazorEnhanced;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Assistant
{
    internal enum Layer : byte
    {
        Invalid = 0x00,

        FirstValid = 0x01,

        RightHand = 0x01,
        LeftHand = 0x02,
        Shoes = 0x03,
        Pants = 0x04,
        Shirt = 0x05,
        Head = 0x06,
        Gloves = 0x07,
        Ring = 0x08,
        Talisman = 0x09,
        Neck = 0x0A,
        Hair = 0x0B,
        Waist = 0x0C,
        InnerTorso = 0x0D,
        Bracelet = 0x0E,
        Face = 0x0F,
        Beard = 0x10,
        MiddleTorso = 0x11,
        Earrings = 0x12,
        Arms = 0x13,
        Cloak = 0x14,
        Backpack = 0x15,
        OuterTorso = 0x16,
        OuterLegs = 0x17,
        InnerLegs = 0x18,

        LastUserValid = 0x18,

        Mount = 0x19,
        ShopBuy = 0x1A,
        ShopResale = 0x1B,
        ShopSell = 0x1C,
        Bank = 0x1D,

        LastValid = 0x1D
    }

    public class Item : UOEntity
    {
        //private TypeID m_ItemID;
        private ushort m_Amount;
        private byte m_Direction;
        private byte m_Light;

        private bool m_Visible;
        private bool m_Movable;

        private bool m_PropsUpdated;

        private Layer m_Layer;
        private string m_Name;
        private object m_Parent;
        private int m_Price;
        private string m_BuyDesc;
        private readonly List<Item> m_Items;

        private bool m_IsNew;
        private bool m_AutoStack;

        private byte[] m_HousePacket;
        private int m_HouseRev;

        private byte m_GridNum;

        private bool m_Updated;

        private static readonly object LockingVar = new();
        internal bool Updated
        {
            get { return m_Updated; }
            set
            {
                if (this.IsContainer || this.IsCorpse)
                {
                    m_Updated = value;
                }
            }
        }
        public class Weapon
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("graphic")]
            public int Graphic { get; set; }

            [JsonProperty("primary")]
            public string Primary { get; set; }

            [JsonProperty("secondary")]
            public string Secondary { get; set; }

            [JsonProperty("twohanded")]
            public bool Twohanded { get; set; }

        }
        private static readonly ConcurrentDictionary<int, Weapon> g_weapons = LoadWeapons();
        internal static ConcurrentDictionary<int, Weapon> Weapons { get { return g_weapons; } }

        internal static ConcurrentDictionary<int, Weapon> LoadWeapons()
        {
            ConcurrentDictionary<int, Weapon> retSet = new();
            string pathName = Path.Combine(Assistant.Engine.RootPath, "Config", "weapons.json");
            try
            {
                lock (LockingVar)
                {
                    if (File.Exists(pathName))
                    {
                        List<Weapon> weaponList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Weapon>>(File.ReadAllText(pathName));
                        foreach (Weapon w in weaponList)
                        {
                            retSet[w.Graphic] = w;
                        }
                    }
                    pathName = Path.Combine(Assistant.Engine.RootPath, "Data", "weapons.json");
                    if (File.Exists(pathName))
                    {
                        List<Weapon> weaponList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Weapon>>(File.ReadAllText(pathName));
                        foreach (Weapon w in weaponList)
                        {
                            retSet[w.Graphic] = w;
                        }
                    }
                }
            }
            catch (Exception)
            {
                var result = RazorEnhanced.UI.RE_MessageBox.Show("Error Loading File",
                        $"File: {pathName}",
                        ok: "Ok", no: null, cancel: null, backColor: null);
            }
            return retSet;
        }

        public static Item Factory(Serial serial, UInt32 itemID)
        {
            // during drag operation item may be removed from World
            if (itemID == 0 && DragDropManager.Holding != null && DragDropManager.Holding.Serial == serial)
            {
                // Dropping this item, but already deleted so use ItemID from dead one
                // Because the itemID of dragged items is not on a drop packet
                itemID = DragDropManager.Holding.TypeID;
            }
            Item item;

            if (ConfigFiles.Maps.Data.MapIDs.Contains(itemID))
            {
                item = new MapItem(serial);
            }
            else if (itemID == 0x2006)
            {
                item = new CorpseItem(serial);
            }
            else
            {
                item = new Item(serial);
            }

            if (item != null)
            {
                item.TypeID = (ushort)itemID;
           }

            return item;
        }
        protected Item(Serial serial)
            : base(serial)
        {
            m_Items = new List<Item>();

            m_Visible = true;
            m_Movable = true;
        }

        /// <summary>
        /// True when the container was opened
        /// </summary>
        internal bool ContainerOpened { get; set; }
        internal byte ArtID { get; set; }

        internal bool PropsUpdated
        {
            get { return m_PropsUpdated; }
            set { m_PropsUpdated = value; }
        }

        // TrueAmount is a hack because OSI messes with the amount field, but for vendor buy/sell I needs the real value
        internal ushort TrueAmount
        {
            get
            {
                return m_Amount;
            }
            set { m_Amount = value; }
        }

        internal ushort Amount
        {
            get
            {
                // On OSI the amount value is used for other purposes if an item is declared not stackable in files.
                try // avoid crash if some bad happen in Ultima.dll
                {
                    if ((RazorEnhanced.Statics.GetItemData(TypeID).Flags & Ultima.TileFlag.Generic) != 0)
                        return m_Amount;
                    if (TypeID == 0x2006)
                        return m_Amount;
                    if (Container != null)
                    {
                        Item cont = Container as Item;
                        if (cont != null)
                        {
                            if (cont.Layer == Layer.ShopBuy ||
                                cont.Layer == Layer.ShopResale ||
                                cont.Layer == Layer.ShopSell)
                                return m_Amount;
                        }
                    }
                }
                catch
                {
                }

                return 1;
            }
            set { m_Amount = value; }
        }

        internal byte Direction
        {
            get { return m_Direction; }
            set { m_Direction = value; }
        }

        internal byte Light
        {
            get { return m_Light; }
            set { m_Light = value; }
        }

        internal bool Visible
        {
            get { return m_Visible; }
            set { m_Visible = value; }
        }

        internal bool Movable
        {
            get { return m_Movable; }
            set { m_Movable = value; }
        }

        internal string Name
        {
            get
            {
                if (m_Name != null && m_Name != "")
                {
                    return m_Name;
                }
                else if (ObjPropList.Content.Count > 0)
                {
                    return ObjPropList.Content[0].ToString();
                }
                else
                {
                    return TypeID.Value.ToString();
                }
            }
            set
            {
                if (value != null)
                    m_Name = value.Trim();
                else
                    m_Name = null;
            }
        }

        internal Layer Layer
        {
            get
            {
                if ((m_Layer < Layer.FirstValid || m_Layer > Layer.LastValid) &&
                    ((this.TypeID.ItemData.Flags & Ultima.TileFlag.Wearable) != 0 ||
                    (this.TypeID.ItemData.Flags & Ultima.TileFlag.Armor) != 0 ||
                    (this.TypeID.ItemData.Flags & Ultima.TileFlag.Weapon) != 0
                    ))
                {
                    m_Layer = (Layer)this.TypeID.ItemData.Quality;
                }

                if ((this.TypeID.ItemData.Flags & Ultima.TileFlag.Weapon) != 0)
                {
                    Weapon w;
                    bool found = Weapons.TryGetValue(this.TypeID.Value, out w);
                    if (found)
                    {
                        if (w.Twohanded == true && IsTwoHanded == false)
                        {
                            m_Layer = Layer.RightHand; // artificially 2 hand is a 1 hand (some servers allow this)
                        }
                    }
                }

                return m_Layer;
            }
            set
            {
                m_Layer = value;
            }
        }

        internal Item FindItemByID(TypeID id)
        {
            return FindItemByID(id, true);
        }

        internal Item FindItemByID(TypeID id, bool recurse)
        {
            foreach (Item t in m_Items)
            {
                Item item = t;
                if (item.TypeID == id)
                {
                    return item;
                }
                else if (recurse)
                {
                    item = item.FindItemByID(id, true);
                    if (item != null)
                        return item;
                }
            }
            return null;
        }

        internal object Container
        {
            get
            {
                if (m_Parent is Serial && UpdateContainer())
                    m_NeedContUpdate.Remove(this);
                return m_Parent;
            }
            set
            {
                if ((m_Parent != null && m_Parent.Equals(value))
                    || (value is Serial && m_Parent is UOEntity && ((UOEntity)m_Parent).Serial == (Serial)value)
                    || (m_Parent is Serial && value is UOEntity && ((UOEntity)value).Serial == (Serial)m_Parent))
                {
                    return;
                }

                if (m_Parent is Mobile)
                    ((Mobile)m_Parent).RemoveItem(this);
                else if (m_Parent is Item)
                    ((Item)m_Parent).RemoveItem(this);

                if (value is Mobile)
                    m_Parent = ((Mobile)value).Serial;
                else if (value is Item)
                    m_Parent = ((Item)value).Serial;
                else
                    m_Parent = value;

                if (!UpdateContainer() && m_NeedContUpdate != null)
                    m_NeedContUpdate.Add(this);
            }
        }

        internal bool UpdateContainer()
        {
            if (!(m_Parent is Serial) || Deleted)
                return true;

            object o = null;
            Serial contSer = (Serial)m_Parent;
            if (contSer.IsItem)
                o = World.FindItem(contSer);
            else if (contSer.IsMobile)
                o = World.FindMobile(contSer);

            if (o == null)
                return false;

            m_Parent = o;

            if (m_Parent is Item)
                ((Item)m_Parent).AddItem(this);
            else if (m_Parent is Mobile)
                ((Mobile)m_Parent).AddItem(this);

            if (World.Player != null && (IsChildOf(World.Player.Backpack) || IsChildOf(World.Player.Quiver)))
            {
                if (m_IsNew)
                {
                    if (m_AutoStack)
                        AutoStackResource();

                    if (RazorEnhanced.Settings.General.ReadBool("AutoSearch")
                        && IsContainer
                        && !(IsSearchable && RazorEnhanced.Settings.General.ReadBool("NoSearchPouches"))
                        && !this.IsBagOfSending
                        )
                    {
                        PlayerData.DoubleClick(this);

                        for (int c = 0; c < Contains.Count; c++)
                        {
                            Item icheck = Contains[c];
                            if (icheck.IsContainer)
                            {
                                if (icheck.IsSearchable && RazorEnhanced.Settings.General.ReadBool("NoSearchPouches"))
                                    continue;
                                if (icheck.IsBagOfSending)
                                    continue;
                                PlayerData.DoubleClick(icheck);
                            }
                        }
                    }
                }
            }
            m_AutoStack = m_IsNew = false;

            return true;
        }

        private static readonly List<Item> m_NeedContUpdate = new();

        internal static void UpdateContainers()
        {
            int i = 0;
            while (i < m_NeedContUpdate.Count)
            {
                if ((m_NeedContUpdate[i]).UpdateContainer())
                    m_NeedContUpdate.RemoveAt(i);
                else
                    i++;
            }
        }

        private static readonly List<Serial> m_AutoStackCache = new();

        internal void AutoStackResource()
        {
            if (!IsResource || !RazorEnhanced.Settings.General.ReadBool("AutoStack") || m_AutoStackCache.Contains(Serial))
                return;

            foreach (Item check in World.Items.Values)
            {
                if (check.Container == null && check.TypeID == TypeID && check.Hue == Hue && Utility.InRange(World.Player.Position, check.Position, 2))
                {
                    DragDropManager.DragDrop(this, check);
                    m_AutoStackCache.Add(Serial);
                    return;
                }
            }

            DragDropManager.DragDrop(this, World.Player.Position);
            m_AutoStackCache.Add(Serial);
        }

        internal object RootContainer
        {
            get
            {
                // if container is null or not an item just return it
                if (this.Container == null)
                    return this.Container;

                if (!(this.Container is Item))
                    return this.Container;

                // try to search parent containers until parent is null or not an item
                // example of ! an Item is Player -> Backpack -> Items
                // the root of Items should be Backpack not player even though the Container of a Backpack is the player
                //
                // if more than 100 give up and return original Container
                int maxTry = 100;
                Item cont = (Item)this.Container;
                while (cont.Container != null && (cont.Container is Item))
                {
                    cont = (Item)cont.Container;
                    if (maxTry-- < 1)
                        return this.Container;
                }

                return cont;
            }
            set
            {
                this.Container = value;
            }
        }

        internal bool IsChildOf(object parent, int maxDepth = 100)
        {
            Serial parentSerial = 0;
            if (parent is Mobile)
                return parent == RootContainer;
            else if (parent is Item)
                parentSerial = ((Item)parent).Serial;
            else
                return false;

            object check = this;

            while (check != null && check is Item && maxDepth-- > 0)
            {
                if (((Item)check).Serial == parentSerial)
                    return true;
                else
                    check = ((Item)check).Container;
            }

            return false;
        }

        internal Point3D GetWorldPosition()
        {
            int die = 100;
            object root = this.Container;
            while (root != null && root is Item && ((Item)root).Container != null && die-- > 0)
                root = ((Item)root).Container;

            if (root is Item)
                return ((Item)root).Position;
            else if (root is Mobile)
                return ((Mobile)root).Position;
            else
                return this.Position;
        }

        private void AddItem(Item item)
        {
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (m_Items[i] == item)
                    return;
            }

            m_Items.Add(item);
        }

        private void RemoveItem(Item item)
        {
            try
            {
                m_Items.Remove(item);
            }
            catch { }
        }

        internal byte GetPacketFlags()
        {
            byte flags = 0;

            if (!m_Visible)
            {
                flags |= 0x80;
            }

            if (m_Movable)
            {
                flags |= 0x20;
            }

            return flags;
        }

        internal int DistanceTo(Mobile m)
        {
            int x = Math.Abs(this.Position.X - m.Position.X);
            int y = Math.Abs(this.Position.Y - m.Position.Y);

            return x > y ? x : y;
        }

        internal void ProcessPacketFlags(byte flags)
        {
            m_Visible = ((flags & 0x80) == 0);
            m_Movable = ((flags & 0x20) != 0);
        }

        //private Timer m_RemoveTimer = null;

        /*  internal void RemoveRequest()
            {
                if (m_RemoveTimer == null)
                    m_RemoveTimer = Timer.DelayedCallback(TimeSpan.FromSeconds(0.25), new TimerCallback(Remove));
                else if (m_RemoveTimer.Running)
                    m_RemoveTimer.Stop();

                m_RemoveTimer.Start();
            }*/

        /*internal bool CancelRemove()
        {
            if (m_RemoveTimer != null && m_RemoveTimer.Running)
            {
                m_RemoveTimer.Stop();
                return true;
            }
            else
            {
                return false;
            }
        }*/

        internal override void Remove()
        {
            if (IsMulti)
                Assistant.UOAssist.PostRemoveMulti(this);

            List<Item> rem = new(m_Items);
            m_Items.Clear();

            foreach (Item r in rem)
                r.Remove();

            if (m_Parent is Mobile)
                ((Mobile)m_Parent).RemoveItem(this);
            else if (m_Parent is Item)
                ((Item)m_Parent).RemoveItem(this);

            World.RemoveItem(this);
            base.Remove();
        }

        internal override void OnPositionChanging(Point3D newPos)
        {
            if (IsMulti && this.Position != Point3D.Zero && newPos != Point3D.Zero && this.Position != newPos)
            {
                Assistant.UOAssist.PostRemoveMulti(this);
                Assistant.UOAssist.PostAddMulti(TypeID.Value, newPos);
            }
            base.OnPositionChanging(newPos);
        }

        internal List<Item> Contains { get { return m_Items; } }

        // possibly 4 bit x/y - 16x16?
        internal byte GridNum
        {
            get { return m_GridNum; }
            set { m_GridNum = value; }
        }

        internal bool OnGround { get { return Container == null; } }

        public class ContainerData
        {
            [JsonProperty("ItemID")]
            public int ItemID { get; set; }

            [JsonProperty("Searchable")]
            public bool Searchable { get; set; }
        }

        internal static ConcurrentDictionary<int, ContainerData> LoadContainersData()
        {
            ConcurrentDictionary<int, ContainerData> retContData = new();
            lock (LockingVar)
            {
                string pathName = Path.Combine(Assistant.Engine.RootPath, "Config", "ContainersData.json");
                if (File.Exists(pathName))
                {
                    string containersData = File.ReadAllText(pathName);
                    List<ContainerData> contData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ContainerData>>(containersData);
                    foreach (var cont in contData)
                    {
                        retContData[cont.ItemID] = cont;
                    }
                }
                pathName = Path.Combine(Assistant.Engine.RootPath, "Data", "ContainersData.json");
                if (File.Exists(pathName))
                {
                    string containersData = File.ReadAllText(pathName);
                    List<ContainerData> contData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ContainerData>>(containersData);
                    foreach (var cont in contData)
                    {
                        retContData[cont.ItemID] = cont;
                    }
                }
            }
            return retContData;
        }

        internal static ConcurrentDictionary<int, ContainerData> m_containerID = LoadContainersData();

        internal bool IsContainer
        {
            get
            {
                if (IsCorpse)
                    return false;

                if (m_Items.Count > 0)
                    return true;

                if (m_containerID.ContainsKey(TypeID.Value))
                    return true;
                else
                    return false;
            }
        }


        internal bool IsInBackpack
        {
            get
            {
                if (RootContainer == World.Player.Backpack)
                    return true;
                if (this == World.Player.Backpack)
                    return true;

                return false;
            }
        }

        internal bool IsLootableTarget
        {
            get
            {
                //if (IsBagOfSending)
                //    return false;
                // Should be false but checking for it is difficult

                if (!IsContainer)
                    return false;

                if (IsInBank)
                    return false;

                if (RootContainer == World.Player.Backpack)
                    return true;
                if (this == World.Player.Backpack)
                    return true;

                return false;

            }
        }

        internal bool IsBagOfSending
        {
            get
            {
                return Name.ToLower().Contains("sending") && Hue >= 0x0400 && TypeID.Value == 0xE76;
            }
        }

        internal bool IsInBank
        {
            get
            {
                if (m_Parent is Item)
                    return ((Item)m_Parent).IsInBank;
                else if (m_Parent is Mobile)
                    return this.Layer == Layer.Bank;
                else
                    return false;
            }
        }

        internal bool IsNew
        {
            get { return m_IsNew; }
            set { m_IsNew = value; }
        }

        internal bool AutoStack
        {
            get { return m_AutoStack; }
            set { m_AutoStack = value; }
        }

        internal bool IsMulti
        {
            get { return TypeID.Value >= 0x4000; }
        }

        internal bool IsSearchable
        {
            get
            {
                if (IsCorpse)
                    return false;

                if (m_Items.Count > 0)
                    return true;

                if (m_containerID.ContainsKey(TypeID.Value))
                    return m_containerID[TypeID.Value].Searchable;
                else
                    return TypeID.Value == 0x0E79;
            }
        }

        internal bool IsCorpse
        {
            get { return TypeID.Value == 0x2006 || (TypeID.Value >= 0x0ECA && TypeID.Value <= 0x0ED2); }
        }

        public int CorpseNumberItems { get; set; } = -1;

        internal static ConcurrentHashSet<uint> LoadDoorData()
        {
            ConcurrentHashSet<uint> ret = new();
            lock (LockingVar)
            {

                string pathName = Path.Combine(Assistant.Engine.RootPath, "Config", "DoorData.json");
                if (File.Exists(pathName))
                {
                    string doorData = File.ReadAllText(pathName);
                    ret = Newtonsoft.Json.JsonConvert.DeserializeObject<ConcurrentHashSet<uint>>(doorData);
                }
                pathName = Path.Combine(Assistant.Engine.RootPath, "Data", "DoorData.json");
                if (File.Exists(pathName))
                {
                    string doorData = File.ReadAllText(pathName);
                    ConcurrentHashSet<uint> dataDoors = Newtonsoft.Json.JsonConvert.DeserializeObject<ConcurrentHashSet<uint>>(doorData);
                    foreach (var door in dataDoors)
                    {
                        ret.Add(door);
                    }
                }

            }
            return ret;
        }

        internal static ConcurrentHashSet<uint> DoorData = LoadDoorData();

        internal bool IsDoor
        {
            get
            {
                ushort iid = TypeID.Value;
                return DoorData.Contains(iid);
            }
        }


        internal static ConcurrentHashSet<int> LoadNotLootableData()
        {
            lock (LockingVar)
            {

                string pathName = Path.Combine(Assistant.Engine.RootPath, "Data", "NotLootableData.json");
                if (File.Exists(pathName))
                {
                    string notLootableData = File.ReadAllText(pathName);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<ConcurrentHashSet<int>>(notLootableData);
                }
                pathName = Path.Combine(Assistant.Engine.RootPath, "Config", "NotLootableData.json");
                if (File.Exists(pathName))
                {
                    string notLootableData = File.ReadAllText(pathName);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<ConcurrentHashSet<int>>(notLootableData);
                }
            }
            return new ConcurrentHashSet<int>();
        }

        //hair beards and horns
        static readonly ConcurrentHashSet<int> NotLootable = LoadNotLootableData();
        internal bool IsLootable
        {
            // Eventine owner found looting items was trying to loot hair and beards.
            // This caused big delay, so I will introduce this "lootable" property
            // but for now all its going to do is return true for anything
            // except beards and hair
            get
            {
                ushort iid = TypeID.Value;
                return !NotLootable.Contains(iid);
            }
        }

        internal bool IsResource
        {
            get
            {
                ushort iid = TypeID.Value;
                return (iid >= 0x19B7 && iid <= 0x19BA) || // ore
                    (iid >= 0x09CC && iid <= 0x09CF) || // fishes
                    (iid >= 0x1BDD && iid <= 0x1BE2) || // logs
                    iid == 0x1779 || // granite / stone
                    iid == 0x11EA || iid == 0x11EB // sand
                ;
            }
        }

        internal bool IsPotion
        {
            get
            {
                return (TypeID.Value >= 0x0F06 && TypeID.Value <= 0x0F0D) ||
                    TypeID.Value == 0x2790 || TypeID.Value == 0x27DB; // Ninja belt (works like a potion)
            }
        }

        internal bool IsVirtueShield
        {
            get
            {
                ushort iid = TypeID.Value;
                return (iid >= 0x1bc3 && iid <= 0x1bc5); // virtue shields
            }
        }

        internal bool IsTwoHanded
        {
            get
            {
                ushort iid = TypeID.Value;
                if (World.Player != null) // non loggato
                {
                    if (m_PropsUpdated)
                    {
                        foreach (var prop in m_ObjPropList.Content)
                        {
                            string propString = prop.ToString();
                            if (propString.ToLower().Contains("one-handed"))
                                return false;
                        }
                    }
                }
                Weapon w;
                bool found = Weapons.TryGetValue(iid, out w);
                if (found)
                    return w.Twohanded;

                return (
                        // everything in layer 2 except shields is 2handed
                        Layer == Layer.LeftHand &&
                        !((iid >= 0x1b72 && iid <= 0x1b7b) || IsVirtueShield) // shields
                    ) ||
                    // and all of these layer 1 weapons:
                    (iid == 0x13fc || iid == 0x13fd) || // hxbow
                    (iid == 0x13AF || iid == 0x13b2) || // war axe & bow
                    (iid >= 0x0F43 && iid <= 0x0F50) || // axes & xbow
                    (iid == 0x1438 || iid == 0x1439) || // war hammer
                    (iid == 0x1442 || iid == 0x1443) || // 2handed axe
                    (iid == 0x1402 || iid == 0x1403) || // short spear
                    (iid == 0x26c1 || iid == 0x26cb) || // aos gay blade
                    (iid == 0x26c2 || iid == 0x26cc) || // aos gay bow
                    (iid == 0x26c3 || iid == 0x26cd) // aos gay xbow
                ;
            }
        }

        public override string ToString()
        {
            return String.Format("{0} ({1})", this.Name, this.Serial);
        }

        internal int Price
        {
            get { return m_Price; }
            set { m_Price = value; }
        }

        internal string BuyDesc
        {
            get { return m_BuyDesc; }
            set { m_BuyDesc = value; }
        }

        internal int HouseRevision
        {
            get { return m_HouseRev; }
            set { m_HouseRev = value; }
        }

        internal byte[] HousePacket
        {
            get { return m_HousePacket; }
            set { m_HousePacket = value; }
        }
    }

    public class CorpseItem : Item
    {
        // Used for the open corpse option to ensure it is only openned first time
        public bool Opened { get; set; }
        public CorpseItem(Serial serial)
            : base(serial)
        {
            Opened = false;
        }

    }

    internal class MapItem : Item
    {

        internal static Dictionary<Serial, MapItem> MapItemHistory { get; set; } = new Dictionary<Serial, MapItem>();

        private RazorEnhanced.Point2D m_PinPosition;
        internal RazorEnhanced.Point2D PinPosition
        {
            get
            {
                if (MapItemHistory.ContainsKey(Serial))
                {
                    Utility.Logger.Debug("{0} has entry for {1:X}", System.Reflection.MethodBase.GetCurrentMethod().Name, Serial);
                    return MapItemHistory[Serial].m_PinPosition;
                }
                Utility.Logger.Debug("{0} NO entry for {1:X}", System.Reflection.MethodBase.GetCurrentMethod().Name, Serial);
                return RazorEnhanced.Point2D.Zero;
            }
            set
            {
                Utility.Logger.Debug("{0} for {1:X} at x: {2} y: {3}", System.Reflection.MethodBase.GetCurrentMethod().Name, Serial, value.X, value.Y);
                if (MapItemHistory.ContainsKey(Serial))
                {
                    MapItemHistory[Serial].m_PinPosition = value;
                }
                else
                {
                    MapItemHistory[Serial] = this;
                    MapItemHistory[Serial].m_PinPosition = value;
                }
                UpdateProperties();
            }
        }

        public RazorEnhanced.Point2D m_MapOrigin;
        internal RazorEnhanced.Point2D MapOrigin
        {
            get
            {
                if (MapItemHistory.ContainsKey(Serial))
                {
                    Utility.Logger.Debug("{0} has entry for {1:X}", System.Reflection.MethodBase.GetCurrentMethod().Name, Serial);
                    return MapItemHistory[Serial].m_MapOrigin;
                }
                Utility.Logger.Debug("{0} NO entry for {1:X}", System.Reflection.MethodBase.GetCurrentMethod().Name, Serial);
                return RazorEnhanced.Point2D.Zero;
            }
            set
            {
                Utility.Logger.Debug("{0} for {1:X} at x: {2} y: {3}", System.Reflection.MethodBase.GetCurrentMethod().Name, Serial, value.X, value.Y);
                if (MapItemHistory.ContainsKey(Serial))
                {
                    MapItemHistory[Serial].m_MapOrigin = value;
                }
                else
                {
                    MapItemHistory[Serial] = this;
                    MapItemHistory[Serial].m_MapOrigin = value;
                }
            }
        }

        public RazorEnhanced.Point2D m_MapEnd;
        internal RazorEnhanced.Point2D MapEnd
        {
            get
            {
                if (MapItemHistory.ContainsKey(Serial))
                {
                    return MapItemHistory[Serial].m_MapEnd;
                }
                return RazorEnhanced.Point2D.Zero;
            }
            set
            {
                Utility.Logger.Debug("{0} for {1:X} at x: {2} y: {3}", System.Reflection.MethodBase.GetCurrentMethod().Name, Serial, value.X, value.Y);
                if (MapItemHistory.ContainsKey(Serial))
                {
                    MapItemHistory[Serial].m_MapEnd = value;
                }
                else
                {
                    MapItemHistory[Serial] = this;
                    MapItemHistory[Serial].m_MapEnd = value;
                }
            }
        }

        static internal float Multiplier { get; set; }

        public ushort m_Facet;
        internal ushort Facet
        {
            get { return m_Facet; }
            set { m_Facet = value; }
        }

        internal MapItem(Serial serial)
            : base(serial)
        {
            m_PinPosition = new RazorEnhanced.Point2D(Point2D.Zero);
            m_MapOrigin = new RazorEnhanced.Point2D(Point2D.Zero);
            m_MapEnd = new RazorEnhanced.Point2D(Point2D.Zero);
            m_Facet = 0;
        }
        void UpdateProperties()
        {
            try
            {
                Utility.Logger.Debug("{0} for {1:X}", System.Reflection.MethodBase.GetCurrentMethod().Name, Serial);
                if (MapItemHistory.ContainsKey(Serial))
                {
                    Utility.Logger.Debug("{0} has MapHistory for {1:X}", System.Reflection.MethodBase.GetCurrentMethod().Name, Serial);
                    int xCoord = MapOrigin.X + (int)(Multiplier * PinPosition.X);
                    int yCoord = MapOrigin.Y + (int)(Multiplier * PinPosition.Y);
                    string location = String.Format("({0}, {1})",
                        xCoord,
                        yCoord
                        );
                    m_ObjPropList.AddOrReplace(new Assistant.ObjectPropertyList.OPLEntry(1061114, location));
                }
                else
                {
                    Utility.Logger.Debug("{0} No has MapHistory for {1:X}", System.Reflection.MethodBase.GetCurrentMethod().Name, Serial);
                }
            }
            catch (Exception)
            {
                m_ObjPropList.AddOrReplace(new Assistant.ObjectPropertyList.OPLEntry(1061114, "Error"));
            }
            Assistant.Client.Instance.SendToClient(new ObjectProperties(Serial, ObjPropList));

        }
        override internal void ReadPropertyList(PacketReader p)
        {
            base.ReadPropertyList(p);
            UpdateProperties();

        }
    }
}
