#ifndef AppVersion
#define AppVersion "0.1.0"
#endif

#ifndef PublishDir
#define PublishDir "..\artifacts\publish\win-x64"
#endif

#define AppExeName "XartZans.WinManager.exe"

[Setup]
AppId={{8B958D54-3A7F-4F20-A8C2-7088E57A3F19}
AppName=XartZan's WinManager
AppVersion={#AppVersion}
AppPublisher=XartZan
DefaultDirName={localappdata}\Programs\XartZan's WinManager
DefaultGroupName=XartZan's WinManager
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName=XartZan's WinManager
UninstallDisplayIcon={app}\{#AppExeName}
OutputDir=..\artifacts\installer
OutputBaseFilename=XartZans-WinManager-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[CustomMessages]
english.LaunchApplication=Launch XartZan's WinManager now
turkish.LaunchApplication=Kurulumdan sonra XartZan's WinManager'ı başlat

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\XartZan's WinManager"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\XartZan's WinManager"; Filename: "{app}\{#AppExeName}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchApplication}"; Flags: postinstall nowait skipifsilent
