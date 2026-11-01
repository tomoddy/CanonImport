# CanonImport

A command-line tool for copying photos off a camera's memory card onto a network share, sorted into date folders (e.g. `Canon EOS 1200D\2026\10 - October\25`) based on when each photo was taken.

The date comes from each photo's EXIF data (`DateTimeOriginal`), so folders are correct even if a file's modified date has been changed. If the EXIF can't be read, the file's modified date is used instead.

## Usage

Run with the card's location as the argument. On Windows that's the drive letter (`E`, `E:` and `E:\` all work):

```
CanonImport E
```

On Linux it's the folder the card is mounted at:

```
canonimport /run/media/$USER/EOS_DIGITAL
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

### Linux

On Linux an optional `appsettings.Linux.json` beside the executable is loaded on top of `appsettings.json`, so the Windows UNC path can stay in the main file. It is gitignored because the mount point is specific to the machine:

```json
{
  "DestinationRoot": "/mnt/robert/personal/Photos/Canon EOS 1200D"
}
```

#### Mounting the share permanently

Install the CIFS tools, create the mount point and a credentials file only root can read:

```
sudo apt install cifs-utils
sudo mkdir -p /mnt/robert
sudo tee /etc/samba/credentials-robert > /dev/null <<'EOF'
username=YOUR_USER
password=YOUR_PASSWORD
EOF
sudo chmod 600 /etc/samba/credentials-robert
```

Add the share to `/etc/fstab` (`id -u` and `id -g` give your uid and gid, usually 1000):

```
echo '//Robert/personal /mnt/robert cifs credentials=/etc/samba/credentials-robert,uid=1000,gid=1000,iocharset=utf8,_netdev,nofail,x-systemd.automount,x-systemd.idle-timeout=60 0 0' | sudo tee -a /etc/fstab
sudo systemctl daemon-reload
sudo mount /mnt/robert
```

`x-systemd.automount` mounts it on first access and `nofail` stops boot hanging if Robert is offline. If `//Robert` doesn't resolve, use its IP address instead.

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

### Linux

A second publish profile (`Properties/PublishProfiles/SelfContainedLinuxX64.pubxml`) produces a single, self-contained `CanonImport` binary, published to `~/.local/share/CanonImport/` alongside the settings files.

```
cd CanonImport
dotnet publish -p:PublishProfile=SelfContainedLinuxX64
```

To run it as `canonimport` from any terminal, symlink it into `~/.local/bin` once (this is on the PATH by default on most distros):

```
mkdir -p ~/.local/bin
ln -sf ~/.local/share/CanonImport/CanonImport ~/.local/bin/canonimport
```

If `~/.local/bin` isn't on your PATH, add it and reload the shell:

```
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.bashrc
source ~/.bashrc
```

Then run `canonimport /run/media/$USER/EOS_DIGITAL` from anywhere.
