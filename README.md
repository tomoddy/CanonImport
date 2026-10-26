# CanonImport

A command-line tool for copying photos off a camera's memory card onto a network share, sorted into date folders (e.g. `Canon EOS 1200D\2026\10 - October\25`) based on when each photo was taken.

The date comes from each photo's EXIF data (`DateTimeOriginal`), so folders are correct even if a file's modified date has been changed. If the EXIF can't be read, the file's modified date is used instead.

## Usage

Run with the card's drive letter as the argument (`E`, `E:` and `E:\` all work):

```
CanonImport E
```

Or run with no arguments and it'll prompt for one:

```
CanonImport
Card drive letter: E
```

Every matching file under the card's `DCIM` folder is copied, and each one prints as `[COPIED]`, `[SKIPPED]`, `[RENAMED]`, or `[FAILED]` with where it went. A summary of the counts is printed at the end.

- **Copied** — new file, copied into its date folder.
- **Skipped** — a file with the same name and size is already there, so it was imported on a previous run. Running the tool twice on the same card is safe.
- **Renamed** — a *different* file with the same name is already there (the camera's file counter wraps at 9999), so the new one is saved with a suffix, e.g. `IMG_0001-1.CR2`.
- **Failed** — the copy failed; the error is shown. Files are copied to a `.partial` name and only renamed once complete, so an interrupted run never leaves a half-written photo behind.

Nothing is ever deleted from the card. Format it in the camera once the photos are safely backed up.

## Configuration

Settings live in `appsettings.json` beside the executable:

```json
{
  "DestinationRoot": "\\\\Robert\\personal\\Photos\\Canon EOS 1200D",
  "Extensions": [ ".CR2", ".JPG" ]
}
```

- **DestinationRoot** — the folder the date folders are created under.
- **Extensions** — which file types to copy. Add `".MOV"` to include videos.

## Lightroom

The tool only copies files; Lightroom doesn't know about them until told. After an import, in Lightroom Classic go to **Library → Folders**, right-click the Photos folder and choose **Synchronize Folder**. Alternatively use **Import → Add** on the new date folders.

## Building

Requires the .NET SDK (targets net10.0). Open `CanonImport.slnx` in Visual Studio and press F5, or from a terminal:

```
cd CanonImport
dotnet run -- E
```

## Publishing a standalone executable

A publish profile (`Properties/PublishProfiles/SelfContainedWinX64.pubxml`) is set up to produce a single, self-contained `CanonImport.exe` with no .NET runtime dependency, published to `%LocalAppData%\Programs\CanonImport\` alongside `appsettings.json`.

In Visual Studio: right-click the project → **Publish**.

From a terminal:

```
cd CanonImport
dotnet publish -p:PublishProfile=SelfContainedWinX64
```

To run it as a plain command from any terminal, add that folder to your PATH once:

```powershell
[Environment]::SetEnvironmentVariable("Path", $env:Path + ";$env:LocalAppData\Programs\CanonImport", "User")
```

Then open a new terminal and run `CanonImport E` from anywhere.
