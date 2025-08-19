using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace CanonBooth
{
    public class CanonSdkWrapper
    {
        private IntPtr _camera = IntPtr.Zero;

        public void Connect()
        {
            uint err = EDSDK.EdsInitializeSDK();
            if (err != EDSDK.EDS_ERR_OK)
                throw new Exception($"EdsInitializeSDK failed: {err}");

            IntPtr camList;
            err = EDSDK.EdsGetCameraList(out camList);
            if (err != EDSDK.EDS_ERR_OK)
                throw new Exception("Cannot get camera list");

            err = EDSDK.EdsGetChildAtIndex(camList, 0, out _camera);
            if (err != EDSDK.EDS_ERR_OK || _camera == IntPtr.Zero)
                throw new Exception("No camera found");

            err = EDSDK.EdsOpenSession(_camera);
            if (err != EDSDK.EDS_ERR_OK)
                throw new Exception("Cannot open session with camera");

            // Giải phóng danh sách camera (theo Canon guideline)
            EDSDK.EdsRelease(camList);
        }

        public void Disconnect()
        {
            if (_camera != IntPtr.Zero)
            {
                EDSDK.EdsCloseSession(_camera);
                EDSDK.EdsRelease(_camera);
                EDSDK.EdsTerminateSDK();
                _camera = IntPtr.Zero;
            }
        }

        public void StartLiveView()
        {
            if (_camera == IntPtr.Zero)
                throw new Exception("Camera not connected");

            uint err;

            // Bật EvfMode
            uint evfMode = 1;
            err = EDSDK.EdsSetPropertyData(_camera, EDSDK.PropID_Evf_Mode, 0, Marshal.SizeOf(evfMode), evfMode);
            if (err != EDSDK.EDS_ERR_OK)
                throw new Exception("Cannot set EvfMode, error=" + err);

            // Lấy output device hiện tại
            uint device;
            err = EDSDK.EdsGetPropertyData(_camera, EDSDK.PropID_Evf_OutputDevice, 0, out device);
            if (err != EDSDK.EDS_ERR_OK)
                throw new Exception("Cannot get EvfOutputDevice, error=" + err);

            // Bật output về PC
            device |= EDSDK.EvfOutputDevice_PC;
            err = EDSDK.EdsSetPropertyData(_camera, EDSDK.PropID_Evf_OutputDevice, 0, Marshal.SizeOf(device), device);
            if (err != EDSDK.EDS_ERR_OK)
                throw new Exception("Cannot set EvfOutputDevice, error=" + err);
        }

        public void StopLiveView()
        {
            if (_camera == IntPtr.Zero)
                return;

            uint device = 0;
            EDSDK.EdsSetPropertyData(_camera, EDSDK.PropID_Evf_OutputDevice, 0, Marshal.SizeOf(device), device);
        }

        public Bitmap GetLiveViewImage()
        {
            if (_camera == IntPtr.Zero)
                throw new Exception("Camera not connected");

            IntPtr stream = IntPtr.Zero;
            IntPtr evfImage = IntPtr.Zero;

            try
            {
                uint err = EDSDK.EdsCreateMemoryStream(0, out stream);
                if (err != EDSDK.EDS_ERR_OK)
                    throw new Exception("EdsCreateMemoryStream failed: " + err);

                err = EDSDK.EdsCreateEvfImageRef(stream, out evfImage);
                if (err != EDSDK.EDS_ERR_OK)
                    throw new Exception("EdsCreateEvfImageRef failed: " + err);

                err = EDSDK.EdsDownloadEvfImage(_camera, evfImage);
                if (err != EDSDK.EDS_ERR_OK)
                {
                    // thường gặp EDS_ERR_DEVICE_BUSY khi buffer chưa sẵn sàng
                    throw new Exception("EdsDownloadEvfImage failed: " + err);
                }

                IntPtr imgPtr;
                ulong length;
                EDSDK.EdsGetPointer(stream, out imgPtr);
                EDSDK.EdsGetLength(stream, out length);

                if (length == 0) return null;

                byte[] buffer = new byte[length];
                Marshal.Copy(imgPtr, buffer, 0, (int)length);

                using (var ms = new MemoryStream(buffer))
                {
                    return new Bitmap(ms);
                }
            }
            finally
            {
                if (evfImage != IntPtr.Zero) EDSDK.EdsRelease(evfImage);
                if (stream != IntPtr.Zero) EDSDK.EdsRelease(stream);
            }
        }

        public void CapturePhoto()
        {
            if (_camera == IntPtr.Zero)
                throw new Exception("Camera not connected");

            uint err = EDSDK.EdsSendCommand(_camera, EDSDK.CameraCommand_TakePicture, 0);
            if (err != EDSDK.EDS_ERR_OK)
                throw new Exception("TakePicture failed: " + err);
        }
    }
}
