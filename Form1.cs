using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CanonBooth
{
    public partial class Form1 : Form
    {
        private CanonSdkWrapper _camera;
        private Timer _lvTimer;
        private CanonBoothServer _server;


        public Form1()
        {
            InitializeComponent();
            _camera = new CanonSdkWrapper();

            // setup timer để refresh liveview
            _lvTimer = new Timer();
            _lvTimer.Interval = 100; // 100ms ~ 10fps
            _lvTimer.Tick += LvTimer_Tick;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CanonBoothApp.StartServer();
            statusLabel.Text = "🚀 API server started on http://localhost:5000";
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                _camera.Connect();
                MessageBox.Show("✅ Camera connected!");

                _camera.Connect();
                _server = new CanonBoothServer(_camera);
                _server.Start("http://localhost:5000/");
                statusLabel.Text = "✅ Camera connected & API started at http://localhost:5000";
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Error: " + ex.Message);
            }
        }

        private void btnLiveView_Click(object sender, EventArgs e)
        {
            try
            {
                _camera.StartLiveView();   // 🔥 bật liveview trong máy ảnh
                _lvTimer.Start();          // 🔥 bắt đầu vòng lặp lấy frame
                statusLabel.Text = "✅ LiveView started";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "❌ Error starting LiveView: " + ex.Message;
            }
        }

        private void btnStopLiveView_Click(object sender, EventArgs e)
        {
            try
            {
                _lvTimer.Stop();
                _camera.StopLiveView();
                pictureBox1.Image = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Error stopping LiveView: " + ex.Message);
            }
        }

        private void LvTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                var bmp = _camera.GetLiveViewImage();
                if (bmp != null)
                {
                    var old = pictureBox1.Image;
                    pictureBox1.Image = bmp;
                    old?.Dispose();

                    statusLabel.Text = "✅ LiveView running...";
                }
                else
                {
                    statusLabel.Text = "⌛ Waiting frame...";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = "❌ LiveView error: " + ex.Message;
            }
        }


        private void btnCapture_Click(object sender, EventArgs e)
        {
            try
            {
                _camera.CapturePhoto();
                MessageBox.Show("📸 Capture sent to camera");
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ " + ex.Message);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _lvTimer.Stop();              // dừng UI timer
            CanonBoothApp.StopServer();   // tắt server + ngắt camera
            _server?.Stop();
        }

        private void statusLabel_Click(object sender, EventArgs e)
        {

        }
    }
}
