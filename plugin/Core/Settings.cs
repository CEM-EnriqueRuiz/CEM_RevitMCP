using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CEM_IAModeler.UI;

namespace CEM_IAModeler.Core
{
    [Transaction(TransactionMode.Manual)]
    public class Settings : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            SettingsWindow window = new SettingsWindow();
            _ = new System.Windows.Interop.WindowInteropHelper(window)
            {
                Owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle
            };
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            window.Show();

            return Result.Succeeded;
        }
    }
}
