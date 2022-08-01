using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TrackChanges.Controllers;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using RDB = Autodesk.Revit.DB;
using RDBE = Autodesk.Revit.DB.Events;
using RUI = Autodesk.Revit.UI;

namespace TrackChanges
{
    internal partial class SettingsForm : Form
    {
        public Controllers.SetingsFormController Controller { get; set; }

        public string reportsDir {get; set;}

        public SettingsForm(RDB.Document doc, RUI.UIApplication uiapp)
        {
            InitializeComponent();
            textBox1.Text = Properties.Settings1.Default.ExportLoaction;
        }

        private void ok_button_Click(object sender, EventArgs e)
        {
            Controller.ApplyChanges();
            this.Close();
        }


        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Controller.tracking = checkBox1.Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                Controller.exportPath = folderBrowserDialog1.SelectedPath;
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void cancel_button_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
