using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Wizard
{
    public partial class Form1 : Form
    {
        private Map map;
        private Thread seachThread;
        public static bool GameRunning { get; private set; }
        public static bool IsPaused { get; private set; }
        public static int Speed { get; private set; }

        public Form1()
        {
            InitializeComponent();

            map = new Map(CreateGraphics(), this.DisplayRectangle);
            Speed = trackBar1.Value;
            GameRunning = false;
            IsPaused = false;
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked) seachThread = new Thread(map.ASearchPath);
            else if (radioButton2.Checked) seachThread = new Thread(map.BSearchPass);
            else seachThread = new Thread(map.DSearchPass);

            seachThread.IsBackground = true;
            seachThread.Start();

            buttonStart.Enabled = false;
            radioButton1.Enabled = false;
            radioButton2.Enabled = false;
            radioButton3.Enabled = false;
            GameRunning = true;
            IsPaused = false;
            buttonPause.Enabled = true;
        }

        private void Loop_Tick(object sender, EventArgs e)
        {
            //Draws all our cells
            map.Render();
            label1.Text = seachThread != null && seachThread.IsAlive ? "SEARCHING" : "";
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            //Checks if we clicked a cell
            if(buttonStart.Enabled)
            map.ClickNode(this.PointToClient(Cursor.Position));
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            if (seachThread.IsAlive) seachThread.Abort();
            map.RestartGame();
            buttonStart.Enabled = true;
            radioButton1.Enabled = true;
            radioButton2.Enabled = true;
            radioButton3.Enabled = true;
            GameRunning = false;

            IsPaused = false;
            buttonPause.Enabled = false;
            buttonPause.Text = "STOP";
        }

        private void buttonPause_Click(object sender, EventArgs e)
        {
            IsPaused = !IsPaused;
            buttonPause.Text = IsPaused ? "RESUME" : "STOP";
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            Speed = trackBar1.Value;
        }
    }
}
