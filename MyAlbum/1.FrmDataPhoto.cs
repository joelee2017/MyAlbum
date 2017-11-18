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

        private void button4_Click(object sender, EventArgs e)
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

                var allRow = dt.NewRow();
                allRow["PhotoType"] = "全部";

                dt.Rows.InsertAt(allRow, 0);

                lstPhotoType.DataSource = dt;
            }
        }

        private void lstPhotoType_SelectedIndexChanged(object sender, EventArgs e)
        {
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
            }

            using (var cn = new SqlConnection(Settings.Default.DP))
            using (var adp = new SqlDataAdapter(sqlText, cn))
            {
                adp.SelectCommand.Parameters.Add("type", SqlDbType.NVarChar).Value = photoType;
                var dt = new DataTable();

                adp.Fill(dt);

                var imgList = new ImageList();
                imgList.ImageSize = new Size(100, 100);
                lstPhoto.LargeImageList = imgList;

                lstPhoto.Items.Clear();
                foreach (var row in dt.AsEnumerable())
                {
                    var photoBytes = row.Field<byte[]>("Photo");
                    var img = Image.FromStream(new MemoryStream(photoBytes));

                    var id = row.Field<int>("Id").ToString();
                    imgList.Images.Add(id, img);
                    var item = lstPhoto.Items.Add(id);
                    item.ImageKey = id;
                }


            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                picUploadPicture.Image = Image.FromStream(openFileDialog1.OpenFile());
                txtFileName.Text = openFileDialog1.SafeFileName;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var cn = new SqlConnection(Settings.Default.DP))
            using (var cmd = new SqlCommand(@"
                INSERT INTO PhotoTable(PhotoDesc, PhotoDate, PhotoType, Photo)
                VALUES(@fileName, @date, @type, @photo);", cn))
            {
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
