using LibVLCSharp.WinForms;

namespace home_cctv_system
{
    partial class LiveViewControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {

            // UI video views (playback)
            videoView1 = new VideoView { Dock = DockStyle.Fill };
            videoView2 = new VideoView { Dock = DockStyle.Fill };

            // Build UI
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                RowCount = 4,
                ColumnCount = 2
            };
            this.Controls.Add(layout);

            layout.Controls.Add(videoView1, 0, 1);
            layout.Controls.Add(videoView2, 1, 1);

            var titleLabel1 = new Label
            {
                Text = "Garden",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 25, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            layout.Controls.Add(titleLabel1, 0, 0);

            var titleLabel2 = new Label
            {
                Text = "Front Door",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 25, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            layout.Controls.Add(titleLabel2, 1, 0);

            var controlPanel1 = new Panel { Dock = DockStyle.Top, BackColor = Color.LightGray };
            layout.Controls.Add(controlPanel1, 0, 2);

            var controlPanel2 = new Panel { Dock = DockStyle.Top, BackColor = Color.LightGray };
            layout.Controls.Add(controlPanel2, 1, 2);

            var buttonLayout1 = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 3 };
            var buttonLayout2 = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 3 };
            layout.Controls.Add(buttonLayout1, 0, 3);
            layout.Controls.Add(buttonLayout2, 1, 3);

            var buttonMotion1 = new Button { Text = "Motion Test", Dock = DockStyle.Fill, BackColor = Color.White, Font = new Font("Segoe UI", 14F) };
            buttonMotion1.Click += Button_Click;
            buttonLayout1.Controls.Add(buttonMotion1, 0, 0);

            var buttonRecorded1 = new Button { Text = "Recorded Video", Dock = DockStyle.Fill, BackColor = Color.White, Font = new Font("Segoe UI", 14F) };
            buttonRecorded1.Click += Button_Click3;
            buttonLayout1.Controls.Add(buttonRecorded1, 1, 0);

            var buttonMotion2 = new Button { Text = "Motion Test", Dock = DockStyle.Fill, BackColor = Color.White, Font = new Font("Segoe UI", 14F) };
            buttonMotion2.Click += Button_Click2;
            buttonLayout2.Controls.Add(buttonMotion2, 0, 0);

            var buttonRecorded2 = new Button { Text = "Recorded Video", Dock = DockStyle.Fill, BackColor = Color.White, Font = new Font("Segoe UI", 14F) };
            buttonRecorded2.Click += Button_Click4;
            buttonLayout2.Controls.Add(buttonRecorded2, 1, 0);

            // Volume bars
            volumeBar1 = new TrackBar { Minimum = 0, Maximum = 100, Value = 50, Width = 500, Left = 200, Top = 5 };
            controlPanel1.Controls.Add(volumeBar1);

            volumeBar2 = new TrackBar { Minimum = 0, Maximum = 100, Value = 50, Width = 500, Left = 200, Top = 5 };
            controlPanel2.Controls.Add(volumeBar2);

            volumeBar1.Scroll += (s, e) => { if (_mediaPlayer1 != null) _mediaPlayer1.Volume = volumeBar1.Value; };
            volumeBar2.Scroll += (s, e) => { if (_mediaPlayer2 != null) _mediaPlayer2.Volume = volumeBar2.Value; };

            // layout stylings
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 5F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 86F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 4F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 5F));

            buttonLayout1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            buttonLayout1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            buttonLayout1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            buttonLayout2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            buttonLayout2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            buttonLayout2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));

        }

    }
}

