using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace home_cctv_system
{
    public partial class Form4 : Form
    {
        private FlowLayoutPanel panel;
        private VideoView videoPlayer;
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private Panel controlPanel;
        private Button btnPlay;
        private Button btnPause;
        private Button btnStop;
        private TrackBar volumeBar;

        public Form4(string videoFolder)
        {
            InitializeComponent();

            _libVLC = new LibVLC("--no-osd", "--no-video-title-show");
            _mediaPlayer = new MediaPlayer(_libVLC);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                RowCount = 3,
                ColumnCount = 1
            };
            this.Controls.Add(layout);

            // --- Video player at the top ---
            videoPlayer = new VideoView
            {
                Dock = DockStyle.Fill,
                MediaPlayer = _mediaPlayer
            };
            layout.Controls.Add(videoPlayer, 0 , 0);

            controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = Color.LightGray
            };
            layout.Controls.Add(controlPanel, 0, 1);

            // --- Scrollable thumbnails below ---
            panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5)
            };
            layout.Controls.Add(panel, 0, 2);

            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

            btnPlay = new Button { Text = "Play", Width = 60, Left = 10, Top = 8 };
            btnPause = new Button { Text = "Pause", Width = 60, Left = 80, Top = 8 };
            btnStop = new Button { Text = "Stop", Width = 60, Left = 150, Top = 8 };

            volumeBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                Width = 120,
                Left = 220,
                Top = 5
            };

            controlPanel.Controls.Add(btnPlay);
            controlPanel.Controls.Add(btnPause);
            controlPanel.Controls.Add(btnStop);
            controlPanel.Controls.Add(volumeBar);

            btnPlay.Click += (s, e) => _mediaPlayer.Play();
            btnPause.Click += (s, e) => _mediaPlayer.Pause();
            btnStop.Click += (s, e) => _mediaPlayer.Stop();

            volumeBar.Scroll += (s, e) =>
            {
                _mediaPlayer.Volume = volumeBar.Value;
            };

            LoadVideos(videoFolder);
        }

        private void LoadVideos(string folder)
        {
            if (!Directory.Exists(folder))
                return;

            string[] videos = Directory.GetFiles(folder, "*.mp4");

            foreach (var path in videos)
            {
                if (HasPlayableFrame(path))
                    panel.Controls.Add(CreateVideoItem(path));
            }
        }

        private bool HasPlayableFrame(string filePath)
        {
            try
            {
                using var capture = new VideoCapture(filePath);
                if (!capture.IsOpened())
                    return false;

                using var frame = new Mat();
                capture.Read(frame);
                return !frame.Empty();
            }
            catch
            {
                return false;
            }
        }

        private Control CreateVideoItem(string filePath)
        {
            var card = new Panel
            {
                Width = 160,
                Height = 100,
                Margin = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle
            };

            var label = new Label
            {
                Text = Path.GetFileName(filePath),
                Dock = DockStyle.Bottom,
                ForeColor = Color.White,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Bitmap thumbnail = GetVideoThumbnail(filePath, 200, 120);

            var thumb = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = thumbnail
            };

            // click handler to play video in embedded player
            card.Click += (s, e) => PlayVideo(filePath);
            thumb.Click += (s, e) => PlayVideo(filePath);
            label.Click += (s, e) => PlayVideo(filePath);

            card.Controls.Add(thumb);
            card.Controls.Add(label);

            return card;
        }

        private Bitmap GetVideoThumbnail(string videoPath, int width, int height)
        {
            try
            {
                using var capture = new VideoCapture(videoPath);
                using var frame = new Mat();
                if (capture.Read(frame)) // grab first frame
                {
                    Cv2.Resize(frame, frame, new OpenCvSharp.Size(width, height));
                    return BitmapConverter.ToBitmap(frame);
                }
            }
            catch { }

            Bitmap fallback = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(fallback))
                g.Clear(Color.Black);
            return fallback;
        }

        private void PlayVideo(string videoPath)
        {
            _mediaPlayer.Stop();
            using var media = new Media(_libVLC, videoPath, FromType.FromPath);
            _mediaPlayer.Play(media);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
        }
    }
}
