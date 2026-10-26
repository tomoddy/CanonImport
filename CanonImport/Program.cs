using CanonImport;
using CanonImport.Models;
using Microsoft.Extensions.Configuration;

// Load the destination share and the file extensions to import from appsettings.json next to the executable.
IConfiguration configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: false).Build();
string destinationRoot = configuration["DestinationRoot"] ?? string.Empty;
List<string> extensions = [.. configuration.GetSection("Extensions").GetChildren().Select(section => section.Value).OfType<string>()];
if (string.IsNullOrWhiteSpace(destinationRoot) || extensions.Count == 0)
{
    Printer.PrintError("appsettings.json must set DestinationRoot and at least one entry in Extensions.");
    return 1;
}

// Accept the card's drive letter as an argument (e.g. `CanonImport E`, `E:` or `E:\`), or prompt for it if none was given.
string? driveArgument = args.Length > 0 ? args[0] : Printer.PromptForDrive();
string? cardRoot = PhotoImporter.GetCardRoot(driveArgument);
if (cardRoot is null)
{
    Printer.PrintError($"'{driveArgument}' isn't a valid drive letter.");
    return 1;
}

// The camera stores photos under DCIM, so bail out if it isn't there (wrong drive, or no card inserted).
string dcimPath = Path.Combine(cardRoot, "DCIM");
if (!Directory.Exists(dcimPath))
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