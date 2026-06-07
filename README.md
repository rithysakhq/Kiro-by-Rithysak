# Kiro by Rithysak

Kiro is a compact Windows workbench for JX PAK archives. It packs folders, unpacks archives, checks sidecar manifest status, inspects extracted outputs, and writes recovery reports without rewriting legacy game payloads.

## Project Layout

- `modern_pak_tool/`: WPF desktop app, legacy engine host, installer script, and release output.
- `assets/`: Kiro source brand assets used by the app.

## Build

```powershell
.\modern_pak_tool\build.ps1
.\modern_pak_tool\build-installer.ps1
```

The release installer is written to:

```text
modern_pak_tool\dist\KiroSetup.exe
```

## Distribution

The public website and download publishing are maintained outside this desktop
tool repository. Build the installer here, then hand `modern_pak_tool\dist\KiroSetup.exe`
and `modern_pak_tool\dist\KiroSetup.exe.sha256` to the website owner for release.
