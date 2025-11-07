using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace home_cctv_system
{
    public partial class Form1 : Form
    {
        public static Form1 Instance { get; private set; }

        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer1;
        private MediaPlayer _mediaPlayer2;

        private bool _alert1Shown = false;
        private bool _alert2Shown = false;
        private bool _running = true;
        public bool Form2Opened = false;
        private Form2 _form2;
        public bool Form3Opened = false;
        private Form3 _form3;

        private BackgroundSubtractorMOG2 _bgSubtractor1;
        private BackgroundSubtractorMOG2 _bgSubtractor2;
        private const int SnapshotIntervalMs = 200;

        public Form1()
        {
            Instance = this;
            InitializeComponent();

            // LibVLC initialization
            string vlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc", "win-x64");
            Core.Initialize(vlcPath);

            _libVLC = new LibVLC("--vout=direct3d11", "--no-osd", "--no-stats");

            // UI Layout
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                RowCount = 3,
                ColumnCount = 2
            };
            this.Controls.Add(layout);
            var titleLabel1 = new Label
            {
                Text = "Garden",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 25, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var titleLabel2 = new Label
            {
                Text = "Front Door",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 25, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            layout.Controls.Add(titleLabel1, 0, 0);
            layout.Controls.Add(titleLabel2, 1, 0);
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
            button3.Click += Button_Click2;
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

            // VideoViews
            var videoView1 = new VideoView { Dock = DockStyle.Fill };
            var videoView2 = new VideoView { Dock = DockStyle.Fill };
            layout.Controls.Add(videoView1, 0, 1);
            layout.Controls.Add(videoView2, 1, 1);

            _mediaPlayer1 = new MediaPlayer(_libVLC);
            _mediaPlayer2 = new MediaPlayer(_libVLC);
            videoView1.MediaPlayer = _mediaPlayer1;
            videoView2.MediaPlayer = _mediaPlayer2;

            // Camera streams
            var media1 = new Media(_libVLC, "rtsp://XR1skfPY:5JSy8HsbOqz6AJRA@192.168.0.28:554/live/ch00", FromType.FromLocation);
            media1.AddOption(":rtsp-tcp");
            media1.AddOption(":network-caching=1000");

            var media2 = new Media(_libVLC, "rtsp://Qz7qJX09:mYNzUjhCmjbHixGj@192.168.0.162:554/live/ch0", FromType.FromLocation);
            media2.AddOption(":rtsp-tcp");
            media2.AddOption(":network-caching=1000");

            _mediaPlayer1.Play(media1);
            _mediaPlayer2.Play(media2);

            // Motion Detection Engine
            _bgSubtractor1 = BackgroundSubtractorMOG2.Create(300, 25, false);
            _bgSubtractor2 = BackgroundSubtractorMOG2.Create(300, 25, false);

            Task.Run(() => MotionDetectionLoop(_mediaPlayer1, _bgSubtractor1, 1));
            Task.Run(() => MotionDetectionLoop(_mediaPlayer2, _bgSubtractor2, 2));
        }

        private async Task MotionDetectionLoop(MediaPlayer _mediaPlayer, BackgroundSubtractorMOG2 bg, int camId)
        {
            int warmup = 0;

            while (_running && !IsDisposed)
            {
                try
                {
                    string tempPath = Path.Combine(
                        "C:\\Users\\Krist\\OneDrive\\Desktop\\snapshots",
                        Guid.NewGuid() + ".png"
                    );

                    _mediaPlayer.TakeSnapshot(0, tempPath, 320, 0);

                    using var mat = Cv2.ImRead(tempPath, ImreadModes.Color);
                    File.Delete(tempPath);

                    if (mat.Empty())
                    {
                        await Task.Delay(SnapshotIntervalMs);
                        continue;
                    }

                    Cv2.Resize(mat, mat, new OpenCvSharp.Size(320, mat.Height * 320 / mat.Width));

                    using var fgMask = new Mat();
                    bg.Apply(mat, fgMask);

                    if (warmup < 10)
                    {
                        warmup++;
                        await Task.Delay(SnapshotIntervalMs);
                        continue;
                    }

                    Cv2.Threshold(fgMask, fgMask, 200, 255, ThresholdTypes.Binary);

                    bool motion = false;
                    var contours = Cv2.FindContoursAsArray(fgMask, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    foreach (var c in contours)
                    {
                        var r = Cv2.BoundingRect(c);
                        if (r.Width * r.Height > 500)
                        {
                            motion = true;
                            Cv2.Rectangle(mat, r, new Scalar(0, 0, 255), 2);

                        }
                    }

                    using Bitmap bmp = BitmapConverter.ToBitmap(mat);

                    if (camId == 1 && Form2Opened && !_form2.IsDisposed)
                        _form2.UpdateFrame(bmp);

                    if (camId == 2 && Form3Opened && !_form3.IsDisposed)
                        _form3.UpdateFrame(bmp);

                    bool alertShown = (camId == 1) ? _alert1Shown : _alert2Shown;

                    if (motion && !alertShown)
                    {
                        if (camId == 1) _alert1Shown = true;
                        else _alert2Shown = true;

                        this.Invoke(() =>
                        {
                            MessageBox.Show($"🚨 Camera {camId}: Motion detected!", "Alert",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        });
                    }
                    else if (!motion)
                    {
                        if (camId == 1) _alert1Shown = false;
                        else _alert2Shown = false;
                    }

                }
                catch { }

                await Task.Delay(SnapshotIntervalMs);
            }
        }

        private void Button_Click(object sender, EventArgs e)
        {
            if (_form2 == null || _form2.IsDisposed)
                _form2 = new Form2();

            Form2Opened = true;
            _form2.Show();
        }

        private void Button_Click2(object sender, EventArgs e)
        {
            if (_form3 == null || _form3.IsDisposed)
                _form3 = new Form3();

            Form3Opened = true;
            _form3.Show();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _running = false;

            _mediaPlayer1?.Stop();
            _mediaPlayer2?.Stop();

            _mediaPlayer1?.Dispose();
            _mediaPlayer2?.Dispose();
            _libVLC?.Dispose();

            base.OnFormClosing(e);
        }
    }
}