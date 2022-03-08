using System.Drawing.Imaging;
using System.IO;
using PDFiumNET4;

namespace TestPdfToPng
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SimplePdf.ToPngFiles(@"files/test01.pdf");

            var fileList = SimplePdf.GetPngBytes(File.ReadAllBytes(@"files/test01.pdf"));
            for (int i = 0; i < fileList.Count; i++)
            {
                var pageBytes = fileList[i];
                File.WriteAllBytes(@"files/test01_BYTES_" + i + ".png", pageBytes);
            }

            var files = Directory.GetFiles("./files", "*.pdf", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var fileBytes = File.ReadAllBytes(file);
                var nameNoExt = Path.GetFileNameWithoutExtension(file);

                var list = SimplePdf.GetPngBytes(fileBytes);
                for (int i = 0; i < list.Count; i++)
                {
                    var pageBytes = list[i];
                    File.WriteAllBytes("./files/" + nameNoExt + @"_" + i + "_1.png", pageBytes);
                }

                var list2 = SimplePdf.GetPngBytes(fileBytes, new SimplePdf.ImageOptions { ImageWidth = 1158, ImageHeight = 1638 });
                for (int i = 0; i < list2.Count; i++)
                {
                    var pageBytes = list2[i];
                    File.WriteAllBytes("./files/" + nameNoExt + @"_" + i + "_2.png", pageBytes);
                }

                var list3 = SimplePdf.GetPngBytes(fileBytes, new SimplePdf.ImageOptions { /*Compressed = true,*/ PixelFormat = PixelFormat.Format24bppRgb });
                for (int i = 0; i < list3.Count; i++)
                {
                    var pageBytes = list3[i];
                    File.WriteAllBytes("./files/" + nameNoExt + @"_" + i + "_3.png", pageBytes);
                }
            }
        }
    }
}
