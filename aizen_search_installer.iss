; ==============================================================================
; AizenRex Search-man Professional Installer Script (Inno Setup 6)
; Architect & Lead Developer: Riyad (Aizen)
; Version: 0.3.6 Professional Edition
; ==============================================================================

#define MyAppName "AizenRex Search-man"
#define MyAppPublisher "Riyad"
#define MyAppURL "https://github.com/aizenrexx/AizenSearch"
#define MyAppExeName "AizenSearch.App.exe"

; Paths are resolved relative to AIZEN_SOURCE_ROOT when set (CI/cloud build),
; otherwise they fall back to the local development folder.
#define MySourceRoot GetEnv('AIZEN_SOURCE_ROOT')
#if MySourceRoot == ""
#define MySourceRoot "H:\My Project Coding\Lindy My Boss\My Software\Searching"
#endif

; Version comes from AIZEN_VERSION when set (CI/cloud build), otherwise local default.
#define MyAppVersion GetEnv('AIZEN_VERSION')
#if MyAppVersion == ""
#define MyAppVersion "0.3.7"
#endif

#define MyAppIcon MySourceRoot + "\src\AizenSearch.App\AizenRex.ico"
#define MyLicense MySourceRoot + "\license.txt"
#define MySourceDir MySourceRoot + "\Distribution\Portable"
#define MyOutputDir MySourceRoot + "\Distribution\Installer"

[Setup]
; Unique application GUID for seamless in-place upgrade and downgrade
AppId={{D37E690B-487A-4D7A-974F-71DF9A3644B2}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile={#MyLicense}
OutputDir={#MyOutputDir}
OutputBaseFilename=AizenRex-Search-man-Setup-v{#MyAppVersion}
SetupIconFile={#MyAppIcon}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequiredOverridesAllowed=commandline dialog
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=AizenRex Search-man Setup - Lightweight Desktop Search
VersionInfoCopyright=Copyright (C) 2026 Riyad. All rights reserved.
DisableWelcomePage=no

; Seamless upgrade/downgrade settings:
; Overwrite existing binaries in-place regardless of file timestamps or versions
; while guaranteeing user cache in %LocalAppData%\AizenSearch is preserved.
UsePreviousAppDir=yes
DisableDirPage=auto
AppMutex=AizenSearchAppMutex
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Launch AizenRex Search-man automatically on Windows startup"; GroupDescription: "System Integration:"; Flags: unchecked
Name: "contextmenu"; Description: "Add 'Search with AizenRex Search-man' to Windows Explorer folder context menu"; GroupDescription: "System Integration:"; Flags: unchecked

[Files]
; ignoreversion flag allows seamless upgrade AND downgrade without prompt or version block
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#MyAppIcon}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\AizenRex.ico"
Name: "{group}\{#MyAppName} (Safe Mode)"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--safe-mode"; IconFilename: "{app}\AizenRex.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\AizenRex.ico"; Tasks: desktopicon

[Registry]
; Windows Startup Task
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "AizenSearch"; ValueData: """{app}\{#MyAppExeName}"" --background"; Flags: uninsdeletevalue; Tasks: startup

; Windows Explorer Directory Context Menu Integration
Root: HKCR; Subkey: "Directory\shell\AizenSearch"; ValueType: string; ValueName: ""; ValueData: "Search with AizenRex Search-man"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCR; Subkey: "Directory\shell\AizenSearch"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AizenRex.ico"""; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCR; Subkey: "Directory\shell\AizenSearch\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Flags: uninsdeletekey; Tasks: contextmenu

; Windows Explorer Directory Background Context Menu Integration
Root: HKCR; Subkey: "Directory\Background\shell\AizenSearch"; ValueType: string; ValueName: ""; ValueData: "Search with AizenRex Search-man"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCR; Subkey: "Directory\Background\shell\AizenSearch"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\AizenRex.ico"""; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKCR; Subkey: "Directory\Background\shell\AizenSearch\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%V"""; Flags: uninsdeletekey; Tasks: contextmenu

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

