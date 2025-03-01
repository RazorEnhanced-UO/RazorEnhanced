using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ultima
{
    public sealed class StringEntry
    {
        [Flags]
        public enum CliLocFlag
        {
            Original = 0x0,
            Custom = 0x1,
            Modified = 0x2
        }

        private string m_Text;

        public int Number { get; private set; }

        public string Text
        {
            get { return m_Text; }
            set
            {
                if (value == null)
                    m_Text = "";
                else
                    m_Text = value;
            }
        }

        public CliLocFlag Flag { get; set; }

        public StringEntry(int number, string text, byte flag)
        {
            Number = number;
            m_Text = text;
            Flag = (CliLocFlag)flag;
        }

        public StringEntry(int number, string text, CliLocFlag flag)
        {
            Number = number;
            m_Text = text;
            Flag = flag;
        }

        // Razor
        private static Regex m_RegEx = new Regex(@"~(\d+)[_\w]*~", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        private string m_FmtTxt;
        private static object[] m_Args = new object[] { "", "", "", "", "", "", "", "", "", "", "" };

        public string Format(params object[] args)
        {
            if (m_FmtTxt == null)
                m_FmtTxt = m_RegEx.Replace(m_Text, @"{$1}");
            List<object> list = new List<object>();
            foreach (object o in args.ToList<object>())
                list.Add(o);

            return String.Format(m_FmtTxt, list.ToArray());
        }

        public string SplitFormat(string argstr)
        {
            if (m_FmtTxt == null)
                m_FmtTxt = m_RegEx.Replace(m_Text, @"{$1}");
            List<string> list = new List<string>();
            list.Add("");
            if (Number == 1041522) // ~1~~2~~3~ is being over-used on free shards
            {
                Regex get_ability = new Regex(@".*(Abilities: )([^,]+, )(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
                Match match = get_ability.Match(argstr);
                if (match.Success)
                {
                    list.Add(match.Groups[1].Value);
                    list.Add(match.Groups[2].Value);
                    list.Add(match.Groups[3].Value);
                }
            }
            else
            {
                //list = argstr.Split('\t').ToList();
                foreach (string s in argstr.Split('\t'))
                    list.Add(s); // adds an extra on to the args array               
            }
            const string pattern = @"(?<!\{)(?>\{\{)*\{\d(.*?)";
            var matches = Regex.Matches(m_FmtTxt, pattern);
            //            var totalMatchCount = matches.Count;
            var uniqueMatchCount = matches.OfType<Match>().Select(m => m.Value).Distinct().Count();
            var parameterMatchCount = (uniqueMatchCount == 0) ? 0 : matches.OfType<Match>().Select(m => m.Value).Distinct().Select(m => int.Parse(m.Replace("{", string.Empty))).Max() + 1;
            int addEmpty = parameterMatchCount - list.Count;
            if (addEmpty > 0)
            {
                for (int loop = 0; loop < addEmpty; loop++)
                {
                    list.Add("");
                }
            }

            string result = String.Format(m_FmtTxt, list.ToArray());
            return result;
        }
    }
}
