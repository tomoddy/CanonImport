using CanonImport;
using CanonImport.Models;
using Microsoft.Extensions.Configuration;

// Load the destination share and the file extensions to import from appsettings.json next to the executable, with an optional Linux override.
ConfigurationBuilder builder = new();
builder.SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: false);
if (OperatingSystem.IsLinux())
    builder.AddJsonFile("appsettings.Linux.json", optional: true);
IConfiguration configuration = builder.Build();
string destinationRoot = configuration["DestinationRoot"] ?? string.Empty;
List<string> extensions = [.. configuration.GetSection("Extensions").GetChildren().Select(section => section.Value).OfType<string>()];
if (string.IsNullOrWhiteSpace(destinationRoot) || extensions.Count == 0)
{
    Printer.PrintError("appsettings.json must set DestinationRoot and at least one entry in Extensions.");
    return 1;
}

// Accept the card's location as an argument (a drive letter such as `E` on Windows, a mount path on Linux), or prompt for it if none was given.
string? cardArgument = args.Length > 0 ? args[0] : Printer.PromptForCard();
string? cardRoot = PhotoImporter.GetCardPath(cardArgument);
if (cardRoot is null)
{
    Printer.PrintError(OperatingSystem.IsWindows() ? $"'{cardArgument}' isn't a valid drive letter." : $"'{cardArgument}' isn't an existing folder.");
    return 1;
}

// The camera stores photos under DCIM, so bail out if it isn't there (wrong location, or no card inserted).
string? dcimPath = PhotoImporter.FindDcimFolder(cardRoot);
if (dcimPath is null)
{
    Printer.PrintError($"No DCIM folder found on {cardRoot}. Is the card inserted?");
    return 1;
}

// Make sure the share is reachable before copying anything.
if (!Directory.Exists(destinationRoot))
{
    Printer.PrintError($"Destination {destinationRoot} isn't reachable.");
    return 1;
}

// Find every photo on the card.
List<string> photos = PhotoImporter.FindPhotos(dcimPath, extensions);
if (photos.Count == 0)
{
    Console.WriteLine($"No photos found on {cardRoot}.");
    return 0;
}

Console.WriteLine();
Console.WriteLine($"Importing {photos.Count} files from {dcimPath} to {destinationRoot}...");
Console.WriteLine();

// Copy photos one at a time so progress prints in order and the network share isn't flooded.
List<ImportResult> results = [];
for (int i = 0; i < photos.Count; i++)
{
    ImportResult result = PhotoImporter.Import(photos[i], destinationRoot);
    Printer.PrintResult(result, i + 1, photos.Count);
    results.Add(result);
}

Console.WriteLine();
Printer.PrintSummary(results);
return results.Any(result => result.Status == ImportStatus.Failed) ? 1 : 0;