using Aspose.Imaging;
using Aspose.Imaging.FileFormats.Png;
using Aspose.Imaging.ImageOptions;
using Aspose.Imaging.Masking;
using Aspose.Imaging.Masking.Options;
using Aspose.Imaging.Masking.Result;
using Aspose.Imaging.Sources;
using System;
using System.Collections.Generic;
using System.IO;

public static class GraphCutMaskingWithAssumedObject
{
    public static void Run(string imagePath, string outputDir)
    {
        // Giả định đối tượng
        List<AssumedObjectData> assumedObjects = new List<AssumedObjectData>();
        assumedObjects.Add(new AssumedObjectData(DetectedObjectType.Human, new Rectangle(90, 130, 60, 60)));

        string tempResult1 = Path.Combine(outputDir, "result.png");
        string finalResult = Path.Combine(outputDir, "result2.png");

        using (RasterImage image = (RasterImage)Image.Load(imagePath))
        {
            AutoMaskingGraphCutOptions options = new AutoMaskingGraphCutOptions
            {
                AssumedObjects = assumedObjects, // Lấy các thông tin giả định
                CalculateDefaultStrokes = true, // Tạo các nét gợi
                FeatheringRadius = (Math.Max(image.Width, image.Height) / 500) + 1, // Làm mượt viền
                Method = SegmentationMethod.GraphCut,
                Decompose = false,
                ExportOptions = new PngOptions
                {
                    ColorType = PngColorType.TruecolorWithAlpha,
                    Source = new FileCreateSource(tempResult1)
                },
                BackgroundReplacementColor = Color.Transparent
            };

            MaskingResult results = new ImageMasking(image).Decompose(options);

            using (RasterImage resultImage = (RasterImage)results[1].GetImage())
            {
                resultImage.Save(finalResult, new PngOptions { ColorType = PngColorType.TruecolorWithAlpha });
            }
        }

        // File.Delete(tempResult1);
        // File.Delete(finalResult);
    }
}

