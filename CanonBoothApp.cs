using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;

namespace CanonBooth
{
    public static class CanonBoothApp
    {
        private static CanonSdkWrapper _camera;
        private static Thread _serverThread;
        private static bool _running = false;

        public static void StartServer()
        {
            if (_running) return; // tránh chạy 2 lần
            _running = true;

            _serverThread = new Thread(ServerLoop);
            _serverThread.IsBackground = true;
            _serverThread.Start();
        }

        private static void ServerLoop()
        {
            try
            {
                _camera = new CanonSdkWrapper();
                _camera.Connect();
                _camera.StartLiveView();

                HttpListener listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:5000/");
                listener.Start();

                Console.WriteLine("🚀 CanonBooth API started at http://localhost:5000");

                while (_running)
                {
                    HttpListenerContext context = listener.GetContext();
                    string path = context.Request.Url.AbsolutePath.ToLower();

                    if (path == "/liveview")
                    {
                        HandleLiveView(context);
                    }
                    else if (path == "/capture")
                    {
                        HandleCapture(context);
                    }
                    else
                    {
                        using (var writer = new StreamWriter(context.Response.OutputStream))
                        {
                            writer.Write("CanonBooth API running: /liveview, /capture");
                        }
                    }

                    context.Response.OutputStream.Close();
                }

                listener.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Server error: " + ex.Message);
            }
        }

        private static void HandleLiveView(HttpListenerContext context)
        {
            try
            {
                using (var bmp = _camera.GetLiveViewImage())
                {
                    if (bmp != null)
                    {
                        using (var ms = new MemoryStream())
                        {
                            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            byte[] buffer = ms.ToArray();
                            context.Response.ContentType = "image/jpeg";
                            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        }
                    }
                    else
                    {
                        WriteText(context, "⌛ No frame");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteText(context, "❌ LiveView error: " + ex.Message);
            }
        }

        private static void HandleCapture(HttpListenerContext context)
        {
            try
            {
                _camera.CapturePhoto();
                WriteText(context, "📸 Capture triggered");
            }
            catch (Exception ex)
            {
                WriteText(context, "❌ Capture error: " + ex.Message);
            }
        }

        private static void WriteText(HttpListenerContext context, string message)
        {
            using (var writer = new StreamWriter(context.Response.OutputStream))
            {
                context.Response.ContentType = "text/plain";
                writer.Write(message);
            }
        }

        public static void StopServer()
        {
            _running = false;
            _camera?.StopLiveView();
            _camera?.Disconnect();
        }
    }
}
