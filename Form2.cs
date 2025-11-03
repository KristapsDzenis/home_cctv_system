using LibVLCSharp.Shared;
using Microsoft.VisualBasic.ApplicationServices;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace home_cctv_system
{
    public partial class Form2 : Form
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private PictureBox _displayBox;
        private BackgroundSubtractorMOG2 _bgSubtractor;

        private bool _alertShown = false;
        private bool _running = true;

        private const int SnapshotIntervalMs = 200;
        private MediaPlayer _sharedMediaPlayer;

        public Form2(MediaPlayer sharedMediaPlayer)
        {
            InitializeComponent();
            _sharedMediaPlayer = sharedMediaPlayer;

            // --- UI Display ---
            _displayBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Black
            };
            Controls.Add(_displayBox);

            // --- Background Subtractor ---
            _bgSubtractor = BackgroundSubtractorMOG2.Create(history: 300, varThreshold: 25, detectShadows: false);

            // --- Start Motion Detection ---
            Task.Run(() => MotionDetectionLoop());
        }

        private async Task MotionDetectionLoop()
        {
            int warmupFrames = 0;

            while (_running && !IsDisposed)
            {
                try
                {
                    string tempPath = Path.Combine("C:\\Users\\Krist\\OneDrive\\Desktop\\snapshots", Guid.NewGuid() + ".png");

                    // Take snapshot from the shared MediaPlayer
                    _sharedMediaPlayer.TakeSnapshot(0, tempPath, 320, 0);


                    using var mat = Cv2.ImRead(tempPath, ImreadModes.Color);
                    File.Delete(tempPath);


                    Cv2.Resize(mat, mat, new OpenCvSharp.Size(320, mat.Height * 320 / mat.Width));

                    using var fgMask = new Mat();
                    _bgSubtractor.Apply(mat, fgMask);

                    // Skip warmup frames
                    if (warmupFrames < 10)
                    {
                        warmupFrames++;
                        await Task.Delay(SnapshotIntervalMs);
                        continue;
                    }

                    Cv2.Threshold(fgMask, fgMask, 200, 255, ThresholdTypes.Binary);
                    Cv2.MorphologyEx(fgMask, fgMask, MorphTypes.Open,
                        Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)));

                    var contours = Cv2.FindContoursAsArray(fgMask, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    bool motionDetected = false;
                    foreach (var contour in contours)
                    {
                        var rect = Cv2.BoundingRect(contour);
                        if (rect.Width * rect.Height > 500)
                        {
                            motionDetected = true;
                            Cv2.Rectangle(mat, rect, new Scalar(0, 0, 255), 2);
                        }
                    }

                    // Display result in PictureBox
                    using Bitmap bmp = BitmapConverter.ToBitmap(mat);
                    _displayBox.Invoke(() =>
                    {
                        _displayBox.Image?.Dispose();
                        _displayBox.Image = new Bitmap(bmp);

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
                    Console.WriteLine($"[MotionDetectionLoop] {ex.Message}");
                }

                await Task.Delay(SnapshotIntervalMs);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _running = false; // Stop loop
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
