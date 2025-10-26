using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace home_cctv_system
{
    public partial class Form1 : Form
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private VideoView _videoView;

        private Bitmap _previousFrame;

        private const int BlockSize = 100;       // Size of blocks for motion detection
        private const int MotionThreshold = 500; // Pixel color difference threshold
        private const double MotionBlockPercent = 0.9; // Percentage of changed pixels per block

        public Form1()
        {
            InitializeComponent();

            // VideoView
            _videoView = new VideoView { Dock = DockStyle.Fill };
            Controls.Add(_videoView);

            // LibVLC
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string vlcLibPath = Path.Combine(exePath, "libvlc", "win-x64");
            Core.Initialize(vlcLibPath);

            _libVLC = new LibVLC("--vout=direct3d11");
            _mediaPlayer = new MediaPlayer(_libVLC);
            _videoView.MediaPlayer = _mediaPlayer;

            // RTSP
            string rtspUrl = "rtsp://Qz7qJX09:mYNzUjhCmjbHixGj@192.168.0.162:554/live/ch0";
            var media = new Media(_libVLC, rtspUrl, FromType.FromLocation);
            media.AddOption(":rtsp-tcp");
            media.AddOption(":network-caching=2000");
            _mediaPlayer.Play(media);

            // Start async motion detection
            StartMotionDetectionLoop();
        }

        private async void StartMotionDetectionLoop()
        {
            while (!IsDisposed)
            {
                try
                {
                    Bitmap frame = CaptureSnapshot();
                    if (frame != null)
                    {
                        Bitmap displayFrame = (Bitmap)frame.Clone();
                        bool motionDetected = false;

                        if (_previousFrame != null)
                        {
                            motionDetected = DetectMotionAndDrawBlocks(_previousFrame, displayFrame);
                        }

                        _previousFrame?.Dispose();
                        _previousFrame = (Bitmap)frame.Clone();
                        frame.Dispose();

                        _videoView.Invoke((Action)(() =>
                        {
                            _videoView.BackgroundImage?.Dispose();
                            _videoView.BackgroundImage = displayFrame;
                            this.Text = motionDetected ? "Motion detected!" : "Home CCTV";
                        }));
                    }
                }
                catch { }

                await Task.Delay(500);
            }
        }

        private Bitmap CaptureSnapshot()
        {
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
                _mediaPlayer.TakeSnapshot(0, tempPath, 0, 0);
                if (File.Exists(tempPath))
                {
                    var bmp = new Bitmap(tempPath);
                    File.Delete(tempPath);
                    return bmp;
                }
            }
            catch { }
            return null;
        }

        private bool DetectMotionAndDrawBlocks(Bitmap previous, Bitmap current)
        {
            bool motionDetected = false;
            int width = previous.Width;
            int height = previous.Height;

            using Graphics g = Graphics.FromImage(current);
            Pen motionPen = new Pen(Color.Red, 2);

            for (int y = 0; y < height; y += BlockSize)
            {
                for (int x = 0; x < width; x += BlockSize)
                {
                    int diffCount = 0;
                    int pixelsInBlock = 0;

                    for (int by = 0; by < BlockSize && y + by < height; by++)
                    {
                        for (int bx = 0; bx < BlockSize && x + bx < width; bx++)
                        {
                            Color p = previous.GetPixel(x + bx, y + by);
                            Color c = current.GetPixel(x + bx, y + by);
                            int delta = Math.Abs(p.R - c.R) + Math.Abs(p.G - c.G) + Math.Abs(p.B - c.B);
                            if (delta > MotionThreshold) diffCount++;
                            pixelsInBlock++;
                        }
                    }

                    if (diffCount > pixelsInBlock * MotionBlockPercent)
                    {
                        motionDetected = true;
                        g.DrawRectangle(motionPen, x, y, BlockSize, BlockSize);
                    }
                }
            }

            return motionDetected;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            _previousFrame?.Dispose();
            base.OnFormClosing(e);
        }
    }
}