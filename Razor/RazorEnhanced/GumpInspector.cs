using Assistant;
using Assistant.UI;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace RazorEnhanced
{
    public class GumpInspector
    {
        internal static void GumpResponseAddLogMain(uint ser, uint tid, int bid)
        {
            if (!Assistant.Engine.MainWindow.GumpInspectorEnable)
                return;

            AddLog("----------- Response Recevied START -----------");
            AddLog($"Gump Sequence: 0x{ser:x}");
            AddLog($"Gump ID: 0x{tid:x}");
            AddLog("Gump Button: " + bid.ToString());
        }

        internal static void GumpResponseAddLogSwitchID(List<int> switchid)
        {
            if (!Assistant.Engine.MainWindow.GumpInspectorEnable)
                return;
            int i = 0;
            foreach (int sid in switchid)
            {
                AddLog("Switch ID: " + i + " Value: " + sid.ToString());
                i++;
            }
        }

        internal static void GumpResponseAddLogTextID(int id, string text)
        {
            if (!Assistant.Engine.MainWindow.GumpInspectorEnable)
                return;
            AddLog("Text ID: " + id + " String: " + text);
        }

        internal static void GumpResponseAddLogEnd()
        {
            if (!Assistant.Engine.MainWindow.GumpInspectorEnable)
                return;
            AddLog("----------- Response Recevied END -----------");
        }

        internal static void GumpCloseAddLog(PacketReader p, PacketHandlerEventArgs args)
        {
            if (!Assistant.Engine.MainWindow.GumpInspectorEnable)
                return;
            AddLog("----------- Close Recevied START -----------");
            ushort ext = p.ReadUInt16(); // Scarto primo uint
            uint gid = p.ReadUInt32();
            AddLog("Gump ID: " + gid.ToString());
            int bid = p.ReadInt32();
            AddLog("Gump Close Button: " + bid.ToString());
            AddLog("----------- Close Recevied END -----------");
        }

        internal static void NewGumpStandardAddLog(uint GumpS, uint GumpI)
        {
            if (!Assistant.Engine.MainWindow.GumpInspectorEnable)
                return;

            AddLog("----------- New Recevied START -----------");

            AddLog("Gump Operation: " + GumpS.ToString());
            AddLog("Gump ID: " + GumpI.ToString());

            AddLog("----------- New Recevied END -----------");
        }

        internal static void NewGumpCompressedAddLog(uint GumpS, uint GumpI)
        {
            if (!Assistant.Engine.MainWindow.GumpInspectorEnable)
                return;

            RazorEnhanced.UI.EnhancedGumpInspector.EnhancedGumpInspectorListBox.BeginUpdate();
            AddLog("----------- New Recevied START -----------");

            AddLog($"Gump Sequence: 0x{GumpS:x}");
            AddLog($"Gump ID: 0x{GumpI:x}");

            foreach (string text in World.Player.CurrentGumpStrings)
                AddLog("Gump Text Data: " + text);

            AddLog("----------- New Recevied END -----------");
            RazorEnhanced.UI.EnhancedGumpInspector.EnhancedGumpInspectorListBox.EndUpdate();
        }

        internal static void AddLog(string addlog)
        {
            RazorEnhanced.UI.EnhancedGumpInspector.EnhancedGumpInspectorListBox.BeginInvoke(new Action(() => RazorEnhanced.UI.EnhancedGumpInspector.EnhancedGumpInspectorListBox.Items.Add(addlog)));
            RazorEnhanced.UI.EnhancedGumpInspector.EnhancedGumpInspectorListBox.BeginInvoke(new Action(() => RazorEnhanced.UI.EnhancedGumpInspector.EnhancedGumpInspectorListBox.SelectedIndex = RazorEnhanced.UI.EnhancedGumpInspector.EnhancedGumpInspectorListBox.Items.Count - 1));
        }
    }
}
