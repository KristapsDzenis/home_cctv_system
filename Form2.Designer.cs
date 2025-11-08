namespace home_cctv_system
{
    partial class Form2
    {
        //  Required designer variable.
        private System.ComponentModel.IContainer components = null;

        //  Clean up any resources being used.
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
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;           // fonts autoscale
            this.Text = "Motion Detection Screen";                                  // title of window
            this.ClientSize = new System.Drawing.Size(900, 550);                    // set specific window size
        }
    }
}