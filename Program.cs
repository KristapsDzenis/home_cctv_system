using System;
using System.Windows.Forms;
using LibVLCSharp.Shared;

namespace home_cctv_system  // <-- make sure this matches your project namespace
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Initialize LibVLC (must be called before any VLC objects)
            Core.Initialize();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Run your main form
            Application.Run(new Form1());
        }
    }
}