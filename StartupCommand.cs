using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;

namespace BTVN1_Application.Commands
{
    /// <summary>
    ///     External command entry point
    /// </summary>
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class StartupCommand : ExternalCommand
    {
        public override void Execute()
        {
        }
    }
}