using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RealBIM
{
    public partial class Form1 : Form
    {
        private Worker _worker;

        public Form1()
        {
            InitializeComponent();

            _worker = new Worker();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _worker.AnimateBuild();
        }
    }
}
