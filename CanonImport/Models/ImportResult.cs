namespace CanonImport.Models;

/// <summary>
/// Result from importing a single file from the card.
/// </summary>
/// <param name="fileName">Name of the file on the card</param>
/// <param name="status">Status of the import</param>
/// <param name="detail">Where the file was copied to, or the error if it failed</param>
public class ImportResult(string fileName, ImportStatus status, string detail)
{
    /// <summary>
    /// Name of the file on the card
    /// </summary>
    public string FileName { get; } = fileName;

    /// <summary>
    /// Status of the import
    /// </summary>
    public ImportStatus Status { get; } = status;

    /// <summary>
    /// Where the file was copied to, or the error if it failed
    /// </summary>
    public string Detail { get; } = detail;
}