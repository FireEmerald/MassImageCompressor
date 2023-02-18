using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NeoFoton
{
    public partial class ProgressDlg : Form
    {
        public ProgressDlg()
        {
            InitializeComponent();
        }

        public void UpdateProgressBar(int progressPercentage, int totalDirToCompress, int currDirCounter, string currDirectory)
        {
            progressBar.Invoke((MethodInvoker)delegate
            {
                lblStatus.Text = "Compressing " + currDirCounter + "/" + totalDirToCompress + " directories. (" + currDirectory + ")";
                progressBar.Value = progressPercentage;
                progressBar.Update();
            });
        }

        private void btnAbort_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void progressBar_Click(object sender, EventArgs e)
        {

        }
      
    }
}
