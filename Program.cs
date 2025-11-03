using System;
using System.Linq;
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

            // Create both forms
            var form1 = new Form1();

            // When *any* form closes, check if all are closed
            void FormClosedHandler(object sender, FormClosedEventArgs e)
            {
                if (Application.OpenForms.Count == 0)
                {
                    Application.Exit();
                }
            }

            form1.FormClosed += FormClosedHandler;

            // Show both
            form1.Show();

            // Run message loop
            Application.Run();
        }
    }
}