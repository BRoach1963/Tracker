# PowerShell script to generate the Tracker User Manual
# Usage: .\GenerateUserManual.ps1 [output_path]

param(
    [string]$OutputPath = "$env:USERPROFILE\Documents\Tracker_UserManual.docx"
)

Write-Host "Generating Tracker User Manual..." -ForegroundColor Cyan
Write-Host "Output: $OutputPath" -ForegroundColor Gray

# Build the project first
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build Tracker.csproj --no-incremental | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Create a simple C# program to generate the manual
$tempScript = @"
using System;
using System.IO;
using Tracker.Documentation;

class Program
{
    static void Main(string[] args)
    {
        string outputPath = @"$OutputPath";
        if (args.Length > 0)
        {
            outputPath = args[0];
        }
        
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
            Console.WriteLine($"✗ Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
"@

$tempScriptPath = "TempManualGenerator.cs"
$tempScript | Out-File -FilePath $tempScriptPath -Encoding UTF8

try {
    # Compile and run
    Write-Host "Generating manual..." -ForegroundColor Yellow
    dotnet run --project . --no-build -- $OutputPath 2>&1 | ForEach-Object {
        if ($_ -match "error|Error") {
            Write-Host $_ -ForegroundColor Red
        } else {
            Write-Host $_
        }
    }
} finally {
    if (Test-Path $tempScriptPath) {
        Remove-Item $tempScriptPath -Force
    }
}

Write-Host "`nDone! Check: $OutputPath" -ForegroundColor Green

