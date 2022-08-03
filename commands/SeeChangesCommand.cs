using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using RDB = Autodesk.Revit.DB;
using RDBE = Autodesk.Revit.DB.Events;
using RUI = Autodesk.Revit.UI;

namespace TrackChanges
{
    [Autodesk.Revit.Attributes.Transaction(TransactionMode.Manual)]
    class SeeChangesCommand : RUI.IExternalCommand
    {
        public RUI.Result Execute(RUI.ExternalCommandData commandData, ref string message, RDB.ElementSet elements)
        {
            //Get application and document objects
            RUI.UIApplication uiapp = commandData.Application;
            RDB.Document doc = uiapp.ActiveUIDocument.Document;
            RDB.Transaction trans = new RDB.Transaction(doc);
            trans.Start("SeeChanges");
            try
            {
                RecordCommandsEdited app = RecordCommandsEdited.thisApp;
                app.seeCurrentChanges(uiapp, doc);
                trans.Commit();
            }
            catch (Exception ex)
            {
                trans.RollBack();
                message = ex.Message;
                return RUI.Result.Failed;
            }
            return RUI.Result.Succeeded;
        }
    }
}
