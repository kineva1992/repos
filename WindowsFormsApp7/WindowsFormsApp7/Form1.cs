using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using BytesRoad.Net.Ftp;
using System.Threading;

namespace WindowsFormsApp7
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Form_Connect secondForm = new Form_Connect();
            //скрываем форму из панели задач
            secondForm.ShowInTaskbar = false;
            //устанавливаем форму по центру экрана
            secondForm.StartPosition = FormStartPosition.CenterParent;
            secondForm.ShowDialog(this);

            try
            {
                //делаем доступными компоненты
                listView1.Items.Clear();
                GetItemsFromFtp();
                button1.Enabled = false;
                button2.Enabled = true;
                menuStrip1.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Подключение не выполнено");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

            FTPparams.client.Disconnect(FTPparams.TimeoutFTP);
            listView1.Items.Clear();
            //делаем не доступными компоненты
            button1.Enabled = true;
            button2.Enabled = false;
            menuStrip1.Enabled = false;
            FTPparams.FTPFullPath = @"/";
            FTPparams.Path = "";
            textBox1.Text = FTPparams.FTPFullPath;
        }

        private void скачатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //проверяем что это файл по тегу
                if (listView1.Items[listView1.SelectedIndices[0]].Tag.ToString() == "2")
                {
                    Thread t = new Thread(LoadFromFtp);
                    t.Start(new Object[] { listView1.Items[listView1.SelectedIndices[0]].Text,
                                            listView1.Items[listView1.SelectedIndices[0]].SubItems[1].Text,
                                            listView1.Items[listView1.SelectedIndices[0]].SubItems[2].Text});
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Файл не выбран");
                // MessageBox.Show(ex.Message);
            }
        }

        private void загрузитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //проверяем что это файл по тегу
                if (listView2.Items[listView2.SelectedIndices[0]].Tag.ToString() == "2")
                {
                    Thread t = new Thread(LoadFromClient);
                    t.Start(new Object[] { listView2.Items[listView2.SelectedIndices[0]].Text,
                                            listView2.Items[listView2.SelectedIndices[0]].SubItems[1].Text,
                                            listView2.Items[listView2.SelectedIndices[0]].SubItems[2].Text});
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Файл не выбран", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void удалитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if ((listView1.Focused == true) && (listView1.Items[listView1.SelectedIndices[0]].Tag.ToString() == "2"))
                {
                    FTPparams.client.DeleteFile(FTPparams.TimeoutFTP, FTPparams.FTPFullPath + @"/" + listView1.Items[listView1.SelectedIndices[0]].Text);
                    listView1.Items.Clear();
                    GetItemsFromFtp();
                }
                if ((listView2.Focused == true) && (listView2.Items[listView2.SelectedIndices[0]].Tag.ToString() == "2"))
                {
                    File.Delete(LocalPath.FullPath + @"\" + listView2.Items[listView2.SelectedIndices[0]].Text);
                    listView2.Items.Clear();
                    GetItemsFromClient();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Файл не выбран");
            }
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            thread_work = true;
        }

        private void cbListLocalDisk_SelectedIndexChanged(object sender, EventArgs e)
        {
            LocalPath.FullPath = cbListLocalDisk.Items[cbListLocalDisk.SelectedIndex].ToString();
            LocalPath.Path = "";
            tbPathlocal.Text = LocalPath.FullPath;
            listView2.Items.Clear();
            GetItemsFromClient();
        }

        private void tbPathlocal_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.Enter))
            {
                //проверяем существует ли директория
                if (Directory.Exists(tbPathlocal.Text))
                {
                    LocalPath.FullPath = tbPathlocal.Text;
                    LocalPath.Path = newPath(LocalPath.FullPath, 92);
                    //записываем имя диска после перехода в комбобокс
                    string buffpach = LocalPath.FullPath.ToString();
                    cbListLocalDisk.Text = buffpach.Substring(0, 3);
                    LocalPath.FullPath = buffpach;

                    listView2.Items.Clear();
                    GetItemsFromClient();
                    tbPathlocal.Text = LocalPath.FullPath;
                    LocalPath.Path = newPath(LocalPath.FullPath, 92);
                }
                else
                {
                    textBox1.Select();
                    MessageBox.Show("Указан неверный путь", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GetLocalDisk();
            listView2.Items.Clear();
            GetItemsFromClient();
        }

    }
}
