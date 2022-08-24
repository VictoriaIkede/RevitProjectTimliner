using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TrackChanges
{
    internal partial class InspectChanges : Form
    {
        public Controllers.InspectChangesController Controller { get; set; }
        public int selectedObjectID = -1;

        public InspectChanges()
        {
            InitializeComponent();
        }

        private void InspectChanges_Shown(object sender, EventArgs e)
        {
            Controller.listview = listView1;
            Controller.getChanges();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonSaveToCSV_Click(object sender, EventArgs e)
        {
            //save data to csv file
            saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV Files (*.csv)|*.csv";
            saveFileDialog1.Title = "Save data to csv file";
            saveFileDialog1.ShowDialog();
            RecordCommandsEdited app = RecordCommandsEdited.thisApp;
            app.saveHastableData(Controller._changes, saveFileDialog1.FileName,Controller.firstChange, Controller.lastChange, Controller.firstChangeUser, Controller.lastChangeUser);
        }

        private void buttonEndSession_Click(object sender, EventArgs e)
        {
            Controller.EndDocSession();
            this.Close();
        }

        private void buttonFocusElement_Click(object sender, EventArgs e)
        {
            //focus change and close the form
            if (listView1.SelectedItems.Count > 0) {
                ListView.SelectedListViewItemCollection selectedItems = listView1.SelectedItems;
                List<int> indexes = new List<int>();
                foreach (ListViewItem element in selectedItems)
                {
                    if (indexes.Contains(element.Index) == false) indexes.Add(element.Index);
                }
                
                Controller.FocusElement(indexes);
                this.Close();
            }
        }
    }
}
