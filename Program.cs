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
            // initialise application
            Core.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Create form1
            var form1 = new Form1();

            // When *any* form closes, check if all are closed
            void FormClosedHandler(object sender, FormClosedEventArgs e)
            {
                // if no forms open shutdown application
                if (Application.OpenForms.Count == 0)
                {
                    Application.Exit();
                }
            }

            // run FormClosedHandler once form1 closed
            form1.FormClosed += FormClosedHandler;

            // Show form1 on start up
            form1.Show();

            // Run message loop
            Application.Run();
        }
    }
}