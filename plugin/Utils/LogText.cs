using System;
using System.Text;

namespace CEM_IAModeler.Utils
{
    /// <summary>
    /// Makes caller-controlled text safe for the plain-text <c>mcp-*.log</c> (decision 0022, CWE-117). CR, LF, tab, the
    /// other C0 and C1 controls, DEL and the Unicode line and paragraph separators become visible escapes (<c>\r</c>,
    /// <c>\n</c>, <c>\t</c>, <c>\uXXXX</c>), so a refused method or an exception message cannot start a forged log line.
    /// Idempotent: the escapes are plain text. <see cref="Logger"/> applies it to every format argument.
    /// </summary>
    public static class LogText
    {
        public static string Escape(string text)
        {
            if (text == null) return "(null)";
            StringBuilder escaped = null;
            for (int i = 0; i < text.Length; i++)
            {
                string replacement = Replacement(text[i]);
                if (replacement == null)
                {
                    escaped?.Append(text[i]);
                    continue;
                }
                if (escaped == null) escaped = new StringBuilder(text, 0, i, text.Length + 16);
                escaped.Append(replacement);
            }
            return escaped?.ToString() ?? text;
        }

        /// <summary>
        /// Every format argument: strings and other objects through their text; numbers, dates, enums and other
        /// <see cref="IFormattable"/> values as they are, so <c>{0:N1}</c> still formats; null stays null.
        /// </summary>
        public static object[] EscapeArgs(object[] args)
        {
            if (args == null) return null;
            var escaped = new object[args.Length];
            for (int i = 0; i < args.Length; i++)
                escaped[i] = args[i] == null || args[i] is IFormattable ? args[i] : Escape(args[i].ToString());
            return escaped;
        }

        private static string Replacement(char c)
        {
            switch (c)
            {
                case '\r': return @"\r";
                case '\n': return @"\n";
                case '\t': return @"\t";
            }
            bool control = c < 0x20 || (c >= 0x7F && c <= 0x9F) || c == (char)0x2028 || c == (char)0x2029;
            return control ? @"\u" + ((int)c).ToString("X4") : null;
        }
    }
}
