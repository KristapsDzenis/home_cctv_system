using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace home_cctv_system
{
    public partial class Form1 : Form
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private VideoView _videoView;
        private PictureBox _overlayBox;

        private BackgroundSubtractorMOG2 _bgSubtractor;
        private bool _alertShown = false;

        private const double MotionThresholdPercent = 0.02;
        private const int SnapshotIntervalMs = 150; 

        public Form1()
        {
            InitializeComponent();

            _videoView = new VideoView { Dock = DockStyle.Fill };
            Controls.Add(_videoView);

            _overlayBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            _videoView.Controls.Add(_overlayBox);

            string vlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc", "win-x64");
            Core.Initialize(vlcPath);

            _libVLC = new LibVLC("--vout=direct3d11");
            _mediaPlayer = new MediaPlayer(_libVLC);
            _videoView.MediaPlayer = _mediaPlayer;

            string rtspUrl = "rtsp://Qz7qJX09:mYNzUjhCmjbHixGj@192.168.0.162:554/live/ch0";
            var media = new Media(_libVLC, rtspUrl, FromType.FromLocation);
            media.AddOption(":rtsp-tcp");
            media.AddOption(":network-caching=500");
            _mediaPlayer.Play(media);

            _bgSubtractor = BackgroundSubtractorMOG2.Create(history: 200, varThreshold: 25, detectShadows: false);

            Task.Run(() => MotionDetectionLoop());
        }

        private async Task MotionDetectionLoop()
        {
            int warmupFrames = 0;

            while (!IsDisposed)
            {
                try
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
                    _mediaPlayer.TakeSnapshot(0, tempPath, 320, 0);

                    int attempts = 0;
                    while (!File.Exists(tempPath) && attempts < 10)
                    {
                        await Task.Delay(20);
                        attempts++;
                    }

                    if (!File.Exists(tempPath))
                        continue;

                    using var mat = Cv2.ImRead(tempPath, ImreadModes.Color);
                    File.Delete(tempPath);
                    if (mat.Empty())
                        continue;

                    Cv2.Resize(mat, mat, new OpenCvSharp.Size(320, mat.Height * 320 / mat.Width));

                    using var fgMask = new Mat();
                    _bgSubtractor.Apply(mat, fgMask);

                    // Skip first few frames while background model stabilizes
                    if (warmupFrames < 10)
                    {
                        warmupFrames++;
                        await Task.Delay(SnapshotIntervalMs);
                        continue;
                    }

                    // Clean noise
                    Cv2.Threshold(fgMask, fgMask, 200, 255, ThresholdTypes.Binary);
                    Cv2.MorphologyEx(fgMask, fgMask, MorphTypes.Open, Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)));

                    var contours = Cv2.FindContoursAsArray(fgMask, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    bool motionDetected = false;
                    var output = mat.Clone();

                    foreach (var contour in contours)
                    {
                        var rect = Cv2.BoundingRect(contour);
                        if (rect.Width * rect.Height > 500) // ignore tiny regions
                        {
                            motionDetected = true;
                            Cv2.Rectangle(output, rect, new Scalar(0, 0, 255), 2);
                        }
                    }

                    using Bitmap bmp = BitmapConverter.ToBitmap(output);
                    _overlayBox.Invoke(() =>
                    {
                        _overlayBox.Image?.Dispose();
                        _overlayBox.Image = new Bitmap(bmp);

                        if (motionDetected && !_alertShown)
                        {
                            _alertShown = true;
                            MessageBox.Show("🚨 Motion detected!", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else if (!motionDetected)
                        {
                            _alertShown = false;
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MotionLoop] {ex.Message}");
                }

                await Task.Delay(SnapshotIntervalMs);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            base.OnFormClosing(e);
        }
    }
}