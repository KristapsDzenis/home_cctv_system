using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.DataFormats;



namespace home_cctv_system
{
    public partial class LiveViewControl : UserControl
    {
        // Media player & VLC
        private LibVLC _libVLC1;
        private LibVLC _libVLC2;
        private MediaPlayer _mediaPlayer1;
        private MediaPlayer _mediaPlayer2;

        // Volume bars
        private TrackBar volumeBar1;
        private TrackBar volumeBar2;

        // Motion detection
        private BackgroundSubtractorMOG2 _bgSubtractor1;
        private BackgroundSubtractorMOG2 _bgSubtractor2;
        private const int SnapshotIntervalMs = 200;
        private int warm_up_counter = 10;
        private int pixel_trashhold = 200;
        private int motion_rectangle_trashhlod = 500;

        // Camera paths
        private string cam1_path = "rtsp://XR1skfPY:5JSy8HsbOqz6AJRA@192.168.0.28:554/live/ch00";
        private string cam2_path = "rtsp://Qz7qJX09:mYNzUjhCmjbHixGj@192.168.0.162:554/live/ch0";

        // Recording flags
        private bool recording1 = false;
        private bool recording2 = false;
        private bool _running = true;

        // References to optional video forms
        public bool Form2Opened { get; private set; } = false;
        public bool Form3Opened { get; private set; } = false;
        public bool Form4Opened { get; private set; } = false;
        private Form2 _form2;
        private Form3 _form3;

        // Events to notify MainForm
        public event Action ShowRecordedViewRequested;
        public bool ForceMuted { get; set; } = false;

        public LiveViewControl()
        {
            //InitializeComponent();
            InitializeLiveView();
        }

        private void InitializeLiveView()
        {
            // Initialize LibVLC
            string vlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc", "win-x64");
            Core.Initialize(vlcPath);
            _libVLC1 = new LibVLC("--vout=direct3d11", "--no-osd", "--no-stats");
            _libVLC2 = new LibVLC("--vout=direct3d11", "--no-osd", "--no-stats");

            // Base layout
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                RowCount = 4,
                ColumnCount = 2
            };
            this.Controls.Add(layout);

            // Titles
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

            // Control panels
            var controlPanel1 = new Panel { Dock = DockStyle.Top, BackColor = Color.LightGray };
            layout.Controls.Add(controlPanel1, 0, 2);

            var controlPanel2 = new Panel { Dock = DockStyle.Top, BackColor = Color.LightGray };
            layout.Controls.Add(controlPanel2, 1, 2);

            // Button layouts
            var buttonLayout1 = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 3 };
            layout.Controls.Add(buttonLayout1, 0, 3);

