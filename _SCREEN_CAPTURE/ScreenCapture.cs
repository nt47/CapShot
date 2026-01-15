using System;
using System.Collections.Generic;
using System.Text;
using static _SCREEN_CAPTURE.Interop.NativeMethods;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows;

namespace _SCREEN_CAPTURE
{
    public class ScreenCapture
    {
        public static IntPtr GetAllMonitorsDC()
        {
            return CreateDC("DISPLAY", null, null, IntPtr.Zero);
        }

        public static Bitmap CaptureScreen()
        {

            var hdcSrc = GetAllMonitorsDC();

            var width = Screen.PrimaryScreen.Bounds.Width;
            var height = Screen.PrimaryScreen.Bounds.Height;
            var hdcDest = CreateCompatibleDC(hdcSrc);
            var hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
            _ = SelectObject(hdcDest, hBitmap);

            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0,
                TernaryRasterOperations.SRCCOPY | TernaryRasterOperations.CAPTUREBLT);

            var image = System.Drawing.Image.FromHbitmap(hBitmap);
            //var bitmap = image.ToBitmapSource();
            //bitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty,
            //    BitmapSizeOptions.FromEmptyOptions());

            DeleteObject(hBitmap);
            DeleteDC(hdcDest);
            DeleteDC(hdcSrc);

            return image;
        }
    }
}
