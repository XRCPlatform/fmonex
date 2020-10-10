;--------------------------------
;Include Modern UI
  !include "TextFunc.nsh" ;Needed for the $GetSize function. I know, doesn't sound logical, it isn't.
  !include "MUI2.nsh"

;--------------------------------
;Variables

  !define PRODUCT_NAME "FreeMarketOne"
  !define PRODUCT_WEB_SITE "https://www.freemarket.one"
  !define PRODUCT_PUBLISHER "Bitcoin Rhodium Developers"
  !define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"

;--------------------------------
;General


  Name "${PRODUCT_NAME}"
  OutFile "freemarketone-setup.exe"
  InstallDir "$PROGRAMFILES\${PRODUCT_NAME}"
  InstallDirRegKey HKCU "Software\${PRODUCT_NAME}" ""
  RequestExecutionLevel admin
  CRCCheck on
  ShowInstDetails show
  ShowUninstDetails show
  InstallColors /windows
  SetCompressor /SOLID lzma
  SetCompressorDictSize 64
  BrandingText "${PRODUCT_NAME} Installer v${PRODUCT_VERSION}"
  Caption "${PRODUCT_NAME}"
  VIProductVersion 1.0.0.0
  VIAddVersionKey ProductName "${PRODUCT_NAME} Installer"
  VIAddVersionKey Comments "The installer for ${PRODUCT_NAME}"
  VIAddVersionKey CompanyName "${PRODUCT_NAME}"
  VIAddVersionKey LegalCopyright "2020 ${PRODUCT_PUBLISHER}"
  VIAddVersionKey FileDescription "${PRODUCT_NAME} Installer"
  VIAddVersionKey FileVersion ${PRODUCT_VERSION}
  VIAddVersionKey ProductVersion ${PRODUCT_VERSION}
  VIAddVersionKey InternalName "${PRODUCT_NAME} Installer"
  VIAddVersionKey LegalTrademarks "${PRODUCT_NAME} is a trademark of ${PRODUCT_PUBLISHER}"
  VIAddVersionKey OriginalFilename "${PRODUCT_NAME}.exe"

;--------------------------------
;Interface Settings

  !define MUI_ABORTWARNING
  !define MUI_ABORTWARNING_TEXT "Are you sure you wish to abort the installation of ${PRODUCT_NAME}?"

  !define MUI_ICON "c:\fmone\FreeMarket.ico"

;--------------------------------
;Pages

  !insertmacro MUI_PAGE_DIRECTORY
  !insertmacro MUI_PAGE_INSTFILES
  !insertmacro MUI_UNPAGE_CONFIRM
  !insertmacro MUI_UNPAGE_INSTFILES

;--------------------------------
;Languages

  !insertmacro MUI_LANGUAGE "English"

;--------------------------------
;Installer Sections

;Check if we have Administrator rights
Function .onInit
	UserInfo::GetAccountType
	pop $0
	${If} $0 != "admin" ;Require admin rights on NT4+
		MessageBox mb_iconstop "Administrator rights required!"
		SetErrorLevel 740 ;ERROR_ELEVATION_REQUIRED
		Quit
	${EndIf}
FunctionEnd

!include LogicLib.nsh
 
!ifndef IPersistFile
!define IPersistFile {0000010b-0000-0000-c000-000000000046}
!endif
!ifndef CLSID_ShellLink
!define CLSID_ShellLink {00021401-0000-0000-C000-000000000046}
!define IID_IShellLinkA {000214EE-0000-0000-C000-000000000046}
!define IID_IShellLinkW {000214F9-0000-0000-C000-000000000046}
!define IShellLinkDataList {45e2b4ae-b1c3-11d0-b92f-00a0c90312e1}
	!ifdef NSIS_UNICODE
	!define IID_IShellLink ${IID_IShellLinkW}
	!else
	!define IID_IShellLink ${IID_IShellLinkA}
	!endif
!endif
 
