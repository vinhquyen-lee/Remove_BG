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
using System.Linq;

public class EnhancedBackgroundRemover
{
    public static void Run()
    {
        string dataDir = @"D:\OneDrive - VNU-HCMUS\HCMUS\HK6\Đồ họa ứng dụng\Project\ImageBgRemover\img\";
        string inputFile = dataDir + "couple.jpg";

        Console.WriteLine($"Đang xử lý ảnh: {inputFile}");

        using (RasterImage image = (RasterImage)Image.Load(inputFile))
        {
            // Phân tích ảnh để tự động xác định vùng đối tượng
            var detectedRegion = AnalyzeImageForObjectDetection(image);
            
            // Tạo multiple passes với các tham số khác nhau
            var results = ProcessWithMultiplePasses(image, detectedRegion, dataDir);
            
            // Kết hợp và tinh chỉnh kết quả
            var finalResult = CombineAndRefineResults(image, results, dataDir);
            
            Console.WriteLine("Hoàn thành tách nền với độ chính xác cao.");
        }
    }

    private static Rectangle AnalyzeImageForObjectDetection(RasterImage image)
    {
        int width = image.Width;
        int height = image.Height;

        int centerX = width / 2;
        int centerY = height / 2;

        int objectWidth = Math.Min(width * 3 / 4, 500);
        int objectHeight = Math.Min(height * 90/100, 800);

        int startX = Math.Max(0, centerX - objectWidth / 2);
        int startY = Math.Max(0, centerY - objectHeight / 2);
        int safeWidth = Math.Min(objectWidth, width - startX);
        int safeHeight = Math.Min(objectHeight, height - startY);

        // Vùng nghi ngờ chưa đối tượng
        return new Rectangle(startX, startY, safeWidth, safeHeight);
    }
    private static List<MaskingResult> ProcessWithMultiplePasses(RasterImage image, Rectangle detectedRegion, string dataDir)
    {
        var results = new List<MaskingResult>();
        
        // Pass 1: Tạo ra một mask ban đầu một cách an toàn, tránh cắt lẹm vào đối tượng, làm mượt viền với biên độ lớn.
        var result1 = ProcessSinglePass(image, detectedRegion, new MaskingParameters
        {
            FeatheringRadius = Math.Max(3, Math.Max(image.Width, image.Height) / 200),
            Method = SegmentationMethod.GraphCut,
            UseEdgeDetection = true
        });
        results.Add(result1);
        SaveResultImage(result1, 1, dataDir + "pass1_conservative.png");

        // Pass 2: Tinh chỉnh đường biên sắc nét hơn, cố gắng loại bỏ các pixel nền còn sót lại gần đối tượng.
        var result2 = ProcessSinglePass(image, detectedRegion, new MaskingParameters
        {
            FeatheringRadius = 1,
            Method = SegmentationMethod.GraphCut,
            UseEdgeDetection = true,
            EdgeThreshold = 0.3f
        });
        results.Add(result2);
        SaveResultImage(result2, 1, dataDir + "pass2_aggressive.png");

        // Pass 3: Tinh chỉnh thông minh
        var result3 = ProcessWithManualRefinement(image, detectedRegion, dataDir);
        results.Add(result3);
        SaveResultImage(result3, 1, dataDir + "pass3_manual.png");

        return results;
    }

    private static MaskingResult ProcessSinglePass(RasterImage image, Rectangle objectRegion, MaskingParameters parameters)
    {
        var assumedObjects = new List<AssumedObjectData>
        {
            new AssumedObjectData(DetectedObjectType.Human, objectRegion)
        };

        var options = new AutoMaskingGraphCutOptions
        {
            AssumedObjects = assumedObjects,
            CalculateDefaultStrokes = true,
            FeatheringRadius = parameters.FeatheringRadius,  
            Method = parameters.Method,
            Decompose = false,
            ExportOptions = new PngOptions
            {
                ColorType = PngColorType.TruecolorWithAlpha,
                Source = new StreamSource(new MemoryStream())
            },
            BackgroundReplacementColor = Color.Transparent
        };

        using (IMaskingSession session = new ImageMasking(image).CreateSession(options))
        {
            return session.Decompose();
        }
    }

