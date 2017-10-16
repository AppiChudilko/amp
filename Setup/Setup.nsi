;NSIS Modern User Interface
;GTA NETWORK INSTALLER

  !include "MUI2.nsh"

  Name "Appi Multiplayer"
  OutFile "AMPSetup.exe"

  InstallDir "C:\AppiMultiplayer"

  RequestExecutionLevel admin

  !define MUI_ABORTWARNING
  
  !insertmacro MUI_PAGE_LICENSE "License.txt"
  !insertmacro MUI_PAGE_DIRECTORY
  !insertmacro MUI_PAGE_INSTFILES
  
  !insertmacro MUI_UNPAGE_CONFIRM
  !insertmacro MUI_UNPAGE_INSTFILES
  !insertmacro MUI_LANGUAGE "English"

Section "Client" SecDummy

  SetOutPath "$INSTDIR"

${If} ${FileExists} "$INSTDIR\*"
     RMDir /r "$INSTDIR"
${EndIf}

  File /r "C:\Users\racke\Desktop\Appi MP\MP\*"
 
  CreateShortCut "$DESKTOP\AppiMultiplayer.lnk" "$INSTDIR\AMPLauncher.exe" ""
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKLM "HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Grand Theft Auto V" "AppiMultiplayerInstallDir" $INSTDIR

SectionEnd

Section "Uninstall"

  Delete "$INSTDIR\Uninstall.exe"
  Delete "$DESKTOP\AppiMultiplayer.lnk"
  RMDir /r /REBOOTOK "$INSTDIR"
  DeleteRegKey /ifempty HKLM "HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Rockstar Games\Grand Theft Auto V\AppiMultiplayerInstallDir"

SectionEnd