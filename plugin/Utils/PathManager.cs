using System.IO;

namespace CEM_IAModeler.Utils
{
    /// <summary>
    /// The add-in's own paths, read only (root decision 0031): on colleagues' machines this DLL loads from the ACC menu,
    /// so nothing here creates a folder or writes a file. Reading what the deploy shipped (Commands\, the registry) is
    /// fine; the logs go to CemTempFolder (<see cref="Logger"/>, <see cref="ActionLogger"/>).
    /// </summary>
    public static class PathManager
    {
        /// <summary>
        /// Gets the folder this add-in was loaded from (the deploy's, not a per-user one).
        /// </summary>
        public static string GetAppDataDirectoryPath()
        {
            string applicationPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string applicationDirectory = Path.GetDirectoryName(applicationPath);

            return applicationDirectory;
        }
        /// <summary>
        /// Gets the path to the Commands directory, where the deploy stages the command set. It is not created.
        /// </summary>
        public static string GetCommandsDirectoryPath()
        {
            return Path.Combine(GetAppDataDirectoryPath(), "Commands");
        }
        /// <summary>
        /// Gets the path to the command registry the deploy ships (CEM_RibbonUI copies command.json there). It is never
        /// created: a missing registry loads no commands (<see cref="Configuration.ConfigurationManager"/>).
        /// </summary>
        /// <returns>Path to the command registry file</returns>
        public static string GetCommandRegistryFilePath()
        {
            return Path.Combine(GetCommandsDirectoryPath(), "commandRegistry.json");
        }
    }
}
