using System;
using System.IO;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace home_cctv_system
{
    public partial class Form1 : Form
    {
        private LibVLC _libVLC;

        private MediaPlayer _mediaPlayer1;
        private MediaPlayer _mediaPlayer2;

        private VideoView _videoView1;
        private VideoView _videoView2;

        // Expose MediaPlayers if needed elsewhere
        public MediaPlayer MediaPlayer1 => _mediaPlayer1;
        public MediaPlayer MediaPlayer2 => _mediaPlayer2;

        public Form1()
        {
            InitializeComponent();

            // Initialize LibVLC
            string vlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc", "win-x64");
            Core.Initialize(vlcPath);
            _libVLC = new LibVLC("--vout=direct3d11", "--no-osd", "--no-stats");

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                RowCount = 3,
                ColumnCount = 2,
            };

            var buttonLayout1 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 3,
            };

            var buttonLayout2 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 3,
            };
            
            var titleLabel1 = new Label
            {
                Text = "Garden",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new System.Drawing.Font("Segoe UI", 25F, System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            var titleLabel2 = new Label
            {
                Text = "Front Door",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new System.Drawing.Font("Segoe UI", 25F, System.Drawing.FontStyle.Bold),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 5F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 90F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 5F));
            buttonLayout1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            buttonLayout1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            buttonLayout1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            buttonLayout2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            buttonLayout2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            buttonLayout2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            this.Controls.Add(layout);
            layout.Controls.Add(buttonLayout1, 0, 2);
            layout.Controls.Add(buttonLayout2, 1, 2);
            layout.Controls.Add(titleLabel1, 0, 0);
            layout.Controls.Add(titleLabel2, 1, 0);

            // --- Button row ---
            var button = new Button
            {
                Text = "Motion Test",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new System.Drawing.Font("Segoe UI", 14F)
            };
            button.Click += Button_Click;
            buttonLayout1.Controls.Add(button, 0, 0);
            
            var button2 = new Button
            {
                Text = "Recorded Video",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new System.Drawing.Font("Segoe UI", 14F)
            };
            button2.Click += Button_Click;
            buttonLayout1.Controls.Add(button2, 3, 0);

            var button3 = new Button
            {
                Text = "Motion Test",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new System.Drawing.Font("Segoe UI", 14F)
            };
            button3.Click += Button_Click;
            buttonLayout2.Controls.Add(button3, 0, 0);

            var button4 = new Button
            {
                Text = "Recorded Video",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new System.Drawing.Font("Segoe UI", 14F)
            };
            button4.Click += Button_Click;
            buttonLayout2.Controls.Add(button4, 3, 0);



            // --- VideoView 1 ---
            var videoView1 = new VideoView { Dock = DockStyle.Fill };
            _mediaPlayer1 = new MediaPlayer(_libVLC);
            videoView1.MediaPlayer = _mediaPlayer1;
            layout.Controls.Add(videoView1, 0, 1);

            // --- VideoView 2 ---
            var videoView2 = new VideoView { Dock = DockStyle.Fill };
            _mediaPlayer2 = new MediaPlayer(_libVLC);
            videoView2.MediaPlayer = _mediaPlayer2;
            layout.Controls.Add(videoView2, 1, 1);

            // --- RTSP streams ---
            string rtspUrl1 = "rtsp://XR1skfPY:5JSy8HsbOqz6AJRA@192.168.0.28:554/live/ch00";
            string rtspUrl2 = "rtsp://Qz7qJX09:mYNzUjhCmjbHixGj@192.168.0.162:554/live/ch0";

            var media1 = new Media(_libVLC, rtspUrl1, FromType.FromLocation);
            media1.AddOption(":rtsp-tcp");
            media1.AddOption(":network-caching=1000");
            _mediaPlayer1.Play(media1);

            var media2 = new Media(_libVLC, rtspUrl2, FromType.FromLocation);
            media2.AddOption(":rtsp-tcp");
            media2.AddOption(":network-caching=1000");
            _mediaPlayer2.Play(media2);

            // --- Optional: pass MediaPlayers to another form for motion detection ---
            var form2 = new Form2(_mediaPlayer1);
            form2.Show();
            var form3 = new Form3(_mediaPlayer2);
            form3.Show();
        }

        private void Button_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Button clicked!");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _mediaPlayer1?.Stop();
            _mediaPlayer1?.Dispose();

            _mediaPlayer2?.Stop();
            _mediaPlayer2?.Dispose();

            _libVLC?.Dispose();

            base.OnFormClosing(e);
        }
    }
}