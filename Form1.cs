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
        // form 1 is set as instance
        public static Form1 Instance { get; private set; }

        private LibVLC _libVLC;                             // vlc player
        private MediaPlayer _mediaPlayer1;                  // media player  for camera 1
        private MediaPlayer _mediaPlayer2;                  // media player  for camera 2

        private bool _alert1Shown = false;                  // motion alert bool for camera 1
        private bool _alert2Shown = false;                  // motion alert bool for camera 2
        private bool _running = true;                       // generic while loop bool for motion detection                  
        public bool Form2Opened = false;                    // generic form 2 active bool
        private Form2 _form2;                               // declare form 2
        public bool Form3Opened = false;                    // generic form 3 active bool
        private Form3 _form3;                               // declare form 3

        private BackgroundSubtractorMOG2 _bgSubtractor1;    // subtractor for motion detection for camera 1( machine learning engine)
        private BackgroundSubtractorMOG2 _bgSubtractor2;    // subtractor for motion detection for camera 2( machine learning engine)

        private const int SnapshotIntervalMs = 200;         // --snapshot interval setter--
        int warm_up_counter = 10;                           // --number of test snapshots taken to read feed before motion detection--( machine learning engine)
        int pixel_trashhold = 200;                          // -- pixel trashold for motion detection (machine learning engine)-- 
        int motion_rectangle_trashhlod = 500;               // -- rectangle trashhold for motion detection (machine learning engine)--
        string cam1_path = "rtsp://XR1skfPY:5JSy8HsbOqz6AJRA@192.168.0.28:554/live/ch00";       // -- camera 1 rtsp path --
        string cam2_path = "rtsp://Qz7qJX09:mYNzUjhCmjbHixGj@192.168.0.162:554/live/ch0";       // -- camera 2 rtsp path --

        public Form1()
        {
            // set form 1 instance and innitialise all components
            Instance = this;
            InitializeComponent();

            // LibVLC initialization
            string vlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc", "win-x64");
            Core.Initialize(vlcPath);
            _libVLC = new LibVLC("--vout=direct3d11", "--no-osd", "--no-stats");

            // UI LAYOUTS
            // base layout
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                RowCount = 3,
                ColumnCount = 2
            };
            this.Controls.Add(layout);
            
            // title 1 and 2 layouts
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

            // button group layouts
            var buttonLayout1 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 3,
            };
            layout.Controls.Add(buttonLayout1, 0, 2);
            var buttonLayout2 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 3,
            };
            layout.Controls.Add(buttonLayout2, 1, 2);

            // button layouts 
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
            button2.Click += Button_Click3;
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
            button4.Click += Button_Click3;
            buttonLayout2.Controls.Add(button4, 3, 0);

            // adding and setting all layout components to form1 window
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

            // VideoView setting and injection in layout
            var videoView1 = new VideoView { Dock = DockStyle.Fill };
            var videoView2 = new VideoView { Dock = DockStyle.Fill };
            layout.Controls.Add(videoView1, 0, 1);
            layout.Controls.Add(videoView2, 1, 1);

            // MediaPlayer setting and injection in VideoView
            _mediaPlayer1 = new MediaPlayer(_libVLC);
            _mediaPlayer2 = new MediaPlayer(_libVLC);
            videoView1.MediaPlayer = _mediaPlayer1;
            videoView2.MediaPlayer = _mediaPlayer2;

            // Camera streams from RTSP camera 1 and 2
            var media1 = new Media(_libVLC, cam1_path, FromType.FromLocation);
            media1.AddOption(":rtsp-tcp");
            media1.AddOption(":network-caching=2000");

            var media2 = new Media(_libVLC, cam2_path, FromType.FromLocation);
            media2.AddOption(":rtsp-tcp");
            media2.AddOption(":network-caching=2000");

            // play stream in MediaPlayer
            _mediaPlayer1.Play(media1);
            _mediaPlayer2.Play(media2);

            // Motion Detection Engine(subtractor)( machine learning engine)
            _bgSubtractor1 = BackgroundSubtractorMOG2.Create(300, 25, false);
            _bgSubtractor2 = BackgroundSubtractorMOG2.Create(300, 25, false);

            // threads to run parallel motion detection task for each camera
            // passing into stream, subtractor, and generic camera id
            Task.Run(() => MotionDetectionLoop(_mediaPlayer1, _bgSubtractor1, 1));
            Task.Run(() => MotionDetectionLoop(_mediaPlayer2, _bgSubtractor2, 2));
        }

        // motion detection method
        private async Task MotionDetectionLoop(MediaPlayer _mediaPlayer, BackgroundSubtractorMOG2 bg, int camId)
        {
            int warmup = 0;//counter for test snapshots

            // motion detection infinte loop
            while (_running && !IsDisposed)
            {
                try
                {
                    // -- TESTING SEQUENCE START--
                    // creates path for snapshot to be created
                    string tempPath = Path.Combine("C:\\Users\\Krist\\OneDrive\\Desktop\\snapshots",Guid.NewGuid() + ".png");

                    _mediaPlayer.TakeSnapshot(0, tempPath, 320, 0);// takes snapshot

                    // reads snapshot into motion detection engine to read feed before motion detection and delete snapshot ( machine learning engine)
                    using var mat = Cv2.ImRead(tempPath, ImreadModes.Color);
                    File.Delete(tempPath);

                    // if nothing read into motion detection engine --> run from start
                    if (mat.Empty())
                    {
                        await Task.Delay(SnapshotIntervalMs);
                        continue;
                    }

                    // resize read snapshot for faster processing
                    Cv2.Resize(mat, mat, new OpenCvSharp.Size(320, mat.Height * 320 / mat.Width));

                    // detects foreground motion in read snaphot ( machine learning engine)
                    using var fgMask = new Mat();
                    bg.Apply(mat, fgMask);

                    // if warmup snapshots are less than warmup counter --> run from start
                    if (warmup < warm_up_counter)
                    {
                        warmup++;
                        await Task.Delay(SnapshotIntervalMs);
                        continue;
                    }
                    // -- TESTING SEQUENCE END--

                    // sets motion trashhold ( machine learning engine)
                    Cv2.Threshold(fgMask, fgMask, pixel_trashhold, 255, ThresholdTypes.Binary);

                    bool motion = false;// motion detection bool
                    
                    // find all outer shapes of movement shapes ( machine learning engine)
                    var contours = Cv2.FindContoursAsArray(fgMask, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    // go through all outer shapes and create smallest possible rectangle around shape ( machine learning engine)
                    foreach (var c in contours)
                    {
                        var r = Cv2.BoundingRect(c);
                        // if area of rectangle bigger than trashhold motion is detected and draw rectangle on snapshot
                        if (r.Width * r.Height > motion_rectangle_trashhlod)
                        {
                            motion = true;
                            Cv2.Rectangle(mat, r, new Scalar(0, 0, 255), 2);

                        }
                    }

                    // store snaphots as bitmap
                    using Bitmap bmp = BitmapConverter.ToBitmap(mat);

                    // if form2 is active send bitmap to form2 
                    if (camId == 1 && Form2Opened && !_form2.IsDisposed)
                        _form2.UpdateFrame(bmp);

                    // if form3 is active send bitmap to form3
                    if (camId == 2 && Form3Opened && !_form3.IsDisposed)
                        _form3.UpdateFrame(bmp);

                    bool alertShown = (camId == 1) ? _alert1Shown : _alert2Shown;// local alert shown bool declaretaion based on camera id

                    // if motion detected show alert for camera 1 and camera 2
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
                    // if no motion detected do not show alert
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

        // function to show form2 on button click
        private void Button_Click(object sender, EventArgs e)
        {
            if (_form2 == null || _form2.IsDisposed)
                _form2 = new Form2();

            Form2Opened = true;
            _form2.Show();
        }

        // function to show form3 on button click
        private void Button_Click2(object sender, EventArgs e)
        {
            if (_form3 == null || _form3.IsDisposed)
                _form3 = new Form3();

            Form3Opened = true;
            _form3.Show();
        }

        // function placeholder
        private void Button_Click3(object sender, EventArgs e)
        {
            MessageBox.Show($"Placeholder message", "Alert",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // function for action when form1 is closing
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