            var buttonLayout2 = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 3 };
            layout.Controls.Add(buttonLayout2, 1, 3);

            // Buttons
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

            // Layout styling
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

            // VideoViews
            var videoView1 = new VideoView { Dock = DockStyle.Fill };
            var videoView2 = new VideoView { Dock = DockStyle.Fill };
            layout.Controls.Add(videoView1, 0, 1);
            layout.Controls.Add(videoView2, 1, 1);

            _mediaPlayer1 = new MediaPlayer(_libVLC1);
            _mediaPlayer2 = new MediaPlayer(_libVLC2);
            videoView1.MediaPlayer = _mediaPlayer1;
            videoView2.MediaPlayer = _mediaPlayer2;

            // Camera streams
            var media1 = new Media(_libVLC1, cam1_path, FromType.FromLocation);
            media1.AddOption(":rtsp-tcp");
            media1.AddOption(":network-caching=2000");

            var media2 = new Media(_libVLC2, cam2_path, FromType.FromLocation);
            media2.AddOption(":rtsp-tcp");
            media2.AddOption(":network-caching=2000");

            _mediaPlayer1.Play(media1);
            _mediaPlayer2.Play(media2);
            _mediaPlayer1.Mute = false;
            _mediaPlayer2.Mute = false;

            // Motion detection
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
                    if (ForceMuted)
                    {
                        _mediaPlayer1.Mute = true;
                        _mediaPlayer2.Mute = true;
                    }

                    string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
                    _mediaPlayer.TakeSnapshot(0, tempPath, 320, 0);
                    using var mat = Cv2.ImRead(tempPath, ImreadModes.Color);
                    File.Delete(tempPath);

                    if (mat.Empty()) { await Task.Delay(SnapshotIntervalMs); continue; }
                    Cv2.Resize(mat, mat, new OpenCvSharp.Size(320, mat.Height * 320 / mat.Width));

                    using var fgMask = new Mat();
                    bg.Apply(mat, fgMask);

                    if (warmup < warm_up_counter) { warmup++; await Task.Delay(SnapshotIntervalMs); continue; }

                    Cv2.Threshold(fgMask, fgMask, pixel_trashhold, 255, ThresholdTypes.Binary);

                    bool motion = false;
                    var contours = Cv2.FindContoursAsArray(fgMask, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                    foreach (var c in contours)
                    {
                        var r = Cv2.BoundingRect(c);
                        if (r.Width * r.Height > motion_rectangle_trashhlod)
                        {
                            motion = true;
                            Cv2.Rectangle(mat, r, new Scalar(0, 0, 255), 2);
                        }
                    }

                    using Bitmap bmp = BitmapConverter.ToBitmap(mat);

                    // Forward frames to optional forms
                    if (camId == 1 && Form2Opened && _form2 != null && !_form2.IsDisposed) _form2.UpdateFrame(bmp);
                    if (camId == 2 && Form3Opened && _form3 != null && !_form3.IsDisposed) _form3.UpdateFrame(bmp);

                    // Start recording if motion
                    if (motion)
                    {
                        if (camId == 1 && !recording1)
                        {
                            recording1 = true;
                            string output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"video_recordings\\cam1\\camera1_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
                            _ = Task.Run(async () => { await RecordClip(cam1_path, output, 30, 1); recording1 = false; });
                        }
                        if (camId == 2 && !recording2)
                        {
                            recording2 = true;
                            string output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"video_recordings\\cam2\\camera2_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
                            _ = Task.Run(async () => { await RecordClip(cam2_path, output, 30, 2); recording2 = false; });
                        }
                    }
                }
                catch { }

                await Task.Delay(SnapshotIntervalMs);
            }
        }

        private async Task RecordClip(string rtspUrl, string outputPath, int seconds, int camId)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            outputPath = Path.ChangeExtension(outputPath, ".mkv");

            LibVLC selectedVlc = camId == 1 ? _libVLC1 : _libVLC2;
            using var media = new Media(selectedVlc, rtspUrl, FromType.FromLocation);
            media.AddOption(":rtsp-tcp");
            media.AddOption(":network-caching=1000");
            media.AddOption($":sout=#duplicate{{dst=std{{access=file,mux=mkv,dst=\"{outputPath}\"}}}}");
            media.AddOption(":sout-all");
            media.AddOption(":sout-keep");

            using var recorder = new MediaPlayer(selectedVlc);
            recorder.Play(media);
            await Task.Delay(seconds * 1000);
            recorder.Stop();
        }

        // Button event handlers
        private void Button_Click(object sender, EventArgs e)
        {
            if (_form2 == null || _form2.IsDisposed) _form2 = new Form2();
            Form2Opened = true;
            _form2.Show();
        }

        private void Button_Click2(object sender, EventArgs e)
        {
            if (_form3 == null || _form3.IsDisposed) _form3 = new Form3();
            Form3Opened = true;
            _form3.Show();
        }

        private void Button_Click3(object sender, EventArgs e)
        {
 
            ShowRecordedViewRequested?.Invoke();
        }

        private void Button_Click4(object sender, EventArgs e)
        {
            // Raise event to MainForm to switch to RecordedView
            ShowRecordedViewRequested?.Invoke();
        }

        public void MuteAll(bool mute)
        {

            if (_mediaPlayer1 != null) _mediaPlayer1.Mute = mute;
            if (_mediaPlayer2 != null) _mediaPlayer2.Mute = mute;
        }

        public void ForceMute(bool mute)
        {
            if (_mediaPlayer1 != null)
            {
                _mediaPlayer1.Mute = mute;
                _mediaPlayer1.Volume = mute ? 0 : volumeBar1.Value; // enforce volume level
            }

            if (_mediaPlayer2 != null)
            {
                _mediaPlayer2.Mute = mute;
                _mediaPlayer2.Volume = mute ? 0 : volumeBar2.Value; // enforce volume level
            }
        }

        public void RefreshAudio()
        {
            if (_mediaPlayer1 != null)
            {
                _mediaPlayer1.Mute = false;
                _mediaPlayer1.Volume = volumeBar1.Value;
            }

            if (_mediaPlayer2 != null)
            {
                _mediaPlayer2.Mute = false;
                _mediaPlayer2.Volume = volumeBar2.Value;
            }
        }

        public void DisposePlayers()
        {
            _running = false;
            _mediaPlayer1?.Stop();
            _mediaPlayer2?.Stop();
            _mediaPlayer1?.Dispose();
            _mediaPlayer2?.Dispose();
            _libVLC1?.Dispose();
            _libVLC2?.Dispose();
        }
    }
}
