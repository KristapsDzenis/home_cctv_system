using System;
using System.Windows.Forms;
using LibVLCSharp.Shared;

namespace home_cctv_system
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Core.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}