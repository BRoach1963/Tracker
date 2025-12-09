using System;
using System.IO;
using Tracker.Documentation;

namespace GenerateUserManual
{
    class Program
    {
        static void Main(string[] args)
        {
            string outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Tracker_UserManual.docx");

            if (args.Length > 0)
            {
                outputPath = args[0];
            }

            Console.WriteLine("========================================");
            Console.WriteLine("Tracker User Manual Generator");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine($"Output: {outputPath}");
            Console.WriteLine();

            try
            {
                Console.WriteLine("Generating manual...");
                UserManualGenerator.GenerateUserManual(outputPath);
                Console.WriteLine();
                Console.WriteLine("✓ User manual generated successfully!");
                Console.WriteLine($"✓ Location: {outputPath}");
                Console.WriteLine();
                Console.WriteLine("You can now open the file in Microsoft Word and add screenshots.");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"✗ Error generating manual: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }
    }
}

