using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace EliteService.Control
{
    public class MemoryClear
    {
        private object lockObj = new object();

        private int clearInterval = 30;

        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        public static extern int SetProcessWorkingSetSize(IntPtr process, int minSize, int maxSize);

        [DllImport("psapi.dll")]
        public static extern bool EmptyWorkingSet(IntPtr hProcess);

        /// <summary>
        /// 释放内存
        /// </summary>
        public void ClearMemory_2()
        {
            Process proc = Process.GetCurrentProcess();
            long usedMemory = proc.PrivateMemorySize64;

            Console.WriteLine(usedMemory.ToString());

            GC.Collect();
            GC.WaitForPendingFinalizers();


            usedMemory = proc.PrivateMemorySize64;

            Console.WriteLine(usedMemory.ToString());

            // 使用
            // 获取当前进程句柄
            Process pProcess = Process.GetCurrentProcess();

            // 尽可能多的清空Working Set释放内存
            bool bRes = EmptyWorkingSet(pProcess.Handle);
            if (!bRes)
            {
                Console.WriteLine("failed");
            }
        }


        /// <summary>
        /// 释放内存
        /// </summary>
        public void ClearMemory()
        {
            Process proc = Process.GetCurrentProcess();
            long usedMemory = proc.PrivateMemorySize64;

            Console.WriteLine(usedMemory.ToString());

            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
            }

            proc = Process.GetCurrentProcess();
            usedMemory = proc.PrivateMemorySize64;

            Console.WriteLine(usedMemory.ToString());
        }


        public void BeginTask()
        {

            Thread thread = new Thread(new ThreadStart(() =>
            {
                clearAction();
            }))
            {
                IsBackground = true
            };

            thread.Start();
        }

        /// <summary>
        ///  
        /// </summary>
        private void clearAction()
        {
            while (true)
            {
                try
                {
                    Console.WriteLine("clearAction");
                    ClearMemory();

                    Thread.Sleep(this.clearInterval * 1000);
                }
                catch
                {

                }
            }
        }

    }


}
