[Setup]
AppName=KORIX Tweaks
AppVersion=1.0.0
AppPublisher=KORIX
DefaultDirName={autopf}\KORIX Tweaks
DefaultGroupName=KORIX Tweaks
OutputDir=C:\Users\hong0\OneDrive\Desktop\KORIX프로젝트\KORIX_Tweaks_Installer
OutputBaseFilename=KORIX_Tweaks_Setup_v6
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
CreateAppDir=yes
CreateUninstallRegKey=yes
UninstallDisplayIcon={app}\KORIX_Tweaks.exe
SetupIconFile=C:\Users\hong0\OneDrive\Desktop\KORIX프로젝트\assets\KORIX.ico
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"

[Files]
Source: "C:\Users\hong0\OneDrive\Desktop\KORIX프로젝트\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\KORIX Tweaks"; Filename: "{app}\KORIX_Tweaks.exe"
Name: "{autodesktop}\KORIX Tweaks"; Filename: "{app}\KORIX_Tweaks.exe"

[Run]
Filename: "{app}\KORIX_Tweaks.exe"; Description: "KORIX Tweaks 실행"; Flags: nowait postinstall skipifsilent shellexec
