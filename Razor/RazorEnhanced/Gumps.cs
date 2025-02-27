using Assistant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace RazorEnhanced
{
    /// <summary>
    /// The Gumps class is used to read and interact with in-game gumps, via scripting.
    /// 
    /// NOTE
    /// ----
    /// During development of scripts that involves interecting with Gumps, is often needed to know gumpids and buttonids.
    /// For this purpose, can be particularly usefull to use *Inspect Gumps* and *Record*, top right, in the internal RE script editor.
    /// </summary>

    public class Gumps
    {
        internal static Mutex gumpIdMutex = new();
        internal static void AddGump(uint gumpSerial, uint gumpId)
        {
            gumpIdMutex.WaitOne(1500);
            try
            {
                var incomingGump = new IncomingGumpData();
                incomingGump.gumpSerial = gumpSerial;
                incomingGump.gumpId = gumpId;
                m_incomingData[gumpId] = incomingGump;
            }
            finally
            {
                gumpIdMutex.ReleaseMutex();
            }
        }
        internal static void RemoveGump(uint gumpId)
        {
            gumpIdMutex.WaitOne(1500);
            try
            {
                if (m_incomingData.ContainsKey(gumpId))
                    m_incomingData.Remove(gumpId);
            }
            finally
            {
                gumpIdMutex.ReleaseMutex();
            }
        }

        // easy access to get text by id
        public static string GetTextByID(GumpData gd, int id)
        {
            for (int i = 0; i < gd.textID.Count; i++)
            {
                if (gd.textID[i] == id)
                {
                    return gd.text[i];
                }
            }
            return null;
        }

        public static bool HasGump(uint gumpId)
        {
            gumpIdMutex.WaitOne(500);
            try
            {
                return m_incomingData.ContainsKey(gumpId);
            }
            finally
            {
                gumpIdMutex.ReleaseMutex();
            }
        }
        public static List<uint> AllGumpIDs()
        {
            gumpIdMutex.WaitOne(500);
            try
            {
                return m_incomingData.Keys.ToList();
            }
            finally
            {
                gumpIdMutex.ReleaseMutex();
            }
        }

        public enum GumpButtonType
        {
            Page = 0,
            Reply = 1
        }

        internal class IncomingGumpData
        {
            public uint gumpSerial;
            public uint gumpId;
            public uint x;
            public uint y;
            public string gumpLayout;
            public List<string> gumpText;
            public List<string> gumpData;
            internal List<string> layoutPieces;
            internal List<string> stringList;

            public IncomingGumpData()
            {
                gumpSerial = 0;
                gumpId = 0;
                x = 0;
                y = 0;
                gumpLayout = "";
                gumpText = new List<string>();
                gumpData = new List<string>();
            }
        }
        public class GumpData
        {
            // vars used to build it
            public uint gumpId;
            public uint serial;
            public uint x;
            public uint y;
            public string gumpDefinition;
            public List<string> gumpStrings;
            //  data returned
            public bool hasResponse;
            public int buttonid;
            public List<int> switches;
            public List<string> text;
            public List<int> textID;
            internal Action<GumpData> action;
            public string gumpLayout;
            public List<string> gumpText;
            public List<string> gumpData;
            public List<string> layoutPieces;
            public List<string> stringList;

            public GumpData()
            {
                gumpId = 0;
                serial = 0;
                x = 0;
                y = 0;
                gumpDefinition = "";
                gumpStrings = new List<string>();
                hasResponse = false;
                buttonid = -1;
                switches = new List<int>();
                text = new List<string>();
                textID = new List<int>();
                action = null;
                gumpLayout = "";
                gumpText = new List<string>();
            }
        }

        /// <summary>
        /// Validates if the gumpid provided exists in the gump file
        /// </summary>
        /// <param name="gumpId"> The id of the gump to check for in the gumps.mul file</param>
        public static bool IsValid(int gumpId)
        {
            return Ultima.Gumps.IsValidIndex(gumpId);
        }

        /// <summary>
        /// Creates an initialized GumpData structure
        /// </summary>
        /// <param name="movable"> allow the gump to be moved</param>
        /// <param name="closable"> allow the gump to be right clicked to close</param>
        /// <param name="disposable"> allow the gump to be disposed (beats me what it does)</param>
        /// <param name="resizeable"> allow the gump to be resized</param>
        public static GumpData CreateGump(bool movable = true, bool closable = true, bool disposable = true, bool resizeable = true)
        {
            GumpData gd = new();
            if (!movable)
                gd.gumpDefinition += "{ nomove}";
            if (!closable)
                gd.gumpDefinition += "{ noclose}";
            if (!disposable)
                gd.gumpDefinition += "{ nodispose}";
            if (!resizeable)
                gd.gumpDefinition += "{ noresize}";
            return gd;
        }

        /// <summary>
        /// Add a page for the gump to have additional pages
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="page"> the number of the page being added</param>
        public static void AddPage(ref GumpData gd, int page)
        {
            string textEntry = String.Format("{{ page {0} }}", page);
            gd.gumpDefinition += textEntry;
        }
        /// <summary>
        /// Add an alpha region for the gump. Its a transparent background
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="width"> width of the transparent backround</param>
        /// <param name="height"> height of the transparent backround</param>
        public static void AddAlphaRegion(ref GumpData gd, int x, int y, int width, int height)
        {
            string textEntry = String.Format("{{ checkertrans {0} {1} {2} {3} }}", x, y, width, height);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// Add a textured background for the gump. Its a transparent background
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="width"> width of the transparent backround</param>
        /// <param name="height"> height of the transparent backround</param>
        /// <param name="gumpId"> The gumpId from gumps.mul that will be used for background</param>
        public static void AddBackground(ref GumpData gd, int x, int y, int width, int height, int gumpId)
        {
            string textEntry = String.Format("{{ resizepic {0} {1} {2} {3} {4} }}", x, y, gumpId, width, height);
            gd.gumpDefinition += textEntry;
        }
        /// <summary>
        /// Add a button to the gump
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="normalID"> id of the button to used when unpressed</param>
        /// <param name="pressedID"> id of the button to show when button is pressed</param>
        /// <param name="buttonID"> button id to return if this is pressed</param>
        /// <param name="type"> button can have a type of 0 - Page or 1 - Reply (I have no idea what Page does)</param>
        /// <param name="param"> button can have a param of any integer (I have no idea what param does)</param>
        public static void AddButton(ref GumpData gd, int x, int y, int normalID, int pressedID, int buttonID, int type, int param)
        {
            string textEntry = String.Format("{{ button {0} {1} {2} {3} {4} {5} {6} }}", x, y, normalID, pressedID, type, param, buttonID);
            gd.gumpDefinition += textEntry;
        }
        /// <summary>
        /// Add a checkbox to the gump
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="inactiveID"> id of the checkmark to used when unclicked</param>
        /// <param name="activeID"> id of the checkmark to use when clicked</param>
        /// <param name="initialState"> active or inactive initially</param>
        /// <param name="switchID"> switch id to return if this is changed</param>
        public static void AddCheck(ref GumpData gd, int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
        {
            string textEntry = String.Format("{{ checkbox {0} {1} {2} {3} {4} {5} }}", x, y, inactiveID, activeID, initialState ? 1 : 0, switchID);
            gd.gumpDefinition += textEntry;
        }
        /// <summary>
        /// Add group to the gump
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="group"> group identifier (I have no idea what this control does)</param>
        public static void AddGroup(ref GumpData gd, int group)
        {
            string textEntry = String.Format("{{ group {0} }}", group);
            gd.gumpDefinition += textEntry;
        }
        /// <summary>
        /// Add tooltip to the previously added control
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="number"> cliloc for tooltip</param>
        public static void AddTooltip(ref GumpData gd, int number)
        {
            string textEntry = string.Format("{{ tooltip {0} }}", number);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// Add tooltip to the previously added control
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="text"> string for tooltip</param>
        public static void AddTooltip(ref GumpData gd, string text)
        {
            string textEntry = string.Format("{{ tooltip {0} @{1}@ }}", 1114778, text);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// Add tooltip to the previously added control
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="cliloc"> cliloc for tooltip</param>
        /// <param name="text"> string for tooltip</param>
        public static void AddTooltip(ref GumpData gd, int cliloc, string text)
        {
            string textEntry = string.Format("{{ tooltip {0} @{1}@ }}", cliloc, text);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// Add a textured background for the gump. Its a transparent background
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="width"> width of the html block</param>
        /// <param name="height"> height of the html block</param>
        /// <param name="text"> The html text to be shown</param>
        /// <param name="background"> False makes background transparent</param>
        /// <param name="scrollbar"> True adds a scroll bar to the control</param>
        public static void AddHtml(ref GumpData gd, int x, int y, int width, int height, string text, bool background, bool scrollbar)
        {
            gd.gumpStrings.Add(text);
            AddHtml(ref gd, x, y, width, height, gd.gumpStrings.Count - 1, background, scrollbar);

        }
        /// <summary>
        /// Add a textured background for the gump. Its a transparent background
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="width"> width of the html block</param>
        /// <param name="height"> height of the html block</param>
        /// <param name="textID"> An index (zero based) into the string being passed</param>
        /// <param name="background"> False makes background transparent</param>
        /// <param name="scrollbar"> True adds a scroll bar to the control</param>
        public static void AddHtml(ref GumpData gd, int x, int y, int width, int height, int textID, bool background, bool scrollbar)
        {
            string textEntry = String.Format("{{ htmlgump {0} {1} {2} {3} {4} {5} {6} }}", x, y, width, height, textID, background ? 1 : 0, scrollbar ? 1 : 0);
            if (gd.gumpStrings.Count > textID)
                gd.gumpDefinition += textEntry;
            else
            {
                // I think this is not a good thing
            }
        }

        /// <summary>
        /// No idea at all why this is different than the OTHER htmml, but SERVEUO had it
        /// </summary>
        public static void AddHtmlLocalized(ref GumpData gd, int x, int y, int width, int height, int number, bool background, bool scrollbar)
        {
            string textEntry = String.Format("{{ xmfhtmlgump {0} {1} {2} {3} {4} {5} {6} }}", x, y, width, height, number, background ? 1 : 0, scrollbar ? 1 : 0);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// No idea at all why this is different than the OTHER htmml, but SERVEUO had it
        /// </summary>
        public static void AddHtmlLocalized(ref GumpData gd, int x, int y, int width, int height, int number, int color, bool background, bool scrollbar)
        {
            string textEntry = String.Format("{{ xmfhtmlgumpcolor {0} {1} {2} {3} {4} {5} {6} {7} }}", x, y, width, height, number, background ? 1 : 0, scrollbar ? 1 : 0, color);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// No idea at all why this is different than the OTHER htmml, but SERVEUO had it
        /// </summary>
        public static void AddHtmlLocalized(ref GumpData gd, int x, int y, int width, int height, int number, string args, int color, bool background, bool scrollbar)
        {
            string textEntry = String.Format("{{ xmfhtmltok {0} {1} {2} {3} {4} {5} {6} {7} @{8}@ }}", x, y, width, height, background ? 1 : 0, scrollbar ? 1 : 0, color, number, args);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// Add image from the Gumps.mul
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="gumpId"> id used to reference gumps.mul</param>
        public static void AddImage(ref GumpData gd, int x, int y, int gumpId)
        {
            string textEntry = String.Format("{{ gumppic {0} {1} {2} }}", x, y, gumpId);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// Add image from a sprite sheet
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> X co-ordinate of the origin</param>
        /// <param name="y"> Y co-ordinate of the origin</param>
        /// <param name="gumpId"> ID used to reference sprite sheet from gumps.mul</param>
        /// <param name="spriteX"> X position on the sprite sheet where the sprite begins</param>
        /// <param name="spriteY"> Y position on the sprite sheet where the sprite begins</param>
        /// <param name="spriteW"> Width of the sprite</param>
        /// <param name="spriteH"> Height of the sprite</param>
        public static void AddSpriteImage(ref GumpData gd, int x, int y, int gumpId, int spriteX, int spriteY, int spriteW, int spriteH)
        {
            string textEntry = String.Format("{{ picinpic {0} {1} {2} {3} {4} {5} {6} }}", x, y, gumpId, spriteX, spriteY, spriteW, spriteH);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// Add hued image from the Gumps.mul
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="gumpId"> id used to reference gumps.mul</param>
        /// <param name="hue"> to re-color the image</param>
        public static void AddImage(ref GumpData gd, int x, int y, int gumpId, int hue)
        {
            string textEntry = String.Format("{{ gumppic {0} {1} {2} hue={3} }}", x, y, gumpId, hue);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// Add image from the Gumps.mul replicated enough to fill an area
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="width"> width of the area</param>
        /// <param name="height"> height of the area</param>
        /// <param name="gumpId">id of gump to be added</param>
        public static void AddImageTiled(ref GumpData gd, int x, int y, int width, int height, int gumpId)
        {
            string textEntry = String.Format("{{ gumppictiled {0} {1} {2} {3} {4} }}", x, y, width, height, gumpId);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// Add button from the Gumps.mul replicated enough to fill an area (guessing)
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="normalID"> id of the button to used when unpressed</param>
        /// <param name="pressedID"> id of the button to show when button is pressed</param>
        /// <param name="buttonID"> button id to return if this is pressed</param>
        /// <param name="type"> button can have a type of 0 - Page or 1 - Reply (I have no idea what Page does)</param>
        /// <param name="param"> button can have a param of any integer (I have no idea what param does)</param>
        /// <param name="itemID"> maybe the button id to be used?</param>
        /// <param name="hue"> color to apply to image</param>
        /// <param name="width"> width of the area</param>
        /// <param name="height"> height of the area</param>       
        public static void AddImageTiledButton(ref GumpData gd,
            int x,
            int y,
            int normalID,
            int pressedID,
            int buttonID,
            GumpButtonType type,
            int param,
            int itemID,
            int hue,
            int width,
            int height)
        {
            string textEntry = String.Format("{{ buttontileart {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} }}", x, y, normalID, pressedID, (int)type, param, buttonID, itemID, hue, width, height);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// Add button from the Gumps.mul replicated enough to fill an area (guessing)
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="normalID"> id of the button to used when unpressed</param>
        /// <param name="pressedID"> id of the button to show when button is pressed</param>
        /// <param name="buttonID"> button id to return if this is pressed</param>
        /// <param name="type"> button can have a type of 0 - Page or 1 - Reply (I have no idea what Page does)</param>
        /// <param name="param"> button can have a param of any integer (I have no idea what param does)</param>
        /// <param name="itemID"> maybe the button id to be used?</param>
        /// <param name="hue"> color to apply to image</param>
        /// <param name="width"> width of the area</param>
        /// <param name="height"> height of the area</param>       
        /// <param name="localizedTooltip"> cliloc to use as tooltip</param> 
        public static void AddImageTiledButton(ref GumpData gd,
            int x,
            int y,
            int normalID,
            int pressedID,
            int buttonID,
            GumpButtonType type,
            int param,
            int itemID,
            int hue,
            int width,
            int height,
            int localizedTooltip)
        {
            string textEntry = String.Format("{{ buttontileart {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} }}{{ tooltip {11} }}", x, y, normalID, pressedID, (int)type, param, buttonID, itemID, hue, width, height, localizedTooltip);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// Add an item from the statics.mul
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="itemID"> id used to reference statics.mul</param>
        public static void AddItem(ref GumpData gd, int x, int y, int itemID)
        {
            string textEntry = String.Format("{{ tilepic {0} {1} {2} }}", x, y, itemID);
            gd.gumpDefinition += textEntry;
        }
        /// <summary>
        /// Add a re-colored item from the statics.mul
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="itemID"> id used to reference statics.mul</param>
        /// <param name="hue"> to re-color the image</param>
        public static void AddItem(ref GumpData gd, int x, int y, int itemID, int hue)
        {
            string textEntry = String.Format("{{ tilepichue {0} {1} {2} {3} }}", x, y, itemID, hue);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// Add colored text to gump
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="hue"> to color the text</param>
        /// <param name="text"> text string to be displayed</param>
        public static void AddLabel(ref GumpData gd, int x, int y, int hue, string text)
        {
            gd.gumpStrings.Add(text);
            string textEntry = String.Format("{{ text {0} {1} {2} {3} }}", x, y, hue, gd.gumpStrings.Count - 1);
            gd.gumpDefinition += textEntry;
        }
        /// <summary>
        /// Add colored text to gump
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="hue"> to color the text</param>
        /// <param name="textID"> index into string passed to the gump</param>
        public static void AddLabel(ref GumpData gd, int x, int y, int hue, int textID)
        {
            string textEntry = String.Format("{{ text {0} {1} {2} {3} }}", x, y, hue, textID);
            if (gd.gumpStrings.Count > textID)
                gd.gumpDefinition += textEntry;
            else
            {
                // I think this is not a good thing
            }
        }

        /// <summary>
        /// Add colored text to gump, will be truncated if area is too small
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="width"> width of the area</param>
        /// <param name="height"> height of the area</param>       
        /// <param name="hue"> to color the text</param>
        /// <param name="text"> text string to be displayed</param>
        public static void AddLabelCropped(ref GumpData gd, int x, int y, int width, int height, int hue, string text)
        {
            gd.gumpStrings.Add(text);
            AddLabelCropped(ref gd, x, y, width, height, hue, gd.gumpStrings.Count - 1);
        }
        /// <summary>
        /// Add colored text to gump, will be truncated if area is too small
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="width"> width of the area</param>
        /// <param name="height"> height of the area</param>       
        /// <param name="hue"> to color the text</param>
        /// <param name="textID"> index into string list passed to gump</param>
        public static void AddLabelCropped(ref GumpData gd, int x, int y, int width, int height, int hue, int textID)
        {
            string textEntry = String.Format("{{ croppedtext {0} {1} {2} {3} {4} {5} }}", x, y, width, height, hue, textID);
            if (gd.gumpStrings.Count > textID)
                gd.gumpDefinition += textEntry;
            else
            {
                // I think this is not a good thing
            }
        }
        /// <summary>
        /// Add a radio button to the gump
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="inactiveID"> id of the checkmark to used when unclicked</param>
        /// <param name="activeID"> id of the checkmark to use when clicked</param>
        /// <param name="initialState"> active or inactive initially</param>
        /// <param name="switchID"> switch id to return if this is changed</param>
        public static void AddRadio(ref GumpData gd, int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
        {
            string textEntry = String.Format("{{ radio {0} {1} {2} {3} {4} {5} }}", x, y, inactiveID, activeID, initialState ? 1 : 0, switchID);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// Add a text entry field to the gump
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="width"> width of the area</param>
        /// <param name="height"> height of the area</param>       
        /// <param name="hue"> to color the text</param>
        /// <param name="entryID"> id to be returned with text to identify the input field</param>
        /// <param name="initialText"> initial text string to be displayed</param>
        public static void AddTextEntry(ref GumpData gd, int x, int y, int width, int height, int hue, int entryID, string initialText)
        {
            gd.gumpStrings.Add(initialText);
            string textEntry = String.Format("{{ textentry {0} {1} {2} {3} {4} {5} {6} }}", x, y, width, height, hue, entryID, gd.gumpStrings.Count - 1);
            gd.gumpDefinition += textEntry;
        }

        /// <summary>
        /// Add a text entry field to the gump
        /// </summary>
        /// <param name="gd"> GumpData structure</param>
        /// <param name="x"> x co-ordinate of the origin</param>
        /// <param name="y"> y co-ordinate of the origin</param>
        /// <param name="width"> width of the area</param>
        /// <param name="height"> height of the area</param>       
        /// <param name="hue"> to color the text</param>
        /// <param name="entryID"> id to be returned with text to identify the input field</param>
        /// <param name="initialTextID"> index into the list of strings passed to the gump</param>
        public static void AddTextEntry(ref GumpData gd, int x, int y, int width, int height, int hue, int entryID, int initialTextID)
        {
            string textEntry = String.Format("{{ textentry {0} {1} {2} {3} {4} {5} {6} }}", x, y, width, height, hue, entryID, initialTextID);
            if (gd.gumpStrings.Count > initialTextID)
                gd.gumpDefinition += textEntry;
            else
            {
                // I think this is not a good thing
            }
        }


        internal static Dictionary<uint, GumpData> m_gumpData = new();
        internal static Dictionary<uint, IncomingGumpData> m_incomingData = new();

        /// <summary>
        /// Sends a gump using an existing GumpData structure
        /// </summary>
        ///
        public static void SendGump(GumpData gd, uint x, uint y)
        {
            m_gumpData[gd.gumpId] = gd;
            gd.hasResponse = false;
            GenericGump gg = new(gd.gumpId, gd.serial, gd.x, gd.y, gd.gumpDefinition, gd.gumpStrings);
            Assistant.Client.Instance.SendToClientWait(gg);
        }

        /// <summary>
        /// Hack some gump test stuff
        /// </summary>
        ///
        public static void SendGump(uint gumpid, uint serial, uint x, uint y,
            string gumpDefinition, List<string> gumpStrings)
        {
            GumpData gd = new()
            {
                gumpId = gumpid,
                serial = serial,
                x = x,
                y = y,
                hasResponse = false,
                gumpDefinition = gumpDefinition,
                gumpStrings = new List<string>()
            };
            gd.gumpStrings.AddRange(gumpStrings);
            //
            m_gumpData[gumpid] = gd;
            GenericGump gg = new(gd.gumpId, gd.serial, gd.x, gd.y, gd.gumpDefinition, gd.gumpStrings);
            Assistant.Client.Instance.SendToClientWait(gg);
        }
        public static GumpData GetGumpData(uint gumpid)
        {
            GumpData gd = null;
            if (Gumps.m_gumpData.ContainsKey(gumpid))
            {
                gd = Gumps.m_gumpData[gumpid];

            }
            if (Gumps.m_incomingData.ContainsKey(gumpid))
            {
                gd = new();
                var tempGd = Gumps.m_incomingData[gumpid];
                gd.gumpId = tempGd.gumpId;
                gd.serial = tempGd.gumpSerial;
                gd.x = tempGd.x;
                gd.y = tempGd.y;
                gd.gumpLayout = tempGd.gumpLayout;
                gd.gumpText = tempGd.gumpText;
                gd.gumpData = tempGd.gumpData;
                gd.layoutPieces = tempGd.layoutPieces;
                gd.stringList = tempGd.stringList;

            }
            return gd;
        }

        /// <summary>
        /// Close a specific Gump.
        /// </summary>
        /// <param name="gumpid">ID of the gump</param>
        public static void CloseGump(uint gumpid)
        {
            if (gumpid == 0)
                Assistant.Client.Instance.SendToClientWait(new CloseGump(World.Player.CurrentGumpI));
            else
            {
                Assistant.Client.Instance.SendToClientWait(new CloseGump(gumpid));
            }

            World.Player.HasGump = false;
            World.Player.CurrentGumpStrings.Clear();
            World.Player.CurrentGumpTile.Clear();
            World.Player.CurrentGumpI = 0;
            Gumps.RemoveGump(gumpid);
        }

        /// <summary>
        /// Clean current status of Gumps.
        /// </summary>
        public static void ResetGump()
        {
            World.Player.HasGump = false;
            World.Player.CurrentGumpStrings.Clear();
            World.Player.CurrentGumpTile.Clear();
            Gumps.RemoveGump(World.Player.CurrentGumpI);
            World.Player.CurrentGumpI = 0;
        }


        /// <summary>
        /// Return the ID of most recent, still open Gump.
        /// </summary>
        /// <returns>ID of gump.</returns>
        public static uint CurrentGump()
        {
            return World.Player.CurrentGumpI;
        }

        /// <summary>
        /// Get status if have a gump open or not.
        /// </summary>
        /// <returns>True: There is a Gump open - False: otherwise.</returns>
        public static bool HasGump()
        {
            return World.Player.HasGump;
        }

        static internal uint ConvertObjectToUint(object anObject)
        {
            if (anObject is int intVal)
            {
                // Convert regular int to uint
                return ((uint)intVal);
            }
            else if (anObject is BigInteger bigIntVal)
            {
                // Check if BigInteger is within the uint range
                if (bigIntVal >= uint.MinValue && bigIntVal <= uint.MaxValue)
                {
                    return((uint)bigIntVal);
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"Value {bigIntVal} is out of range for uint.");
                }
            }
            else
            {
                throw new InvalidCastException("The object contains unsupported types.");
            }
        }


        /// <summary>@nodoc @experimental
        /// wait for one of a list of gump ids
        /// </summary>
        /// <returns>True: one of the ids appeared - False: otherwise.</returns>
        public static bool WaitForGump(IronPython.Runtime.PythonList gumpIDs, int delay)
        {
            bool found = false;
            bool local = false;
            List<uint> waitList = new();
            foreach (var gumpID in gumpIDs)
            {
                uint gumpid = ConvertObjectToUint(gumpID);
                if (Gumps.m_incomingData.ContainsKey(gumpid))
                    return true;
                if (Gumps.m_gumpData.ContainsKey(gumpid))
                {
                    local = true;
                }
                waitList.Add(gumpid);
            }

            if (local)
            {
                found = Utility.DelayUntil(() =>
                {
                    foreach (var gumpID in waitList)
                    {
                        if (Gumps.m_gumpData[gumpID].hasResponse == true)
                            return true;
                    }
                    return false;
                }, delay);
            }
            else
            {
                found = Utility.DelayUntil(() => World.Player.HasGump == true && waitList.Contains(World.Player.CurrentGumpI), delay);
            }

            return found;
        }

        /// <summary>
        /// Waits for a specific Gump to appear, for a maximum amount of time. If gumpid is 0 it will match any Gump.
        /// </summary>
        /// <param name="gumpid">ID of the gump. (0: any)</param>
        /// <param name="delay">Maximum wait, in milliseconds.</param>
        /// <returns>True: wait found the gump - False: otherwise.</returns>
        public static bool WaitForGump(uint gumpid, int delay) // Delay in MS
        {
            bool found = false;
            if (gumpid == 0)
            {
                found = Utility.DelayUntil(() => World.Player.HasGump == true, delay);
            }
            else
            {
                if (Gumps.m_gumpData.ContainsKey(gumpid))
                {
                    GumpData gd = Gumps.m_gumpData[gumpid];
                    found = Utility.DelayUntil(() => gd.hasResponse == true, delay);
                }
                else
                {

                    // Check if gump is already up
                    if (Gumps.m_incomingData.ContainsKey(gumpid))
                        found = true;
                    else
                        found = Utility.DelayUntil(() => World.Player.HasGump == true && World.Player.CurrentGumpI == gumpid, delay);
                }

            }
            return found;
        }

        /// <summary>
        /// Adds a response to the gump
        /// </summary>
        /// WorldResponse
        internal static void AddResponse(uint gumpid, int x, int y, string layout,
            List<string> parsedText, List<string> parsedData,
            string[] stringList, string[] layoutPieces
            )
        {
            // not ignore 3856070194
            if (m_gumpData.ContainsKey(gumpid))
            {
                m_gumpData[gumpid].gumpLayout = layout;
                m_gumpData[gumpid].gumpText = parsedText;
                m_gumpData[gumpid].gumpData = parsedData;
                m_gumpData[gumpid].layoutPieces = new List<string>(layoutPieces);
                m_gumpData[gumpid].stringList = new List<string>(stringList);
            }
            else if (m_incomingData.ContainsKey(gumpid))
            {
                m_incomingData[gumpid].gumpLayout = layout;
                m_incomingData[gumpid].gumpText = parsedText;
                m_incomingData[gumpid].gumpData = parsedData;
                m_incomingData[gumpid].layoutPieces = new List<string>(layoutPieces);
                m_incomingData[gumpid].stringList = new List<string>(stringList);
            }
        }

        /// <summary>
        /// Send a Gump response by gumpid and buttonid.
        /// </summary>
        /// <param name="gumpid">ID of the gump.</param>
        /// <param name="buttonid">ID of the Button to press.</param>
        public static void SendAction(uint gumpid, int buttonid)
        {

            int[] nullswitch = new int[0];
            GumpTextEntry[] nullentries = new GumpTextEntry[0];

            if (gumpid == 0)
            {
                Assistant.Client.Instance.SendToClientWait(new CloseGump(World.Player.CurrentGumpI));
                Assistant.Client.Instance.SendToServerWait(new GumpResponse(World.Player.CurrentGumpS, World.Player.CurrentGumpI, buttonid, nullswitch, nullentries));
            }
            else
            {
                Assistant.Client.Instance.SendToClientWait(new CloseGump(gumpid));
                if (m_gumpData.ContainsKey(gumpid))
                {
                    var gd = m_gumpData[gumpid];
                    GumpResponse gumpResp = new(gd.serial, gd.gumpId, buttonid, nullswitch, nullentries);
                    PacketReader p = new(gumpResp.ToArray(), false);

                    PacketHandlerEventArgs args = new();
                    p.ReadByte(); // through away the packet id
                    p.ReadInt16(); // throw away the packet length
                    Assistant.PacketHandlers.ClientGumpResponse(p, args);
                }
                if (m_incomingData.ContainsKey(gumpid))
                {
                    var gd = m_incomingData[gumpid];
                    GumpResponse gumpResp = new(gd.gumpSerial, gd.gumpId, buttonid, nullswitch, nullentries);
                    Assistant.Client.Instance.SendToServerWait(gumpResp);
                }
                Gumps.RemoveGump(gumpid);
            }

            World.Player.HasGump = false;
            World.Player.CurrentGumpStrings.Clear();
            World.Player.CurrentGumpTile.Clear();
            World.Player.CurrentGumpI = 0;
        }

        private static int[] ConvertToIntList(IronPython.Runtime.PythonList pythonList)
        {
            int[] retList = new int[pythonList.Count];
            for (int i = 0; i < pythonList.Count; i++)
            {
                retList[i] = (int)pythonList.ElementAt(i);
            }
            return retList;
        }

        private static string[] ConvertToStringList(IronPython.Runtime.PythonList pythonList)
        {
            string[] retList = new string[pythonList.Count];
            for (int i = 0; i < pythonList.Count; i++)
            {
                retList[i] = (string)pythonList.ElementAt(i);
            }
            return retList;
        }

        // Just a wrapper to accomodate existing code
        // new way is WAY easier
        public static void SendAdvancedAction(uint gumpid, int buttonid,
            List<int> inSwitches)
        {
            IronPython.Runtime.PythonList switches = new();

            foreach (var item in inSwitches)
            {
                switches.Add(item);
            }
            SendAdvancedAction(gumpid, buttonid, switches);

        }

        //AutoDoc concatenates description coming from Overloaded methods
        /// <summary>
        /// This method can also be used only Switches, without Text fileds.
        /// </summary>
        public static void SendAdvancedAction(uint gumpid, int buttonid,
            IronPython.Runtime.PythonList switchs)
        {
            GumpTextEntry[] entries = new GumpTextEntry[0];

            if (gumpid == 0)
            {
                Assistant.Client.Instance.SendToClientWait(new CloseGump(World.Player.CurrentGumpI));
                Assistant.Client.Instance.SendToServerWait(new GumpResponse(World.Player.CurrentGumpS, World.Player.CurrentGumpI, buttonid, ConvertToIntList(switchs), entries));
            }
            else
            {
                Assistant.Client.Instance.SendToClientWait(new CloseGump(gumpid));
                if (m_gumpData.ContainsKey(gumpid))
                {
                    var gd = m_gumpData[gumpid];
                    GumpResponse gumpResp = new(gd.serial, gumpid, buttonid, ConvertToIntList(switchs), entries);
                    PacketReader p = new(gumpResp.ToArray(), false);
                    PacketHandlerEventArgs args = new();
                    p.ReadByte(); // through away the packet id
                    p.ReadInt16(); // throw away the packet length
                    Assistant.PacketHandlers.ClientGumpResponse(p, args);
                }
                if (m_incomingData.ContainsKey(gumpid))
                {
                    var gd = m_incomingData[gumpid];
                    GumpResponse gumpResp = new(gd.gumpSerial, gumpid, buttonid, ConvertToIntList(switchs), entries);
                    Assistant.Client.Instance.SendToServerWait(gumpResp);
                }
                Gumps.RemoveGump(gumpid);
            }

            World.Player.HasGump = false;
            World.Player.CurrentGumpStrings.Clear();
            World.Player.CurrentGumpTile.Clear();
        }

        // Just a wrapper to accomodate existing code
        // new way is WAY easier
        public static void SendAdvancedAction(uint gumpid, int buttonid,
            List<int> textlist_id, List<string> textlist_str)
        {
            IronPython.Runtime.PythonList textIDs = new();
            IronPython.Runtime.PythonList textStrings = new();

            foreach (var item in textlist_id)
            {
                textIDs.Add(item);
            }
            foreach (var item in textlist_str)
            {
                textStrings.Add(item);
            }
            SendAdvancedAction(gumpid, buttonid, textIDs, textStrings);

        }

        //AutoDoc concatenates description coming from Overloaded methods
        /// <summary>
        /// This method can also be used only Text fileds, without Switches.
        /// </summary>
        public static void SendAdvancedAction(uint gumpid, int buttonid,
            IronPython.Runtime.PythonList textlist_id, IronPython.Runtime.PythonList textlist_str)
        {
            IronPython.Runtime.PythonList switchs = new();
            SendAdvancedAction(gumpid, buttonid, switchs, textlist_id, textlist_str);
        }

        // Just a wrapper to accomodate existing code
        // new way is WAY easier
        public static void SendAdvancedAction(uint gumpid, int buttonid,
            List<int> inSwitches, List<int> textlist_id, List<string> textlist_str)

        {
            IronPython.Runtime.PythonList textIDs = new();
            IronPython.Runtime.PythonList textStrings = new();
            IronPython.Runtime.PythonList switches = new();

            foreach (var item in inSwitches)
            {
                switches.Add(item);
            }
            foreach (var item in textlist_id)
            {
                textIDs.Add(item);
            }
            foreach (var item in textlist_str)
            {
                textStrings.Add(item);
            }
            SendAdvancedAction(gumpid, buttonid, switches, textIDs, textStrings);

        }

        /// <summary>
        /// Send a Gump response, with gumpid and buttonid and advanced switch in gumps. 
        /// This function is intended for more complex Gumps, with not only Buttons, but also Switches, CheckBoxes and Text fileds.
        /// </summary>
        /// <param name="gumpid">ID of the gump.</param>
        /// <param name="buttonid">ID of the Button.</param>
        /// <param name="switchlist_id">List of ID of ON/Active switches. (empty: all Switches OFF)</param>
        /// <param name="textlist_id">List of ID of Text fileds. (empty: all text fileds empty )</param>
        /// <param name="textlist_str">List of the contents of the Text fields, provided in the same order as textlist_id.</param>
        public static void SendAdvancedAction(uint gumpid, int buttonid, IronPython.Runtime.PythonList switchlist_id, IronPython.Runtime.PythonList textlist_id, IronPython.Runtime.PythonList textlist_str)
        {
            if (textlist_id.Count == textlist_str.Count)
            {
                int i = 0;
                GumpTextEntry[] entries = new GumpTextEntry[textlist_id.Count];
                var stringList = ConvertToStringList(textlist_str);
                foreach (int entry in textlist_id)
                {
                    GumpTextEntry entrie = new(0, string.Empty)
                    {
                        EntryID = (ushort)entry,
                        Text = stringList[i]
                    };
                    entries[i] = entrie;
                    i++;
                }

                if (gumpid == 0)
                {
                    Assistant.Client.Instance.SendToClientWait(new CloseGump(World.Player.CurrentGumpI));
                    Assistant.Client.Instance.SendToServerWait(new GumpResponse(World.Player.CurrentGumpS,
                            World.Player.CurrentGumpI, buttonid, ConvertToIntList(switchlist_id), entries));
                }
                else
                {
                    Assistant.Client.Instance.SendToClientWait(new CloseGump(gumpid));
                    if (m_gumpData.ContainsKey(gumpid))
                    {
                        var gd = m_gumpData[gumpid];
                        GumpResponse gumpResp = new(gd.serial, gumpid,
                            buttonid, ConvertToIntList(switchlist_id), entries);
                        PacketReader p = new(gumpResp.ToArray(), false);
                        PacketHandlerEventArgs args = new();
                        p.ReadByte(); // through away the packet id
                        p.ReadInt16(); // throw away the packet length
                        Assistant.PacketHandlers.ClientGumpResponse(p, args);
                    }
                    if (m_incomingData.ContainsKey(gumpid))
                    {
                        var gd = m_incomingData[gumpid];
                        GumpResponse gumpResp = new(gd.gumpSerial, gumpid,
                            buttonid, ConvertToIntList(switchlist_id), entries);
                        Assistant.Client.Instance.SendToServerWait(gumpResp);
                    }
                    Gumps.RemoveGump(gumpid);
                }

                World.Player.HasGump = false;
                World.Player.CurrentGumpStrings.Clear();
            }
            else
            {
                Scripts.SendMessageScriptError("Script Error: SendAdvancedAction: entryID and entryS lenght not match");
            }
        }

        /// <summary>
        /// @nodoc  
        /// Testing, I'll remove nodoc after a while @todo Credzba
        /// Text pieces and HTML pieces have a reference to a seperate 
        /// string list (for performance? maybe) 
        /// This matches up the entries, and return the pieces as they were in layout, 
        /// but with the associated string appended.
        /// e.g. piece Text 14 200 2  and stringlist (a, b, c, d)
        /// the Resolved string ould be Text 14 200 2, c
        /// I added the comma to make it easy to parse Text from data
        /// </summary>
        /// <param name="gumpId">gump id to get data from</param>
        /// <returns>Text content of the line. (empty: line not found)</returns>
        public static List<string> GetResolvedStringPieces(uint gumpid)
        {
            List<string> relsolvedPieces = new();
            if (Gumps.m_gumpData.ContainsKey(gumpid))
            {
                foreach (var text in Gumps.m_gumpData[gumpid].layoutPieces)
                {
                    string resolvedText = text;
                    try
                    {
                        var pieces = text.Split(' ');
                        var id = pieces[0].ToLower();
                        if (id == "text" || id == "html" || id == "croppedtext" || id == "htmlgump")
                        {
                            var splitText = text.Split(' ');
                            int index = Int32.Parse(splitText[splitText.Length - 1]);
                            resolvedText += "," + Gumps.m_incomingData[gumpid].stringList[index];
                        }
                        else continue;
                    }
                    catch (FormatException)
                    {
                        resolvedText += ",string not found";
                    }
                    catch (Exception)
                    {
                        resolvedText += $",Error parsing {text} - report bug to discord channel";
                    }
                    relsolvedPieces.Add(resolvedText);
                }
            }

            if (Gumps.m_incomingData.ContainsKey(gumpid))
            {
                foreach (var text in Gumps.m_incomingData[gumpid].layoutPieces)
                {
                    string resolvedText = text;
                    try
                    {
                        var pieces = text.Split(' ');
                        var id = pieces[0].ToLower();
                        if (id == "text" || id == "html" || id == "croppedtext" || id == "htmlgump")
                        {
                            var splitText = text.Split(' ');
                            int index = Int32.Parse(splitText[splitText.Length - 1]);
                            resolvedText += "," + Gumps.m_incomingData[gumpid].stringList[index];
                        }
                        else continue;
                    }
                    catch (FormatException)
                    {
                        resolvedText += ",string not found";
                    }
                    catch (Exception)
                    {
                        resolvedText += $",Error parsing {text} - report bug to discord channel";
                    }
                    relsolvedPieces.Add(resolvedText);
                }
            }
            return relsolvedPieces;

        }

        /// <summary>
        /// Get a specific DATA line from the gumpId if it exists. Filter by line number.
        /// The textual strings are not considered
        /// </summary>
        /// <param name="gumpId">gump id to get data from</param>
        /// <param name="line_num">Number of the line.</param>
        /// <returns>Text content of the line. (empty: line not found)</returns>
        public static string GetLine(uint gumpId, int line_num)
        {
            if (m_incomingData.ContainsKey(gumpId))
            {
                if (line_num >= 0 && line_num < m_incomingData[gumpId].gumpData.Count)
                {
                    return m_incomingData[gumpId].gumpData[line_num];
                }
            }
            return "";

        }


        /// <summary>
        /// Get a specific line from the most recent and still open Gump. Filter by line number.
        /// The text constants on the gump ARE included in indexing
        /// </summary>
        /// <param name="line_num">Number of the line.</param>
        /// <returns>Text content of the line. (empty: line not found)</returns>
        public static string LastGumpGetLine(int line_num)
        {
            try
            {
                if (line_num > World.Player.CurrentGumpStrings.Count)
                {
                    Scripts.SendMessageScriptError("Script Error: LastGumpGetLine: Text line (" + line_num + ") not exist");
                    return "";
                }
                else
                {
                    return World.Player.CurrentGumpStrings[line_num];
                }
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Get all text from the specified Gump if still open
        /// </summary>
        /// <param name="gumpId">gump id to get data from</param>
        /// <returns>Text of the gump.</returns>
        public static List<string> GetLineList(uint gumpId, bool dataOnly = false)
        {
            if (m_gumpData.ContainsKey(gumpId))
            {
                if (dataOnly)
                    return m_gumpData[gumpId].gumpData;

                List<string> lines = new(m_gumpData[gumpId].gumpText);
                lines.AddRange(m_gumpData[gumpId].gumpData);

                return lines;

            }

            if (m_incomingData.ContainsKey(gumpId))
            {
                if (dataOnly)
                    return m_incomingData[gumpId].gumpData;

                List<string> lines = new(m_incomingData[gumpId].gumpText);
                lines.AddRange(m_incomingData[gumpId].gumpData);

                return lines;
            }
            return new List<string>();
        }

        /// <summary>
        /// Get all text from the most recent and still open Gump.
        /// </summary>
        /// <returns>Text of the gump.</returns>
        public static List<string> LastGumpGetLineList()
        {
            return World.Player.CurrentGumpStrings;
        }



        /// <summary>
        /// Search for text inside the most recent and still open Gump.
        /// </summary>
        /// <param name="text">Text to search.</param>
        /// <returns>True: Text found in active Gump - False: otherwise</returns>
        public static bool LastGumpTextExist(string text)
        {
            try
            {
                return World.Player.CurrentGumpStrings.Any(stext => stext.Contains(text));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Search for text, in a spacific line of the most recent and still open Gump.
        /// </summary>
        /// <param name="line_num">Number of the line.</param>
        /// <param name="text">Text to search.</param>
        /// <returns></returns>
        public static bool LastGumpTextExistByLine(int line_num, string text)
        {
            try
            {
                if (line_num > World.Player.CurrentGumpStrings.Count)
                {
                    Scripts.SendMessageScriptError("Script Error: LastGumpTextExistByLine: Text line (" + line_num + ") not exist");
                    return false;
                }
                else
                {
                    return World.Player.CurrentGumpStrings[line_num].Contains(text);
                }
            }
            catch
            {
                return false;
            }
        }


        /// <summary>@nodoc @deprecate
        /// In the future I'd like this to be the RAW version of the text,
        /// meaning the cliloc number before translation. 
        /// For language independent code, the numbers would be more useful than the text</summary>
        /// <param name="gumpid"></param>
        /// <returns>List<string></string></returns>
        public static List<string> GetGumpRawText(uint gumpid)
        {
            return GetGumpText(gumpid);
        }

        /// <summary>
        /// Get the Text of a specific Gump.
        /// It is the cliloc translation of the #s in the gump
        /// </summary>
        /// <returns>List of Text in the gump</returns>
        public static List<string> GetGumpText(uint gumpid)
        {
            if (m_gumpData.ContainsKey(gumpid))
            {
                return m_gumpData[gumpid].gumpText;
            }
            else if (m_incomingData.ContainsKey(gumpid))
            {
                return m_incomingData[gumpid].gumpText;
            }

            return new List<string>();
        }

        /// <summary>@nodoc @deprecate</summary>
        /// kept only for backward compatibility 9/4/2024.
        /// Should be removed in future
        /// 
        public static string GetGumpRawData(uint gumpid)
        {
            return GetGumpRawLayout(gumpid);
        }

        /// <summary>
        /// Get the Raw layout (definition) of a specific gumpid
        /// </summary>
        /// <returns>layout (definition) of the gump.</returns>
        public static string GetGumpRawLayout(uint gumpid)
        {
            if (m_gumpData.ContainsKey(gumpid))
            {
                return m_gumpData[gumpid].gumpLayout;
            }
            else if (m_incomingData.ContainsKey(gumpid))
            {
                return m_incomingData[gumpid].gumpLayout;
            }
            return string.Empty;
        }

        /// <summary>
        /// Get the raw layout (definition) of the most recent and still open Gump.
        /// </summary>
        /// <returns>layout (definition) of the gump.</returns>
        public static string LastGumpRawLayout()
        {
            try
            {
                return World.Player.CurrentGumpRawLayout;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Get the list of Gump Tile (! this documentation is a stub !) 
        /// </summary>
        /// <returns>List of Gump Tile.</returns>
        public static List<int> LastGumpTile()
        {
            try
            {
                return World.Player.CurrentGumpTile;
            }
            catch
            {
                return new List<int>();
            }
        }
    }
}
