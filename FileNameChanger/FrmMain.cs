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
using System.Globalization;

namespace FileNameChanger
{
    public partial class FrmMain : Form
    {
        public class FileData
        {
            public string filename;

            public string originalFilename;
            public DateTime originalCDate;
            public DateTime originalMDate;

            public FileData(string filename)
            {
                this.filename = filename;

                originalFilename = filename;
                originalCDate = File.GetCreationTime(filename);
                originalMDate = File.GetLastWriteTime(filename);
            }

            public bool Equals(object obj)
            {
                return filename.Equals(obj);
            }

            public void SyncTo(ListViewItem item)
            {
                string path = Path.GetDirectoryName(filename);
                string name = Path.GetFileName(filename);
                DateTime date = DateFromFile(filename);

                while (item.SubItems.Count < 4)
                    item.SubItems.Add("");

                item.SubItems[0].Text = name;
                item.SubItems[1].Text = date.ToString();
                item.SubItems[2].Text = path;
                item.SubItems[3].Text = Path.GetFileName(originalFilename);
            }
        }

        public FrmMain()
        {
            InitializeComponent();
        }

        private static DateTime DateFromFilename(string filename)
        {
            string name = Path.GetFileNameWithoutExtension(filename);
            if (name.Length > 14)
                name = name.Substring(0, 14);

            return DateTime.ParseExact(name, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        }

        private static DateTime DateFromFile(string filename)
        {
            //DateTime date = File.GetCreationTime(filename);
            DateTime date = File.GetLastWriteTime(filename);

            if (date.Year < 1900)
                throw new Exception("Failed to get creation time of " + filename);

            return date;
        }

        private void FrmMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void FrmMain_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string filename in files)
            {
                AddFile(filename);
            }
        }

        private void AddFile(string filename)
        {
            DateTime date = DateFromFile(filename);
            try
            {
                DateTime dateFromName = DateFromFilename(filename);
                if (date.Equals(dateFromName))
                {
                    WriteLog(filename + " has same creation datetime. ignored");
                    return;
                }
            }
            catch {} // ignore

            foreach (ListViewItem i in lvFiles.Items)
            {
                if (i.Tag.Equals(filename))
                    return;
            }

            FileData fileData = new FileData(filename);
            ListViewItem item = new ListViewItem();
            item.Tag = fileData;
            fileData.SyncTo(item);

            lvFiles.Items.Add(item);
        }

        private void DeleteFile()
        {
            if (lvFiles.SelectedItems.Count <= 0)
                return;

            ListViewItem[] items = new ListViewItem[lvFiles.SelectedItems.Count];
            lvFiles.SelectedItems.CopyTo(items, 0);
            foreach (ListViewItem item in items)
            {
                lvFiles.Items.Remove(item);
            }
        }

        private void ClearFile()
        {
            lvFiles.Items.Clear();
        }

        private void RevertFile(ListViewItem item)
        {
            FileData fileData = (FileData)item.Tag;

            File.SetCreationTime(fileData.filename, fileData.originalCDate);
            File.SetLastWriteTime(fileData.filename, fileData.originalMDate);
            File.Move(fileData.filename, fileData.originalFilename);

            fileData.filename = fileData.originalFilename;
            fileData.SyncTo(item);
        }

        private void WriteLog(string msg)
        {
            tbLog.AppendText(msg + "\r\n");
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = true;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                foreach (string filename in dlg.FileNames)
                {
                    AddFile(filename);
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            DeleteFile();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearFile();
        }

        private void lvFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteFile();
            }
        }

        private void DateToFilename(ListViewItem item)
        {
            FileData fileData = (FileData)item.Tag;
            string filename = fileData.filename;
            DateTime date = DateFromFile(filename);

            string path = Path.GetDirectoryName(filename);
            string ext = Path.GetExtension(filename);
            string name = date.ToString("yyyyMMddHHmmss");

            string newFilename = $"{path}\\{name}{ext}";

            if (!filename.Equals(newFilename))
            {
                try
                {
                    File.Move(filename, newFilename);

                    fileData.filename = newFilename;
                    fileData.SyncTo(item);
                }
                catch (Exception e)
                {
                    throw new Exception($"File rename failed [{filename} -> {newFilename}]", e);
                }
            }
        }

        private void btnDate2Name_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in lvFiles.Items)
            {
                try
                {
                    DateToFilename(item);
                }
                catch (Exception ex)
                {
                    WriteLog(ex.Message);
                }
            }
        }

        private void FilenameToDate(ListViewItem item)
        {
            FileData fileData = (FileData)item.Tag;
            string filename = fileData.filename;
            try
            {
                DateTime date = DateFromFilename(filename);

                File.SetCreationTime(filename, date);
                File.SetLastWriteTime(filename, date);

                fileData.SyncTo(item);
            }
            catch (FormatException e)
            {
                throw new Exception(filename + " doesn't have date information", e);
            }
        }

        private void btnName2Date_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in lvFiles.Items)
            {
                try
                {
                    FilenameToDate(item);
                }
                catch (Exception ex)
                {
                    WriteLog(ex.Message);
                }
            }
        }

        private void btnRevert_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in lvFiles.Items)
            {
                try
                {
                    RevertFile(item);
                }
                catch (Exception ex)
                {
                    WriteLog(ex.Message);
                }
            }
        }
    }
}
