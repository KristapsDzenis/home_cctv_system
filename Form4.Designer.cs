namespace home_cctv_system
{
    partial class Form4
    {
        // Required designer variable.
        private System.ComponentModel.IContainer components = null;

        // Clean up any resources being used.
        // <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        //  Required method for Designer support - do not modify the contents of this method with the code editor.
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;   // fonts autoscale
            this.Text = "🏠 Home CCTV Record Viewer";                       // title of window
            this.WindowState = FormWindowState.Maximized;                   // maximise window size for screen
        }
    }
}