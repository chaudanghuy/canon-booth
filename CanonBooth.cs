using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace CanonBooth
{
    public class CanonBoothApp
    {
        private static CanonSdkWrapper _camera = new CanonSdkWrapper();

        public static async Task StartServer()
        {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();

            // Endpoint test
            app.MapGet("/", () => "CanonBooth API running...");

            // Kết nối camera
            app.MapGet("/connect", () =>
            {
                try
                {
                    _camera.Connect();
                    return Results.Ok("Camera connected");
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            // Ngắt kết nối
            app.MapGet("/disconnect", () =>
            {
                try
                {
                    _camera.Disconnect();
                    return Results.Ok("Camera disconnected");
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            // Bật live view
            app.MapGet("/start-liveview", () =>
            {
                try
                {
                    _camera.StartLiveView();
                    return Results.Ok("LiveView started");
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            // Tắt live view
            app.MapGet("/stop-liveview", () =>
            {
                try
                {
                    _camera.StopLiveView();
                    return Results.Ok("LiveView stopped");
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            // Lấy hình ảnh live view
            app.MapGet("/liveview", async context =>
            {
                try
                {
                    using var bmp = _camera.GetLiveViewImage();
                    if (bmp == null)
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("No image");
                        return;
                    }

                    context.Response.ContentType = "image/jpeg";
                    using var ms = new MemoryStream();
                    bmp.Save(ms, ImageFormat.Jpeg);
                    ms.Position = 0;
                    await ms.CopyToAsync(context.Response.Body);
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(ex.Message);
                }
            });

            // Chụp ảnh
            app.MapGet("/capture", () =>
            {
                try
                {
                    _camera.CapturePhoto();
                    return Results.Ok("Photo captured");
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            await app.RunAsync("http://localhost:5000"); // API server chạy port 5000
        }
    }
}
