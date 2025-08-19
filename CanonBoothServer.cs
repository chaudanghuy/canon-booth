using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace CanonBooth
{
    public class CanonBoothServer
    {
        private readonly CanonSdkWrapper _camera;
        private HttpListener _listener;
        private Thread _serverThread;

        public CanonBoothServer(CanonSdkWrapper camera)
        {
            _camera = camera;
        }

        public void Start(string url = "http://localhost:5000/")
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _listener.Start();

            _serverThread = new Thread(ServerLoop);
            _serverThread.IsBackground = true;
            _serverThread.Start();
        }

        private void ServerLoop()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    ProcessRequest(ctx);
                }
                catch { }
            }
        }

        private void ProcessRequest(HttpListenerContext ctx)
        {
            string path = ctx.Request.Url.AbsolutePath.ToLower();

            if (path == "/liveview")
            {
                try
                {
                    var bmp = _camera.GetLiveViewImage();
                    if (bmp != null)
                    {
                        using (var ms = new MemoryStream())
                        {
                            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            var buffer = ms.ToArray();

                            ctx.Response.ContentType = "image/jpeg";
                            ctx.Response.ContentLength64 = buffer.Length;
                            ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        }
                    }
                    else
                    {
                        WriteText(ctx, "No frame");
                    }
                }
                catch (Exception ex)
                {
                    WriteText(ctx, "Error: " + ex.Message);
                }
            }
            else
            {
                WriteText(ctx, "CanonBooth API running...");
            }

            ctx.Response.OutputStream.Close();
        }

        private void WriteText(HttpListenerContext ctx, string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            ctx.Response.ContentType = "text/plain";
            ctx.Response.ContentLength64 = buffer.Length;
            ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
        }

        public void Stop()
        {
            _listener?.Stop();
            _listener = null;
        }
    }
}
