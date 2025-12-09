using System;
using System.IO;
using Tracker.Documentation;

namespace Tracker.Tools
{
    /// <summary>
    /// Simple tool to generate the user manual Word document.
    /// </summary>
    public static class ManualGenerator
    {
        public static void Generate(string? outputPath = null)
        {
            outputPath ??= Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Tracker_UserManual.docx");

            Console.WriteLine("Generating Tracker User Manual...");
            Console.WriteLine($"Output: {outputPath}");

            try
            {
                UserManualGenerator.GenerateUserManual(outputPath);
                Console.WriteLine("✓ User manual generated successfully!");
                Console.WriteLine($"Location: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error generating manual: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
    }
}

