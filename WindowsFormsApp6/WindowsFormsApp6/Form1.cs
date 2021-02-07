using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
//using AForge;
//using AForge.Imaging;
//using AForge.Imaging.Filters;
//using AForge.Imaging.ComplexFilters;
//using AForge.Imaging.ColorReduction;
using Accord.Imaging;
using Accord.Collections;
using Accord.DataSets;
using Accord.IO;
using Accord.Statistics;
using Accord.Imaging.Filters;
using Accord.Imaging.Converters;
using Accord.Math;
using NUnit.Framework;
using System.Drawing.Imaging;


namespace WindowsFormsApp6
{
    public partial class Form1 : Form
    {
        private string fileName;

        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pictureBox1.Image = (Bitmap)System.Drawing.Image.FromFile(openFileDialog1.FileName);
            }
        }

        private void binarToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Bitmap img = new Bitmap(pictureBox1.Image);

            SauvolaThreshold sav = new SauvolaThreshold();

            Bitmap result = sav.Apply(img);


            pictureBox2.Image = result;


            //pictureBox2.Image = result.Apply((Bitmap)pictureBox1.Image);


        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if(pictureBox2 != null)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Сохранить как ";
                sfd.OverwritePrompt = true;
                sfd.Filter = "Image Files(*.BMP)|*.BMP|Image Files(*.JPG)|*.JPG|Image Files(*.GIF)|*.GIF|Image Files(*.PNG)|*.PNG|All files (*.*)|*.*";
                sfd.ShowHelp = true;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        pictureBox2.Image.Save(sfd.FileName);
                        MessageBox.Show("Сохраненно успешно");
                    }
                    catch
                    {
                        MessageBox.Show("невозможно сохранить изображение","Ошибка",MessageBoxButtons.OK);
                    }
                }

            }

               

            
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
