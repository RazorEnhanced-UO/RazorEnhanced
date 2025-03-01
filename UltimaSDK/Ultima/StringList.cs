using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ultima
{
    public sealed class StringList
    {
        private int m_Header1;
        private short m_Header2;

        public List<StringEntry> Entries { get; set; }
        public string Language { get; private set; }

        private Dictionary<int, string> m_StringTable;
        private Dictionary<int, StringEntry> m_EntryTable;

        private static byte[] m_Buffer = new byte[1024];

        internal static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initialize <see cref="StringList"/> of Language
        /// </summary>
        /// <param name="language"></param>
        public StringList(string language)
        {
            Language = language;
            string filePath = Files.GetFilePath(String.Format("cliloc.{0}", language));
            logger.Debug($"StringList is using {filePath}");
            LoadEntry(filePath);
        }

        /// <summary>
        /// Initialize <see cref="StringList"/> of Language from path
        /// </summary>
        /// <param name="language"></param>
        /// <param name="path"></param>
        public StringList(string language, string path)
        {
            Language = language;
            LoadEntry(path);
        }

        // New decompression algorithm Thanks to Karasho@ClassicUO
        private void LoadEntry(string path)
        {
            if (path == null)
            {
                Entries = new List<StringEntry>(0);
                return;
            }
            Entries = new List<StringEntry>();
            m_StringTable = new Dictionary<int, string>();
            m_EntryTable = new Dictionary<int, StringEntry>();

            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {

                int bytesRead;
                var totalRead = 0;
                var buf = new byte[fileStream.Length];
                while ((bytesRead = fileStream.Read(buf, totalRead, Math.Min(4096, buf.Length - totalRead))) > 0)
                    totalRead += bytesRead;

                var output = buf[3] == 0x8E ? Ultima.BwtDecompress.Decompress(buf) : buf;

                var reader = new StackDataReader(output);
                m_Header1 = reader.ReadInt32LE();
                m_Header2 = reader.ReadInt16LE();

                while (reader.Remaining > 0)
                {
                    var number = reader.ReadInt32LE();
                    var flag = reader.ReadUInt8();
                    var length = reader.ReadInt16LE();
                    var text = string.Intern(reader.ReadUTF8(length));

                    m_StringTable[number] = text;
                    StringEntry se = new StringEntry(number, text, flag);
                    Entries.Add(se);
                    m_EntryTable[number] = se;

                }
            }
        }

        /// <summary>
        /// Saves <see cref="SaveStringList"/> to FileName
        /// </summary>
        /// <param name="FileName"></param>
        public void SaveStringList(string FileName)
        {
            using (FileStream fs = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                using (BinaryWriter bin = new BinaryWriter(fs))
                {
                    bin.Write(m_Header1);
                    bin.Write(m_Header2);
                    Entries.Sort(new StringList.NumberComparer(false));
                    foreach (StringEntry entry in Entries)
                    {
                        bin.Write(entry.Number);
                        bin.Write((byte)entry.Flag);
                        byte[] utf8String = Encoding.UTF8.GetBytes(entry.Text);
                        ushort length = (ushort)utf8String.Length;
                        bin.Write(length);
                        bin.Write(utf8String);
                    }
                }
            }
        }

        public string GetString(int number)
        {
            if (m_StringTable == null || !m_StringTable.ContainsKey(number))
                return null;

            return m_StringTable[number];
        }

        public StringEntry GetEntry(int number)
        {
            if (m_EntryTable == null || !m_EntryTable.ContainsKey(number))
                return null;

            return m_EntryTable[number];
        }

        #region SortComparer

        public class NumberComparer : IComparer<StringEntry>
        {
            private bool m_desc;

            public NumberComparer(bool desc)
            {
                m_desc = desc;
            }

            public int Compare(StringEntry objA, StringEntry objB)
            {
                if (objA.Number == objB.Number)
                    return 0;
                else if (m_desc)
                    return (objA.Number < objB.Number) ? 1 : -1;
                else
                    return (objA.Number < objB.Number) ? -1 : 1;
            }
        }

        public class FlagComparer : IComparer<StringEntry>
        {
            private bool m_desc;

            public FlagComparer(bool desc)
            {
                m_desc = desc;
            }

            public int Compare(StringEntry objA, StringEntry objB)
            {
                if ((byte)objA.Flag == (byte)objB.Flag)
                {
                    if (objA.Number == objB.Number)
                        return 0;
                    else if (m_desc)
                        return (objA.Number < objB.Number) ? 1 : -1;
                    else
                        return (objA.Number < objB.Number) ? -1 : 1;
                }
                else if (m_desc)
                    return ((byte)objA.Flag < (byte)objB.Flag) ? 1 : -1;
                else
                    return ((byte)objA.Flag < (byte)objB.Flag) ? -1 : 1;
            }
        }

        public class TextComparer : IComparer<StringEntry>
        {
            private bool m_desc;

            public TextComparer(bool desc)
            {
                m_desc = desc;
            }

            public int Compare(StringEntry objA, StringEntry objB)
            {
                if (m_desc)
                    return String.Compare(objB.Text, objA.Text);
                else
                    return String.Compare(objA.Text, objB.Text);
            }
        }

        #endregion SortComparer
    }
}
