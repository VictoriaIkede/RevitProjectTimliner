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

        public InspectChanges()
        {
            InitializeComponent();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

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

        private void button2_Click(object sender, EventArgs e)
        {
            //save data to csv file
            saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "CSV Files (*.csv)|*.csv";
            saveFileDialog1.Title = "Save data to csv file";
            saveFileDialog1.ShowDialog();
            RecordCommandsEdited app = RecordCommandsEdited.thisApp;
            app.saveHastableData(Controller._changes, saveFileDialog1.FileName,Controller.firstChange, Controller.lastChange, Controller.firstChangeUser, Controller.lastChangeUser);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Controller.EndDocSession();
            this.Close();
        }
    }
}
