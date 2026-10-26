namespace CanonImport.Models;

/// <summary>
/// Outcome of importing a single file.
/// </summary>
public enum ImportStatus
{
    Copied,
    Skipped,
    Renamed,
    Failed
}