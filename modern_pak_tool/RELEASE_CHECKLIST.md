# Modern PAK Tool Release Checklist

Use this before publishing `PAKToolInstallation.exe`.

## Build

- Run `.\build-installer.ps1`.
- Confirm `dist\PAKToolInstallation.exe` exists.
- Confirm `dist\PAKToolInstallation.exe.sha256` exists.
- Confirm `obj\installer-stage` contains only:
  - `ModernPakTool.exe`
  - `Engine\PakEngineHost.exe`
  - `Engine\engine.dll`
  - `Engine\lualibdll.dll`

## Installer Checks

- Install to the default Program Files location.
- Install to a custom path with spaces.
- Install for current user if admin elevation is not available.
- Install with desktop shortcut enabled.
- Install with desktop shortcut disabled.
- Launch from Start Menu.
- Launch from desktop shortcut.
- Launch directly from the installed folder.
- Uninstall and confirm shortcuts are removed.
- Reinstall over the same version.
- Upgrade from the previous public version when one exists.

## Runtime Checks

- Confirm startup preflight does not show `Engine Missing`.
- Pack a small folder and confirm `.pak` plus `.pak.txt` are created.
- Unpack the new `.pak` and compare file contents.
- Unpack a `.pak` with a real sidecar.
- Unpack a `.pak` without a sidecar.
- Unpack a `.pak` with `_unpacked_pak\<pak name>\_manifest.tsv` nearby.
- Test a long output path.
- Test a path containing non-ASCII characters without renaming, normalising, or re-encoding game files.
- Test an output folder without write permission and confirm the error is clear.

## Platform Checks

- Windows 10 x64.
- Windows 11 x64.
- Windows 11 ARM hardware, confirming the app runs through x86 emulation.
- Windows 10 ARM only if that platform is intentionally supported.

## Public Download Checks

- Publish the SHA256 checksum beside the installer.
- State clearly that Windows on ARM support is x86 emulation, not native ARM64.
- Do not upload installer binaries into a SQL database.
- Keep the future website in a separate project from this desktop app folder.
- Use real app screenshots on the website.
- Keep the primary download action plain: `Download for Windows`.

## Release Boundaries

- Do not ship `PAKMAKER.exe`.
- Do not ship source files.
- Do not ship docs inside the installed app folder.
- Do not ship `super_logo.png` or `super_logo.ico` as runtime files.
- Do not delete user-created PAK files, extracted files, sidecars, or recovery outputs during uninstall.
