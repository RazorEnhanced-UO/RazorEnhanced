using CrashReporterDotNET;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace Assistant
{
    public partial class Engine
    {
        private static string _rootPath = null;

        public static string RootPath =>
            _rootPath ?? (_rootPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Engine)).Location));

        private static IPAddress m_ip;

        internal static IPAddress IP
        {
            get { return m_ip; }
            set { m_ip = value; }
        }

        internal static void LogCrash(object exception)
        {
            if (exception == null || (exception is ThreadAbortException))
                return;

            ReportCrash((Exception)exception);

            using StreamWriter txt = new("Crash.log", true);
            txt.AutoFlush = true;
            txt.WriteLine("Exception @ {0}", DateTime.Now.ToString("MM-dd-yy HH:mm:ss.ffff"));
            txt.WriteLine(exception.ToString());
            txt.WriteLine("");
            txt.WriteLine("");
        }

        private static Version m_ClientVersion = null;

        internal static Version ClientVersion
        {
            get
            {
                if (m_ClientVersion == null || m_ClientVersion.Major < 2)
                {
                    string str = Client.Instance.GetClientVersion();
                    string[] split = str.Split('.');
                    if (split.Length < 3)
                        return new Version(4, 0, 0, 0);

                    int rev = 0;

                    if (split.Length > 3)
                        rev = Utility.ToInt32(split[3], 0);

                    m_ClientVersion = new Version(
                        Utility.ToInt32(split[0], 0),
                        Utility.ToInt32(split[1], 0),
                        Utility.ToInt32(split[2], 0),
                        rev);

                    if (m_ClientVersion.Major == 0) // sanity check if the client returns 0.0.0.0
                        m_ClientVersion = new Version(4, 0, 0, 0);
                }

                return m_ClientVersion;
            }
        }

        internal static int ClientBuild = 50;
        internal static int ClientMajor = 6;

        internal static bool UseNewMobileIncoming
        {
            get
            {
                if (ClientVersion.Major > 7)
                {
                    return true;
                }
                else if (ClientVersion.Major == 7)
                {
                    if (ClientVersion.Minor > 0 || ClientVersion.Build >= 33)
                    {
                        return true;
                    }
                }

                return false;
            }
        }


        internal static bool UsePostSAChanges
        {
            get
            {
                if (ClientVersion.Major >= 7)
                {
                    return true;
                }

                return false;
            }
        }

        internal static bool UsePostKRPackets
        {
            get
            {
                if (ClientVersion.Major >= 7)
                {
                    return true;
                }
                else if (ClientVersion.Major >= 6)
                {
                    if (ClientVersion.Minor == 0)
                    {
                        if (ClientVersion.Build == 1)
                        {
                            if (ClientVersion.Revision >= 7)
                                return true;
                        }
                        else if (ClientVersion.Build > 1)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        //internal static string ExePath { get { return Process.GetCurrentProcess().MainModule.FileName; } }
        internal static MainForm MainWindow { get { return MainWnd; } }
        internal static Form ActiveWindow { get { return m_ActiveWnd; } set { m_ActiveWnd = value; } }

        // Blocco parametri salvataggio uscita
        internal static int MainWindowX { get { return m_MainWindowX; } set { m_MainWindowX = value; } }

        internal static int MainWindowY { get { return m_MainWindowY; } set { m_MainWindowY = value; } }
        internal static int ToolBarX { get { return m_ToolBarX; } set { m_ToolBarX = value; } }
        internal static int ToolBarY { get { return m_ToolBarY; } set { m_ToolBarY = value; } }
        internal static int GridX { get { return m_GridX; } set { m_GridX = value; } }
        internal static int GridY { get { return m_GridY; } set { m_GridY = value; } }
        internal static bool CDepPresent { get { return m_cdeppresent; } set { m_cdeppresent = value; } }

        internal static string Version
        {
            get
            {
                if (m_Version == null)
                {
                    Version v = Assembly.GetCallingAssembly().GetName().Version;
                    m_Version = String.Format("{0}.{1}.{2}.{3}", v.Major, v.Minor, v.Build, v.Revision); //v.Revision
                }

                return m_Version;
            }
        }

        internal static string ShardList { get; private set; }

        public static MainForm MainWnd;
        private static Form m_ActiveWnd;
        private static bool m_cdeppresent = true;
        private static int m_ToolBarX;
        private static int m_ToolBarY;
        private static int m_GridX;
        private static int m_GridY;
        private static int m_MainWindowX;
        private static int m_MainWindowY;

        private static string m_Version;

        [STAThread]
        public static void Main(string[] args)
        {


            Application.ThreadException += ApplicationThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Thread.CurrentThread.Name = "Razor Main Thread";

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Set default font
            System.Drawing.Font defaultFont = new("Arial", 8);

            RazorEnhanced.AutoDocIO.UpdateDocs();
            // these are the available options, note that they set the variables
            bool autoDocsOnly = false;
            bool showHelp = false;
            var options = new OptionSet {
                    { "a|autoDocsOnly", "Exit after generating autodocs", a => autoDocsOnly = a != null },
                    { "h|help", "show help on console and exit", h => showHelp = h != null },
                    // The following are parsed in Client.SelectShard but listed here for help 
                    { "d|description=", "the name of shard.", (x) => { } },
                    { "u|uoPath=", "the path only to UO client code.", (x) => { } },
                    { "c|cuoPath=", "the path and .exe name for CUO.", (x) => { } },
                    { "i|ip=", "the ip or dns name for the server.", (x) => { } },
                    { "p|port=", "the port number for the server. (often 2592)", (x) => { } },
                    { "e|encryptPatch", "patch encryption (usually true)", (x) => { } },
                    { "o|osiEncryption", "use OSI encrytpion (usually only for paid UO server)", (x) => { } },
                    { "s|startCuoClient", "use the cuopath to start CUO instead of OSI client", (x) => { } },
                    };

            List<string> extra = new();
            try
            {
                // parse the command line
                extra = options.Parse(args);
            }
            catch (OptionException)
            {
                showHelp = true;
            }
            if (extra.Count > 0 || showHelp)
            {
                StringWriter writer = new();
                options.WriteOptionDescriptions(writer);
                string descr = writer.ToString();
                RazorEnhanced.UI.RE_MessageBox.Show("Invalid Arguments", descr,
                    ok: "Ok", no: null, cancel: null, backColor: null);
                return;
            }
            if (autoDocsOnly)
            {
                return;
            }

            Client.EnsureDirectoriesExist();
            RazorEnhanced.Shard shardSelected = Client.SelectShard(args);
            if (shardSelected != null)
            {
                if (shardSelected.StartTypeSelected == RazorEnhanced.Shard.StartType.CUO)
                {
                    Client.Instance = new ClassicUOClient();
                    Client.Instance.SelectedShard(shardSelected);
                    Client.Instance.Init(shardSelected);
                }
                else
                {
                    Client.Instance = new OSIClient();
                    Client.Instance.SelectedShard(shardSelected);
                    Client.Instance.Init(shardSelected);
                    Client.Instance.RunUI();
                    Client.Instance.Close();
                }

            }
        }



        internal static IPAddress Resolve(string addr)
        {
            IPAddress ipAddr = IPAddress.None;

            if (string.IsNullOrEmpty(addr))
                return ipAddr;

            try
            {
                ipAddr = IPAddress.Parse(addr);
            }
            catch
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

        internal static bool IsElevated
        {
            get
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            ReportCrash((Exception)unhandledExceptionEventArgs.ExceptionObject);
            Environment.Exit(0);
        }

        private static void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
        {
            if (e.Exception is FileNotFoundException || e.Exception is FileLoadException || e.Exception is BadImageFormatException) 
                if (Assistant.Engine.MainWindow != null)
                    Assistant.Engine.MainWindow.DisableRecorder();
            else
                ReportCrash(e.Exception);
        }

        internal static void ReportCrash(Exception exception)
        {
            ReportCrash reportCrash = new("razorenhanced@gmail.com");
            reportCrash.CaptureScreen = true;
            reportCrash.IncludeScreenshot = true;
            reportCrash.DoctorDumpSettings = new DoctorDumpSettings
            {
                ApplicationID = new Guid("87af7b0b-2407-4944-b572-caee9d031325"),
            };
            reportCrash.Send(exception);
        }
    }
}
