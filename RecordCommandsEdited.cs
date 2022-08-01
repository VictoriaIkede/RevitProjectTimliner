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
        public Result OnShutdown(UIControlledApplication application)
            {
                application.ControlledApplication.DocumentChanged -= ChangeTracker;
                return Result.Succeeded;
            }

            public Result OnStartup(UIControlledApplication application)
            {

                thisApp = this;
                try
                {
                    //TODO create app UI butons
                    //button to open setings
                    //button to open a form for inspecting recoreded changes

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
                    application.ControlledApplication.DocumentChanged += new EventHandler<DocumentChangedEventArgs>(ChangeTracker);
                
                    string filesDir = Properties.Settings1.Default.ExportLoaction;
                    if (filesDir == "") { filesDir = "C:\\Reports"; }
                        var fileName = "_FileName_Changes.csv";
                        var outputFile = Path.Combine(filesDir, fileName);
                        //TODO when trackig a new doc first write the date of tracking. Is this even nesesery we can get the date of tracking from the earliest change
                        using (StreamWriter sw = new StreamWriter(outputFile, true))
                        {
                            DateTime now = DateTime.Now;
                            sw.WriteLine("Project tracking started at ," + now + ".");
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
            if (Properties.Settings1.Default.TrackChanges && Properties.Settings1.Default.ExportLoaction.Length > 0)
            {
                Application app = sender as Application;
                UIApplication uiapp = new UIApplication(app);
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;
                View currentView = uidoc.ActiveView;


                string filesDir = Properties.Settings1.Default.ExportLoaction;
                if (filesDir == "") { filesDir = "C:\\Reports"; }
                string user = doc.Application.Username;
                string filename = doc.PathName;
                string filenameShort = Path.GetFileNameWithoutExtension(filename);
                var outputFile = Path.Combine(filesDir, filenameShort + "_Changes.csv");

                Selection sel = uidoc.Selection;
                ICollection<ElementId> deleted = args.GetDeletedElementIds();
                ICollection<ElementId> changed = args.GetModifiedElementIds();
                ICollection<ElementId> added = args.GetAddedElementIds();
                ICollection<ElementId> selected = sel.GetElementIds();
                int counter = deleted.Count + changed.Count + added.Count;

                using (StreamWriter sw = new StreamWriter(outputFile, true))
                {
                    DateTime now = DateTime.Now;

                    if (deleted.Count != 0)
                    {
                        foreach (ElementId id in deleted)
                        {
                            sw.WriteLine("Deleted ," + now + ", " + doc.GetElement(id).Id + " ," + doc.GetElement(id).Category.Name + " ," + doc.GetElement(id).Name);
                        }
                    }
                    if (added.Count != 0)
                    {
                        foreach (ElementId id in added)
                        {
                            sw.WriteLine("Added ," + now + ", " + doc.GetElement(id).Id + " ," + doc.GetElement(id).Category.Name + " ," + doc.GetElement(id).Name);
                        }
                    }
                    if (changed.Count != 0)
                    {
                        foreach (ElementId id in changed)
                        {
                            //why only selected some changes can effect multiple elements that arent neseserly selected
                            //if (selected.Contains(id))
                            //{
                            //    sw.WriteLine("Modified ," + now + ", " + doc.GetElement(id).Id + " ," + doc.GetElement(id).Category.Name + " ," + doc.GetElement(id).Name);
                            //}
                            sw.WriteLine("Modified ," + now + ", " + doc.GetElement(id).Id + " ," + doc.GetElement(id).Category.Name + " ," + doc.GetElement(id).Name);
                        }
                    }
                }
            }
        }
    }
}


