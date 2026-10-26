using CanonImport.Models;
using System.Globalization;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using IODirectory = System.IO.Directory;
using MetadataDirectory = MetadataExtractor.Directory;

namespace CanonImport;

/// <summary>
/// Finds photos on a memory card and copies them into date-based folders (e.g. 2026\10 - October\25) on the destination.
/// </summary>
internal static class PhotoImporter
{
    /// <summary>
    /// Turns a drive argument such as "E", "E:" or "E:\" into the drive's root path.
    /// </summary>
    /// <param name="drive">Drive letter as typed by the user</param>
    /// <returns>Drive root (e.g. "E:\"), or null if the argument isn't a drive letter</returns>
    internal static string? GetCardRoot(string? drive)
    {
        string letter = drive?.Trim().TrimEnd('\\', '/').TrimEnd(':') ?? string.Empty;
        return letter.Length == 1 && char.IsAsciiLetter(letter[0]) ? $"{char.ToUpperInvariant(letter[0])}:\\" : null;
    }

    /// <summary>
    /// Finds every file under the DCIM folder with one of the given extensions, in file name order.
    /// </summary>
    /// <param name="dcimPath">Path to the card's DCIM folder</param>
    /// <param name="extensions">File extensions to include (e.g. ".CR2", ".JPG")</param>
    /// <returns>Full paths of the photos found</returns>
    internal static List<string> FindPhotos(string dcimPath, IReadOnlyList<string> extensions)
    {
        return [.. IODirectory.EnumerateFiles(dcimPath, "*", SearchOption.AllDirectories).Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)).Order(StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Copies a single photo into its date folder on the destination, skipping it if it's already been imported.
    /// </summary>
    /// <param name="sourcePath">Full path of the photo on the card</param>
    /// <param name="destinationRoot">Root folder to import into</param>
    /// <returns>Import result</returns>
    internal static ImportResult Import(string sourcePath, string destinationRoot)
    {
        string fileName = Path.GetFileName(sourcePath);
        try
        {
            // Work out the destination folder from the date the photo was taken, e.g. 2026\10 - October\25.
            DateTime captureDate = GetCaptureDate(sourcePath);
            string relativeFolder = Path.Combine(captureDate.ToString("yyyy"), captureDate.ToString("MM - MMMM", CultureInfo.InvariantCulture), captureDate.ToString("dd"));
            string destinationFolder = Path.Combine(destinationRoot, relativeFolder);
            IODirectory.CreateDirectory(destinationFolder);

            // Find a free name. A file with the same name and size was imported on a previous run, so skip it. A different file with the same name means the camera's file counter has wrapped round, so keep both by adding a suffix.
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            long sourceSize = new FileInfo(sourcePath).Length;
            string destinationPath = Path.Combine(destinationFolder, fileName);
            for (int suffix = 1; File.Exists(destinationPath); suffix++)
            {
                if (new FileInfo(destinationPath).Length == sourceSize)
                    return new ImportResult(fileName, ImportStatus.Skipped, Path.Combine(relativeFolder, Path.GetFileName(destinationPath)));
                destinationPath = Path.Combine(destinationFolder, $"{baseName}-{suffix}{extension}");
            }

            // Copy to a temporary name first and rename once complete, so an interrupted copy never leaves a half-written photo behind.
            string tempPath = $"{destinationPath}.partial";
            File.Copy(sourcePath, tempPath, overwrite: true);
            File.Move(tempPath, destinationPath);
            ImportStatus status = Path.GetFileName(destinationPath) == fileName ? ImportStatus.Copied : ImportStatus.Renamed;
            return new ImportResult(fileName, status, Path.Combine(relativeFolder, Path.GetFileName(destinationPath)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ImportResult(fileName, ImportStatus.Failed, ex.Message);
        }
    }

    /// <summary>
    /// Gets the date a photo was taken from its EXIF data, falling back to the file's modified date if the EXIF can't be read.
    /// </summary>
    /// <param name="path">Full path of the photo</param>
    /// <returns>Date and time the photo was taken</returns>
    private static DateTime GetCaptureDate(string path)
    {
        try
        {
            IReadOnlyList<MetadataDirectory> directories = ImageMetadataReader.ReadMetadata(path);
            ExifSubIfdDirectory? exif = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (exif is not null && exif.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime taken))
                return taken;
        }
        catch (ImageProcessingException)
        {
            // Unreadable metadata, so fall back to the file date below.
        }

        return File.GetLastWriteTime(path);
    }
}