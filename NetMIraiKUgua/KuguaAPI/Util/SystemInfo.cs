using NvAPIWrapper.GPU;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
//using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace MMDK.Util
{
    ///  
    /// 系统信息类 - 获取CPU、内存、磁盘、进程信息 
    ///  
    public class SystemInfo
    {
     

        #region 结束指定进程 
        ///  
        /// 结束指定进程 
        ///  
        /// 进程的 Process ID 
        public static void EndProcess(int pid)
        {
            try
            {
                Process process = Process.GetProcessById(pid);
                process.Kill();
            }
            catch { }
        }


        public static void EndProcess(string processName)
        {
            string res = RunCmd($"taskkill /im {processName}.exe /f ");
            Console.WriteLine(res);
        }


        #endregion


        public static string RunCmd(string command)
        {
            //實例一個Process類，啟動一個獨立進程   
            System.Diagnostics.Process p = new System.Diagnostics.Process();

            //Process類有一個StartInfo屬性，這個是ProcessStartInfo類，包括了一些屬性和方法，下面我們用到了他的幾個屬性：   

            p.StartInfo.FileName = "cmd.exe";           //設定程序名   
            p.StartInfo.Arguments = "/c " + command;    //設定程式執行參數   
            p.StartInfo.UseShellExecute = false;        //關閉Shell的使用   
            p.StartInfo.RedirectStandardInput = true;   //重定向標準輸入   
            p.StartInfo.RedirectStandardOutput = true;  //重定向標準輸出   
            p.StartInfo.RedirectStandardError = true;   //重定向錯誤輸出   
            p.StartInfo.CreateNoWindow = true;          //設置不顯示窗口   

            p.Start();   //啟動   

            //p.StandardInput.WriteLine(command);       //也可以用這種方式輸入要執行的命令   
            //p.StandardInput.WriteLine("exit");        //不過要記得加上Exit要不然下一行程式執行的時候會當機   

            return p.StandardOutput.ReadToEnd();        //從輸出流取得命令執行結果   

        }


        //#region 查找所有应用程序标题 
        /////  
        ///// 查找所有应用程序标题 
        /////  
        ///// 应用程序标题范型 
        //public static List FindAllApps(int Handle)
        //{
        //    List Apps = new List();

        //    int hwCurr;
        //    hwCurr = GetWindow(Handle, GW_HWNDFIRST);

        //    while (hwCurr > 0)
        //    {
        //        int IsTask = (WS_VISIBLE | WS_BORDER);
        //        int lngStyle = GetWindowLongA(hwCurr, GWL_STYLE);
        //        bool TaskWindow = ((lngStyle & IsTask) == IsTask);
        //        if (TaskWindow)
        //        {
        //            int length = GetWindowTextLength(new IntPtr(hwCurr));
        //            StringBuilder sb = new StringBuilder(2 * length + 1);
        //            GetWindowText(hwCurr, sb, sb.Capacity);
        //            string strTitle = sb.ToString();
        //            if (!string.IsNullOrEmpty(strTitle))
        //            {
        //                Apps.Add(strTitle);
        //            }
        //        }
        //        hwCurr = GetWindow(hwCurr, GW_HWNDNEXT);
        //    }

        //    return Apps;
        //}
        //#endregion













        public static string GetNvidiaGpuAndMemoryUsage()
        {
            // 获取物理 GPU 的第一个实例
            var gpu = PhysicalGPU.GetPhysicalGPUs()[0];

            // 获取 GPU 使用率
            var utilization = gpu.UsageInformation;
            int gpuUsagePercent = utilization.GPU.Percentage; // 使用 GPU.Usage 获取百分比

            // 获取显存使用率
            var memoryInfo = gpu.MemoryInformation;
            float totalMemory = memoryInfo.AvailableDedicatedVideoMemoryInkB; // 总显存（MB）
            float unusedMemory = memoryInfo.CurrentAvailableDedicatedVideoMemoryInkB; // 已用显存（MB）
            float memoryUsagePercent = (1 - (unusedMemory / totalMemory)) * 100;

            // 格式化成字符串
            return $"GPU({gpuUsagePercent.ToString(".0")}%) 显存({memoryUsagePercent.ToString(".0")}%)";
        }
    }
}
