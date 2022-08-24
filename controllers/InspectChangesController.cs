using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using RDB = Autodesk.Revit.DB;
using RDBE = Autodesk.Revit.DB.Events;
using RUI = Autodesk.Revit.UI;

using System.IO;
using System.Data;

namespace TrackChanges.Controllers
{
    internal class InspectChangesController
    {
        private RUI.UIApplication _uiapp { get; set; }
        private RDB.Document _doc { get; set; }
        public Hashtable _changes { get; set; }
        public System.Windows.Forms.ListView listview {get; set;}
        public string lastChange { get; set; }
        public string lastChangeUser { get; set; }
        public string firstChange { get; set; }
        public string firstChangeUser { get; set; }

        List<RecordCommandsEdited.ElementData> ChangesList { get; set; }

        public InspectChangesController(RUI.UIApplication uiapp, Hashtable changes )
        {
            _uiapp = uiapp;
            RUI.UIDocument uidoc = uiapp.ActiveUIDocument;
            _doc = uidoc.Document;
            _changes = changes;
            exportPath = Properties.Settings1.Default.ExportLoaction;
        }
        public void getChanges()
        {
            listview.Columns.Clear();
            listview.Columns.Add("Timestamp", 70);
            listview.Columns.Add("ID", 70);
            listview.Columns.Add("Action", 70);
            listview.Columns.Add("User", 70);
            listview.Columns.Add("Type", 100);
            listview.Columns.Add("Name", 70);
            listview.Columns.Add("Category", 100);
            listview.Columns.Add("UniqueID", 70);
            listview.Columns.Add("SessionID", 70);
            listview.Items.Clear();
            System.Collections.ICollection keys = _changes.Keys;
            List<RecordCommandsEdited.ElementData> changes = new List<RecordCommandsEdited.ElementData>();
            DataTable dt = new DataTable();
            foreach (var key in keys)
            {
                RecordCommandsEdited.ElementData data = (RecordCommandsEdited.ElementData)(_changes[key]);
                changes.Add(data);
                
            }
            //show data in a table
            changes.Sort((x, y) => Convert.ToDateTime(y.changeTimestamp).CompareTo(Convert.ToDateTime(x.changeTimestamp)));
            ChangesList = changes;
            lastChange = changes[0].changeTimestamp;
            lastChangeUser = changes[0].user;
            firstChange = changes[changes.Count-1].changeTimestamp;
            lastChangeUser = changes[changes.Count - 1].user;
            foreach (RecordCommandsEdited.ElementData data in changes)
            {
                System.Windows.Forms.ListViewItem entry = new System.Windows.Forms.ListViewItem(data.changeTimestamp);
                entry.SubItems.Add(data.id);
                entry.SubItems.Add(data.action);
                entry.SubItems.Add(data.user);
                entry.SubItems.Add(data.type);
                entry.SubItems.Add(data.name);
                entry.SubItems.Add(data.CategoryType);
                entry.SubItems.Add(data.uniqueId);
                entry.SubItems.Add(data.sessionId);
                entry.Tag = data;
                listview.Items.Add(entry);
            }
        }
        public void FocusElement(List<int> indexes) {
            List<RDB.ElementId> listOFids = new List<RDB.ElementId>();
            foreach (int index in indexes)
            {
                RecordCommandsEdited.ElementData element = ChangesList[index];
                int id = Convert.ToInt32(element.id);
                listOFids.Add(new RDB.ElementId(id));
            }
            
            //RDB.View view = _uiapp.ActiveUIDocument.Document.ActiveView;
            //view.IsolateElementsTemporary(liIds);
            _uiapp.ActiveUIDocument.Selection.SetElementIds(listOFids);
            _uiapp.ActiveUIDocument.ShowElements(listOFids);
            _uiapp.ActiveUIDocument.RefreshActiveView();


        }
        public void EndDocSession()
        {
            RecordCommandsEdited app = RecordCommandsEdited.thisApp;
            string filename = _doc.PathName;
            string filenameShort = Path.GetFileNameWithoutExtension(filename);
            string user = _doc.Application.Username;
            app.EndSessionDoc(filenameShort, "sessionEndedByUser: " + user);
        }
        
        public string exportPath { get; set; }

    }
}
