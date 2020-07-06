using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MetroFramework.Forms;

namespace SuspendProcess
{
    public partial class Form1 : MetroForm
    {
        public static bool isWork = false;

        public Form1()
        {
            InitializeComponent();
        }

        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);


        public static void SuspendProcess(int pid)
        {
            isWork = true;

            var process = Process.GetProcessById(pid);
            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }
                SuspendThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
        }

        public static void ResumeProcess(int pid)
        {
            var process = Process.GetProcessById(pid);
            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }
                var suspendCount = 0;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                } 
                while (suspendCount > 0);
                CloseHandle(pOpenThread);
                isWork = false;
            }
        }

        private void SuspendButton_Click(object sender, EventArgs e)
        {
            var process = Process.GetProcessesByName(ProcessNameLabel.Text)
                ?.FirstOrDefault(x => x.ProcessName == ProcessNameLabel.Text);
            if (process == null)
            {
                MessageBox.Show(ProcessNameLabel.Text + " process not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateListButton.PerformClick();
                return;
            }
            var pid = process.Id;
            SuspendProcess(pid);

            SuspendButton.Enabled = false;
            ExitButton.Enabled = false;
            listBox1.Enabled = false;
            metroTrackBar1.Enabled = false;
            UpdateListButton.Enabled = false;

            Thread.Sleep(Convert.ToInt32(metroTrackBar1.Value));
            ResumeProcess(pid);

            SuspendButton.Enabled = true;
            ExitButton.Enabled = true;
            listBox1.Enabled = true;
            metroTrackBar1.Enabled = true;
            UpdateListButton.Enabled = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            getProcess();
        }

        public void getProcess()
        {
            List<string> arrayProcess = new List<string>();
            arrayProcess.Clear();
            listBox1.Items.Clear();
            foreach (Process winProc in Process.GetProcesses())
            {
                arrayProcess.Add(winProc.ProcessName + ": " + winProc.Id);
            }
            arrayProcess.Sort();
            foreach (string item in arrayProcess)
            {
                listBox1.Items.Add(item);
            }
        }

        private void listBox1_Click(object sender, EventArgs e)
        {
            string text = listBox1.Items[listBox1.SelectedIndex].ToString();
            var result = text.Split(':')[0];
            ProcessNameLabel.Text = result.ToString();
        }

        private void UpdateListButton_Click(object sender, EventArgs e)
        {
            getProcess();
        }

        private void metroTrackBar1_Scroll(object sender, ScrollEventArgs e)
        {
            metroLabel2.Text = "Time: " + metroTrackBar1.Value.ToString();
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            try
            { Application.Exit(); }
            catch
            { Close(); }
        }
    }
}
