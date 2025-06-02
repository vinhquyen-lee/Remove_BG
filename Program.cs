using System;

class Program
{
    static void Main(string[] args)
    {

        Console.WriteLine("Chọn chức năng thực hiện:");
        Console.WriteLine("1. Graph Cut Feathering");
        Console.WriteLine("2. Graph Cut with Assumed Object");
        // Console.WriteLine("3. Manual Masking");
        Console.WriteLine("3. Repeated Auto Masking");

        string choice = Console.ReadLine();

        Console.WriteLine(); 

        switch (choice)
        {
            case "1":
                GraphCutFeathering.Run();
                break;
            case "2":
                string templatesFolder = @"D:\OneDrive - VNU-HCMUS\HCMUS\HK6\Đồ họa ứng dụng\Project\ImageBgRemover\img\";
                string imagePath = Path.Combine(templatesFolder, "couple.jpg");

                GraphCutMaskingWithAssumedObject.Run(imagePath, templatesFolder);
                //GraphCutMaskingWithAssumedObject.Run();
                break;

            case "3":
                RepeatedAutoMasking.Run();
                break;
            default:
                Console.WriteLine("Vui lòng nhập số từ 1 đến 3.");
                break;
        }

        Console.WriteLine("Hoàn tất xử lý.");
    }
}