Function ShellLinkSetRunAs
System::Store S
pop $9
System::Call "ole32::CoCreateInstance(g'${CLSID_ShellLink}',i0,i1,g'${IID_IShellLink}',*i.r1)i.r0"
${If} $0 = 0
	System::Call "$1->0(g'${IPersistFile}',*i.r2)i.r0" ;QI
	${If} $0 = 0
		System::Call "$2->5(w '$9',i 0)i.r0" ;Load
		${If} $0 = 0
			System::Call "$1->0(g'${IShellLinkDataList}',*i.r3)i.r0" ;QI
			${If} $0 = 0
				System::Call "$3->6(*i.r4)i.r0" ;GetFlags
				${If} $0 = 0
					System::Call "$3->7(i $4|0x2000)i.r0" ;SetFlags ;SLDF_RUNAS_USER
					${If} $0 = 0
						System::Call "$2->6(w '$9',i1)i.r0" ;Save
					${EndIf}
				${EndIf}
				System::Call "$3->2()" ;Release
			${EndIf}
		System::Call "$2->2()" ;Release
		${EndIf}
	${EndIf}
	System::Call "$1->2()" ;Release
${EndIf}
push $0
System::Store L
FunctionEnd

Section
  SetOutPath $INSTDIR

  ;Uninstall previous version files
  RMDir /r "$INSTDIR\*.*"
  Delete "$DESKTOP\${PRODUCT_NAME}.lnk"
  Delete "$SMPROGRAMS\${PRODUCT_NAME}\*.*"

  ;Files to pack into the installer
  File /r "c:\fmone\*.*"

  ;Store installation folder
  WriteRegStr HKCU "Software\${PRODUCT_NAME}" "" $INSTDIR

  ;Create uninstaller
  DetailPrint "Creating uninstaller..."
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  ;Create desktop shortcut
  DetailPrint "Creating desktop shortcut..."
  CreateShortCut "$DESKTOP\${PRODUCT_NAME}.lnk" "$INSTDIR\FreeMarketApp.exe" "" "$INSTDIR\FreeMarket.ico" 0
  ;hack to set run as admin
  push "$DESKTOP\${PRODUCT_NAME}.lnk"
  call ShellLinkSetRunAs
  pop $0
  DetailPrint HR=$0

  ;Create start-menu items
  DetailPrint "Creating start-menu items..."
  CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\Uninstall.lnk" "$INSTDIR\Uninstall.exe" "" "$INSTDIR\Uninstall.exe" 0
  CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk" "$INSTDIR\FreeMarketApp.exe" "" "$INSTDIR\FreeMarket.ico" 0
  ;hack to set run as admin
  push "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk"
  call ShellLinkSetRunAs
  pop $0
  DetailPrint HR=$0

  ;Create link in app folder
  CreateShortCut "$INSTDIR\${PRODUCT_NAME}.lnk" "$INSTDIR\FreeMarketApp.exe" "" "$INSTDIR\FreeMarket.ico" 0
  ;hack to set run as admin
  push "$INSTDIR\${PRODUCT_NAME}.lnk"
  call ShellLinkSetRunAs
  pop $0
  DetailPrint HR=$0
  
  ;Adds an uninstaller possibility to Windows Uninstall or change a program section
  WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "DisplayName" "$(^Name)"
  WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
  WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
  WriteRegStr HKCU "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\FreeMarket.ico"

  ;Fixes Windows broken size estimates
  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKCU "${PRODUCT_UNINST_KEY}" "EstimatedSize" "$0"
SectionEnd

;--------------------------------
;Descriptions

;--------------------------------
;Uninstaller Section

Section "Uninstall"
  RMDir /r "$INSTDIR\*.*"

  RMDir "$INSTDIR"

  Delete "$DESKTOP\${PRODUCT_NAME}.lnk"
  Delete "$SMPROGRAMS\${PRODUCT_NAME}\*.*"
  RMDir  "$SMPROGRAMS\${PRODUCT_NAME}"

  DeleteRegKey HKCU "Software\Classes\bitcoin"
  DeleteRegKey HKCU "Software\${PRODUCT_NAME}"
  DeleteRegKey HKCU "${PRODUCT_UNINST_KEY}"
SectionEnd
