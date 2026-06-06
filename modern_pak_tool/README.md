# Kiro by Rithysak

Compact WPF recovery workbench for JX2 PAK archives. It still packs and unpacks through the legacy engine, and now adds inspection, inventory, and safer explanations for sidecar-less files.

## Workbench Modes

- `Pack`: builds a PAK from a folder and writes the matching `.pak.txt` sidecar.
- `Unpack`: extracts an archive, prefers real sidecars, and generates structured recovery summaries.
- `Inspect`: scans any unpacked output folder without rewriting game payloads, classifies recovered names, typed unknowns, and unknown binaries, and writes `_pak_tool_inventory.tsv`.
- `Reports`: shows the latest operation summary and opens generated inventory/recovery reports.
- `Settings`: shows safe-first defaults and the reference manifest paths searched for the selected PAK.

## Local Build

Run from this folder:

```powershell
.\build.ps1
```

Development output is written to `bin\`:

```text
bin\
  ModernPakTool.exe
  PakEngineHost.exe
  engine.dll
  lualibdll.dll
  kiro_app_icon.ico
```

`kiro_app_icon.ico` is a build artifact used for the executable and installer icon. The Kiro app icon and full wordmark PNGs are embedded into `ModernPakTool.exe`; they are not needed as runtime files after a clean build.

## Installer Build

Install Inno Setup 6, then run:

```powershell
.\build-installer.ps1
```

The script:

- runs a clean app build,
- stages a compact install layout under `obj\installer-stage`,
- verifies the staged legacy engine with `PakEngineHost.exe probe`,
- compiles `installer\ModernPakTool.iss`,
- writes `dist\KiroSetup.exe`,
- writes `dist\KiroSetup.exe.sha256`.

Temporary staging is removed after a successful build. Use `-KeepStage` if you need to inspect it.

If `ISCC.exe` is not on `PATH`, pass it explicitly:

```powershell
.\build-installer.ps1 -InnoSetupCompiler "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

## Installed Layout

The installer intentionally keeps the installed folder small:

```text
Kiro by Rithysak\
  ModernPakTool.exe
  Engine\
    PakEngineHost.exe
    engine.dll
    lualibdll.dll
  Uninstall\
    unins000.exe
    unins000.dat
```

The app prefers `Engine\PakEngineHost.exe` in installed builds and still supports same-folder engine files for local development builds.

The installer does not include:

- `PAKMAKER.exe`
- `RecoverySmoke.exe`
- `Kiro by Rithysak - AppIcon.png`
- `Kiro by Rithysak - full logo.png`
- `kiro_app_icon.ico`
- source files or notes

## Platform Support

- x86 Windows: supported natively.
- x64 Windows: supported through normal 32-bit app compatibility.
- Windows on ARM: supported through Windows x86 emulation.
- Native ARM64: not supported in v1 because `engine.dll` and `lualibdll.dll` are x86 native DLLs.

## Runtime Notes

- The app is built as x86 because the legacy backend DLLs are x86.
- The pack flow writes the PAK header, payloads, index table, and matching TXT sidecar through Kiro's managed builder. Users do not need to provide a TXT file when packing a folder.
- Packing validates duplicate archive IDs, GBK path encoding, generated index data, header CRC, uncompressed entry flags, and sidecar consistency before reporting success.
- The unpack flow prefers the matching TXT sidecar because it contains original file paths.
- If no TXT sidecar exists, the app first looks for a reference `_unpacked_pak\<pak name>\_manifest.tsv` near the selected client data.
- When that reference manifest is found, the app shows known/unmapped counts before extraction, stages extraction in a temp folder, restores known files to original paths, and preserves manifest-unmapped IDs under `_unknown_by_id` with conservative inferred extensions where file signatures or text patterns are clear.
- If no reference manifest is found, the app falls back to `_ID_<hash>` names and adds conservative inferred extensions where file signatures or text patterns are clear.
- After unpacking, the app writes `_pak_tool_inventory.tsv` and `_pak_tool_recovery_summary.txt` beside the output. These reports explain that unknown-ID files are valid extracted entries whose original paths were not recovered, not automatically corrupted files.
- Exact-reference recovery does not leave generated-ID files, temporary manifests, or recovery reports in the output folder.
- Extension-only fallback writes `_pak_tool_name_recovery_report.txt` because that mode is inferential and should document its guesses.
- See `PAK_FORMAT_NOTES.md` for the investigated TXT/sidecar behavior.
- See `PAK_NAME_RECOVERY_NOTES.md` for why sidecar-less archives unpack as `_ID_<hash>` files without original extensions.

## Website Distribution Boundary

The future download website should be a separate project. Do not mix website files into this desktop app folder.

Recommended v1 website stack:

- Vercel for hosting.
- GitHub Releases, Supabase Storage, or similar object storage for `KiroSetup.exe`.
- Supabase database only if accounts, download logs, release metadata, or a waitlist are needed.

The website should state that Windows on ARM support uses x86 emulation, not native ARM64.

See `RELEASE_CHECKLIST.md` before publishing a public installer.
