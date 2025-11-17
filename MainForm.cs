using System;
using System.Windows.Forms;

namespace home_cctv_system
{
    public partial class MainForm : Form
    {
        private LiveViewControl liveView;
        private RecordedViewControl recordedView;

        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.AutoScaleMode = AutoScaleMode.Font;
            this.WindowState = FormWindowState.Maximized;
            this.Text = "Home CCTV System";

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 1
            };
            this.Controls.Add(layout);

            liveView = new LiveViewControl { Dock = DockStyle.Fill };
            recordedView = new RecordedViewControl("C:\\Users\\Krist\\Desktop\\video_recordings\\cam1") { Dock = DockStyle.Fill };

            layout.Controls.Add(liveView);
            layout.Controls.Add(recordedView);

            recordedView.Visible = false;

            // Switch to recorded view
            liveView.ShowRecordedViewRequested += () =>
            {
                recordedView.MuteAll(false);
                liveView.Visible = false;
                recordedView.Visible = true;
            };

            // Switch back to live view
            recordedView.ShowLiveViewRequested += () =>
            {
                recordedView.Visible = false;
                liveView.Visible = true;
                recordedView.MuteAll(true);
            };
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            liveView?.DisposePlayers();
            recordedView?.DisposePlayer();
            base.OnFormClosing(e);
        }
    }
}