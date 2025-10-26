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
        private MediaPlayer _mediaPlayer;

        public Form1()
        {
            InitializeComponent();

            var videoView = new VideoView { Dock = DockStyle.Fill };
            Controls.Add(videoView);

            // Auto-detect libvlc folder relative to exe
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string vlcLibPath = Path.Combine(exePath, "libvlc", "win-x64");

            if (!Directory.Exists(vlcLibPath))
            {
                MessageBox.Show("LibVLC folder not found: " + vlcLibPath);
                return;
            }

            // Initialize LibVLC with the path to native DLLs
            Core.Initialize(vlcLibPath);

            _libVLC = new LibVLC("--vout=direct3d11");
            _libVLC.Log += (s, e) => Console.WriteLine($"[{e.Level}] {e.Message}");

            _mediaPlayer = new MediaPlayer(_libVLC);
            videoView.MediaPlayer = _mediaPlayer;

            // Play RTSP stream
            string rtspUrl = "rtsp://Qz7qJX09:mYNzUjhCmjbHixGj@192.168.0.162:554/live/ch0";
            var media = new Media(_libVLC, rtspUrl, FromType.FromLocation);
            media.AddOption(":rtsp-tcp");
            media.AddOption(":network-caching=5000");

            _mediaPlayer.Play(media);
        }
    }
}