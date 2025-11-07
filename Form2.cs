using System;
using System.Drawing;
using System.Windows.Forms;

namespace home_cctv_system
{
    public partial class Form2 : Form
    {
        private PictureBox _displayBox;

        public Form2()
        {
            InitializeComponent();

            _displayBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Black
            };

            Controls.Add(_displayBox);
        }

        public void UpdateFrame(Image frame)
        {
            if (IsDisposed) return;

            _displayBox.Invoke(() =>
            {
                _displayBox.Image?.Dispose();
                _displayBox.Image = new Bitmap(frame);
            });
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            Form1.Instance.Form2Opened = false;
        }
    }
}
