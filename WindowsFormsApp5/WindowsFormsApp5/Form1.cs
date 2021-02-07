using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Imaging.Filters;
using System.IO;



namespace WindowsFormsApp5
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pictureBox1.Image = new Bitmap(openFileDialog1.FileName);

            }


        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            Threshold threshold = new Threshold(128);
            Invert invert = new Invert();
            pictureBox1.Image = invert.Apply(threshold.Apply((Bitmap)pictureBox1.Image));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                BradleyLocalThresholding threshold = new BradleyLocalThresholding();
                Invert invert = new Invert();

                string[] files = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*.bmp");
                foreach (string currentFile in files)
                {
                    Bitmap bmp = new Bitmap(currentFile);
                    Bitmap res = threshold.Apply(bmp);
                   

                    res.Save(currentFile.Replace(".bmp", "_res.bmp"));
                }
            }
        }
    }
}
