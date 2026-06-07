# Kiro by Rithysak

Kiro is a compact Windows workbench for JX PAK archives. It packs folders, unpacks archives, checks sidecar manifest status, inspects extracted outputs, and writes recovery reports without rewriting legacy game payloads.

Website: https://rithysakhq.github.io/Kiro-by-Rithysak/

## Project Layout

- `modern_pak_tool/`: WPF desktop app, legacy engine host, installer script, and release output.
- `modern-pak-tool-site/`: static Vercel website and public installer download.
- `assets/`: Kiro source brand assets used by the app and website.

## Build

```powershell
.\modern_pak_tool\build.ps1
.\modern_pak_tool\build-installer.ps1
```

The release installer is written to:

```text
modern_pak_tool\dist\KiroSetup.exe
```

## Website

The public website is a static site under `modern-pak-tool-site/`. The current download file is:

```text
modern-pak-tool-site\downloads\KiroSetup.exe
```

Vercel should deploy from the `modern-pak-tool-site` directory.

The root `vercel.json` also supports repo-root imports by rewriting public routes to `modern-pak-tool-site/`.
