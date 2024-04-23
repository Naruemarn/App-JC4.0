using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace App_JC4._0
{
    public partial class timeline : Form
    {
        string connStr = "";
        string serial = "";
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public timeline()
        {
            InitializeComponent();
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void dataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            try
            {
                // Show Line Number
                var grid = sender as DataGridView;
                var rowIdx = (e.RowIndex + 1).ToString();

                var centerFormat = new StringFormat()
                {
                    // right alignment might actually make more sense for numbers
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
                e.Graphics.DrawString(rowIdx, this.Font, SystemBrushes.ControlText, headerBounds, centerFormat);
            }
            catch (Exception ex)
            {

            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void timeline_Load(object sender, EventArgs e)
        {
            connStr = main.connStr;

            // Set color GridView
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.Blue;
            dataGridView1.EnableHeadersVisualStyles = false;

            serial = main.serial_global;

            Show_Status_GridView();

            timer1.Enabled = true;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string Show_Status_GridView()
        {
            var result = string.Empty;
            try
            {

                dataGridView1.DataSource = null;
                dataGridView1.Rows.Clear();
                dataGridView1.Refresh();


                string query = "SELECT serial, filename, status, datetime_start, datetime_end, different_time_mins, DATE_FORMAT(datetime_start, '%Y-%m-%d') AS today FROM tr_altima_status WHERE DATE(datetime_start) = CURDATE() AND serial = '" + serial + "' order by id asc; ";


                using (MySqlConnection conn = new MySqlConnection(connStr))
                {
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn))
                    {
                        DataSet ds = new DataSet();
                        adapter.Fill(ds);

                        if (ds != null)
                        {
                            if (ds.Tables[0].Rows.Count != 0)
                            {
                                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                {
                                    dataGridView1.Rows.Add();
                                    dataGridView1.AllowUserToAddRows = false;

                                    dataGridView1.Rows[i].Cells[0].Value = ds.Tables[0].Rows[i]["serial"].ToString();
                                    dataGridView1.Rows[i].Cells[1].Value = ds.Tables[0].Rows[i]["filename"].ToString();
                                    dataGridView1.Rows[i].Cells[2].Value = ds.Tables[0].Rows[i]["status"].ToString();
                                    dataGridView1.Rows[i].Cells[3].Value = ds.Tables[0].Rows[i]["datetime_start"].ToString();
                                    dataGridView1.Rows[i].Cells[4].Value = ds.Tables[0].Rows[i]["datetime_end"].ToString();
                                    dataGridView1.Rows[i].Cells[5].Value = ds.Tables[0].Rows[i]["different_time_mins"].ToString();

                                    // Change Color Status
                                    if (dataGridView1.Rows[i].Cells[2].Value.ToString() == "1")
                                    {
                                        dataGridView1.Rows[i].DefaultCellStyle.ForeColor = Color.Black;
                                        dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.Lime;
                                    }

                                    if (dataGridView1.Rows[i].Cells[2].Value.ToString() == "2")
                                    {
                                        dataGridView1.Rows[i].DefaultCellStyle.ForeColor = Color.Black;
                                        dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.Yellow;
                                    }

                                    if (dataGridView1.Rows[i].Cells[2].Value.ToString() == "3")
                                    {
                                        dataGridView1.Rows[i].DefaultCellStyle.ForeColor = Color.Black;
                                        dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.Red;
                                    }



                                }

                            }
                            else
                            {
                                dataGridView1.DataSource = null;
                            }
                        }

                        // Remove Select Focus
                        if (dataGridView1.RowCount > 0 && dataGridView1.ColumnCount > 0)
                        {
                            dataGridView1.CurrentCell = this.dataGridView1[0, 0];
                            this.dataGridView1.CurrentCell.Selected = false;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                //MessageBox.Show("Show_Status_DataGridView --------> " +ex.ToString());
            }

            return result;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void timer1_Tick(object sender, EventArgs e)
        {
            Show_Status_GridView();
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}
