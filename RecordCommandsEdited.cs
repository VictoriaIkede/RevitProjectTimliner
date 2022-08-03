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
using System.Windows.Media.Imaging;
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
            public string firstChange { get; set; }
            public string lastChange { get; set; }

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

        public documentSession currentSesion;

        public List<documentSession> TrackedDocuments;

        public Result OnShutdown(UIControlledApplication application)
        {
            application.ControlledApplication.DocumentChanged -= ChangeTracker;
            //save the Hastable data to a csv file at export location if we didnt save it alredy manualy

            string filesDirectory = Properties.Settings1.Default.ExportLoaction;
            if (filesDirectory == "") { Properties.Settings1.Default.ExportLoaction = Path.GetTempPath(); }

            foreach (documentSession item in TrackedDocuments)
            {
                var outputFileName = Path.Combine(filesDirectory, item.fileName + ".csv");
                saveHastableData(item.newValues, outputFileName, item.firstChange, item.lastChange, "on shutdown");
            }
            TrackedDocuments.Clear();

            LogEndOfSession();

            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {

            thisApp = this;
            try
            {

                String tabName = "Timliner";
                application.CreateRibbonTab(tabName);
                RibbonPanel curlPanel = application.CreateRibbonPanel(tabName, "Timliner");
                //locating the dll directory
                string curlAssembly = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string curlAssemblyPath = System.IO.Path.GetDirectoryName(curlAssembly);
                //set the button to preform changeSetingsCommand
                PushButtonData buttonData1 = new PushButtonData("Settings", "Settings", curlAssembly, "TrackChanges.ChangeSetingsCommand");
                buttonData1.LargeImage = new BitmapImage(new Uri(System.IO.Path.Combine(curlAssemblyPath, "resources\\gear-icon.png")));
                PushButton button1 = (PushButton)curlPanel.AddItem(buttonData1);

                PushButtonData buttonData2 = new PushButtonData("Changes", "Changes", curlAssembly, "TrackChanges.SeeChangesCommand");
                buttonData2.LargeImage = new BitmapImage(new Uri(System.IO.Path.Combine(curlAssemblyPath, "resources\\table-icon-21.png")));
                PushButton button2 = (PushButton)curlPanel.AddItem(buttonData2);


                //set event handler
                TrackedDocuments = new List<documentSession>(); //create a empty list of all document sessions
                application.ControlledApplication.DocumentChanged += new EventHandler<DocumentChangedEventArgs>(ChangeTracker);

                //create session log when starting app
                LogStartOfSession();

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
            DateTime now = DateTime.Now;

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
                newSession.firstChange = ""+now;
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

            
            if (deleted.Count != 0)
            {
                foreach (ElementId id in deleted)
                {
                    if (currentSesion.oldValues.ContainsKey(id)) //get old data that was deleted and update timestamp user session
                    {
                        ElementData oldData = (ElementData)(currentSesion.oldValues[id]);
                        oldData.changeType = "Deleted";
                        oldData.changeTimestamp = "" + now;
                        oldData.user = user;
                        oldData.sessionId = ""+Properties.Settings1.Default.SessionID;
                        currentSesion.newValues.Add(id, oldData);
                    }
                    else if (currentSesion.newValues.ContainsKey(id)) {
                        ElementData oldData = (ElementData)(currentSesion.newValues[id]);
                        if (oldData.changeType == "Modified") //element was first modified then deleted
                        {
                            oldData.changeType = "Deleted";
                            oldData.changeTimestamp = "" + now;
                            oldData.user = user;
                            oldData.sessionId = "" + Properties.Settings1.Default.SessionID;
                        }
                        else { currentSesion.newValues.Remove(id); }  //element wasn't present in the start was added and then removed (nothing changed we dont report the changes)

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
                    //TODO handel edge cases
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
            currentSesion.lastChange = "" + now;

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
            else {
                TaskDialog.Show("Changes", "No changes to show");
            }
        }

        public void saveHastableData(System.Collections.Hashtable hashtable, string outputFile,string firstChange="",string lastChange="",string firstUser="",string lastUser="")
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
                sw.WriteLine("First change on;" + firstChange + ";" + firstUser + ";" + Properties.Settings1.Default.SessionID + ";Last change on;" + lastChange + ";" + lastUser + ";" + Properties.Settings1.Default.SessionID); //comment out if trying to avoid brakes
                sw.Close();
            }
        }
        public void LogEndOfSession() {
            string filesDir = Properties.Settings1.Default.ExportLoaction;
            if (filesDir == "") { Properties.Settings1.Default.ExportLoaction = Path.GetTempPath(); }
            var fileName = "Sessions_log.csv";
            var outputFile = Path.Combine(filesDir, fileName);
            using (StreamWriter sw = new StreamWriter(outputFile, true))
            {
                DateTime now = DateTime.Now;
                sw.WriteLine("stoped;" + now + ";" + Properties.Settings1.Default.SessionID);
                sw.Close();
            }
            Properties.Settings1.Default.SessionID += 1; //update sesion number
            
        }
        public void LogStartOfSession()
        {
            string filesDir = Properties.Settings1.Default.ExportLoaction;
            if (filesDir == "") { Properties.Settings1.Default.ExportLoaction = Path.GetTempPath(); }
            var fileName = "Sessions_log.csv";
            var outputFile = Path.Combine(filesDir, fileName);
            using (StreamWriter sw = new StreamWriter(outputFile, true))
            {
                DateTime now = DateTime.Now;
                sw.WriteLine("started;" + now + ";" + Properties.Settings1.Default.SessionID);
                sw.Close();
            }
        }

        public void EndSessionDoc(string filenameShort, string comment = "") {
            string filesDirectory = Properties.Settings1.Default.ExportLoaction;
            if (filesDirectory == "") { Properties.Settings1.Default.ExportLoaction = Path.GetTempPath(); }
            
            foreach (documentSession item in TrackedDocuments)
            {
                if(filenameShort == item.fileName)
                {
                    var outputFileName = Path.Combine(filesDirectory, item.fileName + ".csv");
                    saveHastableData(item.newValues, outputFileName, item.firstChange, item.lastChange, comment);
                    TrackedDocuments.Remove(item);
                    Properties.Settings1.Default.SessionID += 1;
                }
            }
        }
    }
}


