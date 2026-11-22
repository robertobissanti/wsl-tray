# WSL Tray

A tiny glowing button in the Windows notification area that shows at a glance whether **WSL** is running, and turns it on or off with one click. Linux only uses RAM while you actually need it.

| Off | On | Shutting down |
|:---:|:---:|:---:|
| <img src="docs/terminal-off.png" width="64"> | <img src="docs/terminal-on.png" width="64"> | <img src="docs/terminal-standby.png" width="64"> |

- **Red**: WSL is off and uses no RAM.
- **Green**: a Linux distribution is running. The tooltip shows which one and how much RAM the VM is using.
- **Amber**: WSL is starting or stopping, or no distro is running but the VM is still in memory. The VM shuts down by itself after `vmIdleTimeout`.

## Usage

- **Left click**: turns WSL on or off. It **always asks for confirmation** first, and the default button is *No*.
- **Right click**: a menu with the status, Turn on, Turn off, Open Linux terminal, Icon style, Start with Windows and Exit.
- **Exit** only closes the icon. WSL stays as it is.

To turn WSL on, WSL Tray starts a hidden `sleep infinity` process in the default distribution so the distro stays up. To turn it off, it runs `wsl --shutdown`, which stops every distribution and the VM and gives the RAM back to Windows.

The status refreshes every 3 seconds using `wsl -l --running -q` and by checking for the `vmmemWSL` process.

### Icon styles

You can pick the icon style from the right-click menu → **Icon style**. Your choice is remembered in `HKCU\Software\WslTray`.

![icon styles](docs/icon-styles.png)

### Languages

The interface follows the Windows display language: **English** (default), Italian, Spanish, French, German and Portuguese. Any other language falls back to English. To force a language, use `WslTray.exe --lang it`.

To add a translation, copy the English block in [`src/Strings.cs`](src/Strings.cs), translate it and send a pull request.

## Download

Grab `WslTray.exe` (or the zip) from the [latest release](https://github.com/robertobissanti/wsl-tray/releases/latest) and run it. Nothing needs installing.

The executable isn't code-signed, so Windows SmartScreen may show *"Windows protected your PC"* the first time you run it. Click **More info → Run anyway**. If you'd rather not, build it from source yourself (see below). Each release lists the SHA-256 checksums of its files.

## Build

There is nothing to install. The build uses the C# compiler that ships with Windows (.NET Framework 4.x).

```powershell
.\build.ps1          # creates bin\WslTray.exe
.\bin\WslTray.exe
```

To start it at sign-in, tick **Start with Windows** in the menu. The setting is written to `HKCU\...\Run`, so no admin rights are needed.

`WslTray.exe --render <folder>` saves the icons as PNG files and builds the comparison sheet shown above.

## Tip: cap WSL's memory

With no configuration, WSL 2 can use up to 50% of your RAM. Here is an example `%USERPROFILE%\.wslconfig`:

```ini
[wsl2]
memory=6GB
swap=4GB
vmIdleTimeout=60000

[experimental]
autoMemoryReclaim=gradual
sparseVhd=true
```

## Requirements

- Windows 10/11 with WSL 2
- .NET Framework 4.x (already included in Windows)

## License

[BSD 3-Clause](LICENSE) © 2026 Roberto Bissanti
