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

namespace TrackChanges.Controllers
{
    internal class SetingsFormController
    {
        public SetingsFormController(RUI.UIApplication uiapp)
        {
            _uiapp = uiapp;
            tracking = Properties.Settings1.Default.TrackChanges;
        }
        private RUI.UIApplication _uiapp {get; set;}

        public void ApplyChanges()
        {
            Properties.Settings1.Default.TrackChanges = tracking;
            if (exportPath != "") //TODO check if valid path else throw error
            {
                Properties.Settings1.Default.ExportLoaction = exportPath;
            }
        }
        public void StartTracking()
        {
            
        }

        public bool tracking { get; set; }
        public string exportPath { get; set; }

    }
}
