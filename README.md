# Kiro Website

Static Vercel landing page for Kiro by Rithysak.

## Structure

```text
index.html
styles.css
script.js
downloads/
  KiroSetup.exe
  KiroSetup.exe.sha256
images/
  app-screenshot.png
  kiro-app-icon.png
  kiro-full-logo.png
vercel.json
```

## Deployment

This site has no build step and no runtime server. Deploy the site root to Vercel as a static project.

Direct installer URL after deployment:

```text
/downloads/KiroSetup.exe
```

The legacy `PAKToolInstallation.exe` URLs redirect to the Kiro setup file.

## Compatibility Copy

The page intentionally states that Windows on ARM support uses x86 emulation, not native ARM64, because the legacy engine DLLs are x86.
