using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using PDFiumCore;

namespace PDFiumNET4
{
    public class SimplePdf
    {
        #region Load Native DLL

        //copied from: https://github.com/marcpabst/PdfiumLight/blob/master/NativeMethods.cs

        static SimplePdf()
        {
            // Load the platform dependent Pdfium.dll if it exists.

            if (!TryLoadNativeLibrary(AppDomain.CurrentDomain.RelativeSearchPath))
            {
                TryLoadNativeLibrary(Path.GetDirectoryName(typeof(SimplePdf).Assembly.Location));
            }

            fpdfview.FPDF_InitLibrary();
        }

        private static bool TryLoadNativeLibrary(string path)
        {
            if (path is null)
                return false;

            path = Path.Combine(path, "build", IntPtr.Size == 4 ? "x86" : "x64", "pdfium.dll");

            return File.Exists(path) && LoadLibrary(path) != IntPtr.Zero;
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPTStr)] string lpFileName);

        #endregion

        public static void ToPngFiles(string fileName, string outputPath = null, int? imageWidth = null, int? imageHeight = null)
        {
            // Load the document.
            var document = fpdfview.FPDF_LoadDocument(fileName, null);
            var pageCount = fpdfview.FPDF_GetPageCount(document);

            var newFileName = String.IsNullOrEmpty(outputPath) 
                ? Path.ChangeExtension(fileName, ".png")
                : Path.Combine(outputPath, Path.ChangeExtension(Path.GetFileName(fileName), ".png"));

            for (var i = 0; i < pageCount; i++)
            {
                var tup = RenderPageToImage(document, i, imageWidth, imageHeight);
                var byteArr = tup.Item1;
                var pageWidth = tup.Item2;
                var pageHeight = tup.Item3;
                var currentFileName = newFileName;
                if (pageCount > 1) currentFileName = currentFileName.Replace(".png", "_" + i + ".png");

                using (var bmp = new Bitmap((int)pageWidth, (int)pageHeight, PixelFormat.Format32bppArgb))
                {
                    AddBytes(bmp, byteArr);

                    using (var stream = new MemoryStream())
                    {
                        bmp.Save(stream, ImageFormat.Png);

                        File.WriteAllBytes(currentFileName, stream.ToArray());
                    }
                }
            }
        }

        public unsafe static List<byte[]> GetPngBytes(byte[] fileContents, int? imageWidth = null, int? imageHeight = null)
        {
            using (AutoPinner ap = new AutoPinner(fileContents))
            {
                var unamanagedFileContents = ap;  // Use the operator to retrieve the IntPtr

                // Load the document.
                var document = fpdfview.FPDF_LoadMemDocument(unamanagedFileContents, fileContents.Length, null);
                var pageCount = fpdfview.FPDF_GetPageCount(document);
                var res = new List<byte[]>(pageCount);

                for (var i = 0; i < pageCount; i++)
                {
                    var tup = RenderPageToImage(document, i, imageWidth, imageHeight);
                    var byteArr = tup.Item1;
                    var pageWidth = tup.Item2;
                    var pageHeight = tup.Item3;
                    using (var bmp = new Bitmap((int)pageWidth, (int)pageHeight, PixelFormat.Format32bppArgb))
                    {
                        AddBytes(bmp, byteArr);

                        using (var stream = new MemoryStream())
                        {
                            bmp.Save(stream, ImageFormat.Png);

                            res.Add(stream.ToArray());
                        }
                    }
                }

                return res;
            }
        }

        private static Tuple<byte[], double, double> RenderPageToImage(FpdfDocumentT document, int pageIndex, int? imageWidthArg = null, int? imageHeightArg = null)
        {
            int imageWidth;
            int imageHeight;

            var page = fpdfview.FPDF_LoadPage(document, pageIndex);
            try
            {
                double pageWidth = 0;
                double pageHeight = 0;
                fpdfview.FPDF_GetPageSizeByIndex(document, pageIndex, ref pageWidth, ref pageHeight);

                float scaleX;
                float scaleY;
                if (imageWidthArg.HasValue && imageHeightArg.HasValue)
                {
                    scaleX = (float)(imageWidthArg.Value * 1.0 / pageWidth);
                    scaleY = (float)(imageHeightArg.Value * 1.0 / pageHeight);
                    imageWidth = imageWidthArg.Value;
                    imageHeight = imageHeightArg.Value;
                }
                else
                {
                    scaleX = 1.0f;
                    scaleY = 1.0f;
                    imageWidth = (int)pageWidth;
                    imageHeight = (int)pageHeight;
                }
                var bitmap = fpdfview.FPDFBitmapCreateEx(
                    imageWidth,
                    imageHeight,
                    (int)FPDFBitmapFormat.BGRA,
                    IntPtr.Zero,
                    0);

                if (bitmap == null)
                    throw new Exception("failed to create a bitmap object");

                byte[] result;
                try
                {
                    // Leave out if you want to make the background transparent.
                    fpdfview.FPDFBitmapFillRect(bitmap, 0, 0, imageWidth, imageHeight, uint.MaxValue); // White color.

                    // |          | a b 0 |
                    // | matrix = | c d 0 |
                    // |          | e f 1 |
                    using (var matrix = new FS_MATRIX_())
                    using (var clipping = new FS_RECTF_())
                    {
                        matrix.A = scaleX;
                        matrix.B = 0;
                        matrix.C = 0;
                        matrix.D = scaleY;
                        matrix.E = 0;
                        matrix.F = 0;

                        clipping.Left = 0;
                        clipping.Right = imageWidth;
                        clipping.Bottom = 0;
                        clipping.Top = imageHeight;

                        fpdfview.FPDF_RenderPageBitmapWithMatrix(bitmap, page, matrix, clipping, 0);

                        var buffer = fpdfview.FPDFBitmapGetBuffer(bitmap);

                        var stride = fpdfview.FPDFBitmapGetStride(bitmap);
                        result = new byte[stride * imageHeight];
                        Marshal.Copy(buffer, result, 0, result.Length);
                    }
                }
                finally
                {
                    fpdfview.FPDFBitmapDestroy(bitmap);
                }

                return new Tuple<byte[], double, double>(result, imageWidth, imageHeight);
            }
            finally
            {
                fpdfview.FPDF_ClosePage(page);
            }
        }

        private static void AddBytes(Bitmap bmp, byte[] rawBytes)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);

            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
            var pNative = bmpData.Scan0;

            Marshal.Copy(rawBytes, 0, pNative, rawBytes.Length);
            bmp.UnlockBits(bmpData);
        }

        #region AutoPinner

        //https://stackoverflow.com/a/23838643/3323941
        private class AutoPinner : IDisposable
        {
            GCHandle _pinnedArray;
            public AutoPinner(Object obj)
            {
                _pinnedArray = GCHandle.Alloc(obj, GCHandleType.Pinned);
            }
            public static implicit operator IntPtr(AutoPinner ap)
            {
                return ap._pinnedArray.AddrOfPinnedObject();
            }
            public void Dispose()
            {
                _pinnedArray.Free();
            }
        } 

        #endregion
    }
}
