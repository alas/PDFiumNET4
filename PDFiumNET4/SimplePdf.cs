using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using LibZopfliSharp;
using PDFiumCore;

namespace PDFiumNET4
{
    public class SimplePdf
    {
        #region Load Native DLLs

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

            var newPath = Path.Combine(path, "build", IntPtr.Size == 4 ? "x86" : "x64", "pdfium.dll");

            var result = File.Exists(newPath) && LoadLibrary(newPath) != IntPtr.Zero;
            if (!result) return false;

            var newPath2 = Path.Combine(path, "build", IntPtr.Size == 4 ? "x86" : "x64", "zopfli.dll");

            return File.Exists(newPath2) && LoadLibrary(newPath2) != IntPtr.Zero;
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPTStr)] string lpFileName);

        #endregion

        public static void ToPngFiles(string fileName, string outputPath = null, ImageOptions options = null)
        {
            // Load the document.
            var document = fpdfview.FPDF_LoadDocument(fileName, null);
            var pageCount = fpdfview.FPDF_GetPageCount(document);

            var newFileName = String.IsNullOrEmpty(outputPath) 
                ? Path.ChangeExtension(fileName, ".png")
                : Path.Combine(outputPath, Path.ChangeExtension(Path.GetFileName(fileName), ".png"));

            var list = GetPngBytesInternal(document, options);

            for (int i = 0; i < list.Count; i++)
            {
                var file = list[i];

                var currentFileName = newFileName;
                if (pageCount > 1) currentFileName = currentFileName.Replace(".png", "_" + i + ".png");

                File.WriteAllBytes(currentFileName, file);
            }
        }

        public unsafe static List<byte[]> GetPngBytes(byte[] fileContents, ImageOptions options = null)
        {
            using (AutoPinner ap = new AutoPinner(fileContents))
            {
                var unamanagedFileContents = ap;  // Use the operator to retrieve the IntPtr

                // Load the document.
                var document = fpdfview.FPDF_LoadMemDocument(unamanagedFileContents, fileContents.Length, null);
                return GetPngBytesInternal(document, options);
            }
        }

        private static List<byte[]> GetPngBytesInternal(FpdfDocumentT document, ImageOptions options = null)
        {
            var pageCount = fpdfview.FPDF_GetPageCount(document);
            var res = new List<byte[]>(pageCount);

            for (var i = 0; i < pageCount; i++)
            {
                var tup = RenderPageToImage(document, i, options);
                var byteArr = tup.Item1;
                var pageWidth = tup.Item2;
                var pageHeight = tup.Item3;
                using (var img32 = new Bitmap((int)pageWidth, (int)pageHeight, PixelFormat.Format32bppArgb))
                {
                    AddBytes(img32, byteArr);

                    var img = (options != null && options.PixelFormat != PixelFormat.Format32bppArgb)
                        ? ConvertToFormat(img32, options.PixelFormat)
                        : img32;
                    using (var stream = new MemoryStream())
                    {
                        img.Save(stream, ImageFormat.Png);

                        var png24 = stream.ToArray();

                        if (options == null || !options.Compressed)
                        {
                            res.Add(png24);
                        }
                        else
                        {
                            using (var compressStream = new MemoryStream())
                            using (var compressor = new ZopfliPNGStream(compressStream))
                            {
                                compressor.Write(png24, 0, png24.Length);
                                compressor.Close();
                                var compressed = compressStream.ToArray();
                                res.Add(compressed);
                            }
                        }
                    }
                    if (img != img32) img.Dispose();
                }
            }

            return res;
        }

        private static Tuple<byte[], double, double> RenderPageToImage(FpdfDocumentT document, int pageIndex, ImageOptions options = null)
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
                if (options != null && options.ImageWidth.HasValue && options.ImageHeight.HasValue)
                {
                    scaleX = (float)(options.ImageWidth.Value * 1.0 / pageWidth);
                    scaleY = (float)(options.ImageHeight.Value * 1.0 / pageHeight);
                    imageWidth = options.ImageWidth.Value;
                    imageHeight = options.ImageHeight.Value;
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

        public static Bitmap ConvertToFormat(Image img, PixelFormat format)
        {
            var bmp = new Bitmap(img.Width, img.Height, format);
            using (var gr = Graphics.FromImage(bmp))
                gr.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height));
            return bmp;
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

        public class ImageOptions
        {
            public int? ImageWidth { get; set; } = null;
            public int? ImageHeight { get; set; } = null;
            public PixelFormat PixelFormat { get; set; } = PixelFormat.Format32bppArgb;
            public bool Compressed { get; set; } = false;
        }

        public enum ImageType
        {
            Unknown = 0,
            Png = 1,
        }
    }
}
