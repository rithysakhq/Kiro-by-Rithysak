#define AppName "Kiro by Rithysak"
#define AppVersion "1.0.0"
#define AppVersionInfo "1.0.0.0"
#define AppPublisher "Rithysak"
#define AppExeName "ModernPakTool.exe"
#define StageDir "..\obj\installer-stage"

[Setup]
AppId={{9A369E7C-1D96-4E49-A865-B2D52F44E881}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=..\dist
OutputBaseFilename=KiroSetup
SetupIconFile=..\bin\kiro_app_icon.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x86compatible
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
UninstallFilesDir={app}\Uninstall
UninstallDisplayIcon={app}\{#AppExeName}
CloseApplications=yes
VersionInfoVersion={#AppVersionInfo}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Installer
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#StageDir}\ModernPakTool.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#StageDir}\Engine\PakEngineHost.exe"; DestDir: "{app}\Engine"; Flags: ignoreversion
Source: "{#StageDir}\Engine\engine.dll"; DestDir: "{app}\Engine"; Flags: ignoreversion
Source: "{#StageDir}\Engine\lualibdll.dll"; DestDir: "{app}\Engine"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
