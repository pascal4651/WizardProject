using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Wizard
{
    public partial class FormStart : Form
    {
        public FormStart()
        {
            InitializeComponent();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            Form1 f = new Form1();
            f.Show();
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonHow_Click(object sender, EventArgs e)
        {
            if(textBox1.Visible == true)
            {
                pictureBox1.Visible = true;
                textBox1.Visible = false;
            }
            else
            {
                pictureBox1.Visible = false;
                textBox1.Visible = true;
            }
        }
    }
}
