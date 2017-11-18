using MyAlbum.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;
using System.Drawing.Imaging;

namespace MyAlbum
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }

        private void PhotoLoad_Click(object sender, EventArgs e)//讀取功能
        {
            // test
            using (var cn = new SqlConnection(Settings.Default.DP))
            using (var adp = new SqlDataAdapter(@"
                SELECT DISTINCT
                    PhotoType
                FROM
                    PhotoTable;", cn))
            {
                var dt = new DataTable();

                adp.Fill(dt);

                //加入全部的選項
                var allRow = dt.NewRow();
                allRow["PhotoType"] = "全部";

                dt.Rows.InsertAt(allRow, 0);//放在第 0個位置

                lstPhotoType.DataSource = dt;
            }
        }

        private void lstPhotoType_SelectedIndexChanged(object sender, EventArgs e)//listbox事件
        {
            //取得DataSource強轉string
            var photoType = (string)lstPhotoType.SelectedValue;

            var sqlText = string.Empty;

            if (photoType == "全部")
            {
                sqlText = @"
                    SELECT
                        Id,
                        Photo
                    FROM
                        PhotoTable;";
            }
            else
            {   
                sqlText = @"
                    SELECT
                        Id,
                        Photo
                    FROM
                        PhotoTable
                    WHERE
                        PhotoType = @type;";
            }//關聯至下方SqlDataAdapter

            using (var cn = new SqlConnection(Settings.Default.DP))
            using (var adp = new SqlDataAdapter(sqlText, cn))
            {
                //SelectCommand.Parameters.Add(string, DB欄位型別.xxxxx).取得值 =從DB PhotoType;
                adp.SelectCommand.Parameters.Add("type", SqlDbType.NVarChar).Value = photoType;

                var dt = new DataTable();//宣告DataTable至下方adp連接中

                adp.Fill(dt);

                var imgList = new ImageList();//宣告ImageList
                imgList.ImageSize = new Size(100, 100);//宣告Size
                lstPhoto.LargeImageList = imgList;//將ImageList指派主顯示區大圖
                //建置圖片控制項大圖設定，此時還未將圖從DB匯入
                lstPhoto.Items.Clear();
                //LINQ 新學習
                var imgs = from row in dt.AsEnumerable() 
                           //宣告imgs =from row in dt.AsEnumerable() = DB = PhotoTable
                           select new 
                           {
                               Id = row.Field<int>("Id").ToString(),
                               Pic = Image.FromStream(new MemoryStream(row.Field<byte[]>("Photo")))
                           };//宣告強型別 ID photo放入

                foreach (var item in imgs)//還需使用foreach將圖放入item之中
                {
                    imgList.Images.Add(item.Id, item.Pic);//將DB資料匯入
                    var listItem = lstPhoto.Items.Add(item.Id);
                    //將item.Id 加到宣告的listItem之中
                    listItem.ImageKey = item.Id;
                    //在讓item.Id成為   listItem的索引值
                }
            }
        }

        private void OpenFile_Click(object sender, EventArgs e)//打開檔案選取
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)//選取動作ok後
            {
                picUploadPicture.Image = Image.FromStream(openFileDialog1.OpenFile());
                txtFileName.Text = openFileDialog1.SafeFileName;
            }
        }

        private void InsertDB_Click(object sender, EventArgs e)
        {
            using (var cn = new SqlConnection(Settings.Default.DP))
            using (var cmd = new SqlCommand(@"
                INSERT INTO PhotoTable(PhotoDesc, PhotoDate, PhotoType, Photo)
                VALUES(@fileName, @date, @type, @photo);", cn))
            {
                cmd.CommandType = CommandType.Text;

                cmd.Parameters.Add("fileName", SqlDbType.NVarChar).Value = txtFileName.Text;
                cmd.Parameters.Add("date", SqlDbType.DateTime).Value = dtPhotoDate.Value;
                cmd.Parameters.Add("type", SqlDbType.NVarChar).Value = txtPhotoType.Text;

                using (var ms = new MemoryStream())
                {
                    picUploadPicture.Image.Save(ms, ImageFormat.Jpeg);

                    cmd.Parameters.Add("photo", SqlDbType.VarBinary).Value = ms.ToArray();
                }

                cn.Open();

                cmd.ExecuteNonQuery();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var selectedItem = lstPhoto.SelectedItems.Cast<ListViewItem>().First();

            var selectedId = int.Parse(selectedItem.Text);
            // MessageBox.Show(selectedItem.Text);

            using (var cn = new SqlConnection(Settings.Default.DP))
            using (var cmd = new SqlCommand(@"
                DELETE PhotoTable
                WHERE
                    Id = @id", cn))
            {
                cmd.Parameters.Add("id", SqlDbType.Int).Value = selectedId;

                cn.Open();

                cmd.ExecuteNonQuery();
            }
        }
    }
}
