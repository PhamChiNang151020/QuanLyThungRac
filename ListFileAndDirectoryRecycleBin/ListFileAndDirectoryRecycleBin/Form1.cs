using Shell32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace ListFileAndDirectoryRecycleBin
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }
        // hàm dùng để hiện thị thông tin vào trong ListView.
        //=> Thư Viết
        public void ListFileAndFolderRecycleBin()
        {
            var recycleBinItems = GetRecycleBinItems();
            listView1.Items.Clear();
            foreach (var item in recycleBinItems)
            {
                ListViewItem lvItem = new ListViewItem(new string[] { item.FileName, item.OriginPath, item.FileType, item.FileSize,  item.ModifyDate,  item.RecycleBinPath});
                listView1.Items.AddRange(new ListViewItem[] { lvItem });
            }
           

        }

        // class RecycleBinItem để chứa các thông tin như file name,RecycleBinPath,OriginPath ,FileType, FileSize,ModifyDate.
        public class RecycleBinItem

        {
            public string FileName { get; set; }
            public string RecycleBinPath { get; set; }
            public string OriginPath { get; set; }
            public string FileType { get; set; }
            public string FileSize { get; set; }
            public string ModifyDate { get; set; }
        }
        // Hàm để lấy tất cả các file trong Recycle Bin và trả về List RecycleBinItem
        public List <RecycleBinItem> GetRecycleBinItems()
        {
            try
            {
                Shell shell = new Shell();
                List<RecycleBinItem> list = new List<RecycleBinItem>();
                Folder recycleBin = shell.NameSpace(10);
                foreach (FolderItem2 f in recycleBin.Items())
                    list.Add(
                            new RecycleBinItem
                            {
                                FileName = f.Name,
                                FileType = f.Type,
                                FileSize = convert.ToPrettySize(GetSize(f)),
                                ModifyDate = f.ModifyDate.ToString("dd/MM/yyyy HH:mm:ss"),
                                OriginPath = recycleBin.GetDetailsOf(f, 1),
                                RecycleBinPath = f.Path
                            });
                Marshal.FinalReleaseComObject(shell);
                return list;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Lỗi khi truy cập vào Recycle Bin: {0}", ex.Message));
                return null;
            }
        }

        public double GetSize(FolderItem folderItem)
        {
            if (!folderItem.IsFolder)
                return (double)folderItem.Size;
            Folder folder = (Folder)folderItem.GetFolder;
            double size = 0;
            foreach (FolderItem2 f in folder.Items())
                size += GetSize(f);
            return size;
        }
        //Viết sự kiện cho nút load danh sách file và thư mục
        private void button1_Click(object sender, EventArgs e)
        {
            ListFileAndFolderRecycleBin();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn dòng dữ liệu để xóa!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Error);           
                return;
            }             

            ListViewItem item = listView1.SelectedItems[0];
            if(item.SubItems[2].Text.Equals("File folder"))
            {
                // xóa folder
                if (Directory.Exists(item.SubItems[5].Text))
                {
                    Directory.Delete(item.SubItems[5].Text, true);
                }
               
            }
            else
            {
                // Xóa file
                File.Delete(item.SubItems[5].Text);
            }           
            ListFileAndFolderRecycleBin();


        }

        private void btnEmptyRecycleBin_Click(object sender, EventArgs e)
        {
            //kiểm tra thùng rác có file hay không
            ListFileAndFolderRecycleBin();
            var recycleBinItems = GetRecycleBinItems();
            if (recycleBinItems.Count > 0)
            {
                DialogResult dialogResult = MessageBox.Show( "Bạn có chắc chắn muốn xóa hết dữ liệu trong thùng rác không?", "Thông báo!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogResult == DialogResult.Yes)
                {
                    foreach(var item in recycleBinItems)
                    {
                        if (item.FileType.Equals("File folder"))
                        {
                            // xóa folder
                            if (Directory.Exists(item.RecycleBinPath))
                            {
                                Directory.Delete(item.RecycleBinPath, true);
                            }
                        }
                        else
                        {
                            //Xóa file
                            File.Delete(item.RecycleBinPath);
                        }
                    }
                    ListFileAndFolderRecycleBin();
                }
                
            }
            else
            {
                MessageBox.Show("Không có tập tin hay thư mục nào trong thùng rác!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }            
        }

        //khôi phục tập tin hoặc thư mục
        private bool Restore(string Item)
        {
            var Shl = new Shell();
            Folder Recycler = Shl.NameSpace(10);
            for (int i = 0; i < Recycler.Items().Count; i++)
            {
                FolderItem FI = Recycler.Items().Item(i);
                string FileName = Recycler.GetDetailsOf(FI, 0);
                if (Path.GetExtension(FileName) == "") FileName += Path.GetExtension(FI.Path);
                //phục vụ cho các phần có đuôi mở rộng bị ẩn
                string FilePath = Recycler.GetDetailsOf(FI, 1);
                if (Item == Path.Combine(FilePath, FileName))
                {
                    DoVerb(FI, "ESTORE");
                    return true;
                }
            }
            return false;
        }
        private bool DoVerb(FolderItem Item, string Verb)
        {
            foreach (FolderItemVerb FIVerb in Item.Verbs())
            {
                if (FIVerb.Name.ToUpper().Contains(Verb.ToUpper()))
                {
                    FIVerb.DoIt();
                    return true;
                }
            }
            return false;
        }

        private void btn_Restore_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn dòng dữ liệu để khôi phục!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ListViewItem item = listView1.SelectedItems[0];
            Restore(item.SubItems[1].Text + "\\" + item.SubItems[0].Text + Path.GetExtension(item.SubItems[5].Text));
            ListFileAndFolderRecycleBin();
        }

        private void btnThoat_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //if (Messagebox.Show("bạn có thích out", "Thông báo..", Messagebuttom.Yesno) == Dialogresult.Yes)
            //{
            //    e.cance = true;
            //}
            if (MessageBox.Show("Thoát Chương Trình", "Thông báo", MessageBoxButtons.YesNo) == DialogResult.No)
                e.Cancel = true;
        }

        private void btnThongTin_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Chương trình được thực hiện bởi:\n     +Phạm Chí Năng: 1851010076\n     +Hứa Thái Anh Thư: 1851050141","Thông tin");
        }

    }
}
