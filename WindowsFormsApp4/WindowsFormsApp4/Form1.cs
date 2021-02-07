using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp4
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string dic = "";
            string tmp = "";
            if (checkBox1.Checked)
            {
                char nchar;
                for (int i = 65; i < 91; i++)
                {
                    nchar = (char)i;
                    tmp += Convert.ToString(nchar);
                }
                dic += tmp;
            }
            if (checkBox2.Checked) dic += "0123456789";
            if (checkBox3.Checked) dic += textBox2.Text;
            if (checkBox4.Checked)
            {
                tmp = "";
                char nchar;
                for (int i = 97; i < 123; i++)
                {
                    nchar = (char)i;
                    tmp += Convert.ToString(nchar);
                }
                dic += tmp;
                string pass = "";
                Random mran = new Random();
                for (int i = 0; i < numericUpDown1.Value; i++)
                {
                    int index = Convert.ToUInt16(mran.NextDouble() * dic.Length) % dic.Length;
                    char ScharS = dic[index];
                    pass += Convert.ToString(ScharS);
                }
                textBox1.Text = pass;

            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
