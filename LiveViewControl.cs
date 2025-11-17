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
        private LibVLC _relaylibVLC1;
        private MediaPlayer _relaymediaPlayer1;
        private LibVLC _relaylibVLC2;
        private MediaPlayer _relaymediaPlayer2;

        // Playback VLC instances (for UI)
        private LibVLC _libVLC1;
        private LibVLC _libVLC2;
        private MediaPlayer _mediaPlayer1;
        private MediaPlayer _mediaPlayer2;

        // Recorder/snapshot VLC instances (independent)
        private LibVLC _libVLC3;
        private LibVLC _libVLC4;
        private MediaPlayer _mediaPlayer3; // used for snapshots & can be used to spawn recording media
        private MediaPlayer _mediaPlayer4;

        // Volume bars
        private TrackBar volumeBar1;
        private TrackBar volumeBar2;

        private VideoView videoView1;
        private VideoView videoView2;

        // Motion detection
        private BackgroundSubtractorMOG2 _bgSubtractor1;
        private BackgroundSubtractorMOG2 _bgSubtractor2;
        private const int SnapshotIntervalMs = 200;
        private int warm_up_counter = 10;
        private int pixel_trashhold = 200;
        private int motion_rectangle_trashhlod = 500;

        // Camera paths
        private string cam1_path = "rtsp://XR1skfPY:5JSy8HsbOqz6AJRA@192.168.0.28:554/live/ch0";
        private string cam2_path = "rtsp://Qz7qJX09:mYNzUjhCmjbHixGj@192.168.0.162:554/live/ch0";

        // Recording flags
        private bool recording1 = false;
        private bool recording2 = false;
        private bool _running = true;

        // Optional forms
        public bool Form2Opened { get; private set; } = false;
        public bool Form3Opened { get; private set; } = false;
        private Form2 _form2;
        private Form3 _form3;

        // Events to notify MainForm
        public event Action ShowRecordedViewRequested;
        public bool ForceMuted { get; set; } = false;

        public LiveViewControl()
        {
            InitializeComponent();
            InitializeLiveView();
        }

        private void InitializeLiveView()
        {
            // Initialize native LibVLC (ensure libvlc folder is present)
            string vlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc", "win-x64");
            Core.Initialize(vlcPath);

            _relaylibVLC1 = new LibVLC("--vout=direct3d11", "--no-osd", "--no-stats");
            _relaylibVLC2 = new LibVLC("--vout=direct3d11", "--no-osd", "--no-stats");

            // Playback instances (used by VideoView controls)
            _libVLC1 = new LibVLC("--vout=direct3d11", "--no-osd", "--no-stats");
            _libVLC2 = new LibVLC("--vout=direct3d11", "--no-osd", "--no-stats");

            // Recorder / snapshot instances (independent)
            _libVLC3 = new LibVLC("--vout=direct3d11", "--no-osd", "--no-video-title-show", "--no-stats");
            _libVLC4 = new LibVLC("--vout=direct3d11", "--no-osd", "--no-video-title-show", "--no-stats");

            _relaymediaPlayer1 = new MediaPlayer(_relaylibVLC1);
            _relaymediaPlayer2 = new MediaPlayer(_relaylibVLC2);

            // create playback media players (for UI)
            _mediaPlayer1 = new MediaPlayer(_libVLC1);
            _mediaPlayer2 = new MediaPlayer(_libVLC2);
            videoView1.MediaPlayer = _mediaPlayer1;
            videoView2.MediaPlayer = _mediaPlayer2;

            // create recorder/snapshot media players (independent)
            _mediaPlayer3 = new MediaPlayer(_libVLC3); // used for snapshots + independent stream
            _mediaPlayer4 = new MediaPlayer(_libVLC4);

            
            var relayMedia1 = new Media(_relaylibVLC1, cam1_path, FromType.FromLocation);
            relayMedia1.AddOption(":sout=#duplicate{dst=http{mux=ts,dst=:9101}}");
            relayMedia1.AddOption(":sout-keep");
            relayMedia1.AddOption(":rtsp-tcp");
            relayMedia1.AddOption(":network-caching=2000");

            var relayMedia2 = new Media(_relaylibVLC2, cam2_path, FromType.FromLocation);
            relayMedia2.AddOption(":sout=#duplicate{dst=http{mux=ts,dst=:9102}}");
            relayMedia2.AddOption(":sout-keep");
            relayMedia2.AddOption(":rtsp-tcp");
            relayMedia2.AddOption(":network-caching=2000");

            _relaymediaPlayer1.Play(relayMedia1);
            _relaymediaPlayer2.Play(relayMedia2);


            // Playback media objects (use libvlc1/2)
            var mediaPlayback1 = new Media(_libVLC1, "http://127.0.0.1:9101", FromType.FromLocation);
            mediaPlayback1.AddOption(":rtsp-tcp");
            mediaPlayback1.AddOption(":network-caching=2000");

            var mediaPlayback2 = new Media(_libVLC2, "http://127.0.0.1:9102", FromType.FromLocation);
            mediaPlayback2.AddOption(":rtsp-tcp");
            mediaPlayback2.AddOption(":network-caching=2000");

            
            // Recorder/snapshot media objects (use libvlc3/4) - these run continuously to provide snapshots
            var mediaRecorderStream1 = new Media(_libVLC3, "http://127.0.0.1:9101", FromType.FromLocation);
            mediaRecorderStream1.AddOption(":rtsp-tcp");
            mediaRecorderStream1.AddOption(":network-caching=2000");

            var mediaRecorderStream2 = new Media(_libVLC4, "http://127.0.0.1:9102", FromType.FromLocation);
            mediaRecorderStream2.AddOption(":rtsp-tcp");
            mediaRecorderStream2.AddOption(":network-caching=2000");

            // Start playback (UI)
            _mediaPlayer1.Play(mediaPlayback1);
            _mediaPlayer2.Play(mediaPlayback2);
            _mediaPlayer1.Mute = false;
            _mediaPlayer2.Mute = false;

            // Start recorder/snapshot streams (these are separate players)
            _mediaPlayer3.Play(mediaRecorderStream1);
            _mediaPlayer4.Play(mediaRecorderStream2);

            // Motion detection subtractors
            _bgSubtractor1 = BackgroundSubtractorMOG2.Create(300, 25, false);
            _bgSubtractor2 = BackgroundSubtractorMOG2.Create(300, 25, false);

            // Wait until both recorder players actually start playing frames
            _mediaPlayer3.Playing += (s, e) =>
            {
                Task.Run(() => MotionDetectionLoop(_mediaPlayer3, _bgSubtractor1, 1));
            };

            _mediaPlayer4.Playing += (s, e) =>
            {
                Task.Run(() => MotionDetectionLoop(_mediaPlayer4, _bgSubtractor2, 2));
            };
        }

        private async Task MotionDetectionLoop(MediaPlayer workerPlayer, BackgroundSubtractorMOG2 bg, int camId)
        {
            int warmup = 0;

            while (_running && !IsDisposed)
            {
                try
                {

                    // Use the workerPlayer (recorder / snapshot player) for snapshots
                    string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
                    workerPlayer.TakeSnapshot(0, tempPath, 320, 0);

                    using var mat = Cv2.ImRead(tempPath, ImreadModes.Color);
                    try { File.Delete(tempPath); } catch { /* ignore */ }

                    if (mat.Empty())
                    {
                        await Task.Delay(SnapshotIntervalMs);
                        continue;
                    }

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
                            string output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"video_recordings\\cam1\\camera1_{DateTime.Now:yyyyMMdd_HHmmss}.mkv");
                            _ = Task.Run(async () => { await RecordClip(cam1_path, output, 30, 1); recording1 = false; });
                        }
                        if (camId == 2 && !recording2)
                        {
                            recording2 = true;
                            string output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"video_recordings\\cam2\\camera2_{DateTime.Now:yyyyMMdd_HHmmss}.mkv");
                            _ = Task.Run(async () => { await RecordClip(cam2_path, output, 30, 2); recording2 = false; });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Swallowing exceptions is dangerous; at least log to console for debugging.
                    Console.WriteLine($"MotionDetectionLoop error (cam {camId}): {ex.Message}");
                }

                await Task.Delay(SnapshotIntervalMs);
            }
        }

        private async Task RecordClip(string rtspUrl, string outputPath, int seconds, int camId)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                outputPath = Path.ChangeExtension(outputPath, ".mkv");

                // Choose the recorder's LibVLC (libVLC3/libVLC4) - we will create a fresh Media object on that instance.
                LibVLC selectedVlc = camId == 1 ? _libVLC3 : _libVLC4;

                string relay = camId == 1 ? "http://127.0.0.1:9101" : "http://127.0.0.1:9102";
                using var media = new Media(selectedVlc, relay, FromType.FromLocation);
                media.AddOption(":rtsp-tcp");
                media.AddOption(":network-caching=1000");

                // Use VLC streaming output to write file (copy streams)
                // Mux to mkv container
                media.AddOption($":sout=#duplicate{{dst=std{{access=file,mux=mkv,dst=\"{outputPath}\"}}}}");
                media.AddOption(":sout-keep");
                media.AddOption(":sout-all");

                using var recorderPlayer = new MediaPlayer(selectedVlc);

                // Start recorder on the recorderPlayer
                var playOk = recorderPlayer.Play(media);
                if (!playOk)
                {
                    Console.WriteLine($"Recorder failed to start for cam{camId}");
                }

                // Wait requested seconds (non-blocking)
                await Task.Delay(seconds * 1000);

                // Stop and dispose recorder
                recorderPlayer.Stop();
                recorderPlayer.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RecordClip error (cam {camId}): {ex.Message}");
            }
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
            ShowRecordedViewRequested?.Invoke();
        }

        public void DisposePlayers()
        {
            _running = false;

            try
            {
                _mediaPlayer1?.Stop();
                _mediaPlayer2?.Stop();
                _mediaPlayer3?.Stop();
                _mediaPlayer4?.Stop();
            }
            catch { /* ignore */ }

            _mediaPlayer1?.Dispose();
            _mediaPlayer2?.Dispose();
            _mediaPlayer3?.Dispose();
            _mediaPlayer4?.Dispose();

            _libVLC1?.Dispose();
            _libVLC2?.Dispose();
            _libVLC3?.Dispose();
            _libVLC4?.Dispose();
        }
    }
}