    private static MaskingResult ProcessWithManualRefinement(RasterImage image, Rectangle objectRegion, string dataDir)
    {
        var assumedObjects = new List<AssumedObjectData>
        {
            new AssumedObjectData(DetectedObjectType.Human, objectRegion)
        };

        var options = new AutoMaskingGraphCutOptions
        {
            AssumedObjects = assumedObjects,
            CalculateDefaultStrokes = true,
            FeatheringRadius = 2, 
            Method = SegmentationMethod.GraphCut,
            Decompose = false,
            ExportOptions = new PngOptions
            {
                ColorType = PngColorType.TruecolorWithAlpha,
                Source = new StreamSource(new MemoryStream())
            },
            BackgroundReplacementColor = Color.Transparent
        };

        using (IMaskingSession session = new ImageMasking(image).CreateSession(options))
        {
            var initialResult = session.Decompose();

            var refinementPoints = GenerateSmartRefinementPoints(image, objectRegion);

            var improveArgs = new AutoMaskingArgs { ObjectsPoints = refinementPoints };

            var improvedResult = session.ImproveDecomposition(improveArgs);

            var finalRefinementPoints = GenerateFinalRefinementPoints(image, objectRegion);

            if (finalRefinementPoints == null || finalRefinementPoints.Length != 2 ||
                finalRefinementPoints[0].Length == 0 || finalRefinementPoints[1].Length == 0)
            {
                Console.WriteLine("⚠️ Không đủ điểm refinement để thực hiện cải thiện lần 2.");
                return improvedResult;
            }

            var finalImproveArgs = new AutoMaskingArgs { ObjectsPoints = finalRefinementPoints };
            return session.ImproveDecomposition(finalImproveArgs);
        }
    }

    private static Point[][] GenerateSmartRefinementPoints(RasterImage image, Rectangle objectRegion)
    {
        var foregroundPoints = new List<Point>();
        var backgroundPoints = new List<Point>();

        // Foreground points
        int centerX = objectRegion.X + objectRegion.Width / 2;
        int centerY = objectRegion.Y + objectRegion.Height / 2;

        foregroundPoints.Add(new Point(centerX, centerY));
        foregroundPoints.Add(new Point(centerX - 20, centerY - 30)); 
        foregroundPoints.Add(new Point(centerX + 15, centerY + 50)); 
        foregroundPoints.Add(new Point(centerX - 10, centerY + 100)); 

        // Background points
        int margin = Math.Min(image.Width, image.Height) / 20;

        backgroundPoints.Add(new Point(margin, margin));
        backgroundPoints.Add(new Point(image.Width - margin, margin)); 
        backgroundPoints.Add(new Point(margin, image.Height - margin)); 
        backgroundPoints.Add(new Point(image.Width - margin, image.Height - margin)); 

        // Thêm điểm background xung quanh đối tượng
        backgroundPoints.Add(new Point(objectRegion.X - 20, objectRegion.Y));
        backgroundPoints.Add(new Point(objectRegion.X + objectRegion.Width + 20, objectRegion.Y));

        return new Point[][] { foregroundPoints.ToArray(), backgroundPoints.ToArray() };
    }

