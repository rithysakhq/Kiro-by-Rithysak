# Modern PAK Tool Website

Static Vercel landing page for Modern PAK Tool by Rithysak.

## Structure

```text
index.html
styles.css
script.js
downloads/
  PAKToolInstallation.exe
  PAKToolInstallation.exe.sha256
images/
  app-screenshot.png
vercel.json
```

## Deployment

This site has no build step and no runtime server. Deploy the repository root to Vercel as a static project.

Direct installer URL after deployment:

```text
/downloads/PAKToolInstallation.exe
```

SHA256:

```text
AE27A2368C5F65F98B89E39CD9A75D00CBE93A1255BF0483CF46A26B7411F6C7
```

## Compatibility Copy

The page intentionally states that Windows on ARM support is x86 emulation, not native ARM64, because the legacy engine DLLs are x86.
