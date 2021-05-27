using System;
using System.IO;

namespace EliteService.Utility
{
    internal class LogHelper
    {
        protected static LogHelper instance = null;

        private static object lockObj = new object();

        private string logRoot = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";


        private static string LogFile()
        {
            return DateTime.Now.ToString("yyyy-MM-dd") + ".log";
        }

        public static LogHelper GetInstance
        {
            get
            {
                if (LogHelper.instance == null)
                {
                    LogHelper.instance = new LogHelper();
                }
                return LogHelper.instance;
            }
        }

        private void CreateRoot()
        {
            if (!Directory.Exists(logRoot))
            {
                Directory.CreateDirectory(logRoot);
            }
        }


        public void Write(string action, byte[] data)
        {
            this.CreateRoot();

            string info = "";
            foreach (byte chr in data)
            {
                info += chr.ToString("X2") + " ";
            }

            string path = this.logRoot + LogFile();

            DateTime now = DateTime.Now;
            lock (lockObj)
            {
                using (StreamWriter streamWriter = new StreamWriter(path, true))
                {
                    streamWriter.WriteLine(now.ToString("HH:mm:ss") + "\t" + action + "\t" + info);
                    streamWriter.Close();
                }
            }
        }

        public void Write(string action, string str, string file = "")
        {
            this.CreateRoot();


            string path = this.logRoot + LogFile();
            if (!string.IsNullOrEmpty(file))
            {
                path = file;
            }

            DateTime now = DateTime.Now;
            lock (lockObj)
            {
                using (StreamWriter streamWriter = new StreamWriter(path, true))
                {
                    streamWriter.WriteLine(now.ToString("HH:mm:ss") + "\t" + action + "\t" + str);
                    streamWriter.Close();
                }
            }
        }
    }

}
