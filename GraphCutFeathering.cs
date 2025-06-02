using Aspose.Imaging;
using Aspose.Imaging.FileFormats.Png;
using Aspose.Imaging.ImageOptions;
using Aspose.Imaging.Masking;
using Aspose.Imaging.Masking.Options;
using Aspose.Imaging.Masking.Result;
using Aspose.Imaging.Sources;
using System;
using System.IO;

public class GraphCutFeathering
{
    public static void Run()
    {
        string templatesFolder = @"D:\OneDrive - VNU-HCMUS\HCMUS\HK6\Đồ họa ứng dụng\Project\ImageBgRemover\img\";
        string dataDir = templatesFolder;

        MaskingResult results;
        using (RasterImage image = (RasterImage)Image.Load(dataDir + "couple.jpg"))
        {
            AutoMaskingGraphCutOptions options = new AutoMaskingGraphCutOptions
            {
                CalculateDefaultStrokes = true, // Xác định vùng foreground và background
                FeatheringRadius = (Math.Max(image.Width, image.Height) / 500) + 1, // r làm mượt viền
                Method = SegmentationMethod.GraphCut, // Phân đoạn

                Decompose = false,
                ExportOptions = new PngOptions()
                {
                    ColorType = PngColorType.TruecolorWithAlpha,
                    Source = new FileCreateSource(dataDir + "result.png")
                },
                BackgroundReplacementColor = Color.Transparent
            };

            results = new ImageMasking(image).Decompose(options); // Trả về một danh sách các kết quả masking.

            using (RasterImage resultImage = (RasterImage)results[1].GetImage())
            {
                resultImage.Save(dataDir + "result2.png", new PngOptions() { ColorType = PngColorType.TruecolorWithAlpha });
            }
        }

        // File.Delete(dataDir + "result.png");
        // File.Delete(dataDir + "result2.png");

        Console.WriteLine("Graph Cut Feathering completed.");
    }
}