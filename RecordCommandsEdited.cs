﻿#region Namespaces
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
                    application.ControlledApplication.DocumentChanged += new EventHandler
                        <DocumentChangedEventArgs>(ChangeTracker);

                    string tempFolder = "C:\\Users\\vican\\Documents\\Personal Files\\THESIS\\Databose Storage of Project Parameters";
                    string outputFile = Path.Combine(tempFolder, "NewProject.csv");
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
            Application app = sender as Application;
            UIApplication uiapp = new UIApplication(app);
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            View currentView = uidoc.ActiveView;

            string user = doc.Application.Username;
            string filename = doc.PathName;
            string filenameShort = Path.GetFileNameWithoutExtension(filename);
            string tempFolder = "C:\\Users\\vican\\Documents\\Personal Files\\THESIS\\Databose Storage of Project Parameters";
            string outputFile = Path.Combine(tempFolder, "NewProject.csv");

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
                        if (selected.Contains(id))
                        {
                            sw.WriteLine("Modified ," + now + ", " + doc.GetElement(id).Id + " ," + doc.GetElement(id).Category.Name + " ," + doc.GetElement(id).Name);
                        }
                    }
                }
            }
        }
    }
}

