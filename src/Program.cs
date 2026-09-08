using System;
using System.Threading;
using System.Windows.Forms;

namespace DeepBlue
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            foreach (string a in args)
            {
                if (a == "--selftest")
                {
                    SelfTest.Run();
                    return;
                }
            }

            bool createdNew;
            Mutex mutex = null;
            try
            {
                mutex = new Mutex(true, "DeepBlue_Shenlan_SingleInstance", out createdNew);
            }
            catch (Exception)
            {
                createdNew = true;
            }
            if (!createdNew)
            {
                MessageBox.Show("深蓝已在运行中。", AppInfo.Name,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new MainForm());
            }
            finally
            {
                if (mutex != null)
                {
                    try { mutex.ReleaseMutex(); } catch (Exception) { }
                    mutex.Dispose();
                }
            }
        }
    }
}
