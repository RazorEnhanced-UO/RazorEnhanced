using AutoUpdaterDotNET;
using Mono.Options;
using RazorEnhanced;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Assistant
{
    public class FeatureBit
    {
        public static readonly int WeatherFilter = 0;
        public static readonly int LightFilter = 1;
        public static readonly int SmartLT = 2;
        public static readonly int RangeCheckLT = 3;
        public static readonly int AutoOpenDoors = 4;
        public static readonly int UnequipBeforeCast = 5;
        public static readonly int AutoPotionEquip = 6;
        public static readonly int BlockHealPoisoned = 7;
        //public static readonly int LoopingMacros = 8; // includes fors and macros running macros
        //public static readonly int UseOnceAgent = 9;
        //public static readonly int RestockAgent = 10;
        //public static readonly int SellAgent = 11;
        //public static readonly int BuyAgent = 12;
        //public static readonly int PotionHotkeys = 13;
        //public static readonly int RandomTargets = 14;
        //public static readonly int ClosestTargets = 15;
        public static readonly int OverheadHealth = 16;
        // These are reserved by Steam, not used in Razor
        public static readonly int AutolootAgent = 17;
        public static readonly int BoneCutterAgent = 18;
        //public static readonly int AdvancedMacros = 19;
        public static readonly int AutoRemount = 20;
        public static readonly int AutoBandage = 21;
        //public static readonly int EnemyTargetShare = 22;
        //public static readonly int FilterSeason = 23;
        //public static readonly int SpellTargetShare = 24;
        //public static readonly int HumanoidHealthChecks = 25;
        //public static readonly int SpeechJournalChecks = 26;
        public static readonly int PacketAgent = 27;
        public static readonly int FPSOverride = 28;

        //public static readonly int MaxBit = 28;

    }
    public abstract class Client
    {
        public static Client Instance;
        public static bool IsOSI;


        internal static bool m_Running;
        internal static bool Running { get { return m_Running; } }

        // Define GetShortPathName API function.
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern uint GetShortPathName(string lpszLongPath, char[] lpszShortPath, int cchBuffer);

        // Return the short file name for a long file name.
        internal string ShortFileName(string long_name)
        {
            char[] name_chars = new char[1024];
            long length = GetShortPathName(
                long_name, name_chars,
                name_chars.Length);

            string short_name = new(name_chars);
            return short_name.Substring(0, (int)length);
        }

        internal static RazorEnhanced.Shard SelectShard(string[] args)
        {
            if (args.Length > 0)
            {
                // these variables will be set when the command line is parsed
                var shardName = "";
                var uoPath = "";
                var cuoPath = "";
                var ip = "";
                ushort port = 0;
                bool patchEncryption = true;
                bool osiEncryption = false;
                bool cuoClientUsed = false;


                // these are the available options, note that they set the variables
                var options = new OptionSet {
                    { "d|description=", "the name of shard.", s => shardName = s },
                    { "u|uoPath=", "the path only to UO client code.", u => uoPath = u },
                    { "c|cuoPath=", "the path and .exe name for CUO.", c => cuoPath = c },
                    { "i|ip=", "the ip or dns name for the server.", i => ip = i },
                    { "p|port=", "the port number for the server. (often 2592)", p => port = Convert.ToUInt16(p) },
                    { "e|encryptPatch", "patch encryption (usually true)", e => patchEncryption = e != null },
                    { "o|osiEncryption", "use OSI encrytpion (usually only for paid UO server)", o => osiEncryption = o != null },
                    { "s|startCuoClient", "use the cuopath to start CUO instead of OSI client", s => cuoClientUsed = s != null },
                    };

                List<string> extra;
                bool error = false;
                try
                {
                    // parse the command line
                    extra = options.Parse(args);
                }
                catch (OptionException)
                {
                    RazorEnhanced.UI.RE_MessageBox.Show("Invalid Arguments passed",
                        "Error in Client.SelectShard.\r\nLooks like bad command line arguments",
                        ok: "Ok", no: null, cancel: null, backColor: null);
                    error = true;
                }
                if (!error)
                {
                    RazorEnhanced.Shard.StartType startType = Shard.StartType.OSI;
                    if (cuoClientUsed)
                    {
                        startType = Shard.StartType.CUO;
                    }
                    RazorEnhanced.Shard argsShard =
                        new("Server", Path.Combine(uoPath, "client.exe"),
                                    uoPath, cuoPath, ip, port,
                                    patchEncryption, osiEncryption, true, startType);

                    return argsShard;
                }
            }


            // Profile
            RazorEnhanced.Profiles.Load();

            // Shard Bookmarks
            RazorEnhanced.Shard.Load();

            var shards = RazorEnhanced.Shard.Read();
            m_Running = true;
            RazorEnhanced.Shard selected = shards.FirstOrDefault(s => s.Selected);

            if (((!Shards.ShowLauncher) && File.Exists(selected.ClientPath) && Directory.Exists(selected.ClientFolder) && selected != null))
            {
                return selected;
            }
            else
            {
                RazorEnhanced.UI.EnhancedLauncher launcher = new();
                DialogResult laucherdialog = DialogResult.Retry;
                while (laucherdialog == DialogResult.Retry)
                {
                    laucherdialog = launcher.ShowDialog();
                }
                if (laucherdialog == DialogResult.OK)                   // Avvia solo se premuto launch e non se exit
                {
                    shards = RazorEnhanced.Shard.Read();
                    selected = shards.FirstOrDefault(s => s.Selected);
                    if (selected == null)
                    {
                        RazorEnhanced.UI.RE_MessageBox.Show("Invalid shard",
                        "You must select a valid shard",
                        ok: "Ok", no: null, cancel: null, backColor: null);
                    }
                }
                else
                {
                    selected = null;
                }
            }
            return selected;
        }

        internal static void EnsureDirectoriesExist()
        {
            System.IO.Directory.CreateDirectory(Path.Combine(Assistant.Engine.RootPath, "Profiles"));
            System.IO.Directory.CreateDirectory(Path.Combine(Assistant.Engine.RootPath, "Backup"));
            System.IO.Directory.CreateDirectory(Path.Combine(Assistant.Engine.RootPath, "Scripts"));
        }

        internal virtual bool Init(RazorEnhanced.Shard selected)
        {
            if (selected.ClientFolder != null && Directory.Exists(selected.ClientFolder))
            {
                Ultima.FilesDirectoryOverride.Directory = selected.ClientFolder;
                Ultima.Files.SetMulPath(selected.ClientFolder);
            }
            else
            {
                RazorEnhanced.UI.RE_MessageBox.Show("Unable to find the Client Folder",
                    $"Unable to find the Client Folder\r\n{selected.ClientFolder}",
                    ok: "Ok", no: null, cancel: null, backColor: null);
                Shards.ShowLauncher = true;
                return false;
            }

            RazorEnhanced.Config.LoadAll();

            RazorEnhanced.Journal.GlobalJournal.Clear(); // really just force it to be instantiated

            EnsureDirectoriesExist();

            List<string> locations = ValidFileLocations();
            RazorEnhanced.Skills.InitData();

            // Setup AutoUpdater Parameters
            // AutoUpdater
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            AutoUpdater.ReportErrors = true;
            AutoUpdater.InstalledVersion = new Version(fvi.FileVersion);
            AutoUpdater.InstallationPath = Assistant.Engine.RootPath;
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;

            // Profile
            RazorEnhanced.Profiles.Load();
            m_Running = true;

            return true;
        }



        internal void Start(RazorEnhanced.Shard selected)
        {
            ClientEncrypted = selected.PatchEnc;
            ServerEncrypted = selected.OSIEnc;
            string clientPath = selected.ClientPath;
            string dataDir = selected.ClientFolder;
            string addr = selected.Host;
            uint port = selected.Port;

            var lang = "enu";
            string filename = Path.Combine(Assistant.Engine.RootPath,
                "Language", String.Format("Razor_lang.{0}", lang));
            Utility.Logger.Debug($"initial filename={filename}");
            filename = Utility.GetCaseInsensitiveFilePath(filename);
            Utility.Logger.Debug($"Case-fixed filename={filename}");
            Ultima.Files.Directory = selected.ClientFolder;
            Utility.Logger.Debug($"Ultima Directory - {selected.ClientFolder}");
            if (!Language.Load("enu"))
            {
                Utility.Logger.Debug($"Language.Load failed to load");
                RazorEnhanced.UI.RE_MessageBox.Show("Unable to load required file",
                    $"Unable to load the file:\r\n{filename}",
                    ok: "Ok", no: null, cancel: null, backColor: null);
                Shards.ShowLauncher = true;
                return;
            }

            Language.LoadCliLoc();

            Initialize(typeof(Assistant.Engine).Assembly);

            Assistant.Client.Instance.SetConnectionInfo(IPAddress.None, -1);
            if (IsOSI)
            {
                Assistant.Client.Loader_Error result = Assistant.Client.Loader_Error.UNKNOWN_ERROR;

                if (clientPath != null && File.Exists(clientPath))
                    result = Assistant.Client.Instance.LaunchClient(clientPath);

                if (result != Assistant.Client.Loader_Error.SUCCESS)
                {
                    if (clientPath == null || File.Exists(clientPath))
                        RazorEnhanced.UI.RE_MessageBox.Show("Unable to find the client",
                            $"Unable to find the client:\r\n{clientPath}",
                            ok: "Ok", no: null, cancel: null, backColor: null);
                    else
                        RazorEnhanced.UI.RE_MessageBox.Show("Unable to launch the client",
                                                    $"Unable to launch the client:\r\n{clientPath}",
                                                    ok: "Ok", no: null, cancel: null, backColor: null);
                    Shards.ShowLauncher = true;
                    return;
                }
                Assistant.MainForm.ForceSize();
            }


            // if these are null then the registry entry does not exist (old razor version)
            Engine.IP = Resolve(addr);
            if (Engine.IP == IPAddress.None || port == 0)
            {
                RazorEnhanced.UI.RE_MessageBox.Show(Language.GetString(LocString.BadServerAddr),
                    $"{Language.GetString(LocString.BadServerAddr)}.\r\nIP: {Engine.IP.ToString()} Port: {port.ToString()}",
                    ok: "Ok", no: null, cancel: null, backColor: null);
                Shards.ShowLauncher = true;
                return;
            }

            Engine.ClientBuild = GetBuildPart();
            Engine.ClientMajor = GetMajorPart();

            Assistant.Client.Instance.SetConnectionInfo(Engine.IP, (int)port);

            Ultima.Multis.PostHSFormat = UsePostHSChanges;

        }
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr ShowWindow(IntPtr hWnd, int nCmdShow);


        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetFocus(IntPtr hWnd);
        public static void BringToFront(IntPtr hWnd)
        {
            const int HWND_TOP = 0;
            const int SWP_NOMOVE = 2;
            const int SWP_NOSIZE = 1;
            const int SW_SHOW = 5;
            SetWindowPos(hWnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            ShowWindow(hWnd, SW_SHOW);
            SetForegroundWindow(hWnd);
            SetFocus(hWnd);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        public System.Drawing.Rectangle GetUoWindowPos()
        {
            RECT rect;
            System.Drawing.Rectangle outRect = new(-1, -1, 0, 0);
            if (GetWindowRect(new HandleRef(this, Assistant.Client.Instance.GetWindowHandle()), out rect))
            {
                outRect.X = rect.Left;
                outRect.Y = rect.Top;
                outRect.Width = rect.Right - rect.Left;
                outRect.Height = rect.Bottom - rect.Top;
            }
            return outRect;
        }

        private static void Initialize(System.Reflection.Assembly a)
        {
            Type[] types = a.GetTypes();

            foreach (Type t in types)
            {
                MethodInfo init = t.GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public);

                if (init != null)
                    init.Invoke(null, null);
            }
        }

        internal static IPAddress Resolve(string addr)
        {
            IPAddress ipAddr = IPAddress.None;

            if (string.IsNullOrEmpty(addr))
                return ipAddr;

            if (!IPAddress.TryParse(addr, out ipAddr))
            {
                try
                {
                    IPHostEntry iphe = Dns.GetHostEntry(addr);

                    if (iphe.AddressList.Length > 0)
                        ipAddr = iphe.AddressList[iphe.AddressList.Length - 1];
                }
                catch
                {
                }
            }

            return ipAddr;
        }

        internal bool Connected { get; set; }

        internal virtual void OnDisconnected()
        {
            Connected = false;
        }
        internal virtual void OnConnected()
        {
            Connected = true;
        }

        internal bool UsePostHSChanges
        {

            get
            {
                if (Engine.ClientVersion.Major > 7)
                {
                    return true;
                }
                else if (Engine.ClientVersion.Major == 7)
                {
                    if (Engine.ClientVersion.Minor > 0)
                    {
                        return true;
                    }
                    else if (Engine.ClientVersion.Build >= 9)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public const int WM_USER = 0x400;

        public const int WM_COPYDATA = 0x4A;
        public const int WM_UONETEVENT = WM_USER + 1;

        private ulong m_Features = 0;

        public bool AllowBit(int bit)
        {
            // ENABLE IT ONLY FOR DEBUG
            if (File.Exists(Path.Combine(Assistant.Engine.RootPath, "bypassnegotiate")))
                return true;
            return (m_Features & (1U << bit)) == 0;
        }

        public void SetFeatures(ulong features)
        {
            m_Features = features;
        }
        public abstract void RunUI();
        internal abstract void SelectedShard(RazorEnhanced.Shard shards);

        protected DateTime m_ConnectionStart;
        //public  DateTime ConnectionStart { get; }
        public DateTime ConnectionStart => m_ConnectionStart;

        protected static IPAddress m_LastConnection;
        public IPAddress LastConnection
        {
            get
            {
                if (m_LastConnection != null)
                    return m_LastConnection;

                if (!IsOSI)
                {
                    string server = RazorEnhanced.CUO.GetSetting("IP");
                    IPAddress address = Dns.GetHostAddresses(server)[0];
                    return address;
                }

                return Engine.IP;
            }
        }

        public bool SmartCpuChecked
        {
            get { return RazorEnhanced.Settings.General.ReadBool("SmartCPU"); }
            set
            {
                RazorEnhanced.Settings.General.WriteBool("SmartCPU", value);
                this.SetSmartCPU(value);
            }
        }

        public abstract Process ClientProcess { get; }
        public abstract bool ClientRunning { get; }

        public abstract void SetMapWndHandle(Form mapWnd);

        public abstract void RequestStatbarPatch(bool preAOS);

        public abstract void SetCustomNotoHue(int hue);

        public abstract void SetSmartCPU(bool enabled);

        public abstract void SetGameSize(int x, int y);


        public enum Loader_Error
        {
            SUCCESS = 0,
            NO_OPEN_EXE,
            NO_MAP_EXE,
            NO_READ_EXE_DATA,

            NO_RUN_EXE,
            NO_ALLOC_MEM,

            NO_WRITE,
            NO_VPROTECT,
            NO_READ,

            UNKNOWN_ERROR = 99
        };
        public abstract Loader_Error LaunchClient(string client);

        public abstract bool ClientEncrypted { get; set; }
        public abstract bool ServerEncrypted { get; set; }

        public abstract bool InstallHooks(IntPtr mainWindow);

        public abstract void SetConnectionInfo(IPAddress addr, int port);
        public abstract void SetNegotiate(bool negotiate);
        public abstract bool Attach(int pid);

        public virtual void Close()
        {
            Client.m_Running = false;

            RazorEnhanced.Settings.General.SaveExitData();

            ProtoControlServer.Instance.Stop();

            // Chiuto toolbar
            if (RazorEnhanced.ToolBar.ToolBarForm != null)
                RazorEnhanced.ToolBar.ToolBarForm.Close();

            // Chiuto Spellgrid
            if (RazorEnhanced.SpellGrid.SpellGridForm != null)
                RazorEnhanced.SpellGrid.SpellGridForm.Close();

            // Stoppo tick timer agent
            if (RazorEnhanced.Scripts.Timer != null)
                RazorEnhanced.Scripts.Timer.Close();

            // Stop forzato di tutti i thread agent
            RazorEnhanced.AutoLoot.AutoMode = false;
            RazorEnhanced.Scavenger.AutoMode = false;
            RazorEnhanced.BandageHeal.AutoMode = false;

            if (Assistant.Engine.MainWindow.OrganizerStop.Enabled == true)
                Assistant.Engine.MainWindow.OrganizerStop.PerformClick();

            if (Assistant.Engine.MainWindow.DressStopButton.Enabled == true)
                Assistant.Engine.MainWindow.DressStopButton.PerformClick();

            if (Assistant.Engine.MainWindow.RestockStop.Enabled == true)
                Assistant.Engine.MainWindow.RestockStop.PerformClick();

            ScriptRecorderService.Instance.RemoveAll();
            //RazorEnhanced.UI.EnhancedScriptEditor.End();

            RazorEnhanced.CSharpEngine.Stop();
        }


        public abstract void SetTitleStr(string str);

        public abstract bool OnMessage(MainForm razor, uint wParam, int lParam);
        public abstract bool OnCopyData(IntPtr wparam, IntPtr lparam);
        public abstract void SendToServer(Packet p);
        public abstract void SendToServer(PacketReader pr);

        public abstract void SendToClient(Packet p);
        public abstract void ForceSendToClient(Packet p);
        public abstract void ForceSendToServer(Packet p);

        // ONLY in Razor Client abstraction
        public abstract void SetPosition(uint x, uint y, uint z, byte dir);
        public abstract string GetClientVersion();
        public abstract string GetUoFilePath();
        public abstract IntPtr GetWindowHandle();
        public abstract uint TotalDataIn();

        public abstract uint TotalDataOut();

        internal abstract bool RequestWalk(Direction m_Dir);
        internal abstract bool RequestRun(Direction m_Dir);
        public abstract void PathFindTo(Assistant.Point3D location);


        public void RequestTitlebarUpdate()
        {
            // throttle updates, since things like counters might request 1000000 million updates/sec
            if (m_TBTimer == null)
                m_TBTimer = new TitleBarThrottle();

            if (!m_TBTimer.Running)
                m_TBTimer.Start();
        }

        private class TitleBarThrottle : Timer
        {
            public TitleBarThrottle() : base(TimeSpan.FromSeconds(0.25))
            {
            }

            protected override void OnTick()
            {
                Instance.UpdateTitleBar();
            }
        }

        private Timer m_TBTimer;
        public StringBuilder TitleBarBuilder = new();
        private string m_LastPlayerName = "";

        public void ResetTitleBarBuilder()
        {
            // reuse the same sb each time for less damn allocations
            //TitleBarBuilder.Remove(0, TitleBarBuilder.Length);
            //TitleBarBuilder.Insert(0, $"{Config.GetString("TitleBarText")}");
        }

        public virtual void UpdateTitleBar()
        {
            if (!ClientRunning)
                return;

            StringBuilder sb = TitleBarBuilder;

            PlayerData p = World.Player;

            if (p.Name != m_LastPlayerName)
            {
                m_LastPlayerName = p.Name;

                Engine.MainWindow.UpdateTitle();
            }

            sb.Replace(@"{shard}", World.ShardName);

            sb.Replace(@"{str}", p.Str.ToString());
            sb.Replace(@"{hpmax}", p.HitsMax.ToString());

            sb.Replace(@"{dex}", World.Player.Dex.ToString());
            sb.Replace(@"{stammax}", World.Player.StamMax.ToString());

            sb.Replace(@"{int}", World.Player.Int.ToString());
            sb.Replace(@"{manamax}", World.Player.ManaMax.ToString());

            sb.Replace(@"{ar}", p.AR.ToString());
            sb.Replace(@"{tithe}", p.Tithe.ToString());

            sb.Replace(@"{physresist}", p.AR.ToString());
            sb.Replace(@"{fireresist}", p.FireResistance.ToString());
            sb.Replace(@"{coldresist}", p.ColdResistance.ToString());
            sb.Replace(@"{poisonresist}", p.PoisonResistance.ToString());
            sb.Replace(@"{energyresist}", p.EnergyResistance.ToString());

            sb.Replace(@"{luck}", p.Luck.ToString());

            sb.Replace(@"{damage}", String.Format("{0}-{1}", p.DamageMin, p.DamageMax));

            sb.Replace(@"{maxweight}", World.Player.MaxWeight.ToString());

            sb.Replace(@"{followers}", World.Player.Followers.ToString());
            sb.Replace(@"{followersmax}", World.Player.FollowersMax.ToString());

            sb.Replace(@"{gold}", World.Player.Gold.ToString());

            //sb.Replace(@"{gps}", GoldPerHourTimer.Running ? $"{GoldPerHourTimer.GoldPerSecond:N2}" : "-");
            //sb.Replace(@"{gpm}", GoldPerHourTimer.Running ? $"{GoldPerHourTimer.GoldPerMinute:N2}" : "-");
            //sb.Replace(@"{gph}", GoldPerHourTimer.Running ? $"{GoldPerHourTimer.GoldPerHour:N2}" : "-");
            //sb.Replace(@"{goldtotal}", GoldPerHourTimer.Running ? $"{GoldPerHourTimer.GoldSinceStart}" : "-");
            //sb.Replace(@"{goldtotalmin}", GoldPerHourTimer.Running ? $"{GoldPerHourTimer.TotalMinutes:N2} min" : "-");

            //sb.Replace(@"{skill}", SkillTimer.Running ? $"{SkillTimer.Count}" : "-");
            //sb.Replace(@"{gate}", GateTimer.Running ? $"{GateTimer.Count}" : "-");

            sb.Replace(@"{stealthsteps}", StealthSteps.Counting ? StealthSteps.Count.ToString() : "-");
            //Client.ConnectionStart != DateTime.MinValue )
            //time = (int)((DateTime.UtcNow - Client.ConnectionStart).TotalSeconds);
            sb.Replace(@"{uptime}",
                ConnectionStart != DateTime.MinValue
                    ? Utility.FormatTime((int)((DateTime.UtcNow - ConnectionStart).TotalSeconds))
                    : "-");

            // sb.Replace(@"{dps}", DamageTracker.Running ? $"{DamageTracker.DamagePerSecond:N2}" : "-");
            //sb.Replace(@"{maxdps}", DamageTracker.Running ? $"{DamageTracker.MaxDamagePerSecond:N2}" : "-");
            //sb.Replace(@"{maxdamagedealt}", DamageTracker.Running ? $"{DamageTracker.MaxSingleDamageDealt}" : "-");
            //sb.Replace(@"{maxdamagetaken}", DamageTracker.Running ? $"{DamageTracker.MaxSingleDamageTaken}" : "-");
            //sb.Replace(@"{totaldamagedealt}", DamageTracker.Running ? $"{DamageTracker.TotalDamageDealt}" : "-");
            //sb.Replace(@"{totaldamagetaken}", DamageTracker.Running ? $"{DamageTracker.TotalDamageTaken}" : "-");


            string buffList = string.Empty;

#if notimplemented
            if (BuffsTimer.Running)
            {
                StringBuilder buffs = new StringBuilder();
                foreach (BuffsDebuffs buff in World.Player.BuffsDebuffs)
                {
                    int timeLeft = 0;

                    if (buff.Duration > 0)
                    {
                        TimeSpan diff = DateTime.UtcNow - buff.Timestamp;
                        timeLeft = buff.Duration - (int)diff.TotalSeconds;
                    }

                    buffs.Append(timeLeft <= 0
                        ? $"{buff.ClilocMessage1}, "
                        : $"{buff.ClilocMessage1} ({timeLeft}), ");
                }

                buffs.Length = buffs.Length - 2;
                buffList = buffs.ToString();
                sb.Replace(@"{buffsdebuffs}", buffList);
            }
            else
            {
                sb.Replace(@"{buffsdebuffs}", "-");
            }
#endif
            SetTitleStr(sb.ToString());
        }

        public Packet MakePacketFrom(PacketReader pr)
        {
            byte[] data = pr.CopyBytes(0, pr.Length);
            return new Packet(data, pr.Length, pr.DynamicLength);
        }


        // NOT IN Razor Client abstract definition
        public abstract bool Ready { get; }

        public abstract void InitSendFlush();
        public abstract void BeginCalibratePosition();
        public abstract void SendToClientWait(Packet p);
        public abstract void SendToServerWait(Packet p);

        public abstract List<string> ValidFileLocations();

        // Used for auto update  prompt
        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args != null)
            {
                if (args.IsUpdateAvailable)
                {
                    DialogResult dialogResult;

                    dialogResult = RazorEnhanced.UI.RE_MessageBox.Show("New Version Available",
                            $@"There is new version {args.CurrentVersion} available.\r\nYou are using version {args.InstalledVersion}.\r\nDo you want to update the application now?",
                            ok: "Yes", no: "No", cancel: null, backColor: null);

                    if (dialogResult.Equals(DialogResult.OK))
                    {
                        try
                        {
                            string[] argsx = Environment.GetCommandLineArgs();
                            if (AutoUpdater.DownloadUpdate(args))
                            {
                                if (Client.Instance.ClientRunning)
                                    if (Client.IsOSI)
                                        Assistant.Client.Instance.ClientProcess.Kill();
                                    else
                                    {
                                        var client = ClassicUOClient.CUOAssembly?.GetType("ClassicUO.Client");
                                        var game = client.GetProperty("Game", BindingFlags.Public | BindingFlags.Static);
                                        if (game != null)
                                        {
                                            var gameController = game.GetValue(null, null);
                                            if (gameController != null)
                                            {
                                                var GameController = ClassicUOClient.CUOAssembly?.GetType("ClassicUO.GameController");
                                                if (GameController != null)
                                                {
                                                    var exitMethod = GameController.GetMethod("Exit", BindingFlags.Instance | BindingFlags.Public);
                                                    if (exitMethod != null)
                                                    {
                                                        exitMethod.Invoke(gameController, new object[] { });
                                                    }
                                                }
                                            }
                                        }
                                    }
                                Application.Exit();
                                Thread.Sleep(2000); // attesa uscita
                            }
                        }
                        catch (Exception exception)
                        {
                            RazorEnhanced.UI.RE_MessageBox.Show("Update Exception Occurred",
                                    $@"{exception.Message}\r\nType: {exception.GetType().ToString()}",
                                    ok: "Ok", no: null, cancel: null, backColor: null);
                        }
                    }
                    else
                    {
                        string reminderPath = Path.Combine(Assistant.Engine.RootPath, "Profiles", "RazorEnhanced.reminder.json");
                        DateTime reminderDate = DateTime.Now.AddDays(7);
                        string xml = Newtonsoft.Json.JsonConvert.SerializeObject(reminderDate, Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(reminderPath, xml);
                    }
                }
            }
            else
            {
                RazorEnhanced.UI.RE_MessageBox.Show("Update Server Unavailable",
                                    @"There is a problem reaching update server please check your internet connection and try again later.",
                                    ok: "Ok", no: null, cancel: null, backColor: null);
            }

        }

        public abstract int GetBuildPart();
        public abstract int GetMajorPart();

    }
}