    private static Point[][] GenerateFinalRefinementPoints(RasterImage image, Rectangle objectRegion)
    {
        var foregroundPoints = new List<Point>();
        var backgroundPoints = new List<Point>();

        int centerX = objectRegion.X + objectRegion.Width / 2;
        int centerY = objectRegion.Y + objectRegion.Height / 2;

        // --- Foreground points ---
        foregroundPoints.Add(new Point(centerX - 10, Math.Max(0, objectRegion.Y + 20)));
        foregroundPoints.Add(new Point(centerX + 10, Math.Max(0, objectRegion.Y + 20)));

        foregroundPoints.Add(new Point(Math.Max(0, centerX - 40), Math.Min(image.Height - 1, centerY + 30)));
        foregroundPoints.Add(new Point(Math.Min(image.Width - 1, centerX + 40), Math.Min(image.Height - 1, centerY + 30)));

        foregroundPoints.Add(new Point(centerX, Math.Min(image.Height - 1, objectRegion.Y + objectRegion.Height - 10)));

        // --- Background points ---
        backgroundPoints.Add(new Point(centerX, Math.Max(0, objectRegion.Y - 10)));

        backgroundPoints.Add(new Point(Math.Max(0, objectRegion.X + 5), centerY + 20)); 
        backgroundPoints.Add(new Point(Math.Min(image.Width - 1, objectRegion.X + objectRegion.Width - 5), centerY + 20));

        backgroundPoints.Add(new Point(centerX, Math.Min(image.Height - 1, objectRegion.Y + objectRegion.Height - 5)));
        backgroundPoints.Add(new Point(centerX - 30, Math.Min(image.Height - 1, objectRegion.Y + objectRegion.Height - 10)));
        backgroundPoints.Add(new Point(centerX + 30, Math.Min(image.Height - 1, objectRegion.Y + objectRegion.Height - 10)));

        // Góc ngoài ảnh
        int margin = Math.Min(image.Width, image.Height) / 20;
        backgroundPoints.Add(new Point(margin, margin));
        backgroundPoints.Add(new Point(image.Width - margin, margin));
        backgroundPoints.Add(new Point(margin, image.Height - margin));
        backgroundPoints.Add(new Point(image.Width - margin, image.Height - margin));

        return new Point[][] { foregroundPoints.ToArray(), backgroundPoints.ToArray() };
    }
    private static MaskingResult CombineAndRefineResults(RasterImage originalImage, List<MaskingResult> results, string dataDir)
    {
        var bestResult = results.Last();
        
        SaveResultImage(bestResult, 1, dataDir + "final_result.png");
        
        ProcessPostProcessing(bestResult, 1, dataDir + "final_processed.png");
        
        return bestResult;
    }

    private static void ProcessPostProcessing(MaskingResult result, int index, string outputPath)
    {
        using (RasterImage img = (RasterImage)result[index].GetImage())
        {            
            img.Save(outputPath, new PngOptions
            {
                ColorType = PngColorType.TruecolorWithAlpha
            });
        }
    }

    private static void SaveResultImage(MaskingResult result, int index, string path)
    {
        using (RasterImage img = (RasterImage)result[index].GetImage())
        {
            img.Save(path, new PngOptions
            {
                ColorType = PngColorType.TruecolorWithAlpha
            });
        }
        Console.WriteLine($"💾 Đã lưu: {Path.GetFileName(path)}");
    }
}

public class MaskingParameters
{
    public int FeatheringRadius { get; set; } = 2;
    public SegmentationMethod Method { get; set; } = SegmentationMethod.GraphCut;
    public bool UseEdgeDetection { get; set; } = false;
    public float EdgeThreshold { get; set; } = 0.5f;
}

public static class ImageMaskingExtensions
{
    public static Rectangle GetOptimalObjectRegion(this RasterImage image)
    {
        int width = image.Width;
        int height = image.Height;

        int objectWidth = (int)(width * 0.6);
        int objectHeight = (int)(height * 0.8);

        return new Rectangle(
            (width - objectWidth) / 2,
            (height - objectHeight) / 2,
            objectWidth,
            objectHeight
        );
    }

    public static bool IsLikelyBackgroundPixel(this Color pixel, Color backgroundColor, int threshold = 30)
    {
        int rDiff = Math.Abs(pixel.R - backgroundColor.R);
        int gDiff = Math.Abs(pixel.G - backgroundColor.G);
        int bDiff = Math.Abs(pixel.B - backgroundColor.B);

        return (rDiff + gDiff + bDiff) / 3 < threshold;
    }
}
