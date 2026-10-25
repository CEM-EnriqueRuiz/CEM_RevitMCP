namespace CEM_IAModeler_CommandSet.Utils
{
    /// <summary>
    /// Where the command set writes (root decision 0031, docs/decisions/0026): temporary artefacts (screenshots, shared
    /// parameter files, families saved only to be reloaded) go to the local, self-cleaning
    /// <c>%TEMP%\Cemengal\RevitMCP\&lt;Kind&gt;</c> through CEM_CSharpExtended's <c>CemTempFolder</c>. An MCP call never
    /// writes under ACC, nor next to the add-in, which loads from the ACC menu on colleagues' machines.
    /// </summary>
    public static class McpLocalFiles
    {
        /// <summary>The app folder under <c>%TEMP%\Cemengal</c>; the plugin uses the same one.</summary>
        public const string App = "RevitMCP";

        /// <summary>The Desktop Connector root's folder name: any path with this segment is on ACC.</summary>
        private const string AccSegment = "ACCDocs";

        /// <summary>
        /// True when <paramref name="path"/> has an <c>ACCDocs</c> folder segment (case-insensitive), i.e. it is synced
        /// to every colleague by Desktop Connector. An MCP call never writes there.
        /// </summary>
        public static bool IsUnderAcc(string path) =>
            !string.IsNullOrEmpty(path) &&
            path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(segment => string.Equals(segment, AccSegment, StringComparison.OrdinalIgnoreCase));
    }
}
