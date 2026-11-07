using CanonImport.Models;

namespace CanonImport;

/// <summary>
/// Helper class for printing output to the console, including prompting for input and formatting import results.
/// </summary>
internal static class Printer
{
    /// <summary>
    /// Prompts the user to type the card's drive letter (Windows) or mount path (Linux). Used when none was passed as a command-line argument.
    /// </summary>
    /// <returns>Drive letter or mount path</returns>
    internal static string? PromptForCard()
    {
        Console.Write(OperatingSystem.IsWindows() ? "Card drive letter: " : "Card mount path: ");
        return Console.ReadLine();
    }

    /// <summary>
    /// Asks whether to delete the imported photos from the card. Anything other than "y" or "yes" keeps them.
    /// </summary>
    /// <param name="count">Number of imported files that would be deleted</param>
    /// <returns>True if the user confirmed</returns>
    internal static bool PromptToClearCard(int count)
    {
        Console.Write($"Delete the {count} imported files from the card? (y/N): ");
        string? answer = Console.ReadLine()?.Trim();
        return string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase) || string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Prints how many files were deleted from the card, followed by any that couldn't be.
    /// </summary>
    /// <param name="deleted">Number of files deleted</param>
    /// <param name="errors">Error message for each file that couldn't be deleted</param>
    internal static void PrintClearResult(int deleted, IReadOnlyList<string> errors)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("[CLEARED] ");
        Console.ResetColor();
        Console.WriteLine($"{deleted} files deleted from the card.");
        foreach (string error in errors)
            PrintError(error);
        Console.WriteLine();
    }

    /// <summary>
    /// Prints an error that stops the import before it starts.
    /// </summary>
    /// <param name="message">Error message</param>
    internal static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("[ERROR] ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    /// <summary>
    /// Prints a single import result, color-coded by its status, with a running count.
    /// </summary>
    /// <param name="result">Import result</param>
    /// <param name="index">Position of this file in the import, starting at 1</param>
    /// <param name="total">Total number of files being imported</param>
    internal static void PrintResult(ImportResult result, int index, int total)
    {
        string counter = $"{index.ToString().PadLeft(total.ToString().Length)}/{total}";
        Console.ForegroundColor = GetColor(result.Status);
        Console.Write($"[{result.Status.ToString().ToUpperInvariant(),-7}] ");
        Console.ResetColor();
        Console.WriteLine(result.Status == ImportStatus.Failed ? $"{counter} {result.FileName}: {result.Detail}" : $"{counter} {result.FileName} -> {result.Detail}");
    }

    /// <summary>
    /// Prints how many files landed in each status, followed by what to do next.
    /// </summary>
    /// <param name="results">Every <see cref="ImportResult"/> produced in this run</param>
    internal static void PrintSummary(IReadOnlyList<ImportResult> results)
    {
        Console.Write("Done: ");
        bool isFirst = true;
        foreach (ImportStatus status in Enum.GetValues<ImportStatus>())
        {
            int count = results.Count(result => result.Status == status);
            if (count == 0)
                continue;
            if (!isFirst)
                Console.Write(", ");
            isFirst = false;

            Console.ForegroundColor = GetColor(status);
            Console.Write($"{count} {status.ToString().ToLowerInvariant()}");
            Console.ResetColor();
        }
        Console.WriteLine();

        // Only remind about Lightroom and the card when something new actually arrived.
        if (results.Any(result => result.Status is ImportStatus.Copied or ImportStatus.Renamed))
        {
            Console.WriteLine();
            Console.WriteLine("Next: in Lightroom, right-click the Photos folder in Library > Folders and choose Synchronize Folder. Once the photos are backed up, format the card in the camera.");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Maps an import status to the console color it's printed in, shared by individual results and the summary so both stay consistent.
    /// </summary>
    /// <param name="status">Status to map</param>
    /// <returns>Console color</returns>
    private static ConsoleColor GetColor(ImportStatus status) => status switch
    {
        ImportStatus.Copied => ConsoleColor.Green,
        ImportStatus.Skipped => ConsoleColor.Cyan,
        ImportStatus.Renamed => ConsoleColor.Yellow,
        ImportStatus.Failed => ConsoleColor.Red,
        _ => ConsoleColor.White
    };
}