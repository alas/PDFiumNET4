using System.IO;
using PDFiumNET4;

namespace TestPdfToPng
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SimplePdf.ToPngFiles(@"files/test.pdf");

            var fileList = SimplePdf.GetPngBytes(File.ReadAllBytes(@"files/test.pdf"));
            for (int i = 0; i < fileList.Count; i++)
            {
                var pageBytes = fileList[i];
                File.WriteAllBytes(@"files/test_BYTES_" + i + ".png", pageBytes);
            }

            var files = Directory.GetFiles("./files", "*.pdf", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var fileBytes = File.ReadAllBytes(file);
                var list = SimplePdf.GetPngBytes(fileBytes);
                for (int i = 0; i < list.Count; i++)
                {
                    byte[] pageBytes = list[i];
                    var newName = Path.GetFileNameWithoutExtension(file);
                    File.WriteAllBytes("./files/" + newName + @"_" + i + "_1.png", pageBytes);
                }
                var array2 = SimplePdf.GetPngBytes(fileBytes, 1158, 1638);
                for (int i = 0; i < array2.Count; i++)
                {
                    byte[] pageBytes = array2[i];
                    var newName = Path.GetFileNameWithoutExtension(file);
                    File.WriteAllBytes("./files/" + newName + @"_" + i + "_2.png", pageBytes);
                }
            }
        }
    }
}
