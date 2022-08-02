#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using TrackChanges;
using Autodesk.Revit.UI.Selection;
using System.Reflection;
#endregion

namespace TrackChanges
    {
    class RecordCommandsEdited : IExternalApplication
    {
        public static RecordCommandsEdited thisApp = null;
        public struct documentSession
        {
            public System.Collections.Hashtable newValues { get; set; }
            public System.Collections.Hashtable oldValues { get; set; }
            public string fileName { get; set; }
            public string exportpath { get; set; }

        }
        public struct ElementData
        {
            public string id { get; set; }
            public string name { get; set; }
            public string user { get; set; }
            public string CategoryType { get; set; }
            public string type { get; set; }
            public string changeType { get; set; }
            public string changeTimestamp { get; set; }
            public string uniqueId { get; set; }
            public string sessionId { get; set; }
        }

        public documentSession currentSesion { get; set; }

        public static List<documentSession> TrackedDocuments;
        public Result OnShutdown(UIControlledApplication application)
        {
            application.ControlledApplication.DocumentChanged -= ChangeTracker;
            //save the Hastable data to a csv file at export location if we didnt save it alredy manualy
            foreach (var item in TrackedDocuments)
            {
                string filesDirectory = Properties.Settings1.Default.ExportLoaction;
                if (filesDirectory == "") { Properties.Settings1.Default.ExportLoaction = Path.GetTempPath(); }
                var outputFileName = Path.Combine(filesDirectory, item.fileName);
                saveHastableData(item.newValues, outputFileName);
            }

            string filesDir = Properties.Settings1.Default.ExportLoaction;
            if (filesDir == "") { Properties.Settings1.Default.ExportLoaction = Path.GetTempPath(); }
            var fileName = "Sessions_log.csv";
            var outputFile = Path.Combine(filesDir, fileName);
            using (StreamWriter sw = new StreamWriter(outputFile, true))
            {
                DateTime now = DateTime.Now;
                sw.WriteLine("Revvit closed at ;" + now + ";" + Properties.Settings1.Default.SessionID);
            }
            Properties.Settings1.Default.SessionID += 1; //update sesion number

            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {

            thisApp = this;
            try
            {

                String tabName = "Timliner";
                application.CreateRibbonTab(tabName);
                RibbonPanel curlPanel = application.CreateRibbonPanel(tabName, "Tools");
                //locating the dll directory
                string curlAssembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string curlAssemblyPath = System.IO.Path.GetDirectoryName(curlAssembly);
                //set the button to preform changeSetingsCommand
                PushButtonData buttonData1 = new PushButtonData("Settings", "Settings", curlAssembly, "TrackChanges.ChangeSetingsCommand");
                //buttonData1.LargeImage = new BitmapImage(new Uri(System.IO.Path.Combine(curlAssemblyPath, "settings.jpg")));
                PushButton button1 = (PushButton)curlPanel.AddItem(buttonData1);


                //set event handler
                TrackedDocuments = new List<documentSession>(); //create a empty list of all document sessions
                application.ControlledApplication.DocumentChanged += new EventHandler<DocumentChangedEventArgs>(ChangeTracker);


                
                //create session log when starting app
                string filesDir = Properties.Settings1.Default.ExportLoaction;
                if (filesDir == "") { Properties.Settings1.Default.ExportLoaction = Path.GetTempPath(); }
                var fileName = "Sessions_log.csv";
                var outputFile = Path.Combine(filesDir, fileName);
                using (StreamWriter sw = new StreamWriter(outputFile, true))
                {
                    DateTime now = DateTime.Now;
                    sw.WriteLine("Revvit opend at ;" + now + ";" + Properties.Settings1.Default.SessionID);
                }

            }
            catch (Exception)
            {
                return Result.Failed;
            }
            return Result.Succeeded;
        }

        //Code for ChangeTracker
        public void ChangeTracker(object sender, DocumentChangedEventArgs args)
        {
            Application app = sender as Application;
            UIApplication uiapp = new UIApplication(app);
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = args.GetDocument();
            string filesDir = Properties.Settings1.Default.ExportLoaction;
            string user = doc.Application.Username;
            string filename = doc.PathName;
            string filenameShort = Path.GetFileNameWithoutExtension(filename);
            var outputFile = Path.Combine(filesDir, filenameShort + Properties.Settings1.Default.ChangesFileEnding + ".csv");
            View currentView = uidoc.ActiveView;

            bool tracking = false;
            foreach (var item in TrackedDocuments)
            {
                if (item.fileName == filenameShort) { tracking = true; currentSesion = item; }//set to the document sesion where change occured
            }
            if (tracking == false) { //we are not yet tracking the file create hash tables for storing changes
                documentSession newSession = new documentSession();
                newSession.fileName = filenameShort;
                newSession.newValues = new System.Collections.Hashtable();
                newSession.oldValues = new System.Collections.Hashtable();
                //fill up oldValues grab all elements
                FilteredElementCollector coll = new FilteredElementCollector(doc);
                coll.WherePasses(new LogicalOrFilter(new ElementIsElementTypeFilter(false),new ElementIsElementTypeFilter(true)));
                var elements = coll.ToArray();
                foreach (var item in elements)
                {
                    newSession.oldValues.Add(item.Id, itemData(item,"Deleted", user, Properties.Settings1.Default.SessionID));
                }
                TrackedDocuments.Add(newSession);
                currentSesion = newSession;
            }

            //Selection sel = uidoc.Selection;
            ICollection<ElementId> deleted = args.GetDeletedElementIds();
            ICollection<ElementId> changed = args.GetModifiedElementIds();
            ICollection<ElementId> added = args.GetAddedElementIds();
            //ICollection<ElementId> selected = sel.GetElementIds();
            //int counter = deleted.Count + changed.Count + added.Count;

            DateTime now = DateTime.Now;
            if (deleted.Count != 0)
            {
                foreach (ElementId id in deleted)
                {
                    if (currentSesion.oldValues.ContainsKey(id)) //
                    {
                        currentSesion.newValues.Add(id, currentSesion.oldValues[id]);
                    }
                    else if (currentSesion.newValues.ContainsKey(id)) { //element wasn't present in the start and was added and removed (nothing changed we dont report the changes)
                        currentSesion.newValues.Remove(id);
                    }
                    else //coudnt find element in new or old hash table: something was deleted not shure what it was but it had an ID
                    {
                        ElementData deletedElement = new ElementData();
                        deletedElement.type = "deleted"; deletedElement.name = "deleted"; deletedElement.CategoryType = "deleted";
                        deletedElement.uniqueId = "deleted"; deletedElement.changeType = "Deleted"; deletedElement.id = "" + id; deletedElement.user = user;
                        deletedElement.sessionId = "" + Properties.Settings1.Default.SessionID; deletedElement.changeTimestamp = "" + now;
                        currentSesion.newValues.Add(id, deletedElement);
                    }
                }
            }
            if (added.Count != 0)
            {
                foreach (ElementId id in added)
                {
                    Element element = doc.GetElement(id);
                    currentSesion.newValues.Add(id, itemData(element, "Added", user, Properties.Settings1.Default.SessionID));
                }
            }
            if (changed.Count != 0)
            {
                foreach (ElementId id in changed)
                {
                    Element element = doc.GetElement(id);
                    if (currentSesion.newValues.ContainsKey(id)) { //modify existing entry
                        ElementData oldData = (ElementData)(currentSesion.newValues[id]);
                        if (oldData.changeType == "Added") currentSesion.newValues[id] = itemData(element, "Added", user, Properties.Settings1.Default.SessionID); //if we modified new element still track it as added
                        else currentSesion.newValues[id] = itemData(element, "Modified", user, Properties.Settings1.Default.SessionID);
                    }
                    currentSesion.newValues.Add(id, itemData(element, "Modified", user, Properties.Settings1.Default.SessionID));

                }
            }

        }
        private ElementData itemData(Element element,string changeType,string user,int sessionID)
        {
            ElementData data = new ElementData();
            data.id = ("" + element.Id);
            data.user = user;
            data.sessionId = ("" + sessionID);
            data.changeType = changeType;
            DateTime now = DateTime.Now;
            data.changeTimestamp = ("" + now);

            try { data.uniqueId = element.UniqueId; } catch (Exception) { data.uniqueId = "none"; }
            try { data.name = element.Name; } catch (Exception) { data.name = "none"; }
            try { data.type = element.GetType().Name; } catch (Exception) { data.type = "none"; }
            try { if(element.Category != null) data.CategoryType = element.Category.Name; } catch (Exception) { data.CategoryType = "none"; }

            return data;
        }
        private string logChange(string actionType, string filename,Document doc, ElementId id) {
            Element element = doc.GetElement(id);
            string categoryName = "none";
            try { categoryName = element.Name; }catch (Exception){}

            return element.UniqueId +";"+ categoryName + ";" + element.Name;
        }
        private string getInfoDeleted(Document doc, ElementId id)
        {
            string uniqueid = "none";
            string categoryName = "none";
            string elementName = "none";
            //look inoto the hastable for previus elements
            return uniqueid+";"+categoryName+";"+elementName+";"+id;
        }
        public void seeCurrentChanges(UIApplication uiapp, Document doc) {
            string filesDir = Properties.Settings1.Default.ExportLoaction;
            string filename = doc.PathName;
            string filenameShort = Path.GetFileNameWithoutExtension(filename);
            var outputFile = Path.Combine(filesDir, filenameShort + Properties.Settings1.Default.ChangesFileEnding + ".csv");

            bool tracking = false;
            foreach (var item in TrackedDocuments)
            {
                if (item.fileName == filenameShort) { tracking = true; currentSesion = item; }//set to the document sesion to current document
            }
            if (tracking)
            {
                //openForm
                Controllers.InspectChangesController controller = new Controllers.InspectChangesController(uiapp, currentSesion.newValues);
                InspectChanges form = new InspectChanges();
                form.Controller = controller;
                form.ShowDialog();
            }
        }

        public void saveHastableData(System.Collections.Hashtable hashtable, string outputFile)
        {
            using (StreamWriter sw = new StreamWriter(outputFile, true))
            {
                if (new FileInfo(outputFile).Length == 0) //first line contains the legend
                {
                    sw.WriteLine("Timestamp;ID;Action;User;Type;Name;Category;UniqueID;SessionID");
                }
                System.Collections.ICollection keys = hashtable.Keys;
                foreach (var key in keys)
                {
                    ElementData data = (ElementData)(hashtable[key]);
                    sw.WriteLine(
                        data.changeTimestamp+";"+data.id+";"+data.changeType+";"+data.user+";"+
                        data.type+";"+ data.name +";"+data.CategoryType+";" + data.uniqueId + ";" + data.sessionId);
                }
                sw.Close();
            }
        }
    }
}


