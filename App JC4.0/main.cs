using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

using MySql.Data.MySqlClient;
using WinSCP;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Globalization;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Reflection;

namespace App_JC4._0
{
    public partial class main : Form
    {
        // ปิด Sleep หน้าจอ , ปิด Screen Saver
        [FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
            // Legacy flag, should not be used.
            // ES_USER_PRESENT = 0x00000004
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        //----------------------------------------------------------------------------

        string connStr = "";

        string timeStart = null;

        string Server_ini = null;
        string User_ini = null;
        string Password_ini = null;
        string Database_ini = null;

        List<string> list_id = new List<string>();
        List<string> list_serial = new List<string>();
        List<string> list_productname = new List<string>();
        List<string> list_machinetype = new List<string>();
        List<string> list_ipaddress = new List<string>();
        List<string> list_ftp_username = new List<string>();
        List<string> list_ftp_password = new List<string>();
        List<string> list_path_log = new List<string>();
        List<string> list_path_recipe = new List<string>();

        string status_timeline = "4";
        string last_datetime_end = "";
        bool f_start = false;
        bool f_update = false;

        int cnt_timer = 0;



        int B = 1;
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public main()
        {
            InitializeComponent();
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        private bool IsStartupItem()
        {
            // The path to the key where Windows looks for startup applications
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (rkApp.GetValue("FTP Program") == null)
                // The value doesn't exist, the application is not set to run at startup
                return false;
            else
                // The value exists, the application is set to run at startup
                return true;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void AutoRun()
        {
            // The path to the key where Windows looks for startup applications
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (!IsStartupItem())
            {
                // Add the value in the registry so that the application runs at startup
                rkApp.SetValue("FTP Program", Application.ExecutablePath.ToString());

                // Remove the value from the registry so that the application doesn't start
                //rkApp.DeleteValue("My app's name", false);
            }            
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Change_System_Datetime_Format()
        {
            RegistryKey rkey = Registry.CurrentUser.OpenSubKey(@"Control Panel\International", true);
            rkey.SetValue("sShortDate", "yyyy-MM-dd");
            rkey.SetValue("sShortTime", "HH:mm");
            rkey.SetValue("sTimeFormat", "HH:mm:ss");
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void prevent_screensaver(bool sw)
        {
            if (sw)
            {
                SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS);
            }
            else
            {
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private void Form1_Load(object sender, EventArgs e)

        {
            Change_System_Datetime_Format();
            prevent_screensaver(true);  // Disable Sleep Mode Windows10
            AutoRun();

            TextBox.CheckForIllegalCrossThreadCalls = false;    // Cross-Thread

            // Set color GridView
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.Blue;
            dataGridView1.EnableHeadersVisualStyles = false;

            Read_Config_Register();

            DateTime buildDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;
            //string buildTime = buildDate.Date.ToString("dd/MM/yyyy");
            string buildTime = buildDate.ToString("yyyy/MM/dd HH:mm:ss");
            this.Text = "[JC4.0 FTP App] Build: " + buildTime;
            Console.WriteLine(buildTime);


            if (coundown.counter == 0)
            {
                button1.PerformClick();
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static Thread Start(Action<string, string, string, string, string, string, string, string> action, string serial, string productname, string machine_type, string ip, string user, string password, string path_log, string path_recipe)
        {
            Thread thread = new Thread(() => { action(serial, productname, machine_type, ip, user, password, path_log, path_recipe); });
            thread.IsBackground = true; //<-- Set the thread to work in background
            thread.Start();
            return thread;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string GetUntilOrEmpty(string text, string stopAt = "_")
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(text))
                {
                    int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

                    if (charLocation > 0)
                    {
                        return text.Substring(0, charLocation);
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("GetUntilOrEmpty --------> " + ex.ToString());
            }
            return String.Empty;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string CheckLastModified_FullName(List<RemoteFileInfo> file)
        {
            string last_modified = "";
            try
            {
                var filesInOrder = file
                                .OrderByDescending(f => f.LastWriteTime)
                                .Select(f => f.FullName)
                                .ToList();
                last_modified = filesInOrder[0];
            }
            catch (Exception ex)
            {
                //MessageBox.Show("CheckLastModified_Fullnamey --------> " + ex.ToString());
            }

            return last_modified;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string CheckLastModified_LastWriteTime(List<RemoteFileInfo> file)
        {
            string last_modified = "";
            try
            {
                var filesInOrder = file
                                .OrderByDescending(f => f.LastWriteTime)
                                .Select(f => f.LastWriteTime)
                                .ToList();
                last_modified = filesInOrder[0].ToString();
            }
            catch (Exception ex)
            {
                //MessageBox.Show("CheckLastModified_lastWriteTimey --------> " + ex.ToString());
            }
            return last_modified;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public bool DownloadFile(Session session, string remotepath, string localpath)
        {
            try
            {
                TransferOptions transferOptions = new TransferOptions();
                transferOptions.TransferMode = TransferMode.Binary;
                TransferOperationResult transferResult;

                string rmp = remotepath.Replace(".csv", "*");
                transferResult = session.GetFiles(rmp, localpath, false, transferOptions);

                // Throw on any error
                transferResult.Check();

                // Print results
                foreach (TransferEventArgs transfer in transferResult.Transfers)
                {
                    Debug.WriteLine(string.Format("download '{0}' succeeded", transfer.FileName));
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                //MessageBox.Show("DownloadFile --------> " + ex.ToString());
                return false;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                //MessageBox.Show("DeleteFile --------> " + ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public bool ReadSettingDB()
        {
            try
            {

                string Query = "SELECT id, serial, product_name, ms_machinetype_id, ipaddress, ftp_username, ftp_password, path_log, path_recipe FROM tbl_setting WHERE ms_machinetype_id='1' OR ms_machinetype_id='2' OR ms_machinetype_id='3'";

                MySqlConnection Conn = new MySqlConnection(connStr);
                MySqlCommand cmd = new MySqlCommand(Query, Conn);

                Conn.Open();

                MySqlDataReader dr;
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {


                    list_id.Add(dr.GetString("id"));
                    list_serial.Add(dr.GetString("serial"));
                    list_productname.Add(dr.GetString("product_name"));
                    list_machinetype.Add(dr.GetString("ms_machinetype_id"));
                    list_ipaddress.Add(dr.GetString("ipaddress"));
                    list_ftp_username.Add(dr.GetString("ftp_username"));
                    list_ftp_password.Add(dr.GetString("ftp_password"));
                    list_path_log.Add(dr.GetString("path_log"));
                    list_path_recipe.Add(dr.GetString("path_recipe"));
                }
                dr.Close();
                return true;
            }
            catch (Exception ex)
            {
                //MessageBox.Show("ReadSettingDBy --------> " + ex.ToString());
                return false;
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public int CountRecordDB(string tableName, string filename, string serial)
        {
            int count = 0;

            string Query = "SELECT COUNT(*) FROM " + tableName + " WHERE filename='" + filename + "' AND serial='" + serial + "';";

            MySqlConnection Conn = new MySqlConnection(connStr);
            MySqlCommand cmd = new MySqlCommand(Query, Conn);

            try
            {                                
                Conn.Open();

                count = Convert.ToInt32(cmd.ExecuteScalar());
                Conn.Close();
                return count;
            }
            catch (Exception ex)
            {
                Thread.Sleep(500);
                Conn.Close();
                Thread.Sleep(500);
                Conn.Open();

                count = Convert.ToInt32(cmd.ExecuteScalar());
                Conn.Close();

                //MessageBox.Show(ex.ToString());
            }
            return count;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public int CountRecord_last_modified(string tablename, string filename, string last_modified, string serial)
        {
            int count = 0;
            try
            {

                string Query = "SELECT COUNT(*) FROM " + tablename + " WHERE filename='" + filename + "' AND last_modified='" + last_modified + "' AND serial='" + serial + "';";

                MySqlConnection Conn = new MySqlConnection(connStr);
                MySqlCommand cmd = new MySqlCommand(Query, Conn);

                Conn.Open();

                count = Convert.ToInt32(cmd.ExecuteScalar());
                Conn.Close();

            }
            catch (Exception ex)
            {
                //MessageBox.Show("CountRecord_Last_modifiedy --------> " + ex.ToString());
            }

            return count;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string Get_last_modified_DB(string tablename, string filename, string serial)
        {
            string last_modified = "";
            try
            {

                string Query = "SELECT last_modified FROM " + tablename + " WHERE filename='" + filename + "' AND serial='" + serial + "' ORDER BY id desc LIMIT 1;";

                MySqlConnection Conn = new MySqlConnection(connStr);
                MySqlCommand cmd = new MySqlCommand(Query, Conn);

                Conn.Open();

                last_modified = cmd.ExecuteScalar().ToString();
                Conn.Close();

            }
            catch (Exception ex)
            {
                //MessageBox.Show("Get_last_modified_DBy --------> " + ex.ToString());
            }

            return last_modified;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string Get_last_update_DB(string serial)
        {
            string last_update = "";
            try
            {

                string Query = "SELECT updated_time FROM tr_status WHERE serial='" + serial + "'";

                MySqlConnection Conn = new MySqlConnection(connStr);
                MySqlCommand cmd = new MySqlCommand(Query, Conn);

                Conn.Open();

                last_update = cmd.ExecuteScalar().ToString();
                Conn.Close();

            }
            catch (Exception ex)
            {
                //MessageBox.Show("Get_last_update_DB --------> " + ex.ToString());
            }

            return last_update;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string Get_last_datetime_start_DB(string tablename, string filename, string serial)
        {
            var result = string.Empty;

            try
            {

                string Query = "SELECT datetime_start FROM " + tablename + " WHERE filename='" + filename + "' AND serial='" + serial + "'  AND status='1' ORDER BY id desc LIMIT 1;";

                MySqlConnection Conn = new MySqlConnection(connStr);
                MySqlCommand cmd = new MySqlCommand(Query, Conn);

                Conn.Open();

                var datetime_start = cmd.ExecuteScalar();

                if (datetime_start != DBNull.Value) // Case where the DB value is null
                {
                    result = Convert.ToString(datetime_start);                                                                                                      
                }

                Conn.Close();

            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
            }

            return result;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string Get_last_datetime_end_DB(string tablename, string filename, string serial)
        {
            var result = string.Empty;
            try
            {

                string Query = "SELECT datetime_end FROM " + tablename + " WHERE filename='" + filename + "' AND serial='" + serial + "' ORDER BY id desc LIMIT 1;";

                MySqlConnection Conn = new MySqlConnection(connStr);
                MySqlCommand cmd = new MySqlCommand(Query, Conn);

                Conn.Open();

                var datetime_end = cmd.ExecuteScalar();
                if (datetime_end != DBNull.Value)
                {
                    result = Convert.ToString(datetime_end);
                }
                
                Conn.Close();
            }
            catch (Exception ex)
            {
                //datetime_end = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                //MessageBox.Show("Get_last_datetime_end_DBy --------> " + ex.ToString());
            }

            return result;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string Get_last_datetime_end_DB(string tablename, string filename, string serial, string status)
        {
            var result = string.Empty;
            try
            {

                string Query = "SELECT datetime_end FROM " + tablename + " WHERE filename='" + filename + "' AND serial='" + serial + "' AND status='" + status + "' ORDER BY id desc LIMIT 1;";

                MySqlConnection Conn = new MySqlConnection(connStr);
                MySqlCommand cmd = new MySqlCommand(Query, Conn);

                Conn.Open();

                var datetime_end = cmd.ExecuteScalar();
                if (datetime_end != DBNull.Value)
                {
                    result = Convert.ToString(datetime_end);
                }

                Conn.Close();
            }
            catch (Exception ex)
            {
                //datetime_end = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                //MessageBox.Show("Get_last_datetime_end_DBy --------> " + ex.ToString());
            }

            return result;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string Get_last_Warning_LocalTime_DB(string type, string tablename, string filename, string serial)
        {
            string last_warning = "";

            try
            {

                string Query = "SELECT local_time FROM " + tablename + " WHERE filename='" + filename + "' AND serial='" + serial + "' AND type= '" + type + "' ORDER BY id desc LIMIT 1;";

                MySqlConnection Conn = new MySqlConnection(connStr);
                MySqlCommand cmd = new MySqlCommand(Query, Conn);

                Conn.Open();



                var x = cmd.ExecuteScalar();

                if (x != null)
                {
                    last_warning = x.ToString();
                }

                Conn.Close();

            }
            catch (Exception ex)
            {
                //MessageBox.Show("Get_last_Warning_LocalTime_DBy --------> " + ex.ToString());
            }

            return last_warning;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string Get_Record_Today_DB(string serial)
        {
            string count = "";

            try
            {

                //string Query = "SELECT COUNT(*) FROM tbl_k2next_log WHERE datetime_file >= CURDATE() && datetime_file < (CURDATE() + INTERVAL 1 DAY) AND ipaddress='" + ip + "';";
                string Query = "SELECT COUNT(*) FROM tbl_k2next_log WHERE casting_start_datetime >= CURDATE() && casting_start_datetime < (CURDATE() + INTERVAL 1 DAY) AND serial='" + serial + "';";

                MySqlConnection Conn = new MySqlConnection(connStr);
                MySqlCommand cmd = new MySqlCommand(Query, Conn);

                Conn.Open();



                var x = cmd.ExecuteScalar();

                if (x != null)
                {
                    count = x.ToString();
                }

                Conn.Close();

            }
            catch (Exception ex)
            {
                //MessageBox.Show("Get_Record_Today_DBy --------> " + ex.ToString());
            }

            return count;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Process_WorkLogRecipe_RBF(string serial, string macine_type, string tablename, string ip, string fullname, string remote_path, string folder_download, Session session)
        {
            try
            {
                // 1.WorkLog Recipe
                string fn_WorkLogRecipe = fullname.Split('/').Last();   // ตัดเอาเฉพาะชื่อไฟล์เท่านั้น filename


                // 1.ค้นหาชื่อไฟล์นี้มีการ insert ใน database แล้วยัง
                int count = CountRecordDB(tablename, fn_WorkLogRecipe, serial);
                if (count > 0)   // ไฟล์ชื่อนี้มีการอ่านและบันทึกในฐานข้อมูลแล้ว
                {
                    //Debug.WriteLine("Old file WorkLogRecipe : " + fullname);
                }
                else
                {
                    string remoteFile = remote_path + fn_WorkLogRecipe;

                    // 2.Download files
                    bool res = DownloadFile(session, remoteFile, folder_download);
                    Thread.Sleep(1000);

                    if (res == true)
                    {
                        // 3.Read .csv
                        string fullpath = folder_download + fn_WorkLogRecipe;
                        res = Read_CSV_WorkLogRecipe_RBF(serial, macine_type, tablename, ip, fn_WorkLogRecipe, fullpath);
                        if (res == true)
                        {
                            // 4.ลบไฟล์ที่ Download มา
                            DeleteFile(fullpath);
                        }
                        else
                        {
                            Debug.WriteLine("Read .CSV Error , Insert Error");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Download Error");
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Process_WorkLogRecipe_RBFy --------> " + ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Process_Recipe_RBF(string serial, string machine_type, RemoteFileInfo fileInfo, string ip, string remote_path, string folder_download, Session session)
        {
            string filename_datemodified = fileInfo.Name + " " + fileInfo.LastWriteTime;

            try
            {
                // 1.ค้นหาชื่อไฟล์นี้มีการ insert ใน database แล้วยัง
                string tablename = "tbl_rbf_recipe";
                int count = CountRecord_last_modified(tablename, fileInfo.Name, fileInfo.LastWriteTime.ToString(), serial);
                if (count > 0)   // ไฟล์ชื่อนี้มีการอ่านและบันทึกในฐานข้อมูลแล้ว
                {
                    //Debug.WriteLine("Old file Recipe : " + filename_datemodified);
                }
                else
                {
                    string remoteFile = remote_path + fileInfo.Name;

                    // 1.Download files
                    bool res = DownloadFile(session, remoteFile, folder_download);
                    Thread.Sleep(1000);

                    if (res == true)
                    {
                        // 2.Delete ลบไฟล์เก่าที่ถูกเก็บไว้ในฐานข้อมูลออกก่อน Insert หากมี ชื่อไฟล์นั้น
                        DeleteRecord(tablename, fileInfo.Name, ip);

                        // 3.Read .csv
                        string fullpath = folder_download + fileInfo.Name;
                        res = Read_CSV_Recipe_RBF(serial, machine_type, tablename, ip, fileInfo.Name, fileInfo.LastWriteTime.ToString(), fullpath);

                        if (res == true)
                        {
                            // 5.ลบไฟล์ที่ Download มา
                            DeleteFile(fullpath);

                        }
                        else
                        {
                            Debug.WriteLine("Read .CSV Error , Insert Error");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Download Error");
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Process_Recipe_RBFy --------> " + ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Process_Recipe_K2NEXT(string serial, string machine_type, string tablename, RemoteFileInfo fileInfo, string ip, string remote_path, string folder_download, Session session)
        {
            string filename_datemodified = fileInfo.Name + " " + fileInfo.LastWriteTime;

            try
            {
                // 1.ค้นหาชื่อไฟล์นี้มีการ insert ใน database แล้วยัง
                int count = CountRecord_last_modified(tablename, fileInfo.Name, fileInfo.LastWriteTime.ToString(), serial);
                if (count > 0)   // ไฟล์ชื่อนี้มีการอ่านและบันทึกในฐานข้อมูลแล้ว
                {
                    //Debug.WriteLine("Old file Recipe : " + filename_datemodified);
                }
                else
                {
                    string remoteFile = remote_path + fileInfo.Name;

                    // 1.Download files
                    bool res = DownloadFile(session, remoteFile, folder_download);
                    Thread.Sleep(1000);

                    if (res == true)
                    {
                        // 2.Delete ลบไฟล์เก่าที่ถูกเก็บไว้ในฐานข้อมูลออกก่อน Insert หากมี ชื่อไฟล์นั้น
                        DeleteRecord(tablename, fileInfo.Name, ip);

                        // 3.Read .csv
                        string fullpath = folder_download + fileInfo.Name;
                        res = Read_CSV_Recipe_K2NEXT(serial, machine_type, tablename, ip, fileInfo.Name, fileInfo.LastWriteTime.ToString(), fullpath);

                        if (res == true)
                        {
                            // 5.ลบไฟล์ที่ Download มา
                            DeleteFile(fullpath);

                        }
                        else
                        {
                            Debug.WriteLine("Read .CSV Error , Insert Error");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Download Error");
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Process_Recipe_K2NEXTy --------> " + ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Process_Recipe_ALTIMA(string serial , string machine_type, string tablename, RemoteFileInfo fileInfo, string ip, string remote_path, string folder_download, Session session)
        {
            string filename_datemodified = fileInfo.Name + " " + fileInfo.LastWriteTime;

            // 1.ค้นหาชื่อไฟล์นี้มีการ insert ใน database แล้วยัง
            int count = CountRecord_last_modified(tablename, fileInfo.Name, fileInfo.LastWriteTime.ToString(), ip);
            if (count > 0)   // ไฟล์ชื่อนี้มีการอ่านและบันทึกในฐานข้อมูลแล้ว
            {
                //Debug.WriteLine("Old file Recipe : " + filename_datemodified);
            }
            else
            {
                string remoteFile = remote_path + fileInfo.Name;

                // 1.Download files
                bool res = DownloadFile(session, remoteFile, folder_download);
                Thread.Sleep(1000);

                if (res == true)
                {
                    // 2.Delete ลบไฟล์เก่าที่ถูกเก็บไว้ในฐานข้อมูลออกก่อน Insert หากมี ชื่อไฟล์นั้น
                    DeleteRecord(tablename, fileInfo.Name, ip);

                    // 3.Read .csv
                    string fullpath = folder_download + fileInfo.Name;
                    res = Read_CSV_Recipe_ALTIMA(serial, machine_type, tablename, ip, fileInfo.Name, fileInfo.LastWriteTime.ToString(), fullpath);

                    if (res == true)
                    {                       
                        // 5.ลบไฟล์ที่ Download มา
                        DeleteFile(fullpath);

                    }
                    else
                    {
                        Debug.WriteLine("Read .CSV Error , Insert Error");
                    }
                }
                else
                {
                    Debug.WriteLine("Download Error");
                }
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void ResetCounterInTextFile(string filename)
        {
            try
            {
                if (!File.Exists(filename)) // Create a file 
                {
                    // Save line number
                    File.WriteAllText(filename, "0");
                }

                // Save line number
                File.WriteAllText(filename, "0");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("ResetCounterInTextFiley --------> " + ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Download_WorkLog_RBF(string serial, string machine_type, string tablename, string ip, string filename, string last_modified, string remote_path, string folder_download, Session session, string filename_textfile)
        {
            try
            {
                // 2.Download files
                bool res = DownloadFile(session, remote_path, folder_download);
                Thread.Sleep(1000);

                if (res == true)
                {

                    string fullpath = folder_download + filename;

                    // 3.Counter
                    var lineCount = File.ReadLines(fullpath).Count(line => !string.IsNullOrWhiteSpace(line));
                    Insert_Update_Counter(serial, machine_type, ip, lineCount);


                    // 3.Read .csv                    
                    res = Read_CSV_WorkLog_RBF(serial, machine_type, tablename, ip, filename, last_modified, fullpath, filename_textfile);
                    if (res == true)
                    {

                        // 4.ลบไฟล์ที่ Download มา
                        DeleteFile(fullpath);
                    }
                    else
                    {
                        Debug.WriteLine("Read .CSV Error , Insert Error");
                    }
                }
                else
                {
                    Debug.WriteLine("Download Error");
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Download_WorkLog_RBF --------> " + ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Download_HistoryLog_RBF(string serial, string machine_type, string tablename, string ip, string filename, string last_modified, string remote_path, string folder_download, Session session, string filename_textfile)
        {
            try
            {
                // 2.Download files
                bool res = DownloadFile(session, remote_path, folder_download);
                Thread.Sleep(1000);

                if (res == true)
                {
                    // 3.Read .csv
                    string fullpath = folder_download + filename;
                    res = Read_CSV_HistoryLog_RBF(serial, machine_type, tablename, ip, filename, last_modified, fullpath, filename_textfile);
                    if (res == true)
                    {
                        // 4.ลบไฟล์ที่ Download มา
                        DeleteFile(fullpath);
                    }
                    else
                    {
                        Debug.WriteLine("Read .CSV Error , Insert Error");
                    }
                }
                else
                {
                    Debug.WriteLine("Download Error");
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Download_HistoryLog_RBFy --------> " + ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Download_HistoryLog_ALTIMA(string serial, string machine_type, string tablename, string ip, string filename, string last_modified, string remote_path, string folder_download, Session session, string filename_textfile)
        {
            try
            {
                // 2.Download files
                bool res = DownloadFile(session, remote_path, folder_download);
                Thread.Sleep(1000);

                if (res == true)
                {
                    // 3.Read .csv
                    string fullpath = folder_download + filename;
                    res = Read_CSV_HistoryLog_ALTIMA(serial, machine_type, tablename, ip, filename, last_modified, fullpath, filename_textfile);
                    if (res == true)
                    {
                        // 4.ลบไฟล์ที่ Download มา
                        DeleteFile(fullpath);
                    }
                    else
                    {
                        Debug.WriteLine("Read .CSV Error , Insert Error");
                    }
                }
                else
                {
                    Debug.WriteLine("Download Error");
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Download_HistoryLog_ALTIMAy --------> " + ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Download_MTTRLog_ALTIMA(string serial, string machine_type, string tablename, string ip, string filename, string last_modified, string remote_path, string folder_download, Session session, string filename_textfile)
        {
            try
            {
                // 2.Download files
                bool res = DownloadFile(session, remote_path, folder_download);
                Thread.Sleep(1000);

                if (res == true)
                {
                    // 3.Read .csv
                    string fullpath = folder_download + filename;
                    res = Read_CSV_MTTRLog_ALTIMA(serial, machine_type, tablename, ip, filename, last_modified, fullpath, filename_textfile);
                    if (res == true)
                    {
                        // 4.ลบไฟล์ที่ Download มา
                        DeleteFile(fullpath);
                    }
                    else
                    {
                        Debug.WriteLine("Read .CSV Error , Insert Error");
                    }
                }
                else
                {
                    Debug.WriteLine("Download Error");
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Download_MTTRLog_ALTIMA --------> " + ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Download_WaxshootLog_ALTIMA(string serial, string machine_type, string tablename, string ip, string filename, string last_modified, string remote_path, string folder_download, Session session, string filename_textfile)
        {
            try
            {
                // 2.Download files
                bool res = DownloadFile(session, remote_path, folder_download);
                //Thread.Sleep(1000);

                string fullpath = folder_download + filename;

                if (res == true)
                {
                    // 3.Counter
                    var lineCount = File.ReadLines(fullpath).Count(line => !string.IsNullOrWhiteSpace(line));
                    lineCount = lineCount - 1; // Remove Header
                    Insert_Update_Counter(serial, machine_type, ip, lineCount);

                    textBox12.Text += ip + " --> " + lineCount.ToString() + " Qty.   -->  Download file --> " + filename + " --> OK.\r\n";
                    textBox12.SelectionStart = textBox12.Text.Length;
                    textBox12.ScrollToCaret();


                    // 4.Read .csv                    
                    res = Read_CSV_WaxshootLog_ALTIMA(serial, machine_type, tablename, ip, filename, last_modified, fullpath, filename_textfile);

                    if (res == true)
                    {

                        // 5.ลบไฟล์ที่ Download มา
                        DeleteFile(fullpath);

                        
                    }
                    else
                    {

                    }
                }
                else
                {
                    Debug.WriteLine("Download Error");

                    Thread.Sleep(1000);

                    DownloadFile(session, remote_path, folder_download);        // re-Download

                    // 3.Counter
                    var lineCount = File.ReadLines(fullpath).Count(line => !string.IsNullOrWhiteSpace(line));
                    lineCount = lineCount - 1; // Remove Header
                    Insert_Update_Counter(serial, machine_type, ip, lineCount);

                    textBox12.Text += ip + " --> " + lineCount.ToString() + " Qty.   -->  Re-Download --> " + filename + "\r\n";
                    textBox12.SelectionStart = textBox12.Text.Length;
                    textBox12.ScrollToCaret();

                    // 4.Read .csv                    
                    res = Read_CSV_WaxshootLog_ALTIMA(serial, machine_type, tablename, ip, filename, last_modified, fullpath, filename_textfile);
                    if (res == true)
                    {

                        // 5.ลบไฟล์ที่ Download มา
                        DeleteFile(fullpath);

                        
                    }
                    else
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Download_WaxShootLog_ALTIMA --------> " + ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Process_WorkLog_RBF(string serial, string machine_type, string tablename, string ip, string fullname, string folder_lastlinenumber, string path, string last_modified, string folder_download, Session session)
        {
            try
            {
                string fn_WorkLog = fullname.Split('/').Last();   // ตัดเอาเฉพาะชื่อไฟล์เท่านั้น filename

                string filename_savelinenumber = folder_lastlinenumber + "WorkLog_" + ip + ".txt";
                string remoteFile = path + fn_WorkLog;

                double total = 0;



                // 1.Realtime Status                                                                            
                /*CheckTime_RealTimeStatus(ip, last_modified);

                string timenow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                textBox13.Text += ip + "--> " + fn_WorkLog + " --> RBF37 Last modified --> " + last_modified + "--> PC Time: " + timenow + "\r\n";
                textBox13.SelectionStart = textBox13.Text.Length;
                textBox13.ScrollToCaret();*/


                // 1.เข้าไปเช็คชื่อไฟล์นี้ มีในฐานข้อมูลแล้วยัง
                int count = CountRecordDB(tablename, fn_WorkLog, serial);
                if (count > 0)   // ไฟล์ชื่อนี้มีการอ่านและบันทึกในฐานข้อมูลแล้ว ให้ อ่าน .csv และ Insert ต่อ
                {
                    // ค้นหา last_modified ก่อนหน้า
                    string last_modified_DB = Get_last_modified_DB(tablename, fn_WorkLog, serial);
                    string last_update_DB = Get_last_update_DB(serial);

                    if (last_modified != last_modified_DB) // file WorkLog มีการ Update
                    {
                        // Machine Status "Running"
                        Insert_Update_Status_RUN_IDLE(ip, true, false, serial, machine_type);

                        // Reset Warning/Error
                        Insert_Update_Status_WARNING_ERROR(ip, false, false); // reset


                        textBox13.Text += DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " --> RBF37 --> " + ip + " --> File Updated --> " + "DB Last odified: " + last_modified_DB.Substring(11, 8) + "     FTP Last modified: " + last_modified.Substring(11, 8) + "                    <- - - - - - - - - - - - - - - - - START\r\n";
                        textBox13.SelectionStart = textBox13.Text.Length;
                        textBox13.ScrollToCaret();

                        // 1.Realtime Status
                        CheckTime_RealTimeStatus(serial, machine_type, ip, last_update_DB);


                        Download_WorkLog_RBF(serial ,machine_type ,tablename, ip, fn_WorkLog, last_modified, remoteFile, folder_download, session, filename_savelinenumber);
                    }
                    else// file Worklog ยังไม่มีการ Update
                    {
                        //string[] res = Get_Counter_UpdateTime("tr_status", fn_WorkLog, ip);

                        //textBox12.Text += ip + " --> " + "   DB: " + last_modified_DB.Substring(11, 8) + "  RBF37: " + last_modified.Substring(11, 8) + " --> Waiting... --> " + res[0].ToString() + " Qty.\r\n";
                        textBox13.Text += DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " --> RBF37 --> " + ip + " --> " + "DB Last modified: " + last_modified_DB.Substring(11, 8) + "  FTP Last modified: " + last_modified.Substring(11, 8) + " --> Waiting ...\r\n";
                        textBox13.SelectionStart = textBox13.Text.Length;
                        textBox13.ScrollToCaret();

                        // 1.Realtime Status                                                                            
                        CheckTime_RealTimeStatus(serial, machine_type, ip, last_update_DB);
                    }
                }
                else
                {
                    // มีไฟล์ใหม่เข้ามา

                    textBox13.Text += DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +  " --> RBF37 --> " + ip + " --> New file --> " + fn_WorkLog + " --> FTP Last modified: " + last_modified.Substring(11, 8) + "\r\n";
                    textBox13.SelectionStart = textBox13.Text.Length;
                    textBox13.ScrollToCaret();


                    // Machine Status "Running"
                    Insert_Update_Status_RUN_IDLE(ip, true, false, serial, machine_type);

                    ResetCounterInTextFile(filename_savelinenumber);

                    Download_WorkLog_RBF(serial, machine_type, tablename, ip, fn_WorkLog, last_modified, remoteFile, folder_download, session, filename_savelinenumber);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Process_WorkLog_RBF --------> "+ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Process_HistoryLog_RBF(string serial, string machine_type, string tablename, string ip, string fullname, string folder_lastlinenumber, string path, string last_modified, string folder_download, Session session)
        {
            try
            {
                string fn_HistoryLog = fullname.Split('/').Last();   // ตัดเอาเฉพาะชื่อไฟล์เท่านั้น filename

                string filename_savelinenumber = folder_lastlinenumber + "HistoryLog_" + ip + ".txt";
                string remoteFile = path + fn_HistoryLog;

                // 1.เข้าไปเช็คชื่อไฟล์นี้ มีในฐานข้อมูลแล้วยัง
                int count = CountRecordDB(tablename, fn_HistoryLog, serial);
                if (count > 0)   // ไฟล์ชื่อนี้มีการอ่านและบันทึกในฐานข้อมูลแล้ว ให้ อ่าน .csv และ Insert ต่อ
                {
                    // ค้นหา last_modified ก่อนหน้า
                    string last_modified_DB = Get_last_modified_DB(tablename, fn_HistoryLog, serial);

                    if (last_modified != last_modified_DB) // ถ้า file WorkLog มีการ Update
                    {
                        Download_HistoryLog_RBF(serial, machine_type, tablename, ip, fn_HistoryLog, last_modified, remoteFile, folder_download, session, filename_savelinenumber);
                    }
                    else
                    {
                        // file Historylog ยังไม่มีการ Update ไม่ต้องทำอะไร
                    }
                }
                else
                {
                    // มีไฟล์ใหม่เข้ามา

                    ResetCounterInTextFile(filename_savelinenumber);

                    Download_HistoryLog_RBF(serial, machine_type, tablename, ip, fn_HistoryLog, last_modified, remoteFile, folder_download, session, filename_savelinenumber);

                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Process_HistoryLog_RBFy --------> "+ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Process_HistoryLog_ALTIMA(string serial, string machine_type, string tablename, string ip, string fullname, string folder_lastlinenumber, string path, string last_modified, string folder_download, Session session)
        {
            try
            {
                string fn_HistoryLog = fullname.Split('/').Last();   // ตัดเอาเฉพาะชื่อไฟล์เท่านั้น filename

                string filename_savelinenumber = folder_lastlinenumber + "HistoryLog_" + ip + ".txt";
                string remoteFile = path + fn_HistoryLog;

                // 1.เข้าไปเช็คชื่อไฟล์นี้ มีในฐานข้อมูลแล้วยัง
                int count = CountRecordDB(tablename, fn_HistoryLog, serial);
                if (count > 0)   // ไฟล์ชื่อนี้มีการอ่านและบันทึกในฐานข้อมูลแล้ว ให้ อ่าน .csv และ Insert ต่อ
                {
                    // ค้นหา last_modified ก่อนหน้า
                    string last_modified_DB = Get_last_modified_DB(tablename, fn_HistoryLog, serial);

                    if (last_modified != last_modified_DB) // ถ้า file WorkLog มีการ Update
                    {
                        // Machine Status "Running"

                        Download_HistoryLog_ALTIMA(serial, machine_type, tablename, ip, fn_HistoryLog, last_modified, remoteFile, folder_download, session, filename_savelinenumber);
                    }
                    else
                    {
                        // file Historylog ยังไม่มีการ Update ไม่ต้องทำอะไร
                    }
                }
                else
                {
                    // มีไฟล์ใหม่เข้ามา

                    ResetCounterInTextFile(filename_savelinenumber);

                    Download_HistoryLog_ALTIMA(serial, machine_type, tablename, ip, fn_HistoryLog, last_modified, remoteFile, folder_download, session, filename_savelinenumber);

                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Process_HistoryLog_ALTIMAy --------> "+ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Process_MTTRLog_ALTIMA(string serial, string machine_type, string tablename, string ip, string fullname, string folder_lastlinenumber, string path, string last_modified, string folder_download, Session session)
        {
            try
            {
                string fn_MTTRLog = fullname.Split('/').Last();   // ตัดเอาเฉพาะชื่อไฟล์เท่านั้น filename

                string filename_savelinenumber = folder_lastlinenumber + "MTTRLog_" + ip + ".txt";
                string remoteFile = path + fn_MTTRLog;

                // 1.เข้าไปเช็คชื่อไฟล์นี้ มีในฐานข้อมูลแล้วยัง
                int count = CountRecordDB(tablename, fn_MTTRLog, serial);

                if (count > 0)   // ไฟล์ชื่อนี้มีการอ่านและบันทึกในฐานข้อมูลแล้ว ให้ อ่าน .csv และ Insert ต่อ
                {
                    // ค้นหา last_modified ก่อนหน้า
                    string last_modified_DB = Get_last_modified_DB(tablename, fn_MTTRLog, serial);

                    if (last_modified != last_modified_DB) // ถ้า file MTTRLog มีการ Update
                    {
                        // Machine Status "Running"

                        Download_MTTRLog_ALTIMA(serial, machine_type, tablename, ip, fn_MTTRLog, last_modified, remoteFile, folder_download, session, filename_savelinenumber);
                    }
                    else
                    {
                        // file Worklog ยังไม่มีการ Update ไม่ต้องทำอะไร
                    }
                }
                else
                {
                    // มีไฟล์ใหม่เข้ามา

                    ResetCounterInTextFile(filename_savelinenumber);                    

                    Download_MTTRLog_ALTIMA(serial, machine_type, tablename, ip, fn_MTTRLog, last_modified, remoteFile, folder_download, session, filename_savelinenumber);

                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Process_MTTRLog_ALTIMAy --------> "+ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public double GetTimeDifferent(string last_datetime)
        {
            double totalminutes=0;
            int total = 0;
            try
            {
                if (last_datetime != "")
                {
                    DateTime s = Convert.ToDateTime(last_datetime, CultureInfo.InvariantCulture);
                    last_datetime = s.ToString("yyyy-MM-dd HH:mm:ss");

                    string[] datetime_end_before = last_datetime.Split(' ');
                    string yearmmdd_before = datetime_end_before[0];
                    string hhmm_before = datetime_end_before[1];

                    string[] date_before = yearmmdd_before.Split('-');
                    int year_before = int.Parse(date_before[0]);
                    int month_before = int.Parse(date_before[1]);
                    int day_before = int.Parse(date_before[2]);

                    string[] time_before = hhmm_before.Split(':');
                    int hh_before = int.Parse(time_before[0]);
                    int min_before = int.Parse(time_before[1]);
                    int sec_before = int.Parse(time_before[2]);

                    // ------------------------------------------------

                    // 2.เวลาปัจจุบัน
                    string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string[] datetime_now = dt.Split(' ');
                    string yearmmdd_current = datetime_now[0];
                    string hhmm_current = datetime_now[1];

                    string[] date_current = yearmmdd_current.Split('-');
                    int year_current = int.Parse(date_current[0]);
                    int month_current = int.Parse(date_current[1]);
                    int day_current = int.Parse(date_current[2]);

                    string[] time_current = hhmm_current.Split(':');
                    int hh_current = int.Parse(time_current[0]);
                    int min_current = int.Parse(time_current[1]);
                    int sec_current = int.Parse(time_current[2]);


                    string timebefore = year_before.ToString() + " " + month_before.ToString() + " " + day_before.ToString() + " " + hh_before.ToString() + " " + min_before.ToString() + " " + sec_before.ToString();
                    string timecurrent = year_current.ToString() + " " + month_current.ToString() + " " + day_current.ToString() + " " + hh_current.ToString() + " " + min_current.ToString() + " " + sec_current.ToString();

                    Debug.WriteLine(timebefore);
                    Debug.WriteLine(timecurrent);

                    // เทียบเวลาที่อ่านจาก DB ล่าสุด กับเวลาปัจจุบัน เกินกี่นาที
                    DateTime date1 = new DateTime(year_before, month_before, day_before, hh_before, min_before, sec_before);
                    DateTime date2 = new DateTime(year_current, month_current, day_current, hh_current, min_current, sec_current);
                    
                    //string date1_str = date1.ToString("yyyy-MM-dd HH:mm:ss");
                    //date1 = Convert.ToDateTime(date1_str, CultureInfo.InvariantCulture);

                    //string date2_str = date2.ToString("yyyy-MM-dd HH:mm:ss");
                    //date2 = Convert.ToDateTime(date2_str, CultureInfo.InvariantCulture);


                    totalminutes = (date2 - date1).TotalMinutes;
                    total = Convert.ToInt32(totalminutes);

                    Debug.WriteLine("Total : " + total + " Minutes.");
                   //label18.Text = "LastModified: " + timebefore + "  Timenow: " + timecurrent + " Total diff : " + total);

                }
                else
                {
                    Debug.WriteLine("Last datetime = null.");
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("GetTimeDifferent --------> " + ex.ToString());
            }
            return totalminutes;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public double Get_CycleTime(string casting_start, string last_modfied)
        {
            double totalsec = 0;
            try
            {
                string[] datetime_before = casting_start.Split(' ');
                string yearmmdd_before = datetime_before[0];
                string hhmm_before = datetime_before[1];

                string[] date_before = yearmmdd_before.Split('-');
                int year_before = int.Parse(date_before[0]);
                int month_before = int.Parse(date_before[1]);
                int day_before = int.Parse(date_before[2]);

                string[] time_before = hhmm_before.Split(':');
                int hh_before = int.Parse(time_before[0]);
                int min_before = int.Parse(time_before[1]);
                int sec_before = int.Parse(time_before[2]);

                // ------------------------------------------------

                // 2.last modified
                string[] datetime_now = last_modfied.Split(' ');
                string yearmmdd_current = datetime_now[0];
                string hhmm_current = datetime_now[1];

                string[] date_current = yearmmdd_current.Split('-');
                int year_current = int.Parse(date_current[0]);
                int month_current = int.Parse(date_current[1]);
                int day_current = int.Parse(date_current[2]);

                string[] time_current = hhmm_current.Split(':');
                int hh_current = int.Parse(time_current[0]);
                int min_current = int.Parse(time_current[1]);
                int sec_current = int.Parse(time_current[2]);


                string timebefore = year_before.ToString() + " " + month_before.ToString() + " " + day_before.ToString() + " " + hh_before.ToString() + " " + min_before.ToString() + " " + sec_before.ToString();
                string timecurrent = year_current.ToString() + " " + month_current.ToString() + " " + day_current.ToString() + " " + hh_current.ToString() + " " + min_current.ToString() + " " + sec_current.ToString();

                Debug.WriteLine(timebefore);
                Debug.WriteLine(timecurrent);

                // เทียบเวลาที่อ่านจาก DB ล่าสุด กับเวลาปัจจุบัน เกินกี่นาที
                DateTime date1 = new DateTime(year_before, month_before, day_before, hh_before, min_before, sec_before);
                DateTime date2 = new DateTime(year_current, month_current, day_current, hh_current, min_current, sec_current);
                totalsec = (date2 - date1).TotalSeconds;

               

                Debug.WriteLine("Total Sec : " + totalsec + " sec.");

            }
            catch (Exception ex)
            {
                //MessageBox.Show("Get_CycleTimey --------> " + ex.ToString());
            }
            return totalsec;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Process_WaxshootLog_ALTIMA(string serial, string machine_type, string tablename, string ip, string fullname,  string folder_lastlinenumber, string path, string last_modified, string folder_download, Session session)
        {
            try
            {
                string fn_WaxshootLog = fullname.Split('/').Last();   // ตัดเอาเฉพาะชื่อไฟล์เท่านั้น filename

                string filename_savelinenumber = folder_lastlinenumber + "WaxshootLog_" + ip + ".txt";
                string remoteFile = path + fn_WaxshootLog;

                // 1.เข้าไปเช็คชื่อไฟล์นี้ มีในฐานข้อมูลแล้วยัง
                int count = CountRecordDB(tablename, fn_WaxshootLog, serial);
                if (count > 0)   // ไฟล์ชื่อนี้มีการอ่านและบันทึกในฐานข้อมูลแล้ว ให้ อ่าน .csv และ Insert ต่อ
                {
                    // ค้นหา last_modified ก่อนหน้า
                    string last_modified_DB = Get_last_modified_DB(tablename, fn_WaxshootLog, serial);                    
                    string last_update_DB = Get_last_update_DB(serial);

                    if (last_modified != last_modified_DB) // ถ้า file WaxshootLog มีการ Update
                    {

                        Insert_Update_Status_WARNING_ERROR(ip, false, false); // reset

                        textBox12.Text += ip + " --> File Updated --> " + "DB: " + last_modified_DB.Substring(11, 8) + "     ALTIMA: " + last_modified.Substring(11, 8) + "                    <- - - - - - - - - - - - - - - - - START\r\n";
                        textBox12.SelectionStart = textBox12.Text.Length;
                        textBox12.ScrollToCaret();

                        

                        Download_WaxshootLog_ALTIMA(serial, machine_type, tablename, ip, fn_WaxshootLog, last_modified, remoteFile, folder_download, session, filename_savelinenumber);
                        last_update_DB = Get_last_update_DB(serial);

                        // 1.Realtime Status
                        CheckTime_RealTimeStatus(serial, machine_type, ip, last_update_DB);


                        // 2.Timeline Status
                        CheckTime_TimelineStatus(serial, machine_type, ip, fn_WaxshootLog, last_update_DB, true);

                        Show_Status_GridView();

                    }
                    else // file Waxshoot_log ยังไม่มีการ Update 
                    {
                        string[] res = Get_Counter_UpdateTime("tr_status", fn_WaxshootLog, ip);

                        textBox12.Text += ip + " --> " + "   DB: " + last_modified_DB.Substring(11, 8) + "  ALTIMA: " + last_modified.Substring(11, 8) + " --> Waiting... --> " + res[0].ToString() + " Qty.\r\n";
                        textBox12.SelectionStart = textBox12.Text.Length;
                        textBox12.ScrollToCaret();

                        // 1.Realtime Status                                                                            
                        CheckTime_RealTimeStatus(serial, machine_type, ip, last_update_DB);

                        // Timeline Status
                        CheckTime_TimelineStatus(serial, machine_type, ip, fn_WaxshootLog, last_update_DB, false);

                    }
                }
                else
                {
                    // มีไฟล์ใหม่เข้ามา New file

                    f_start = true;

                    f_update = true; // อาจต้องลบ ทดสอบ

                    // 1.Machine Status "Running"          
                    // Realtime Status : Machine Status "Running"
                    Insert_Update_Status_RUN_IDLE(ip, true, false, serial, machine_type);


                    // 2.Timeline Status: Green
                    string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    /*double total = GetTimeDifferent(last_modified);

                    status_timeline = "1"; // Green
                    string diff_time = total.ToString("0.#  min ago");

                    if ((total >= 0) && (total <= 2)) // 1-2 Minutes --> ignore old file
                    {

                        textBox9.Text += ip + " --> New file --> " + fn_WaxshootLog + "\r\n";
                        textBox9.SelectionStart = textBox9.Text.Length;
                        textBox9.ScrollToCaret();


                        textBox11.Text += ip + " --> New file --> " + fn_WaxshootLog + "\r\n";
                        textBox11.SelectionStart = textBox11.Text.Length;
                        textBox11.ScrollToCaret();

                        Insert_Timeline_ALTIMA(ip, fn_WaxshootLog, status_timeline, dt, dt, last_modified, dt, diff_time);
                    }
                    else
                    {
                        textBox9.Text += ip + " --> Old file --> " + fn_WaxshootLog + "\r\n";
                        textBox9.SelectionStart = textBox9.Text.Length;
                        textBox9.ScrollToCaret();


                        textBox11.Text += ip + " --> Old file --> " + fn_WaxshootLog + "\r\n";
                        textBox11.SelectionStart = textBox11.Text.Length;
                        textBox11.ScrollToCaret();
                    }*/




                    textBox9.Text += ip + " --> New file --> " + fn_WaxshootLog + "\r\n";
                    textBox9.SelectionStart = textBox9.Text.Length;
                    textBox9.ScrollToCaret();


                    textBox11.Text += ip + " --> New file --> " + fn_WaxshootLog + "\r\n";
                    textBox11.SelectionStart = textBox11.Text.Length;
                    textBox11.ScrollToCaret();

                    Insert_Timeline_ALTIMA(serial, machine_type, ip, fn_WaxshootLog, status_timeline, dt, dt, last_modified, dt, "0 min ago");




                    // ค้นหา last datetime_end ล่าสุด
                    last_datetime_end = Get_last_datetime_end_DB("tr_altima_status", fn_WaxshootLog, ip);

                    //----------------------------------------------

                    ResetCounterInTextFile(filename_savelinenumber);

                    Download_WaxshootLog_ALTIMA(serial, machine_type, tablename, ip, fn_WaxshootLog, last_modified, remoteFile, folder_download, session, filename_savelinenumber);

                    //Insert_Update_Status_WARNING_ERROR(ip, false, false); // reset
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Process_WaxShootLog_ALTIMA --------> " + ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void CheckTime_RealTimeStatus(string serial, string machine_type, string ip, string last_modified)
        {
            // 1.เทียบเวลา เกินจาก ไฟล์ที่ถูกแก้ไขล่าสุด ไปกี่นาที
            double total = GetTimeDifferent(last_modified);

            //label20.Text = "last_update_DB : " + last_modified  + "   time diff: " + total.ToString();

            if (total > 3)    // 2mins
            {
                // file Worklog ยังไม่มีการ Update ไม่ต้องทำอะไร
                // Machine Status "IDLE"
                Insert_Update_Status_RUN_IDLE(ip, false, true, serial, machine_type);
                textBox9.Text += ip + " --> IDLE --> " + total.ToString("0.0") + " m ago\r\n";
                textBox9.SelectionStart = textBox9.Text.Length;
                textBox9.ScrollToCaret();
            }
            else if (total < 0)    // ติดลบ
            {
                // file Worklog ยังไม่มีการ Update ไม่ต้องทำอะไร
                // Machine Status "IDLE"
                Insert_Update_Status_RUN_IDLE(ip, false, true, serial, machine_type);
                textBox9.Text += ip + " --> IDLE --> " + total.ToString("0.0") + " m ago\r\n";
                textBox9.SelectionStart = textBox9.Text.Length;
                textBox9.ScrollToCaret();
            }
            else
            {
                // Machine Status "Running"
                Insert_Update_Status_RUN_IDLE(ip, true, false, serial, machine_type);
                textBox9.Text += ip + " --> RUN  --> " + total.ToString("0.0") + " m ago\r\n";
                textBox9.SelectionStart = textBox9.Text.Length;
                textBox9.ScrollToCaret();
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void CheckTime_TimelineStatus(string serial, string machine_type, string ip, string filename, string last_modified, bool updated)
        {
            try
            {
                // 2.เวลาที่เก็บเข้า database ก่อนหน้า และอ่านขึ้นมา
                string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                int GREEN0 = 0;
                int GREEN1 = 3;

                int YELLOW0 = 3;
                int YELLOW1 = 15;

                int RED0 = 15;
                int RED1 = 30;

                //string last_datetime_start = Get_last_datetime_start_DB("tr_altima_status", filename, ip);      // เวลาที่เริ่มการบันทึก Timeline Status=1

                double total = GetTimeDifferent(last_modified);

                /*if (last_datetime_start != "")
                {*/
                    if ((total >= GREEN0) && (total < GREEN1)) // 1-2 Minutes Green
                    {

                    status_timeline = Get_last_status_DB("tr_altima_status", filename, serial);

                    /*if (status_timeline == "1") // Green
                    {
                        status_timeline = "1";  
                        Update_Timeline_ALTIMA(ip, filename, "1", dt, last_modified, dt, total.ToString());       
                        textBox11.Text += ip + " --> Green --> " + total.ToString("0.0") + " m ago\r\n";
                    }
                    else if (status_timeline == "2") // Yellow
                    {
                        string last_datetime_end = Get_last_datetime_end_DB("tr_altima_status", filename, ip, "2");

                        status_timeline = "1";  
                        Insert_Timeline_ALTIMA(ip, filename, status_timeline, last_datetime_end, dt, last_modified, dt, total.ToString());
                        textBox11.Text += ip + " --> Green --> " + total.ToString("0.0") + " m ago  <----- Insert\r\n";
                    }
                    else if (status_timeline == "3") // Red
                    {
                        status_timeline = "1";
                        Insert_Timeline_ALTIMA(ip, filename, status_timeline, dt, dt, last_modified, dt, total.ToString());
                        textBox11.Text += ip + " --> Green --> " + total.ToString("0.0") + " m ago  <----- Insert\r\n";
                    }*/

                        string last_datetime_start = Get_last_datetime_start_DB("tr_altima_status", filename, serial);      // เวลาที่เริ่มการบันทึก Timeline Status=1

                        if(status_timeline != "1")
                        {
                            status_timeline = "1";
                            Insert_Timeline_ALTIMA(serial, machine_type, ip, filename, status_timeline, dt, dt, last_modified, dt, total.ToString("0.# min ago"));
                            textBox11.Text += ip + " --> Green --> " + total.ToString("0.0") + " m ago  <----- Insert\r\n";
                        }
                        else
                        {
                            Update_Timeline_ALTIMA(serial, machine_type, ip, filename, "1", dt, last_modified, dt, total.ToString("0.# min ago"));
                            textBox11.Text += ip + " --> Green --> " + total.ToString("0.0") + " m ago\r\n";
                        }
                        

                    }
                    else if ((total >= YELLOW0) && (total < YELLOW1))  // 3-15 Yellow
                    {
                        status_timeline = Get_last_status_DB("tr_altima_status", filename, serial);

                        if (status_timeline == "1")// Insert
                        {
                            string last_datetime_end = Get_last_datetime_end_DB("tr_altima_status", filename, serial, "1");

                            status_timeline = "2";  // Yellow

                            Insert_Timeline_ALTIMA(serial, machine_type, ip, filename, status_timeline, last_datetime_end, dt, last_modified, dt, total.ToString("0.# min ago"));

                            textBox11.Text += ip + " --> Yellow --> " + total.ToString("0.0") + " m ago\r\n";
                        }
                        else // Update
                        {
                            string last_datetime_end = Get_last_datetime_end_DB("tr_altima_status", filename, serial, "1");

                            status_timeline = "2";
                            Update_Timeline_ALTIMA(serial, machine_type, ip, filename, status_timeline, dt, last_modified, dt, total.ToString("0.# min ago"));        // Yellow

                            textBox11.Text += ip + " --> Yellow --> " + total.ToString("0.0") + " m ago\r\n";
                        }
                    }
                    else if ((total >= RED0) && (total < RED1))  // 15-30 Red
                    {
                        status_timeline = Get_last_status_DB("tr_altima_status", filename, serial);

                        if (status_timeline == "2") // Insert
                        {
                            string last_datetime_end = Get_last_datetime_end_DB("tr_altima_status", filename, serial, "2");
                            // string last_time_end = last_datetime_end.Substring(10, 9); error

                            status_timeline = "3";  // Red

                            Insert_Timeline_ALTIMA(serial, machine_type, ip, filename, status_timeline, last_datetime_end, dt, last_modified, dt, total.ToString("0.# min ago"));

                            textBox11.Text += ip + " --> Red --> " + total.ToString("0.0") + " m ago\r\n";
                        }
                        else // Update
                        {
                            status_timeline = "3";

                            string last_datetime_end = Get_last_datetime_end_DB("tr_altima_status", filename, serial, "2");

                            Update_Timeline_ALTIMA(serial, machine_type, ip, filename, status_timeline, dt, last_modified, dt, total.ToString("0.# min ago"));        // Red

                            textBox11.Text += ip + " --> Red --> " + total.ToString("0.0") + " m ago\r\n";
                        }
                    }
                    else
                    {
                        // OFF-LINE  (White)
                        /*if (updated)
                        {
                            status_timeline = Get_last_status_DB("tr_altima_status", filename, ip);


                            if (status_timeline == "1") // Green
                            {
                                status_timeline = "1";
                                Update_Timeline_ALTIMA(ip, filename, "1", dt, last_modified, dt, total.ToString());
                                textBox11.Text += ip + " --> Green --> " + total.ToString("0.0") + " m ago\r\n";
                            }
                            else if (status_timeline == "2")
                            {
                                string last_datetime_end = Get_last_datetime_end_DB("tr_altima_status", filename, ip, "2");

                                status_timeline = "1";
                                Insert_Timeline_ALTIMA(ip, filename, status_timeline, dt, dt, last_modified, dt, total.ToString());
                                textBox11.Text += ip + " --> Green --> " + total.ToString("0.0") + " m ago  <----- Insert\r\n";
                            }
                            else if (status_timeline == "3")
                            {
                                status_timeline = "1";
                                Insert_Timeline_ALTIMA(ip, filename, status_timeline, dt, dt, last_modified, dt, total.ToString());
                                textBox11.Text += ip + " --> Green --> " + total.ToString("0.0") + " m ago  <----- Insert\r\n";
                            }
                        }*/
                    }

                    textBox11.SelectionStart = textBox11.Text.Length;
                    textBox11.ScrollToCaret();
                /*}
                else
                {
                    // null
                    textBox11.Text += ip + " null\r\n";
                    textBox11.SelectionStart = textBox11.Text.Length;
                    textBox11.ScrollToCaret();
                }*/
            }
            catch(Exception ex)
            {
                //MessageBox.Show("CheckTime_TimeLineStatusy --------> " + "IP: " + ip + "  Filename: " + filename + "  Last Modified: " + last_modified + "  Update: " + updated.ToString() + "  " + ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void RunFTP(string serial, string product_name, string machine_type, string ip, string user, string password, string remotePath_log, string remotePath_recipe)
        {


            if(machine_type == "1")
            {
                machine_type = "K2NEXT";
                remotePath_log = "/";
                remotePath_recipe = "/";
            }
            else if(machine_type == "2")
            {
                machine_type = "ALTIMA";
                remotePath_log = "/log/";
                remotePath_recipe = "/recipe/";
            }
            else if(machine_type == "3")
            {
                machine_type = "RBF";
                remotePath_log = "/log/";
                remotePath_recipe = "/recipe/";
            }

            //string currentDir = Directory.GetCurrentDirectory(); // ติด permission ต้องไปวางใน user path
            string user_path = Application.UserAppDataPath; // C:\Users\Narue\AppData\Roaming\Yasui\App JC4.0\1.0.0.0
            //Debug.WriteLine(user_path);
            

            //user_path = @"C:\Users\Dell13\Documents\Test";

            string folderSave_lastlinenumber = user_path + "\\" + ip + "\\Last Line Number\\";
            string folder_FTP_Download = user_path + "\\" + ip + "\\FTP Download\\";
            string folderSave_last_modified = user_path + "\\" + ip + "\\Last Modified\\";

            try
            {
                // Create New Folder
                if (!Directory.Exists(folderSave_lastlinenumber))
                {
                    Directory.CreateDirectory(folderSave_lastlinenumber);
                }

                // Create New Folder
                if (!Directory.Exists(folder_FTP_Download))
                {
                    Directory.CreateDirectory(folder_FTP_Download);
                }

                // Create New Folder
                if (!Directory.Exists(folderSave_last_modified))
                {
                    Directory.CreateDirectory(folderSave_last_modified);
                }
            }
            catch(Exception ex)
            {
                //MessageBox.Show(ex.ToString());
            }

            // Reset
            // Machine Status "RUN" , "IDLE"
            Insert_Update_Status_RUN_IDLE(ip, false, false, serial, machine_type);     // reset                                                                 
            //Insert_Update_Status_WARNING_ERROR(ip, false, false); // reset

            while (true)
            {
                try
                {
                    /*string timenow_start = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    string[] x = timenow_start.Split(' ');
                    string yearmmdd_before = x[0];
                    string hhmm_before = x[1];

                    string[] date_before = yearmmdd_before.Split('-');
                    int year_before = int.Parse(date_before[0]);
                    int month_before = int.Parse(date_before[1]);
                    int day_before = int.Parse(date_before[2]);

                    string[] time_before = hhmm_before.Split(':');
                    int hh_before = int.Parse(time_before[0]);
                    int min_before = int.Parse(time_before[1]);
                    int sec_before = int.Parse(time_before[2]);*/



                    if ((user == "") && (password == ""))
                    {
                        user = " ";
                        password = "";
                    }

                    // Setup session options
                    SessionOptions sessionOptions = new SessionOptions
                    {
                        Protocol = Protocol.Ftp,
                        HostName = ip,

                        UserName = user,
                        Password = password,

                        //SshHostKeyFingerprint = "ssh-rsa 2048 xxxxxxxxxxx...", 
                    };


                    using (Session session = new Session())
                    {


                        // Connect                        
                        session.Open(sessionOptions);

                        //label13.Text = "FTP";



                        // Update ONLINE
                        Insert_Update_Status_ONLINE(ip, true, false, serial, machine_type);        // Online

                        // Machine Status "IDLE"
                        //Insert_Update_Status_RUN_IDLE(ip, false, true);     // IDLE

                        RemoteDirectoryInfo directoryInfo_log = session.ListDirectory(remotePath_log);
                        RemoteDirectoryInfo directoryInfo_recipe = session.ListDirectory(remotePath_recipe);


                        if (machine_type == "RBF")
                        {
                            List<RemoteFileInfo> fileInfo_WorkLog = new List<RemoteFileInfo>();
                            List<RemoteFileInfo> fileInfo_WorkLogRecipe = new List<RemoteFileInfo>();
                            List<RemoteFileInfo> fileInfo_HistoryLog = new List<RemoteFileInfo>();


                            // 1.Folder log
                            foreach (RemoteFileInfo fileInfo in directoryInfo_log.Files)
                            {

                                if (fileInfo.Name != "..")
                                {
                                    string separate_file_name = GetUntilOrEmpty(fileInfo.Name); // ตัดชื่อไฟล์ก่อนถึง '_' ก็คือ แยกชื่อไฟล์ที่มีชื่อข้างหน้า WorkLog และ WorkLogRecipe

                                    if (separate_file_name == "WorkLog")
                                    {
                                        // 1.WorkLog_recipe_20220930111927.csv
                                        if (fileInfo.ToString().Contains("recipe")) // ค้นหาไฟล์ที่มีคำว่า "recepi"
                                        {
                                            fileInfo_WorkLogRecipe.Add(fileInfo);
                                        }
                                        else// 2.WorkLog_202209301119.csv
                                        {
                                            fileInfo_WorkLog.Add(fileInfo);
                                        }
                                    }
                                    else if (separate_file_name == "HistoryLog")
                                    {
                                        // 3.HistoryLog_2023.csv
                                        fileInfo_HistoryLog.Add(fileInfo);
                                    }
                                }
                            }

                            //1
                            string WorkLogRecipe_FullName = "";
                            string WorkLogRecipe_LastModified = "";

                            if (fileInfo_WorkLogRecipe.Count > 0)
                            {
                                WorkLogRecipe_FullName = CheckLastModified_FullName(fileInfo_WorkLogRecipe);                 // file ที่ถูกแก้ไขล่าสุด
                                WorkLogRecipe_LastModified = CheckLastModified_LastWriteTime(fileInfo_WorkLogRecipe);

                                string tablename = "tbl_rbf_worklog_recipe";
                                Process_WorkLogRecipe_RBF(serial, machine_type, tablename, ip, WorkLogRecipe_FullName, remotePath_log, folder_FTP_Download, session);
                            }

                            //2
                            string WorkLog_FullName = "";
                            string WorkLog_LastModified = "";

                            if (fileInfo_WorkLog.Count > 0)
                            {
                                WorkLog_FullName = CheckLastModified_FullName(fileInfo_WorkLog);                             // file ที่ถูกแก้ไขล่าสุด
                                WorkLog_LastModified = CheckLastModified_LastWriteTime(fileInfo_WorkLog);

                                string tablename = "tbl_rbf_worklog";
                                Process_WorkLog_RBF(serial, machine_type, tablename, ip, WorkLog_FullName, folderSave_lastlinenumber, remotePath_log, WorkLog_LastModified, folder_FTP_Download, session);
                            }

                            //3
                            string HistoryLog_FullName = "";
                            string HistoryLog_LastModified = "";

                            if (fileInfo_HistoryLog.Count > 0)
                            {
                                HistoryLog_FullName = CheckLastModified_FullName(fileInfo_HistoryLog);                       // file ที่ถูกแก้ไขล่าสุด
                                HistoryLog_LastModified = CheckLastModified_LastWriteTime(fileInfo_HistoryLog);

                                string tablename = "tbl_rbf_history";
                                Process_HistoryLog_RBF(serial, machine_type, tablename, ip, HistoryLog_FullName, folderSave_lastlinenumber, remotePath_log, HistoryLog_LastModified, folder_FTP_Download, session);

                            }

                            //Debug.WriteLine("\r\nLast modified RBF37 WorkLog Recipe: ", WorkLogRecipe_FullName + "  ,  " + WorkLogRecipe_LastModified);
                            //Debug.WriteLine("\r\nLast modified RBF37 Worklog: ", WorkLog_FullName + "  ,  " + WorkLog_LastModified);
                            //Debug.WriteLine("\r\nLast modified RBF37 HistoryLog: ", HistoryLog_FullName + "  ,  " + HistoryLog_LastModified);

                            // ------------------------------------------
                            if (directoryInfo_recipe.Files.Count > 0)
                            {
                                // 4.Folder recipe
                                foreach (RemoteFileInfo fileInfo in directoryInfo_recipe.Files)
                                {
                                    if (fileInfo.Name != "..")
                                    {
                                        Process_Recipe_RBF(serial, machine_type, fileInfo, ip, remotePath_recipe, folder_FTP_Download, session);

                                    }
                                }
                            }
                        }
                        else if (machine_type == "K2NEXT")
                        {
                            // ------------------------------------------
                            string tablename = "";

                            // 1. Folder recipe
                            if (directoryInfo_recipe.Files.Count > 0)
                            {
                                List<RemoteFileInfo> fileInfo_CastRecipe = new List<RemoteFileInfo>();

                                // 1.Folder recipe
                                foreach (RemoteFileInfo fileInfo in directoryInfo_recipe.Files)
                                {
                                    if (fileInfo.Name != "..")
                                    {
                                        if (fileInfo.Name.Contains("recipe")) // ค้นหาไฟล์ recipe มีกี่ไฟล์
                                        {
                                            fileInfo_CastRecipe.Add(fileInfo);

                                            tablename = "tbl_k2next_recipe";
                                            Process_Recipe_K2NEXT(serial, machine_type, tablename, fileInfo, ip, remotePath_recipe, folder_FTP_Download, session);
                                        }
                                        else
                                        {

                                        }                                       
                                    }
                                }
                            }

                            // 2. Folder Log
                            //MessageBox.Show("\r\nDirectoryInfo_log.Files.Count : " +  directoryInfo_log.Files.Count.ToString());
                            if (directoryInfo_log.Files.Count > 0)
                            {
                                List<RemoteFileInfo> fileInfo_Castlog = new List<RemoteFileInfo>();

                                foreach (RemoteFileInfo fileInfo in directoryInfo_log.Files)
                                {

                                    if (fileInfo.Name != "..")
                                    {
                                        string separate_file_name = GetUntilOrEmpty(fileInfo.Name); // ตัดชื่อไฟล์ก่อนถึง '_' ก็คือ แยกชื่อไฟล์ที่มีชื่อข้างหน้า cast

                                        if (separate_file_name == "cast")
                                        {
                                            fileInfo_Castlog.Add(fileInfo);
                                        }
                                        else
                                        {
                                           
                                        }
                                    }
                                }

                                string Castlog_FullName = CheckLastModified_FullName(fileInfo_Castlog);                 // file ที่ถูกแก้ไขล่าสุด
                                string Castlog_LastModified = CheckLastModified_LastWriteTime(fileInfo_Castlog);


                                Debug.WriteLine("\r\nLast modified : ", Castlog_FullName + "  ,  " + Castlog_LastModified);
                                //MessageBox.Show("\r\nLast modified : " + Castlog_FullName + "  ,  " + Castlog_LastModified);

                                string filename = Castlog_FullName.Substring(Castlog_FullName.Length - 23); // cast_20230217162938.csv

                                string datetime_file = filename.Substring(5, 14);
                                string year = datetime_file.Substring(0, 4);
                                string month = datetime_file.Substring(4, 2);
                                string day = datetime_file.Substring(6, 2);

                                string hour = datetime_file.Substring(8, 2);
                                string min = datetime_file.Substring(10, 2);
                                string sec = datetime_file.Substring(12, 2);

                                datetime_file = year + "-" + month + "-" + day + " " + hour + ":" + min + ":" + sec;


                                tablename = "tbl_k2next_log";
                                int count = CountRecordDB(tablename, filename, serial);
                                if (count > 0)   // ไฟล์ชื่อนี้มีการบันทึกในฐานข้อมูลแล้ว 
                                {
                                    //ไฟล์เก่า ให้ update last_modified

                                    /*double seconds = Get_CycleTime(datetime_file, Castlog_LastModified);

                                    // Convert Sec to DateTime format
                                    TimeSpan time = TimeSpan.FromSeconds(seconds);
                                    DateTime dateTime = DateTime.Today.Add(time);
                                    string cycle_time = dateTime.ToString("mm:ss");

                                    Update_Last_Modified_CycleTime(ip, filename, Castlog_LastModified, cycle_time);*/
                                }
                                else
                                {
                                    // ไฟล์ใหม่
                                    // 1.Download 
                                    string remoteFile = remotePath_log + filename;

                                    bool res = DownloadFile(session, remoteFile, folder_FTP_Download);
                                    Thread.Sleep(1000);

                                    // Read .CSV
                                    if (res == true)
                                    {
                                        // 3.Read .csv
                                        string fullpath = folder_FTP_Download + filename;
                                        res = Read_CSV_K2NEXT(serial, machine_type, tablename, ip, filename, datetime_file, fullpath, Castlog_LastModified);       // Castlog
                                        if (res == true)
                                        {
                                            string timenow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                            textBox13.Text += ip + "--> " + filename + " --> K2NEXT-16000 Last modified --> " + Castlog_LastModified +  "--> PC Time: " + timenow  + "\r\n";
                                            textBox13.SelectionStart = textBox13.Text.Length;
                                            textBox13.ScrollToCaret();

                                            // 4.ลบไฟล์ที่ Download มา
                                            DeleteFile(fullpath);


                                            string xx = Get_Record_Today_DB(ip);
                                            Insert_Update_Counter(serial, machine_type, ip, int.Parse(xx));
                                        }
                                        else
                                        {
                                            Debug.WriteLine("Read .CSV Error , Insert Error");
                                        }
                                    }
                                    else
                                    {
                                        Debug.WriteLine("Download Error");
                                    }
                                }


                                
                            }                           
                        }
                        else if (machine_type == "ALTIMA")
                        {
                            string tablename = "";

                            List<RemoteFileInfo> fileInfo_HistoryLog = new List<RemoteFileInfo>();
                            List<RemoteFileInfo> fileInfo_MTTRLog = new List<RemoteFileInfo>();
                            List<RemoteFileInfo> fileInfo_WaxshootLog = new List<RemoteFileInfo>();

                            //MessageBox.Show("\r\nDirectoryInfo_log.Files.Count : " + directoryInfo_log.Files.Count.ToString());
                            // 1.Folder log
                            foreach (RemoteFileInfo fileInfo1 in directoryInfo_log.Files)
                            {

                                if (fileInfo1.Name != "..")
                                {
                                    if (fileInfo1.Name == "history") // Folder name
                                    {
                                        string path = remotePath_log + fileInfo1.Name + "/";

                                        

                                        RemoteDirectoryInfo folder_history = session.ListDirectory(path);

                                        foreach (RemoteFileInfo fileInfo2 in folder_history.Files)
                                        {

                                            if (fileInfo2.Name != "..")
                                            {
                                                fileInfo_HistoryLog.Add(fileInfo2);
                                            }
                                        }
                                    }
                                    else if (fileInfo1.Name == "mttr") // Folder name
                                    {
                                        string path = remotePath_log + fileInfo1.Name + "/";

                                        RemoteDirectoryInfo folder_mttr = session.ListDirectory(path);

                                        foreach (RemoteFileInfo fileInfo2 in folder_mttr.Files)
                                        {

                                            if (fileInfo2.Name != "..")
                                            {
                                                fileInfo_MTTRLog.Add(fileInfo2);
                                            }
                                        }
                                    }
                                    else if (fileInfo1.Name == "waxshoot") // Folder name
                                    {
                                        string path = remotePath_log + fileInfo1.Name + "/";

                                        RemoteDirectoryInfo folder_waxshoot = session.ListDirectory(path);

                                        foreach (RemoteFileInfo fileInfo2 in folder_waxshoot.Files)
                                        {

                                            if (fileInfo2.Name != "..")
                                            {
                                                fileInfo_WaxshootLog.Add(fileInfo2);
                                            }
                                        }
                                    }
                                }


                                /*if (fileInfo.Name != "..")
                                {
                                    string separate_file_name = GetUntilOrEmpty(fileInfo.Name); // ตัดชื่อไฟล์ก่อนถึง '_' ก็คือ แยกชื่อไฟล์ที่มีชื่อข้างหน้า WorkLog และ WorkLogRecipe

                                    if (separate_file_name == "historyLog")
                                    {
                                        // HistoryLog_2023.csv
                                        fileInfo_HistoryLog.Add(fileInfo);
                                    }
                                    else if (separate_file_name == "mttr")
                                    {
                                        // 3.MTTRLog_2023.csv
                                        fileInfo_MTTRLog.Add(fileInfo);
                                    }
                                    else if (separate_file_name == "waxshoot")
                                    {
                                        // 3.WaxshootLog_20230810.csv
                                        fileInfo_WaxshootLog.Add(fileInfo);
                                    }
                                }*/
                            }



                            string WaxshootLog_FullName = "";
                            string WaxshootLog_LastModified = "";
                            if (fileInfo_WaxshootLog.Count > 0)
                            {
                                WaxshootLog_FullName = CheckLastModified_FullName(fileInfo_WaxshootLog);                       // file ที่ถูกแก้ไขล่าสุด
                                WaxshootLog_LastModified = CheckLastModified_LastWriteTime(fileInfo_WaxshootLog);

                                tablename = "tbl_altima_waxshootlog";
                                string path = remotePath_log + "waxshoot/";
                                Process_WaxshootLog_ALTIMA(serial, machine_type, tablename, ip, WaxshootLog_FullName, folderSave_lastlinenumber, path, WaxshootLog_LastModified, folder_FTP_Download, session);

                            }

                            string HistoryLog_FullName = "";
                            string HistoryLog_LastModified = "";
                            if (fileInfo_HistoryLog.Count > 0)
                            {
                                HistoryLog_FullName = CheckLastModified_FullName(fileInfo_HistoryLog);                       // file ที่ถูกแก้ไขล่าสุด
                                HistoryLog_LastModified = CheckLastModified_LastWriteTime(fileInfo_HistoryLog);

                                tablename = "tbl_altima_history";
                                string path = remotePath_log + "history/";
                                Process_HistoryLog_ALTIMA(serial, machine_type, tablename, ip, HistoryLog_FullName, folderSave_lastlinenumber, path, HistoryLog_LastModified, folder_FTP_Download, session);

                            }

                            string MTTRLog_FullName = "";
                            string MTTRLog_LastModified = "";
                            if (fileInfo_MTTRLog.Count > 0)
                            {
                                MTTRLog_FullName = CheckLastModified_FullName(fileInfo_MTTRLog);                       // file ที่ถูกแก้ไขล่าสุด
                                MTTRLog_LastModified = CheckLastModified_LastWriteTime(fileInfo_MTTRLog);

                                tablename = "tbl_altima_mttrlog";
                                string path = remotePath_log + "mttr/";
                                Process_MTTRLog_ALTIMA(serial, machine_type, tablename, ip, MTTRLog_FullName, folderSave_lastlinenumber, path, MTTRLog_LastModified, folder_FTP_Download, session);

                            }



                            //Debug.WriteLine("Last modified : " + WaxshootLog_FullName + "  ,  " + WaxshootLog_LastModified);
                            //Debug.WriteLine("Last modified : " + HistoryLog_FullName + "  ,  " + HistoryLog_LastModified);
                            //Debug.WriteLine("Last modified : " + MTTRLog_FullName + "  ,  " + MTTRLog_LastModified);
                            
                            //MessageBox.Show("\r\nLast modified : " + WaxshootLog_FullName + "  ,  " + WaxshootLog_LastModified);


                            // 2. Folder recipe
                            if (directoryInfo_recipe.Files.Count > 0)
                            {
                                List<RemoteFileInfo> fileInfo_job_recipe = new List<RemoteFileInfo>();
                                List<RemoteFileInfo> fileInfo_SsY_recipe = new List<RemoteFileInfo>();

                                // 1.Folder recipe
                                foreach (RemoteFileInfo fileInfo in directoryInfo_recipe.Files)
                                {
                                    if (fileInfo.Name != "..")
                                    {
                                        string separate_file_name = GetUntilOrEmpty(fileInfo.Name); // ตัดชื่อไฟล์ก่อนถึง '_' ก็คือ แยกชื่อไฟล์ที่มีชื่อข้างหน้า job และ Ssy

                                        if (separate_file_name == "job")
                                        {
                                            fileInfo_job_recipe.Add(fileInfo);
                                        }
                                        else if (separate_file_name == "SsY")
                                        {
                                            tablename = "tbl_altima_recipe";
                                            Process_Recipe_ALTIMA(serial, machine_type, tablename, fileInfo, ip, remotePath_recipe, folder_FTP_Download, session);
                                        }
                                    }
                                }
                            }
                        }
                        //--------------------------------------------------------------------------------------------

                        session.Close();
                        //Debug.WriteLine("Close Session...");
                        //Debug.WriteLine("Sleeping 60s ...");

                        //label13.Text = "";


                        //Thread.Sleep(60000);
                        Thread.Sleep(5000);

                        /*string timenow_end = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                        string[] a = timenow_end.Split(' ');
                        string yearmmdd_current = a[0];
                        string hhmm_current = a[1];

                        string[] date_current = yearmmdd_current.Split('-');
                        int year_current = int.Parse(date_current[0]);
                        int month_current = int.Parse(date_current[1]);
                        int day_current = int.Parse(date_current[2]);

                        string[] time_current = hhmm_current.Split(':');
                        int hh_current = int.Parse(time_current[0]);
                        int min_current = int.Parse(time_current[1]);
                        int sec_current = int.Parse(time_current[2]);

                        DateTime date1 = new DateTime(year_before, month_before, day_before, hh_before, min_before, sec_before);
                        DateTime date2 = new DateTime(year_current, month_current, day_current, hh_current, min_current, sec_current);
                        double totalminutes = (date2 - date1).TotalMilliseconds / 1000;*/
                        //label14.Text = totalminutes.ToString("0") + " sec";
                    }
                }
                catch (Exception ex)
                {
                    /*if (ip == "192.168.100.105")
                    {
                        MessageBox.Show("IP : " + ip + "          " + ex.ToString());
                    }*/
                    //Debug.WriteLine("IP : " + ip + "          " + ex.ToString());

                    Thread.Sleep(5000);

                    // Update OFFLINE
                    Insert_Update_Status_ONLINE(ip, false, true, serial, machine_type);        // offline

                    // Machine Status "RUN" , "IDLE"
                    Insert_Update_Status_RUN_IDLE(ip, false, false, serial, machine_type);     // reset

                    // Machine Status "Warning" , "Error"
                    //Insert_Update_Status_WARNING_ERROR(ip, false, false); // reset
                }
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        string ReadTextFile(string filename)
        {
            string res = "";

            try
            {
                if (!File.Exists(filename)) // Create a file 
                {
                    //string timenow = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    //AppendTextFile(filename, "START " + timenow);
                    using (StreamWriter sw = File.CreateText(filename))
                    {
                        //sw.WriteLine("New file created: {0}", DateTime.Now.ToString());
                    }
                }

                string[] lines = File.ReadAllLines(filename);

                res = lines[0];
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                res = "0";
            }

            return res;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Save_Config_Register(string server, string user, string password, string databasename)
        {
            RegistryKey RegKeyWrite = Registry.CurrentUser;
            RegKeyWrite = RegKeyWrite.CreateSubKey(@"Software\CSHARP\WriteRegistryValue");
            RegKeyWrite.SetValue("server", server);
            RegKeyWrite.SetValue("user", user);
            RegKeyWrite.SetValue("password", password);
            RegKeyWrite.SetValue("databasename", databasename);
            RegKeyWrite.Close();
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Read_Config_Register()
        {
            try
            {
                RegistryKey RegKeyRead = Registry.CurrentUser;
                RegKeyRead = RegKeyRead.OpenSubKey(@"Software\CSHARP\WriteRegistryValue");


                Object server = RegKeyRead.GetValue("server");
                Object user = RegKeyRead.GetValue("user");
                Object password = RegKeyRead.GetValue("password");
                Object databasename = RegKeyRead.GetValue("databasename");

                textBox1.Text = server.ToString();
                textBox2.Text = user.ToString();
                textBox3.Text = password.ToString();
                textBox4.Text = databasename.ToString();

                Server_ini = textBox1.Text;
                User_ini = textBox2.Text;
                Password_ini = textBox3.Text;
                Database_ini = textBox4.Text;


                RegKeyRead.Close();

            }
            catch (Exception ex)
            {
                //MessageBox.Show("Read_Config_Registery --------> " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------


        bool Read_CSV_Recipe_RBF(string serial, string machine_type, string tablename, string ip, string filename, string last_modified, string fullpath)
        {
            bool res = false;
            try
            {
                string STEP = "";
                string FLG = "";
                string ELV = "";
                string TEMP = "";
                string KEEP = "";
                string ROT = "";
                string AB = "";


                // Read .CSV
                using (var reader = new StreamReader(fullpath))
                {
                    int cnt = 0;
                    string[] input_name = new string[2];

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split('\t');

                        if (cnt == 0)
                        {
                            input_name = values[1].Split('\t');
                        }
                        else
                        {
                            STEP = values[0];
                            FLG = values[1];
                            ELV = values[2];
                            TEMP = values[3];
                            KEEP = values[4];
                            ROT = values[5];
                            AB = values[6];

                            // Insert to Database
                            Insert_Recipe_RBF(serial, machine_type, tablename, ip, filename, last_modified, input_name[0], STEP, FLG, ELV, TEMP, KEEP, ROT, AB);
                        }

                        cnt++;
                    }

                }

                res = true;
                //MessageBox.Show("Done !!!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                res = false;
                //MessageBox.Show("ReadCSV_Recipe_RBF"+ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return res;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        bool Read_CSV_Recipe_K2NEXT(string serial, string machine_type, string tablename, string ip, string filename, string last_modified, string fullpath)
        {
            bool res = false;
            try
            {
                List<string> data = new List<string>();

                // Read .CSV
                using (var reader = new StreamReader(fullpath))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split('\t');

                        if (values.Length == 1) // Name บางครั้งไม่มีข้อมูล ว่าง
                        {
                            data.Add("");
                        }
                        else
                        {
                            data.Add(values[1]);
                        }
                    }

                    // Insert to Database
                    Insert_Recipe_K2NEXT(serial, machine_type, tablename, ip, filename, last_modified, data);

                }

                res = true;
                //MessageBox.Show("Done !!!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                res = false;
                //MessageBox.Show("ReadCSV_Recipe_K2NEXTy --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return res;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        bool Read_CSV_Recipe_ALTIMA(string serial, string machine_type, string tablename, string ip, string filename, string last_modified, string fullpath)
        {
            bool res = false;
            try
            {
                List<string> data = new List<string>();

                // Read .CSV
                using (var reader = new StreamReader(fullpath))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split('\t');

                        data.Add(values[1]);
                    }

                    // Insert to Database
                    Insert_Recipe_ALTIMA(serial, machine_type, tablename, ip, filename, last_modified, data);

                }

                res = true;
                //MessageBox.Show("Done !!!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                res = false;
                //MessageBox.Show("Read_CSV_Recipe_ALTIMA --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return res;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        bool Read_CSV_WorkLogRecipe_RBF(string serial, string machine_type, string tablename, string ip, string filename, string fullpath)
        {
            bool res = false;
            try
            {
                //WorkLog_recipe_20220930111955.csv
                string[] a = filename.Split('_');
                string b = a[2].Substring(0, 12); // เอาเฉพาะวันเดือนปีและเวลา YYYYMMDDHHmm

                string year = b.Substring(0, 4);
                string month = b.Substring(4, 2);
                string day = b.Substring(6, 2);

                string hour = b.Substring(8, 2);
                string min = b.Substring(10, 2);

                string datetime_file = year + "-" + month + "-" + day + " " + hour + ":" + min + ":00";

                string STEP = "";
                string FLG = "";
                string ELV = "";
                string TEMP = "";
                string KEEP = "";
                string ROT = "";
                string AB = "";


                // Read .CSV
                using (var reader = new StreamReader(fullpath))
                {
                    int cnt = 0;
                    string[] input_name = new string[2];

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split('\t');

                        if (cnt == 0)
                        {
                            input_name = values[1].Split('\t');
                        }
                        else
                        {
                            STEP = values[0];
                            FLG = values[1];
                            ELV = values[2];
                            TEMP = values[3];
                            KEEP = values[4];
                            ROT = values[5];
                            AB = values[6];

                            // Insert to Database
                            Insert_WorkLogRecipe_RBF(serial, machine_type, tablename, ip, filename, datetime_file, input_name[0], STEP, FLG, ELV, TEMP, KEEP, ROT, AB);
                        }

                        cnt++;
                    }

                }

                res = true;
                //MessageBox.Show("Done !!!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                res = false;
                //MessageBox.Show("Read_CSV_WorkLogRecipey --------> "+ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return res;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        bool Read_CSV_WorkLog_RBF(string serial, string machine_type, string tablename, string ip, string filename, string last_modified, string fullpath, string filename_save_cnt)
        {
            bool res = false;
            try
            {
                //WorkLog_202209301119.csv
                string[] a = filename.Split('_');
                string b = a[1].Substring(0, 12); // เอาเฉพาะวันเดือนปีและเวลา YYYYMMDDHHmm

                string year = b.Substring(0, 4);
                string month = b.Substring(4, 2);
                string day = b.Substring(6, 2);

                string hour = b.Substring(8, 2);
                string min = b.Substring(10, 2);

                string datetime_file = year + "-" + month + "-" + day + " " + hour + ":" + min + ":00";


                string DATETIME = "";
                string TEMP = "";
                string KEEP = "";

                // Read Line Counter
                string x = ReadTextFile(filename_save_cnt);
                int current_line = int.Parse(x);

                // Read .CSV
                var lines = File.ReadLines(fullpath).Skip(current_line);

                foreach (string line in lines)
                {
                    if (line != "")
                    {
                        var values = line.Split('\t');

                        string date = values[2];
                        string time = values[3];
                        string dateString = date + " " + time;

                        // 2022/09/30 11:19:18:173
                        DateTime dt = DateTime.ParseExact(dateString, "yyyy/MM/dd HH:mm:ss:fff", CultureInfo.InvariantCulture);

                        DATETIME = dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        TEMP = values[5];
                        KEEP = values[6];


                        // Insert
                        Insert_WorkLog_RBF(serial, machine_type, tablename, ip, filename, datetime_file, last_modified, DATETIME, TEMP, KEEP);


                        current_line++;

                        // Save line number
                        File.WriteAllText(filename_save_cnt, current_line.ToString());

                    }
                    else
                    {

                    }

                }


                res = true;
            }
            catch (Exception ex)
            {
                res = false;
                //MessageBox.Show("Read_CSV_WorkLog --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return res;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        bool Read_CSV_HistoryLog_RBF(string serial, string machine_type, string tablename, string ip, string filename, string last_modified, string fullpath, string filename_save_cnt)
        {
            bool res = false;
            try
            {
                //HistoryLog_2023.csv


                string JAPAN_TIME = "";
                string LOCAL_TIME = "";
                string TYPE = "";
                string VALUE = "";

                // Read Line Counter
                string x = ReadTextFile(filename_save_cnt);

                int current_line = int.Parse(x);

                // Read .CSV
                var lines = File.ReadLines(fullpath).Skip(current_line);

                foreach (string line in lines)
                {
                    if (line != "")
                    {
                        var values = line.Split('\t');

                        string japan_time = values[0];
                        string local_time = values[1];

                        DateTime dt_japan = DateTime.ParseExact(japan_time, "yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
                        DateTime dt_local = DateTime.ParseExact(local_time, "yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);

                        JAPAN_TIME = dt_japan.ToString("yyyy-MM-dd HH:mm");
                        LOCAL_TIME = dt_local.ToString("yyyy-MM-dd HH:mm");

                        VALUE = values[2];

                        if ((VALUE.Contains("Error") || VALUE.Contains("Warning") || VALUE.Contains("Emergency") || VALUE.Contains("User USB write error") || VALUE.Contains("Power ON.")))
                        {
                            if (VALUE.Contains("Error"))
                            {
                                // Error
                                //TYPE = "Error"; 
                                Debug.WriteLine("Error");

                                TYPE = values[2];
                                VALUE = values[3];

                                // Insert
                                Insert_HistoryLog(serial, machine_type, tablename, ip, filename, last_modified, JAPAN_TIME, LOCAL_TIME, TYPE, VALUE);

                                // Update Error
                                Insert_Update_Status_WARNING_ERROR(ip, false, true);
                            }
                            else if (VALUE.Contains("Warning"))
                            {
                                // Alarm 
                                //TYPE = "Warning";
                                Debug.WriteLine("Warning");

                                TYPE = values[2];
                                VALUE = values[3];


                                // Update Error
                                Insert_Update_Status_WARNING_ERROR(ip, true, false);

                                // ต้องเข้าไปเอา Local Time ก่อนหน้า จาก Database แล้วมาลบกับเวลาที่ Warning ใหม่
                                string time_warning_before = Get_last_Warning_LocalTime_DB("Warning", tablename, filename, serial);

                                if (time_warning_before != "")
                                {

                                    // 2023-02-14 18:30:00 เวลาที่เก็บเข้า database ก่อนหน้า
                                    // 2023-02-24 23:37     เวลาที่ warning ใหม่

                                    // 1.เวลาที่เก็บเข้า database ก่อนหน้า และอ่านขึ้นมา
                                    string[] localtime_before = time_warning_before.Split(' ');
                                    string yearmmdd_before = localtime_before[0];
                                    string hhmm_before = localtime_before[1];

                                    string[] date_before = yearmmdd_before.Split('-');
                                    int year_before = int.Parse(date_before[0]);
                                    int month_before = int.Parse(date_before[1]);
                                    int day_before = int.Parse(date_before[2]);

                                    string[] time_before = hhmm_before.Split(':');
                                    int hh_before = int.Parse(time_before[0]);
                                    int min_before = int.Parse(time_before[1]);

                                    // ------------------------------------------------

                                    // 2.เวลาที่ warning ล่าสุด
                                    string[] localtime_current = LOCAL_TIME.Split(' ');
                                    string yearmmdd_current = localtime_current[0];
                                    string hhmm_current = localtime_current[1];

                                    string[] date_current = yearmmdd_current.Split('-');
                                    int year_current = int.Parse(date_current[0]);
                                    int month_current = int.Parse(date_current[1]);
                                    int day_current = int.Parse(date_current[2]);

                                    string[] time_current = hhmm_current.Split(':');
                                    int hh_current = int.Parse(time_current[0]);
                                    int min_current = int.Parse(time_current[1]);


                                    string timebefore = year_before.ToString() + " " + month_before.ToString() + " " + day_before.ToString() + " " + hh_before.ToString() + " " + min_before.ToString();
                                    string timecurrent = year_current.ToString() + " " + month_current.ToString() + " " + day_current.ToString() + " " + hh_current.ToString() + " " + min_current.ToString();

                                    //Debug.WriteLine(timebefore);
                                    //Debug.WriteLine(timecurrent);


                                    // เทียบเวลา เกิน 2นาทีไหม ถ้าไม่เกิน 2นาที ไม่ต้อง Insert
                                    DateTime date1 = new DateTime(year_before, month_before, day_before, hh_before, min_before, 00);
                                    DateTime date2 = new DateTime(year_current, month_current, day_current, hh_current, min_current, 00);
                                    double totalminutes = (date2 - date1).TotalMinutes;
                                    int total = Convert.ToInt32(totalminutes);

                                    if (total > 2)
                                    {
                                        // Insert
                                        Insert_HistoryLog(serial, machine_type, tablename, ip, filename, last_modified, JAPAN_TIME, LOCAL_TIME, TYPE, VALUE);

                                        // Update Warning
                                        Insert_Update_Status_WARNING_ERROR(ip, true, false);
                                    }
                                }
                                else
                                {

                                    TYPE = values[2];
                                    VALUE = values[3];

                                    // Insert
                                    Insert_HistoryLog(serial, machine_type, tablename, ip, filename, last_modified, JAPAN_TIME, LOCAL_TIME, TYPE, VALUE);
                                }
                            }
                            else if (VALUE.Contains("Emergency"))
                            {
                                // Alarm 
                                //TYPE = "Emergency Stop";
                                Debug.WriteLine("Emergency Stop");

                                TYPE = values[2];
                                VALUE = values[3];

                                // Insert
                                Insert_HistoryLog(serial, machine_type, tablename, ip, filename, last_modified, JAPAN_TIME, LOCAL_TIME, TYPE, VALUE);
                            }
                            else if (VALUE.Contains("User USB write error"))
                            {
                                // Alarm 
                                //TYPE = "User USB write error";
                                Debug.WriteLine("User USB write error");

                                TYPE = values[2];
                                VALUE = values[3];

                                // Insert
                                Insert_HistoryLog(serial, machine_type, tablename, ip, filename, last_modified, JAPAN_TIME, LOCAL_TIME, TYPE, VALUE);
                            }
                            else if (VALUE.Contains("Power ON."))
                            {
                                // Alarm 
                                //TYPE = "Power ON.";
                                Debug.WriteLine("Power ON.");

                                TYPE = values[2];
                                VALUE = values[3];

                                // Insert
                                Insert_HistoryLog(serial, machine_type, tablename, ip, filename, last_modified, JAPAN_TIME, LOCAL_TIME, TYPE, VALUE);

                                // Reset Warning/Error
                                Insert_Update_Status_WARNING_ERROR(ip, false, false);
                            }
                        }
                        else
                        {
                            // Insert
                            TYPE = "";

                            //TYPE = values[2];
                            VALUE = values[2];

                            Insert_HistoryLog(serial, machine_type, tablename, ip, filename, last_modified, JAPAN_TIME, LOCAL_TIME, TYPE, VALUE);
                        }

                        current_line++;

                        // Save line number
                        File.WriteAllText(filename_save_cnt, current_line.ToString());

                    }
                    else
                    {

                    }

                }


                res = true;
            }
            catch (Exception ex)
            {
                res = false;
                //MessageBox.Show("Read_CSV_HistoryLog_RBF --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return res;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        bool Read_CSV_HistoryLog_ALTIMA(string serial , string machine_type, string tablename, string ip, string filename, string last_modified, string fullpath, string filename_save_cnt)
        {
            bool res = false;
            try
            {
                //HistoryLog_2023.csv


                string JAPAN_TIME = "";
                string LOCAL_TIME = "";
                string TYPE = "";
                string VALUE = "";

                // Read Line Counter
                string x = ReadTextFile(filename_save_cnt);

                int current_line = int.Parse(x);

                // Read .CSV
                var lines = File.ReadLines(fullpath).Skip(current_line);

                foreach (string line in lines)
                {
                    if (line != "")
                    {
                        var values = line.Split('\t');

                        string japan_time = values[0];
                        string local_time = values[1];

                        DateTime dt_japan = DateTime.ParseExact(japan_time, "yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
                        DateTime dt_local = DateTime.ParseExact(local_time, "yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);

                        JAPAN_TIME = dt_japan.ToString("yyyy-MM-dd HH:mm");
                        LOCAL_TIME = dt_local.ToString("yyyy-MM-dd HH:mm");

                        VALUE = values[2];

                        if ((VALUE.Contains("Error") || VALUE.Contains("Warning") || VALUE.Contains("Emergency") || VALUE.Contains("User USB write error") || VALUE.Contains("Power ON.")))
                        {
                            if (VALUE.Contains("Error"))
                            {
                                // Error
                                //TYPE = "Error"; 
                                Debug.WriteLine("Error");

                                TYPE = values[2];
                                VALUE = values[3];

                                // Insert
                                Insert_HistoryLog(serial, machine_type, tablename, ip, filename, last_modified, JAPAN_TIME, LOCAL_TIME, TYPE, VALUE);

                                // Update Error
                                Insert_Update_Status_WARNING_ERROR(ip, false, true);
                            }
                            else if (VALUE.Contains("Warning"))
                            {
                                // Alarm 
                                //TYPE = "Warning";
                                Debug.WriteLine("Warning");

                                TYPE = values[2];
                                VALUE = values[3];

                                // Update Error
                                Insert_Update_Status_WARNING_ERROR(ip, true, false);


                                // ต้องเข้าไปเอา Local Time ก่อนหน้า จาก Database แล้วมาลบกับเวลาที่ Warning ใหม่
                                string time_warning_before = Get_last_Warning_LocalTime_DB("Warning", tablename, filename, ip);

                                if (time_warning_before != "")
                                {

                                    // 2023-02-14 18:30:00 เวลาที่เก็บเข้า database ก่อนหน้า
                                    // 2023-02-24 23:37     เวลาที่ warning ใหม่

                                    // 1.เวลาที่เก็บเข้า database ก่อนหน้า และอ่านขึ้นมา
                                    string[] localtime_before = time_warning_before.Split(' ');
                                    string yearmmdd_before = localtime_before[0];
                                    string hhmm_before = localtime_before[1];

                                    string[] date_before = yearmmdd_before.Split('-');
                                    int year_before = int.Parse(date_before[0]);
                                    int month_before = int.Parse(date_before[1]);
                                    int day_before = int.Parse(date_before[2]);

                                    string[] time_before = hhmm_before.Split(':');
                                    int hh_before = int.Parse(time_before[0]);
                                    int min_before = int.Parse(time_before[1]);

                                    // ------------------------------------------------

                                    // 2.เวลาที่ warning ล่าสุด
                                    string[] localtime_current = LOCAL_TIME.Split(' ');
                                    string yearmmdd_current = localtime_current[0];
                                    string hhmm_current = localtime_current[1];

                                    string[] date_current = yearmmdd_current.Split('-');
                                    int year_current = int.Parse(date_current[0]);
                                    int month_current = int.Parse(date_current[1]);
                                    int day_current = int.Parse(date_current[2]);

                                    string[] time_current = hhmm_current.Split(':');
                                    int hh_current = int.Parse(time_current[0]);
                                    int min_current = int.Parse(time_current[1]);


                                    string timebefore = year_before.ToString() + " " + month_before.ToString() + " " + day_before.ToString() + " " + hh_before.ToString() + " " + min_before.ToString();
                                    string timecurrent = year_current.ToString() + " " + month_current.ToString() + " " + day_current.ToString() + " " + hh_current.ToString() + " " + min_current.ToString();

                                    //Debug.WriteLine(timebefore);
                                    //Debug.WriteLine(timecurrent);


                                    // เทียบเวลา เกิน 2นาทีไหม ถ้าไม่เกิน 2นาที ไม่ต้อง Insert
                                    DateTime date1 = new DateTime(year_before, month_before, day_before, hh_before, min_before, 00);
                                    DateTime date2 = new DateTime(year_current, month_current, day_current, hh_current, min_current, 00);
                                    double totalminutes = (date2 - date1).TotalMinutes;
                                    int total = Convert.ToInt32(totalminutes);

                                    if (total > 2)
                                    {
                                        // Insert
                                        Insert_HistoryLog(serial, machine_type, tablename, ip, filename, last_modified, JAPAN_TIME, LOCAL_TIME, TYPE, VALUE);

                                        // Update Warning
                                        Insert_Update_Status_WARNING_ERROR(ip, true, false);
                                    }
                                }
                                else
                                {

                                    TYPE = values[2];
                                    VALUE = values[3];

                                    // Insert
                                    Insert_HistoryLog(serial, machine_type, tablename, ip, filename, last_modified, JAPAN_TIME, LOCAL_TIME, TYPE, VALUE);
                                }
                            }
                            else if (VALUE.Contains("Emergency"))
                            {
                                // Alarm 
                                //TYPE = "Emergency Stop";
                                Debug.WriteLine("Emergency Stop");

                                TYPE = values[2];
                                VALUE = values[3];

                                // Insert
                                Insert_HistoryLog(serial, machine_type, tablename, ip, filename, last_modified, JAPAN_TIME, LOCAL_TIME, TYPE, VALUE);
                            }
                            else if (VALUE.Contains("User USB write error"))
                            {
                                // Alarm 
                                //TYPE = "User USB write error";
                                Debug.WriteLine("User USB write error");

                                TYPE = values[2];
                                VALUE = values[3];

                                // Insert
                                Insert_HistoryLog(serial, machine_type, tablename, ip, filename, last_modified, JAPAN_TIME, LOCAL_TIME, TYPE, VALUE);

                                // Reset Warning/Error
                                Insert_Update_Status_WARNING_ERROR(ip, false, true);
                            }
                            else if (VALUE.Contains("Power ON."))
                            {
                                // Alarm 
                                //TYPE = "Power ON.";
                                Debug.WriteLine("Power ON.");

                                TYPE = values[2];
                                VALUE = values[3];

                                // Insert
                                Insert_HistoryLog(serial, machine_type, tablename, ip, filename, last_modified, JAPAN_TIME, LOCAL_TIME, TYPE, VALUE);

                                // Reset Warning/Error
                                Insert_Update_Status_WARNING_ERROR(ip, false, false);
                            }
                        }
                        else
                        {
                            // Insert
                            TYPE = "";

                            //TYPE = values[2];
                            VALUE = values[2];

                            Insert_HistoryLog(serial, machine_type, tablename, ip, filename, last_modified, JAPAN_TIME, LOCAL_TIME, TYPE, VALUE);
                        }

                        current_line++;

                        // Save line number
                        File.WriteAllText(filename_save_cnt, current_line.ToString());

                    }
                    else
                    {

                    }

                }


                res = true;
            }
            catch (Exception ex)
            {
                res = false;
                //MessageBox.Show("Read_CSV_HistoryLog_ALTIMA --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return res;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        bool Read_CSV_MTTRLog_ALTIMA(string serial, string machine_type, string tablename, string ip, string filename, string last_modified, string fullpath, string filename_save_cnt)
        {
            bool res = false;
            try
            {
                //MTTRLog_2023.csv


                // Read Line Counter
                string x = ReadTextFile(filename_save_cnt);

                int cnt = 0;

                int current_line = int.Parse(x);


                // Read .CSV
                var lines = File.ReadLines(fullpath).Skip(current_line);

                foreach (string line in lines)
                {
                    if (line != "")
                    {
                        var values = line.Split('\t');

                        

                        if (cnt == 0)
                        {

                        }
                        else
                        {
                            string system_time = values[0];
                            string local_time = values[1];
                            string status = values[2];
                            string value = values[3];


                            DateTime dt_system = DateTime.ParseExact(system_time, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                            DateTime dt_local = DateTime.ParseExact(local_time, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

                            system_time = dt_system.ToString("yyyy-MM-dd HH:mm:ss");
                            local_time = dt_local.ToString("yyyy-MM-dd HH:mm:ss");


                            Insert_Update_Status_Altima_maintenace(ip, status); // 0='All of other status', 1='Regular maintenance', 2='Repair'

                            // Insert to Database
                            Insert_MTTRLog(serial, machine_type, tablename, ip, filename, last_modified, system_time, local_time, status, value);

                            current_line++;

                            // Save line number
                            File.WriteAllText(filename_save_cnt, current_line.ToString());
                        }

                        cnt++;

                        

                    }
                    else
                    {

                    }

                }


                res = true;
            }
            catch (Exception ex)
            {
                res = false;
                //MessageBox.Show("Read_CSV_MTTRLog_ALTIMA --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return res;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        bool Read_CSV_WaxshootLog_ALTIMA(string serial, string machine_type, string tablename, string ip, string filename, string last_modified, string fullpath, string filename_save_cnt)
        {
            bool res;
            bool f_insert = false;
            try
            {
                //WaxshootLog_20230810.csv


                // Read Line Counter
                string x = ReadTextFile(filename_save_cnt);

                int current_line = int.Parse(x);

                var lines = File.ReadLines(fullpath).Skip(current_line);
                string[] xxx = File.ReadLines(fullpath).Skip(current_line).ToArray();                

                int cnt = 0;

                if (xxx.Count() > 0)
                {
                    foreach (string line in lines)
                    {
                        if (line != "")
                        {
                            var values = line.Split('\t');


                            if (values[0] != "")
                            {
                                if (cnt == 0)
                                {
                                    if (current_line > 0)
                                    {

                                        // Insert to Database
                                        f_insert = Insert_WaxshootLog(serial, machine_type, tablename, ip, filename, last_modified, values);
                                    }
                                }
                                else
                                {
                                    // Insert to Database
                                    f_insert = Insert_WaxshootLog(serial, machine_type, tablename, ip, filename, last_modified, values);
                                }

                                cnt++;

                                if (f_insert)
                                {
                                    current_line++;
                                    File.WriteAllText(filename_save_cnt, current_line.ToString());
                                }
                            }
                            else
                            {
                                Debug.WriteLine("Read WagshootLog = null");
                            }

                        }
                        else
                        {
                           
                        }

                    }
                }
                else
                {
                    //1.update last modified to DB
                    Update_last_modified_ALTIMA(ip, filename, last_modified);
                    Insert_Update_Status_RUN_IDLE(ip, false, false, serial, machine_type);
                }

                res = true;
            }
            catch (Exception ex)
            {
                res = false;
                //MessageBox.Show("Read_CSV_WaxShootLog_ALTIMA --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return res;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Update_last_modified_ALTIMA(string ip_, string filename_, string last_modified_)
        {
            try
            {
                string tablename = "tbl_altima_waxshootlog";
                string last_id = Get_last_ID_DB(tablename, filename_, ip_);

                DateTime s = Convert.ToDateTime(last_modified_, CultureInfo.InvariantCulture);
                last_modified_ = s.ToString("yyyy-MM-dd HH:mm:ss");

                if (last_id != "")
                {
                    MySqlConnection conn = new MySqlConnection(connStr);
                    conn.Open();
                    MySqlCommand comm = conn.CreateCommand();

                    string sql = "UPDATE " + tablename + " SET last_modified = @last_modified WHERE ipaddress=@ipaddress AND filename=@filename AND id=@id";
                    comm.CommandText = sql;

                    comm.Parameters.AddWithValue("@ipaddress", ip_);
                    comm.Parameters.AddWithValue("@filename", filename_);
                    comm.Parameters.AddWithValue("@id", last_id);
                    comm.Parameters.AddWithValue("@last_modified", last_modified_);
                    comm.ExecuteNonQuery();
                    conn.Close();

                    Debug.WriteLine("Updated Table tr_altima_status");
                }
                else
                {
                    Debug.WriteLine("last id Altima Timeline = null ");
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Update_Timeline_ALTIMA --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        bool Read_CSV_K2NEXT(string serial_, string machine_type_, string tablename, string ip, string filename, string datetime_file, string fullpath, string last_modified)
        {
            bool res = false;
            try
            {
                //cast_20230217162938.csv

                List<string> data = new List<string>();

                // Read .CSV
                using (var reader = new StreamReader(fullpath))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split('\t');
                        //var values = line.Split(',');

                        data.Add(values[2]);
                    }


                    double seconds = Get_CycleTime(datetime_file, last_modified);

                    // Convert Sec to DateTime format
                    TimeSpan time = TimeSpan.FromSeconds(seconds);
                    DateTime dateTime = DateTime.Today.Add(time);
                    string cycle_time = dateTime.ToString("mm:ss");

                    // Insert to DB
                    //Insert_log_K2NEXT(tablename, ip, filename, datetime_file, data, last_modified, cycle_time);

                    string timenow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    Insert_log_K2NEXT(serial_, machine_type_, tablename, ip, filename, datetime_file, timenow, data, last_modified, cycle_time);



                }

                res = true;
            }
            catch (Exception ex)
            {
                res = false;
                //MessageBox.Show("Read_CSV_K2NEXT --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return res;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void DeleteRecord(string tablename, string filename, string ip)
        {
            try
            {

                string Query = "DELETE FROM " + tablename + " WHERE filename='" + filename + "' AND ipaddress='" + ip + "';";

                MySqlConnection Conn = new MySqlConnection(connStr);
                MySqlCommand MyCommand = new MySqlCommand(Query, Conn);
                MySqlDataReader MyReader;

                Conn.Open();

                MyReader = MyCommand.ExecuteReader();

                while (MyReader.Read())
                {
                }

                Conn.Close();

                Debug.WriteLine("Deleted");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("DeleteRecord --------> " + ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Insert_Recipe_RBF(string serial_, string machine_type_, string tablename, string ip_, string filename_, string last_modified_, string name_, string step_, string flag_, string elv_, string temp_, string keep_, string rot_, string ab_)
        {
            try
            {
                DateTime s = Convert.ToDateTime(last_modified_, CultureInfo.InvariantCulture);
                last_modified_ = s.ToString("yyyy-MM-dd HH:mm:ss");

                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();

                string sql = "INSERT INTO " + tablename + "(ipaddress, filename, last_modified, name_input, step, flag, elv, temp, keep, rot, ab, serial, machinetype) " +
                    "VALUES(@ipaddress, @filename, @last_modified, @name_input, @step, @flag, @elv, @temp, @keep, @rot, @ab, @serial, @machinetype)";

                comm.CommandText = sql;

                comm.Parameters.AddWithValue("@ipaddress", ip_);
                comm.Parameters.AddWithValue("@filename", filename_);
                comm.Parameters.AddWithValue("@last_modified", last_modified_);
                comm.Parameters.AddWithValue("@name_input", name_);
                comm.Parameters.AddWithValue("@step", step_);
                comm.Parameters.AddWithValue("@flag", flag_);
                comm.Parameters.AddWithValue("@elv", elv_);
                comm.Parameters.AddWithValue("@temp", temp_);
                comm.Parameters.AddWithValue("@keep", keep_);
                comm.Parameters.AddWithValue("@rot", rot_);
                comm.Parameters.AddWithValue("@ab", ab_);
                comm.Parameters.AddWithValue("@serial", serial_);
                comm.Parameters.AddWithValue("@machinetype", machine_type_);
                comm.ExecuteNonQuery();
                conn.Close();

                Debug.WriteLine("Inserted Recipe RBF");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Insert_Recipe_RFB --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Insert_Recipe_K2NEXT(string serial_, string machine_type_, string tablename, string ip_, string filename_, string last_modified_, List<string> data)
        {
            try
            {
                DateTime s = Convert.ToDateTime(last_modified_, CultureInfo.InvariantCulture);
                last_modified_ = s.ToString("yyyy-MM-dd HH:mm:ss");

                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();

                string sql = "INSERT INTO " + tablename + "(ipaddress, filename, last_modified, name, mode, after_mode, melt_temp, cast_temp, p_keep, vac, m_gas, oxy, suc_u, suc_l, pour_delay, press, open_time, auto_pour, c_temp_prm_p, c_temp_prm_i, c_temp_prm_d, c_temp_keep_time, casting_zone, " +
                    "max_power1, max_power2, max_power3, max_power1_temp, max_power2_temp, preheat_power, preheat_time, flask_up_timing, stopper_lift_time, oxy_timing, press_speed, keep_level, keep_time, p_gas, btn_p_ctrl, btn_de_gas, shot_temp, shot_tc_no_use, shot_heat_power, mixing, alloy, weight, flask_temp, pca_mode, connect_delay, add_gas_delay, add_gas_open, serial, machinetype) " +
                    "VALUES(@ipaddress, @filename, @last_modified, @name, @mode, @after_mode, @melt_temp, @cast_temp, @p_keep, vac, @m_gas, oxy, @suc_u, suc_l, @pour_delay, @press, @open_time, @auto_pour, @c_temp_prm_p, @c_temp_prm_i, @c_temp_prm_d, @c_temp_keep_time, @casting_zone," +
                    "@max_power1, @max_power2, @max_power3, @max_power1_temp, @max_power2_temp, @preheat_power, @preheat_time, @flask_up_timing, @stopper_lift_time, @oxy_timing, @press_speed, @keep_level, @keep_time, @p_gas, @btn_p_ctrl, @btn_de_gas, @shot_temp, @shot_tc_no_use, @shot_heat_power, @mixing, @alloy, @weight, @flask_temp, @pca_mode, @connect_delay, @add_gas_delay, @add_gas_open, @serial, @machinetype)";

                comm.CommandText = sql;

                comm.Parameters.AddWithValue("@ipaddress", ip_);
                comm.Parameters.AddWithValue("@filename", filename_);
                comm.Parameters.AddWithValue("@last_modified", last_modified_);
                comm.Parameters.AddWithValue("@name", data[0]);
                comm.Parameters.AddWithValue("@mode", data[1]);
                comm.Parameters.AddWithValue("@after_mode", data[2]);
                comm.Parameters.AddWithValue("@melt_temp", data[3]);
                comm.Parameters.AddWithValue("@cast_temp", data[4]);
                comm.Parameters.AddWithValue("@p_keep", data[5]);
                comm.Parameters.AddWithValue("@vac", data[6]);
                comm.Parameters.AddWithValue("@m_gas", data[7]);
                comm.Parameters.AddWithValue("@oxy", data[8]);
                comm.Parameters.AddWithValue("@suc_u", data[9]);
                comm.Parameters.AddWithValue("@suc_l", data[10]);

                comm.Parameters.AddWithValue("@pour_delay", data[11]);
                comm.Parameters.AddWithValue("@press", data[12]);
                comm.Parameters.AddWithValue("@open_time", data[13]);
                comm.Parameters.AddWithValue("@auto_pour", data[14]);
                comm.Parameters.AddWithValue("@c_temp_prm_p", data[15]);
                comm.Parameters.AddWithValue("@c_temp_prm_i", data[16]);
                comm.Parameters.AddWithValue("@c_temp_prm_d", data[17]);
                comm.Parameters.AddWithValue("@c_temp_keep_time", data[18]);
                comm.Parameters.AddWithValue("@casting_zone", data[19]);
                comm.Parameters.AddWithValue("@max_power1", data[20]);
                comm.Parameters.AddWithValue("@max_power2", data[21]);
                comm.Parameters.AddWithValue("@max_power3", data[22]);
                comm.Parameters.AddWithValue("@max_power1_temp", data[23]);
                comm.Parameters.AddWithValue("@max_power2_temp", data[24]);
                comm.Parameters.AddWithValue("@preheat_power", data[25]);
                comm.Parameters.AddWithValue("@preheat_time", data[26]);
                comm.Parameters.AddWithValue("@flask_up_timing", data[27]);
                comm.Parameters.AddWithValue("@stopper_lift_time", data[28]);
                comm.Parameters.AddWithValue("@oxy_timing", data[29]);
                comm.Parameters.AddWithValue("@press_speed", data[30]);
                comm.Parameters.AddWithValue("@keep_level", data[31]);
                comm.Parameters.AddWithValue("@keep_time", data[32]);
                comm.Parameters.AddWithValue("@p_gas", data[33]);

                comm.Parameters.AddWithValue("@btn_p_ctrl", data[34]);
                comm.Parameters.AddWithValue("@btn_de_gas", data[35]);
                comm.Parameters.AddWithValue("@shot_temp", data[36]);
                comm.Parameters.AddWithValue("@shot_tc_no_use", data[37]);
                comm.Parameters.AddWithValue("@shot_heat_power", data[38]);
                comm.Parameters.AddWithValue("@mixing", data[39]);
                comm.Parameters.AddWithValue("@alloy", data[40]);
                comm.Parameters.AddWithValue("@weight", data[41]);
                comm.Parameters.AddWithValue("@flask_temp", data[42]);

                comm.Parameters.AddWithValue("@pca_mode", data[43]);
                comm.Parameters.AddWithValue("@connect_delay", data[44]);
                comm.Parameters.AddWithValue("@add_gas_delay", data[45]);
                comm.Parameters.AddWithValue("@add_gas_open", data[46]);

                comm.Parameters.AddWithValue("@serial", serial_);
                comm.Parameters.AddWithValue("@machinetype", machine_type_);


                comm.ExecuteNonQuery();
                conn.Close();

                Debug.WriteLine("Inserted K2NEXT Recepe");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Insert_Recipe_K2NEXT --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Insert_Recipe_ALTIMA(string serial_, string machine_type_, string tablename, string ip_, string filename_, string last_modified_, List<string> data)
        {
            try
            {
                DateTime s = Convert.ToDateTime(last_modified_, CultureInfo.InvariantCulture);
                last_modified_ = s.ToString("yyyy-MM-dd HH:mm:ss");

                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();

                string sql = "INSERT INTO " + tablename + "(ipaddress, filename, last_modified, wax_type, wax_temp_wax_pot, wax_temp_wax_nozzle, vacuum, injection, press1, p_time, press2, clamp1, c_time, clamp2, forward, hold1, hold2, y_position, z_position, measurement, mold_level_to_expose, mold_level_to_injection, clamp_type, x_position, serial, machinetype) " +
                    "VALUES(@ipaddress, @filename, @last_modified, @wax_type, @wax_temp_wax_pot, @wax_temp_wax_nozzle, @vacuum, @injection, @press1, @p_time, @press2, @clamp1, @c_time, @clamp2, @forward, @hold1, @hold2, @y_position, @z_position, @measurement, @mold_level_to_expose, @mold_level_to_injection, @clamp_type, @x_position, @serial, @machinetype)";

                comm.CommandText = sql;

                comm.Parameters.AddWithValue("@ipaddress", ip_);
                comm.Parameters.AddWithValue("@filename", filename_);
                comm.Parameters.AddWithValue("@last_modified", last_modified_);
                comm.Parameters.AddWithValue("@wax_type", data[0]);
                comm.Parameters.AddWithValue("@wax_temp_wax_pot", data[1]);
                comm.Parameters.AddWithValue("@wax_temp_wax_nozzle", data[2]);
                comm.Parameters.AddWithValue("@vacuum", data[3]);
                comm.Parameters.AddWithValue("@injection", data[4]);
                comm.Parameters.AddWithValue("@press1", data[5]);
                comm.Parameters.AddWithValue("@p_time", data[6]);
                comm.Parameters.AddWithValue("@press2", data[7]);
                comm.Parameters.AddWithValue("@clamp1", data[8]);
                comm.Parameters.AddWithValue("@c_time", data[9]);
                comm.Parameters.AddWithValue("@clamp2", data[10]);

                comm.Parameters.AddWithValue("@forward", data[11]);
                comm.Parameters.AddWithValue("@hold1", data[12]);
                comm.Parameters.AddWithValue("@hold2", data[13]);
                comm.Parameters.AddWithValue("@y_position", data[14]);
                comm.Parameters.AddWithValue("@z_position", data[15]);
                comm.Parameters.AddWithValue("@measurement", data[16]);
                comm.Parameters.AddWithValue("@mold_level_to_expose", data[17]);

                comm.Parameters.AddWithValue("@mold_level_to_injection", data[18]);
                comm.Parameters.AddWithValue("@clamp_type", data[19]);
                comm.Parameters.AddWithValue("@x_position", data[20]);

                comm.Parameters.AddWithValue("@serial", serial_);
                comm.Parameters.AddWithValue("@machinetype", machine_type_);

                comm.ExecuteNonQuery();
                conn.Close();

                Debug.WriteLine("Inserted Recipe ALTIMA");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Insert_Recipe_ALTIMA --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Insert_WorkLogRecipe_RBF(string serial_, string machine_type_, string tablename, string ip_, string filename_, string datetime_file_, string name_, string step_, string flag_, string elv_, string temp_, string keep_, string rot_, string ab_)
        {
            try
            {

                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();

                string sql = "INSERT INTO " + tablename + "(ipaddress, filename, datetime_file, name_input, step, flag, elv, temp, keep, rot, ab, serial, machinetype) " +
                    "VALUES(@ipaddress, @filename, @datetime_file, @name_input, @step, @flag, @elv, @temp, @keep, @rot, @ab, @serial, @machinetype)";

                comm.CommandText = sql;

                comm.Parameters.AddWithValue("@ipaddress", ip_);
                comm.Parameters.AddWithValue("@filename", filename_);
                comm.Parameters.AddWithValue("@datetime_file", datetime_file_);
                comm.Parameters.AddWithValue("@name_input", name_);
                comm.Parameters.AddWithValue("@step", step_);
                comm.Parameters.AddWithValue("@flag", flag_);
                comm.Parameters.AddWithValue("@elv", elv_);
                comm.Parameters.AddWithValue("@temp", temp_);
                comm.Parameters.AddWithValue("@keep", keep_);
                comm.Parameters.AddWithValue("@rot", rot_);
                comm.Parameters.AddWithValue("@ab", ab_);
                comm.Parameters.AddWithValue("@serial", serial_);
                comm.Parameters.AddWithValue("@machinetype", machine_type_);
                comm.ExecuteNonQuery();
                conn.Close();

                Debug.WriteLine("Inserted RBF Recipe");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Insert_WorkLog_Recipe --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Insert_WorkLog_RBF(string serial_, string machine_type_, string tablename, string ip_, string filename_, string datetime_file_, string last_modified_, string datetime_, string temp_, string keep_)
        {
            try
            {
                DateTime s = Convert.ToDateTime(last_modified_, CultureInfo.InvariantCulture);
                last_modified_ = s.ToString("yyyy-MM-dd HH:mm:ss");

                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();

                string sql = "INSERT INTO " + tablename + "(ipaddress, filename, datetime_file, last_modified, datetime, temp, keep, serial, machinetype) " +
                    "VALUES(@ipaddress, @filename, @datetime_file, @last_modified,  @datetime, @temp, @keep, @serial, @machinetype)";

                comm.CommandText = sql;

                comm.Parameters.AddWithValue("@ipaddress", ip_);
                comm.Parameters.AddWithValue("@filename", filename_);
                comm.Parameters.AddWithValue("@datetime_file", datetime_file_);
                comm.Parameters.AddWithValue("@last_modified", last_modified_);
                comm.Parameters.AddWithValue("@datetime", datetime_);       // Data type should be DATETIME(6) for microseconds and DATETIME(3) for milliseconds.
                comm.Parameters.AddWithValue("@temp", temp_);
                comm.Parameters.AddWithValue("@keep", keep_);
                comm.Parameters.AddWithValue("@serial", serial_);
                comm.Parameters.AddWithValue("@machinetype", machine_type_);
                comm.ExecuteNonQuery();
                conn.Close();

                Debug.WriteLine("Inserted WorkLog RBF");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Insert_WorkLog --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Insert_HistoryLog(string serial_, string machine_type_, string tablename, string ip_, string filename_, string last_modified_, string japantime_, string localtime_, string type_, string value_)
        {
            try
            {
                DateTime s = Convert.ToDateTime(last_modified_, CultureInfo.InvariantCulture);
                last_modified_ = s.ToString("yyyy-MM-dd HH:mm:ss");

                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();

                string sql = "INSERT INTO " + tablename + "(ipaddress, filename, last_modified, japan_time, local_time, type, value, serial, machinetype) " +
                    "VALUES(@ipaddress, @filename, @last_modified,  @japan_time, @local_time, @type, @value, @serial, @machinetype)";

                comm.CommandText = sql;

                comm.Parameters.AddWithValue("@ipaddress", ip_);
                comm.Parameters.AddWithValue("@filename", filename_);
                comm.Parameters.AddWithValue("@last_modified", last_modified_);
                comm.Parameters.AddWithValue("@japan_time", japantime_);
                comm.Parameters.AddWithValue("@local_time", localtime_);
                comm.Parameters.AddWithValue("@type", type_);
                comm.Parameters.AddWithValue("@value", value_);
                comm.Parameters.AddWithValue("@serial", serial_);
                comm.Parameters.AddWithValue("@machinetype", machine_type_);
                comm.ExecuteNonQuery();
                conn.Close();

                Debug.WriteLine("Inserted History RBF");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Insert_HistoryLog --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Insert_MTTRLog(string serial_, string machine_type_, string tablename, string ip_, string filename_, string last_modified_, string system_time_, string localtime_, string status_, string value_)
        {
            try
            {
                DateTime s = Convert.ToDateTime(last_modified_, CultureInfo.InvariantCulture);
                last_modified_ = s.ToString("yyyy-MM-dd HH:mm:ss");

                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();

                string sql = "INSERT INTO " + tablename + "(ipaddress, filename, last_modified, system_time, local_time, status, value, serial, machinetype) " +
                    "VALUES(@ipaddress, @filename, @last_modified,  @system_time, @local_time, @status, @value, @serial, @machinetype)";

                comm.CommandText = sql;

                comm.Parameters.AddWithValue("@ipaddress", ip_);
                comm.Parameters.AddWithValue("@filename", filename_);
                comm.Parameters.AddWithValue("@last_modified", last_modified_);
                comm.Parameters.AddWithValue("@system_time", system_time_);
                comm.Parameters.AddWithValue("@local_time", localtime_);
                comm.Parameters.AddWithValue("@status", status_);
                comm.Parameters.AddWithValue("@value", value_);
                comm.Parameters.AddWithValue("@serial", serial_);
                comm.Parameters.AddWithValue("@machinetype", machine_type_);

                comm.ExecuteNonQuery();
                conn.Close();

                Debug.WriteLine("Inserted MTTRLog ALTIMA");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Insert_MTTRLog --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public bool Insert_WaxshootLog(string serial_ , string machine_type_, string tablename, string ip_, string filename_, string last_modified_, string[] data)
        {
            bool res;

            try
            {
                DateTime s = Convert.ToDateTime(last_modified_, CultureInfo.InvariantCulture);
                last_modified_ = s.ToString("yyyy-MM-dd HH:mm:ss");

                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();

                string sql = "INSERT INTO " + tablename + "(ipaddress, filename, last_modified, system_time, local_time, tank_temp, nozzle_temp,recipe_name, wax_type, wax_temp_wax_pot, wax_temp_wax_nozzle, vacuum, injection, press1, p_time, press2, clamp1, c_time, clamp2, forward, hold1, hold2, y_position, z_position, measurement, mold_level_to_expose, mold_level_to_injection, clamp_type, x_position, serial, machinetype) " +
                    "VALUES(@ipaddress, @filename, @last_modified, @system_time, @local_time, @tank_temp, @nozzle_temp, @recipe_name, @wax_type, @wax_temp_wax_pot, @wax_temp_wax_nozzle, @vacuum, @injection, @press1, @p_time, @press2, @clamp1, @c_time, @clamp2, @forward, @hold1, @hold2, @y_position, @z_position, @measurement, @mold_level_to_expose, @mold_level_to_injection, @clamp_type, @x_position, @serial, @machinetype)";

                comm.CommandText = sql;


                string system_time = data[0];
                string local_time = data[1];

                DateTime dt_system = DateTime.ParseExact(system_time, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                DateTime dt_local = DateTime.ParseExact(local_time, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

                system_time = dt_system.ToString("yyyy-MM-dd HH:mm:ss");
                local_time = dt_local.ToString("yyyy-MM-dd HH:mm:ss");


                comm.Parameters.AddWithValue("@serial", serial_);
                comm.Parameters.AddWithValue("@machinetype", machine_type_);
                comm.Parameters.AddWithValue("@ipaddress", ip_);
                comm.Parameters.AddWithValue("@filename", filename_);
                comm.Parameters.AddWithValue("@last_modified", last_modified_);
                comm.Parameters.AddWithValue("@system_time", system_time);
                comm.Parameters.AddWithValue("@local_time", local_time);
                comm.Parameters.AddWithValue("@tank_temp", data[2]);
                comm.Parameters.AddWithValue("@nozzle_temp", data[3]);
                comm.Parameters.AddWithValue("@recipe_name", data[4]);
                comm.Parameters.AddWithValue("@wax_type", data[5]);
                comm.Parameters.AddWithValue("@wax_temp_wax_pot", data[6]);
                comm.Parameters.AddWithValue("@wax_temp_wax_nozzle", data[7]);
                comm.Parameters.AddWithValue("@vacuum", data[8]);
                comm.Parameters.AddWithValue("@injection", data[9]);
                comm.Parameters.AddWithValue("@press1", data[10]);
                comm.Parameters.AddWithValue("@p_time", data[11]);
                comm.Parameters.AddWithValue("@press2", data[12]);
                comm.Parameters.AddWithValue("@clamp1", data[13]);
                comm.Parameters.AddWithValue("@c_time", data[14]);
                comm.Parameters.AddWithValue("@clamp2", data[15]);
                comm.Parameters.AddWithValue("@forward", data[16]);
                comm.Parameters.AddWithValue("@hold1", data[17]);
                comm.Parameters.AddWithValue("@hold2", data[18]);
                comm.Parameters.AddWithValue("@y_position", data[19]);
                comm.Parameters.AddWithValue("@z_position", data[20]);
                comm.Parameters.AddWithValue("@measurement", data[21]);
                comm.Parameters.AddWithValue("@mold_level_to_expose", data[22]);
                comm.Parameters.AddWithValue("@mold_level_to_injection", data[23]);
                comm.Parameters.AddWithValue("@clamp_type", data[24]);

                if (data.Length == 26)
                {
                    comm.Parameters.AddWithValue("@x_position", data[25]);
                }
                else
                {
                    comm.Parameters.AddWithValue("@x_position", DBNull.Value);
                }



                comm.ExecuteNonQuery();
                conn.Close();

                Debug.WriteLine("Inserted Table " + tablename);

                res = true;
            }
            catch (Exception ex)
            {
                res = false;
                //MessageBox.Show("Insert_WaxShootLog --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return res;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Insert_log_K2NEXT(string serial_, string machine_type_, string tablename, string ip_, string filename_, string datetime_file_, string datetimenow,  List<string> data, string last_modified_ , string cycle_time)
        {
            try
            {
                DateTime s = Convert.ToDateTime(last_modified_, CultureInfo.InvariantCulture);
                last_modified_ = s.ToString("yyyy-MM-dd HH:mm:ss");

                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();

                string sql = "INSERT INTO " + tablename + "(ipaddress, filename, datetime_file, casting_start_datetime, machine_model, serial_no, alloy, weight, flask_temp, recipe_no, recipe_name, auto_pour, after_mode, mixing, control, degas, cast_set_temp, cast_pour_temp, vacuum_time, m_gas, oxy, oxy_timing, suction_upper, suction_lower, pour_delay, " +
                    "flask_up_timing, press, press_open, charge_tank_value_at_press, open_time, c_tempprm_p, c_tempprm_i, c_tempprm_d, c_temp_keep_time, casting_zone, max_power1, max_power2, max_power3, max_power1_temp, max_power2_temp, preheat_power, preheat_time, lift_time_for_stopper, pre_press_for_degas, delay_time_for_press, protection_gas, last_modified, cycle_time_mm_ss, serial, machinetype) " +
                    "VALUES(@ipaddress, @filename, @datetime_file, @casting_start_datetime, @machine_model, @serial_no, @alloy, @weight, @flask_temp, @recipe_no, @recipe_name, @auto_pour, @after_mode, @mixing, @control, @degas, @cast_set_temp, @cast_pour_temp, @vacuum_time, @m_gas, @oxy, @oxy_timing, @suction_upper, @suction_lower, @pour_delay, " +
                    "@flask_up_timing, @press, @press_open, @charge_tank_value_at_press, @open_time, @c_tempprm_p, @c_tempprm_i, @c_tempprm_d, @c_temp_keep_time, @casting_zone, @max_power1, @max_power2, @max_power3, @max_power1_temp, @max_power2_temp, @preheat_power, @preheat_time, @lift_time_for_stopper, @pre_press_for_degas, @delay_time_for_press, @protection_gas, @last_modified, @cycle_time_mm_ss, @serial, @machinetype)";

                comm.CommandText = sql;


                //DateTime dt = DateTime.ParseExact(data[0], "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                //string start_time = dt.ToString("yyyy-MM-dd HH:mm:ss");

                string datetime_ = data[0];
                //DateTime dt = DateTime.ParseExact(data[0], "yyyy/M/d H:mm", CultureInfo.InvariantCulture);      // 24hr หลักเดียวไม่มี0ด้านหน้า
                DateTime dt = DateTime.ParseExact(data[0], "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);      
                string start_time = dt.ToString("yyyy-MM-dd HH:mm:ss");


                comm.Parameters.AddWithValue("@ipaddress", ip_);
                comm.Parameters.AddWithValue("@filename", filename_);
                comm.Parameters.AddWithValue("@datetime_file", datetime_file_);
                //comm.Parameters.AddWithValue("@casting_start_datetime", start_time);
                comm.Parameters.AddWithValue("@casting_start_datetime", datetimenow);
                comm.Parameters.AddWithValue("@machine_model", data[1]);
                comm.Parameters.AddWithValue("@serial_no", data[2]);
                comm.Parameters.AddWithValue("@alloy", data[3]);
                comm.Parameters.AddWithValue("@weight", data[4]);
                comm.Parameters.AddWithValue("@flask_temp", data[5]);
                comm.Parameters.AddWithValue("@recipe_no", data[6]);

                comm.Parameters.AddWithValue("@recipe_name", data[7]);
                comm.Parameters.AddWithValue("@auto_pour", data[8]);
                comm.Parameters.AddWithValue("@after_mode", data[9]);
                comm.Parameters.AddWithValue("@mixing", data[10]);
                comm.Parameters.AddWithValue("@control", data[11]);
                comm.Parameters.AddWithValue("@degas", data[12]);
                comm.Parameters.AddWithValue("@cast_set_temp", data[13]);
                comm.Parameters.AddWithValue("@cast_pour_temp", data[14]);
                comm.Parameters.AddWithValue("@vacuum_time", data[15]);
                comm.Parameters.AddWithValue("@m_gas", data[16]);

                comm.Parameters.AddWithValue("@oxy", data[17]);
                comm.Parameters.AddWithValue("@oxy_timing", data[18]);
                comm.Parameters.AddWithValue("@suction_upper", data[19]);
                comm.Parameters.AddWithValue("@suction_lower", data[20]);
                comm.Parameters.AddWithValue("@pour_delay", data[21]);
                comm.Parameters.AddWithValue("@flask_up_timing", data[22]);
                comm.Parameters.AddWithValue("@press", data[23]);
                comm.Parameters.AddWithValue("@press_open", data[24]);
                comm.Parameters.AddWithValue("@charge_tank_value_at_press", data[25]);
                comm.Parameters.AddWithValue("@open_time", data[26]);

                comm.Parameters.AddWithValue("@c_tempprm_p", data[27]);
                comm.Parameters.AddWithValue("@c_tempprm_i", data[28]);
                comm.Parameters.AddWithValue("@c_tempprm_d", data[29]);
                comm.Parameters.AddWithValue("@c_temp_keep_time", data[30]);
                comm.Parameters.AddWithValue("@casting_zone", data[31]);
                comm.Parameters.AddWithValue("@max_power1", data[32]);
                comm.Parameters.AddWithValue("@max_power2", data[33]);
                comm.Parameters.AddWithValue("@max_power3", data[34]);
                comm.Parameters.AddWithValue("@max_power1_temp", data[35]);
                comm.Parameters.AddWithValue("@max_power2_temp", data[36]);

                comm.Parameters.AddWithValue("@preheat_power", data[37]);
                comm.Parameters.AddWithValue("@preheat_time", data[38]);
                comm.Parameters.AddWithValue("@lift_time_for_stopper", data[39]);
                comm.Parameters.AddWithValue("@pre_press_for_degas", data[40]);
                comm.Parameters.AddWithValue("@delay_time_for_press", data[41]);
                comm.Parameters.AddWithValue("@protection_gas", data[42]);

                comm.Parameters.AddWithValue("@last_modified", last_modified_);
                comm.Parameters.AddWithValue("@cycle_time_mm_ss", cycle_time);

                comm.Parameters.AddWithValue("@serial", serial_);
                comm.Parameters.AddWithValue("@machinetype", machine_type_);

                comm.ExecuteNonQuery();
                conn.Close();

                Debug.WriteLine("Inserted K2NEXT Log");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Insert_Log_K2NEXT --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Insert_Update_Status_Altima_maintenace(string ip_, string status_)
        {
            try
            {

                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();


                string sql = "INSERT INTO tr_status(ipaddress, altima_maintenance) VALUES(@ipaddress, @altima_maintenance)" +
                    " ON DUPLICATE KEY UPDATE altima_maintenance=@altima_maintenance;";

                comm.CommandText = sql;

                comm.Parameters.AddWithValue("@ipaddress", ip_);
                comm.Parameters.AddWithValue("@altima_maintenance", int.Parse(status_));
                comm.ExecuteNonQuery();
                conn.Close();

                Debug.WriteLine("Inserted or Update Table tr_status");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Insert_Updzate_Status_Altima_maintenace --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Insert_Update_Status_RUN_IDLE(string ip_, bool run_, bool idle_, string serial_, string machine_type_)
        {
            try
            {

                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();


                string sql = "INSERT INTO tr_status(ipaddress, run, idle, serial, machinetype) VALUES(@ipaddress, @run, @idle, @serial, @machinetype)" +
                    " ON DUPLICATE KEY UPDATE run=@run, idle=@idle;";

                comm.CommandText = sql;

                comm.Parameters.AddWithValue("@ipaddress", ip_);
                comm.Parameters.AddWithValue("@run", run_);
                comm.Parameters.AddWithValue("@idle", idle_);
                comm.Parameters.AddWithValue("@serial", serial_);
                comm.Parameters.AddWithValue("@machinetype", machine_type_);
                comm.ExecuteNonQuery();
                conn.Close();

                //Debug.WriteLine("Inserted or Update RUN=" + run_.ToString() + " IDLE=" + idle_.ToString() + " Table tr_status");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                //MessageBox.Show("Insert_Update_Status_RUN_IDLE --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Insert_Update_Counter(string serial_, string machine_type_, string ip_, int counter_)
        {
            try
            {

                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();


                string sql = "INSERT INTO tr_status(ipaddress, counter, updated_time, serial, machinetype) VALUES(@ipaddress, @counter, @updated_time, @serial, @machinetype)" +
                    " ON DUPLICATE KEY UPDATE counter=@counter, updated_time=@updated_time";

                comm.CommandText = sql;

                comm.Parameters.AddWithValue("@ipaddress", ip_);
                comm.Parameters.AddWithValue("@counter", counter_.ToString());
                comm.Parameters.AddWithValue("@updated_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                comm.Parameters.AddWithValue("@serial", serial_);
                comm.Parameters.AddWithValue("@machinetype", machine_type_);
                comm.ExecuteNonQuery();
                conn.Close();

                //textBox12.Text += ip_ + " --> " + counter_.ToString() + " Qty.\r\n";

                Debug.WriteLine("Inserted or Update " + counter_.ToString() + " Table tr_status");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                //MessageBox.Show("Insert_Update_Counter --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Insert_Update_HeartBeat()
        {
            try
            {
                // Epoh time
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                int secondsSinceEpoch = (int)t.TotalSeconds;


                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();


                string sql = "INSERT INTO tr_status(heartbeat_epoh, heartbeat_local) VALUES(@heartbeat_epoh, @heartbeat_local)" +
                    " ON DUPLICATE KEY UPDATE heartbeat_epoh=@heartbeat_epoh, heartbeat_local=@heartbeat_local";

                comm.CommandText = sql;

                comm.Parameters.AddWithValue("@heartbeat_epoh", secondsSinceEpoch.ToString());
                comm.Parameters.AddWithValue("@heartbeat_local", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                comm.ExecuteNonQuery();
                conn.Close();

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                //MessageBox.Show(ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Insert_Update_Status_WARNING_ERROR(string ip_, bool warning_, bool error_)
        {
            try
            {

                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();


                string sql = "INSERT INTO tr_status(ipaddress, warning, err) VALUES(@ipaddress, @warning, @err)" +
                    " ON DUPLICATE KEY UPDATE warning=@warning, err=@err;";

                comm.CommandText = sql;

                comm.Parameters.AddWithValue("@ipaddress", ip_);
                comm.Parameters.AddWithValue("@warning", warning_);
                comm.Parameters.AddWithValue("@err", error_);
                comm.ExecuteNonQuery();
                conn.Close();

                Debug.WriteLine("Inserted or Update WARNING ERROR Table tr_status");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Insert_Update_Status_WARNING_ERROR --------> "+ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Insert_Update_Status_ONLINE(string ip_, bool online_, bool offline_, string serial_, string machine_type_)
        {
            try
            {

                //-------------------- ONLINE
                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();


                string sql_online = "INSERT INTO tr_status(ipaddress, online, offline, serial, machinetype) VALUES(@ipaddress, @online, @offline, @serial, @machinetype)" +
                    " ON DUPLICATE KEY UPDATE online=@online ,offline=@offline;";

                comm.CommandText = sql_online;

                comm.Parameters.AddWithValue("@ipaddress", ip_);
                comm.Parameters.AddWithValue("@online", online_);
                comm.Parameters.AddWithValue("@offline", offline_);
                comm.Parameters.AddWithValue("@serial", serial_);
                comm.Parameters.AddWithValue("@machinetype", machine_type_);
                comm.ExecuteNonQuery();
                conn.Close();

                if(online_ == true)
                {
                    Debug.WriteLine("IP: " + ip_ + "  -------> ONLINE");
                }
                else
                {
                    Debug.WriteLine("IP: " + ip_ + "  -------> OFFLINE");
                }
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                //MessageBox.Show("Insert_Update_Status_ONLINE --------> "+ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Reset_To_IDLE()
        {
            try
            {

                //-------------------- ONLINE
                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();

                string sql = "UPDATE tr_status SET online = 0, offline = 0 , run = 0 , idle = 0 WHERE run = 1";

                comm.CommandText = sql;

                comm.ExecuteNonQuery();
                conn.Close();

                Debug.WriteLine("Reset ONLINE/OFFLINE Table tr_status");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                //MessageBox.Show("Reset_To_IDLE --------> "+ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Insert_Timeline_ALTIMA(string serial_, string machine_type_, string ip_, string filename_, string status_, string datetime_start_, string datetime_end_, string last_modified_, string timenow_, string diff_time_)
        {
            try
            {
                DateTime s = Convert.ToDateTime(last_modified_, CultureInfo.InvariantCulture);
                last_modified_ = s.ToString("yyyy-MM-dd HH:mm:ss");


                MySqlConnection conn = new MySqlConnection(connStr);
                conn.Open();
                MySqlCommand comm = conn.CreateCommand();

                string sql = "INSERT INTO tr_altima_status (ipaddress, filename, status, datetime_start, datetime_end, last_modified, current_time_, different_time_mins, serial, machinetype) " +
                                                   "VALUES(@ipaddress, @filename, @status, @datetime_start, @datetime_end, @last_modified, @current_time_, @different_time_mins, @serial, @machinetype);";

                comm.CommandText = sql;

                comm.Parameters.AddWithValue("@ipaddress", ip_);
                comm.Parameters.AddWithValue("@filename", filename_);
                comm.Parameters.AddWithValue("@status", status_);
                comm.Parameters.AddWithValue("@datetime_start", datetime_start_);
                comm.Parameters.AddWithValue("@datetime_end", datetime_end_);
                comm.Parameters.AddWithValue("@last_modified", last_modified_);
                comm.Parameters.AddWithValue("@current_time_", timenow_);
                comm.Parameters.AddWithValue("@different_time_mins", diff_time_);
                comm.Parameters.AddWithValue("@serial", serial_);
                comm.Parameters.AddWithValue("@machinetype", machine_type_);
                comm.ExecuteNonQuery();
                conn.Close();

                Debug.WriteLine("Inserted Table tr_altima_status");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Insert_Timeline_ALTIMA --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string Get_last_ID_DB(string tablename, string filename, string ip)
        {
            var result = string.Empty;
            try
            {
                string Query = "SELECT id FROM " + tablename + " WHERE filename='" + filename + "' AND ipaddress='" + ip + "' ORDER BY id desc LIMIT 1;";

                MySqlConnection Conn = new MySqlConnection(connStr);
                MySqlCommand cmd = new MySqlCommand(Query, Conn);

                Conn.Open();

                var last_id = cmd.ExecuteScalar();

                if (last_id != DBNull.Value) // Case where the DB value is null
                {
                    result = Convert.ToString(last_id);                                                        
                }

                Conn.Close();

            }
            catch (Exception ex)
            {
                //MessageBox.Show("Getlast_ID_DB --------> " + ex.ToString());
            }

            return result;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string[] Get_Counter_UpdateTime(string tablename, string filename, string ip)
        {
            string[] result = new string[2];
            try
            {

                using (MySqlConnection connection = new MySqlConnection(connStr))
                {
                    connection.Open();

                    string Query = "SELECT counter, updated_time FROM " + tablename + " WHERE ipaddress='" + ip + "';";

                    using (MySqlCommand command = new MySqlCommand(Query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {

                                result[0] = reader["counter"].ToString();
                                result[1] = reader["updated_time"].ToString();
                            }
                        }
                    }

                    connection.Close();
                }

            }
            catch (Exception ex)
            {
                //MessageBox.Show("Get_Counter_UpdateTime --------> "+ex.ToString());
            }

            return result;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string[] Read_Heartbeat()
        {
            string[] result = new string[2];

            try
            {            
                using (MySqlConnection connection = new MySqlConnection(connStr))
                {
                    connection.Open();

                    string Query = "SELECT heartbeat_epoh, heartbeat_local FROM tr_status WHERE serial='XXX123';";

                    using (MySqlCommand command = new MySqlCommand(Query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            
                            while (reader.Read())
                            {
                                
                                result[0] = reader["heartbeat_epoh"].ToString();
                                result[1] = reader["heartbeat_local"].ToString();

                                result[0] = result[0] == null ? "-" : result[0];
                                result[1] = result[1] == null ? "-" : result[1];
                                return result;
                            }

                            
                        }
                    }

                    connection.Close();
                }

            }
            catch (Exception ex)
            {
                //MessageBox.Show("Get_Counter_UpdateTime --------> "+ex.ToString());
                result[0] = "-";
                result[1] = "-";
               

            }

            return result;
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

                //string query = "SELECT ipaddress, run, warning, err, online, altima_maintenance, counter, updated_time FROM tr_status WHERE online=1";


                string query = "SELECT " +
                    "tbl_setting.serial , tbl_setting.product_name, " +
                    "tr_status.ipaddress, tr_status.online, tr_status.run, tr_status.warning, tr_status.err, tr_status.altima_maintenance, tr_status.counter, tr_status.updated_time FROM tbl_setting " +
                    "INNER JOIN tr_status ON tbl_setting.serial = tr_status.serial WHERE online = 1;";


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
                                    dataGridView1.Rows[i].Cells[1].Value = ds.Tables[0].Rows[i]["product_name"].ToString();
                                    dataGridView1.Rows[i].Cells[2].Value = ds.Tables[0].Rows[i]["ipaddress"].ToString();
                                    dataGridView1.Rows[i].Cells[3].Value = ds.Tables[0].Rows[i]["online"].ToString();
                                    dataGridView1.Rows[i].Cells[4].Value = ds.Tables[0].Rows[i]["run"].ToString();
                                    dataGridView1.Rows[i].Cells[5].Value = ds.Tables[0].Rows[i]["warning"].ToString();
                                    dataGridView1.Rows[i].Cells[6].Value = ds.Tables[0].Rows[i]["err"].ToString();
                                    dataGridView1.Rows[i].Cells[7].Value = ds.Tables[0].Rows[i]["altima_maintenance"].ToString();
                                    dataGridView1.Rows[i].Cells[8].Value = ds.Tables[0].Rows[i]["counter"].ToString();
                                    dataGridView1.Rows[i].Cells[9].Value = ds.Tables[0].Rows[i]["updated_time"].ToString();


                                    // Change Color Status
                                    if (dataGridView1.Rows[i].Cells[4].Value.ToString() == "1")
                                    {
                                        dataGridView1.Rows[i].DefaultCellStyle.ForeColor = Color.Black;
                                        dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.Green;
                                    }

                                    if (dataGridView1.Rows[i].Cells[5].Value.ToString() == "1")
                                    {
                                        dataGridView1.Rows[i].DefaultCellStyle.ForeColor = Color.Black;
                                        dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.Yellow;
                                    }

                                    if (dataGridView1.Rows[i].Cells[6].Value.ToString() == "1")
                                    {
                                        dataGridView1.Rows[i].DefaultCellStyle.ForeColor = Color.Black;
                                        dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.Red;
                                    }

                                    // Normal
                                    if((dataGridView1.Rows[i].Cells[4].Value.ToString() == "0") && (dataGridView1.Rows[i].Cells[5].Value.ToString() == "0") && (dataGridView1.Rows[i].Cells[6].Value.ToString() == "0"))
                                    {
                                        dataGridView1.Rows[i].DefaultCellStyle.ForeColor = Color.White;
                                        dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.Gray;
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
        public void Check_Online()
        {

           

            List<string[]> serial = new List<string[]>();
            List<string[]> productName = new List<string[]>();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connStr))
                {
                    connection.Open();

                    string Query = "SELECT " + Database_ini + ".tbl_setting.product_name , " + Database_ini + ".tr_status.serial FROM " + Database_ini + ".tbl_setting " +
                        "INNER JOIN " + Database_ini + ".tr_status ON " + Database_ini + ".tbl_setting.serial = " + Database_ini + ".tr_status.serial WHERE online = 1;";

                    using (MySqlCommand command = new MySqlCommand(Query, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string[] values = new string[reader.FieldCount];

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    values[i] = reader[i].ToString();
                                }

                                serial.Add(values);
                            }
                        }
                    }
                }

                //textBox14.Text = "";

                // Display the results from the list
                foreach (string[] values in serial)
                {
                    //foreach (string value in values)
                    //{
                        /*textBox14.Text += values[0] + " --> " + values[1] + " --> Online\r\n";
                        textBox14.SelectionStart = textBox12.Text.Length;
                        textBox14.ScrollToCaret();*/
                    //}
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Check_Online --------> "+ex.ToString());
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string Get_last_status_DB(string tablename, string filename, string serial)
        {
            var result = string.Empty;
            try
            {

                string Query = "SELECT status FROM " + tablename + " WHERE filename='" + filename + "' AND serial='" + serial + "' ORDER BY id desc LIMIT 1;";

                MySqlConnection Conn = new MySqlConnection(connStr);
                MySqlCommand cmd = new MySqlCommand(Query, Conn);

                Conn.Open();

                var last_status = cmd.ExecuteScalar();

                if (last_status != DBNull.Value) // Case where the DB value is null
                {
                    result = Convert.ToString(last_status);
                }

                Conn.Close();

            }
            catch (Exception ex)
            {
                result = "4";
                //MessageBox.Show("Get_last_status_DB --------> " + ex.ToString());
            }

            return result;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Update_Timeline_ALTIMA(string serial_, string machine_type_, string ip_, string filename_, string status_, string datetime_end_, string last_modified_, string time_now_, string diff_time_)
        {
            try
            {
                DateTime s = Convert.ToDateTime(last_modified_, CultureInfo.InvariantCulture);
                last_modified_ = s.ToString("yyyy-MM-dd HH:mm:ss");

                string tablename = "tr_altima_status";
                string last_id = Get_last_ID_DB(tablename, filename_, ip_);


                if (last_id != "")
                {
                    MySqlConnection conn = new MySqlConnection(connStr);
                    conn.Open();
                    MySqlCommand comm = conn.CreateCommand();

                    string sql = "UPDATE " + tablename + " SET status = @status, datetime_end = @datetime_end, last_modified = @last_modified, current_time_ = @current_time_, different_time_mins = @different_time_mins WHERE serial=@serial AND filename=@filename AND id=@id ";
                    comm.CommandText = sql;

                    comm.Parameters.AddWithValue("@ipaddress", ip_);
                    comm.Parameters.AddWithValue("@filename", filename_);
                    comm.Parameters.AddWithValue("@id", last_id);
                    comm.Parameters.AddWithValue("@status", status_);
                    comm.Parameters.AddWithValue("@datetime_end", datetime_end_);
                    comm.Parameters.AddWithValue("@last_modified", last_modified_);
                    comm.Parameters.AddWithValue("@current_time_", time_now_);
                    comm.Parameters.AddWithValue("@different_time_mins", diff_time_);
                    comm.Parameters.AddWithValue("@serial", serial_);

                    comm.ExecuteNonQuery();
                    conn.Close();

                    Debug.WriteLine("Updated Table tr_altima_status");
                }
                else
                {
                    Debug.WriteLine("last id Altima Timeline = null ");
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Update_Timeline_ALTIMA --------> " + ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Reset_To_IDLE();

            Environment.Exit(Environment.ExitCode);
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox3.PasswordChar = checkBox1.Checked ? '\0' : '*';
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void button1_Click(object sender, EventArgs e)
        {
       




            Save_Config_Register(textBox1.Text, textBox2.Text, textBox3.Text, textBox4.Text);
            Read_Config_Register();


            connStr = "server=" + Server_ini + ";database=" + Database_ini + ";user=" + User_ini + ";password=" + Password_ini + ";SslMode=none;";


            if ((textBox1.Text != "") && (textBox2.Text != "") && (textBox3.Text != "") && (textBox4.Text != ""))
            {
                if (button1.Text == "START")
                {
                    bool res = ReadSettingDB();

                    if (res)
                    {

                        Show_Status_GridView();

                        int cnt_id = list_id.Count();

                        for (int i = 0; i < cnt_id; i++)
                        {
                            /*textBox5.Text += list_ipaddress[i] + "\r\n";
                            textBox6.Text += list_serial[i] + "\r\n";
                            textBox7.Text += list_productname[i] + "\r\n";
                            textBox8.Text += list_path_log[i] + "\r\n";
                            textBox10.Text += list_path_recipe[i] + "\r\n";*/

                            timeStart = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            timer1.Enabled = true;


                            Check_Online();

                            Start(new Action<string, string, string, string, string, string, string, string>(RunFTP), list_serial[i], list_productname[i], list_machinetype[i],  list_ipaddress[i], list_ftp_username[i], list_ftp_password[i], list_path_log[i], list_path_recipe[i]);
                        }


                        //Start(new Action<string, string, string, string, string, string, string>(RunFTP), "ABC123", "ALTIMA", "192.168.2.100", "pi", "dct@123", "/home/pi/log/", "/home/pi/recipe/");  // Test
                        //Start(new Action<string, string, string, string, string, string, string>(RunFTP),  "ABC456", "K2NEXT-16000", "192.168.2.100", "pi", "dct@123", "/home/pi/", "/home/pi/");  // Test
                        //Start(new Action<string, string, string, string, string, string, string>(RunFTP), "ABC789", "RBF37", "192.168.2.100", "pi", "dct@123", "/home/pi/log/", "/home/pi/recipe/");  // Test



                        textBox1.Enabled = false;
                            textBox2.Enabled = false;
                            textBox3.Enabled = false;
                            textBox4.Enabled = false;

                            checkBox1.Enabled = false;

                        //button1.Enabled = false;

                            button1.Text = "EXIT";
                            button1.BackColor = Color.Green;

                            textBox9.Select();
                            textBox9.Focus();
                            textBox9.Text = "";

                    }
                    else
                    {
                        MessageBox.Show("Please check the inputbox.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        textBox1.Select();
                        textBox1.Focus();
                    }
                }
                else // Exit
                {
                    // Reset
                    Reset_To_IDLE();

                    Environment.Exit(Environment.ExitCode);
                }
            }
            else
            {
                MessageBox.Show("Please fill out the inputbox completely.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox1.Select();
                textBox1.Focus();
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void button2_Click(object sender, EventArgs e)
        {
            

            textBox9.Text = "";
            textBox11.Text = "";
            textBox12.Text = "";
            textBox13.Text = "";

            dataGridView1.DataSource = null;
            dataGridView1.Rows.Clear();
            dataGridView1.Refresh();

        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {

                Connect_Time();

                Insert_Update_HeartBeat();
                string[] result = Read_Heartbeat();

                if (result[0] != null)
                {
                    label19.Text = "Heartbeat Epoh Time: " + result[0].ToString();
                    label18.Text = "Local Time: " + result[1].ToString();

                    label20.BackColor = label20.BackColor == Color.Gray ? Color.Lime : Color.Gray;
                }

               

                if (++cnt_timer >= 30)
                {
                    cnt_timer = 0;
                    //Check_Online();
                    Show_Status_GridView();
                }
            }
            catch (Exception ex)
            {

            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        void Connect_Time()
        {
            

            DateTime d1 = DateTime.ParseExact(timeStart, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            DateTime d2 = DateTime.Now;

            TimeSpan ts = d2 - d1;
            string formatted = String.Format(@"Connect Time : {0:%d} days, {0:hh\:mm\:ss}", ts);
            label14.Text = formatted;

            

           
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
            catch(Exception ex)
            {

            }
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}
