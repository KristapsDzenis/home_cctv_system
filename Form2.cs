using System;
using System.Drawing;
using System.Windows.Forms;

namespace home_cctv_system
{
    public partial class Form2 : Form
    {
        private PictureBox _displayBox;             // picture box for display

        public Form2()
        {
            InitializeComponent();// initialize all components

            // display box layout
            _displayBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Black
            };

            Controls.Add(_displayBox);
        }

        // method to pass current processed bitmap frame into display box
        public void UpdateFrame(Image frame)
        {
            if (IsDisposed) return;

            _displayBox.Invoke(() =>
            {
                _displayBox.Image?.Dispose();
                _displayBox.Image = new Bitmap(frame);
            });
        }

        // function for action when form2 is closing
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            Form1.Instance.Form2Opened = false;
        }
    }
}
