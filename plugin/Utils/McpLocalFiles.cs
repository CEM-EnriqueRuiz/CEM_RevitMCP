using CEM_CSharpExtended.IO;

namespace CEM_IAModeler.Utils
{
    /// <summary>
    /// Where the plugin writes (root decision 0031, docs/decisions/0026): its logs go to the local, self-cleaning
    /// <c>%TEMP%\Cemengal\RevitMCP</c> through CEM_CSharpExtended's <c>CemTempFolder</c>, never next to this DLL, which
    /// loads from the ACC menu on colleagues' machines. The socket's token goes to <c>%LOCALAPPDATA%\Cemengal\RevitMCP</c>
    /// (decision 0022, <see cref="Security.McpAuthToken"/>).
    /// </summary>
    public static class McpLocalFiles
    {
        /// <summary>The app folder under <c>%TEMP%\Cemengal</c>; the command set uses the same one.</summary>
        public const string App = "RevitMCP";

        /// <summary>
        /// True when <paramref name="path"/> is a junction, a symbolic link or another reparse point (SEC-8, CWE-59). A
        /// missing path is not one. Fails closed: attributes that cannot be read count as a reparse point. The same rule
        /// as the command set's <c>McpLocalFiles</c>.
        /// </summary>
        public static bool IsReparsePoint(string path)
        {
            try { return (System.IO.File.GetAttributes(path) & System.IO.FileAttributes.ReparsePoint) != 0; }
            catch (System.IO.FileNotFoundException) { return false; }
            catch (System.IO.DirectoryNotFoundException) { return false; }
            catch { return true; }
        }

        /// <summary>Refuses a path we derive ourselves when it is a reparse point, which would send our write elsewhere.</summary>
        /// <exception cref="CemTempFolderUnsafeException">One of the paths is a reparse point.</exception>
        public static void RefuseReparsePoint(params string[] paths)
        {
            foreach (var path in paths)
                if (!string.IsNullOrEmpty(path) && IsReparsePoint(path)) throw new CemTempFolderUnsafeException(path);
        }
    }
}
