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
    public partial class RecordedViewControl : UserControl
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private VideoView videoPlayer;
        private FlowLayoutPanel panel;
        private TrackBar volumeBar;

        public event Action ShowLiveViewRequested;

        public RecordedViewControl(string videoFolder)
        {
            _videoFolder = videoFolder;

            InitializeComponent();
            InitializeVLC();
            LoadVideos(videoFolder);
        }

        private void InitializeComponent()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                RowCount = 3,
                ColumnCount = 1
            };
            this.Controls.Add(layout);

            videoPlayer = new VideoView { Dock = DockStyle.Fill };
            layout.Controls.Add(videoPlayer, 0, 0);

            var buttonLayout1 = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2 };
            layout.Controls.Add(buttonLayout1, 0, 1);

            var controlPanel = new Panel { Dock = DockStyle.Top, BackColor = Color.LightGray,};
            buttonLayout1.Controls.Add(controlPanel, 1, 0);

            panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(5) };
            layout.Controls.Add(panel, 0, 2);

            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 5F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            buttonLayout1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            buttonLayout1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 67F));

            var btnPlay = new Button { Text = "Play", Width = 60, Left = 10, Top = 8 };
            var btnPause = new Button { Text = "Pause", Width = 60, Left = 80, Top = 8 };
            var btnStop = new Button { Text = "Stop", Width = 60, Left = 150, Top = 8 };
            var btnBack = new Button { Text = "Back", Width = 60, Left = 150, Top = 8 };
            volumeBar = new TrackBar { Minimum = 0, Maximum = 100, Value = 50, Width = 120, Left = 220, Top = 5 };

            controlPanel.Controls.Add(btnPlay);
            controlPanel.Controls.Add(btnPause);
            controlPanel.Controls.Add(btnStop);
            buttonLayout1.Controls.Add(btnBack);
            controlPanel.Controls.Add(volumeBar);

            btnPlay.Click += (s, e) => _mediaPlayer?.Play();
            btnPause.Click += (s, e) => _mediaPlayer?.Pause();
            btnStop.Click += (s, e) => _mediaPlayer?.Stop();
            btnBack.Click += Button_Click;
            volumeBar.Scroll += (s, e) => { if (_mediaPlayer != null) _mediaPlayer.Volume = volumeBar.Value; };
        }

        private void InitializeVLC()
        {
            _libVLC = new LibVLC("--no-osd", "--no-video-title-show");
            _mediaPlayer = new MediaPlayer(_libVLC);
            videoPlayer.MediaPlayer = _mediaPlayer;
        }

        private void LoadVideos(string folder)
        {
            if (!Directory.Exists(folder)) return;
            string[] videos = Directory.GetFiles(folder, "*.mkv");

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
                using var frame = new Mat();
                capture.Read(frame);
                return !frame.Empty();
            }
            catch { return false; }
        }

        private Control CreateVideoItem(string filePath)
        {
            var card = new Panel { Width = 160, Height = 100, Margin = new Padding(5), BorderStyle = BorderStyle.FixedSingle };
            var label = new Label { Text = Path.GetFileName(filePath), Dock = DockStyle.Bottom, ForeColor = Color.White, Height = 30, TextAlign = ContentAlignment.MiddleCenter };
            var thumb = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.StretchImage, Image = GetVideoThumbnail(filePath, 200, 120) };

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
                if (capture.Read(frame))
                {
                    Cv2.Resize(frame, frame, new OpenCvSharp.Size(width, height));
                    return BitmapConverter.ToBitmap(frame);
                }
            }
            catch { }

            Bitmap fallback = new Bitmap(width, height);
            using Graphics g = Graphics.FromImage(fallback); g.Clear(Color.Black);
            return fallback;
        }

        private void PlayVideo(string videoPath)
        {
            _mediaPlayer.Stop();
            using var media = new Media(_libVLC, videoPath, FromType.FromPath);
            _mediaPlayer.Play(media);
        }

        private void Button_Click(object sender, EventArgs e)
        {
            // Raise event to MainForm to switch to LiveView
            ShowLiveViewRequested?.Invoke();
        }

        public void MuteAll(bool mute)
        {
            if (_mediaPlayer != null) _mediaPlayer.Mute = mute;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (this.Visible)
            {
                ReloadVideos();

                // Restart VLC audio output so sound works on first show
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Mute = false;

                    if (_mediaPlayer.IsPlaying)
                    {
                        // Restart the media for proper audio initialization
                        var current = _mediaPlayer.Media;
                        if (current != null)
                        {
                            _mediaPlayer.Stop();
                            _mediaPlayer.Play(current);
                        }
                    }

                }
            }
        }

        private string _videoFolder;

        private void ReloadVideos()
        {
            panel.Controls.Clear();
            LoadVideos(_videoFolder);
        }

        public void DisposePlayer()
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
        }
    }
}
