namespace CEM_IAModeler.Utils
{
    /// <summary>
    /// Where the plugin writes (root decision 0031, docs/decisions/0026): its logs go to the local, self-cleaning
    /// <c>%TEMP%\Cemengal\RevitMCP</c> through CEM_CSharpExtended's <c>CemTempFolder</c>, never next to this DLL, which
    /// loads from the ACC menu on colleagues' machines.
    /// </summary>
    public static class McpLocalFiles
    {
        /// <summary>The app folder under <c>%TEMP%\Cemengal</c>; the command set uses the same one.</summary>
        public const string App = "RevitMCP";
    }
}
