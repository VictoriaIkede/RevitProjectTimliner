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

using System.IO;

namespace TrackChanges.Controllers
{
    internal class SetingsFormController
    {
        public RUI.UIApplication _uiapp { get; set; }
        public RDB.Document _doc { get; set; }
        public SetingsFormController(RUI.UIApplication uiapp)
        {
            _uiapp = uiapp;
            RUI.UIDocument uidoc = uiapp.ActiveUIDocument;
            _doc = uidoc.Document;
            tracking = Properties.Settings1.Default.TrackChanges;
        }

        public void ApplyChanges()
        {
            Properties.Settings1.Default.TrackChanges = tracking;
            if (exportPath != "") //TODO check if valid path else throw error
            {
                Properties.Settings1.Default.ExportLoaction = exportPath;
            }
        }
        public void OpenInspectChangesForm()
        {
            RecordCommandsEdited app = RecordCommandsEdited.thisApp;
            app.seeCurrentChanges(_uiapp, _doc);
        }

        public bool tracking { get; set; }
        public string exportPath { get; set; }

    }
}
