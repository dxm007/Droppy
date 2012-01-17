;------------------------------------------------------------------------------
;------------------------------------------------------------------------------
;
; Copyright (c) 2012 Dennis Mnuskin
;
; This file is part of Droppy application.
;
; This source code is distributed under the MIT license.  For full text, see
; <http://www.opensource.org/licenses/mit-license.php>. The same text can be
; found in LICENSE file, which is located in root directory of the project.
;
;------------------------------------------------------------------------------
;------------------------------------------------------------------------------

;------------------------------------------------------------------------------
; Project/Application Settings
;------------------------------------------------------------------------------
!define PROJECT_ROOT_FOLDER     "..\.."
!define PRODUCT_NAME            "Droppy"
!define PRODUCT_VERSION         "1.0.0.0"
!define SETUP_NAME              "${PRODUCT_NAME}Setup-${PRODUCT_VERSION}.exe"
!define APP_EXE_NAME            "${PRODUCT_NAME}.exe"
!define APP_EXE_PATH            "$INSTDIR\${APP_EXE_NAME}"
!define OUTPUT_DIRECTORY        "${PROJECT_ROOT_FOLDER}\Redist"


;------------------------------------------------------------------------------
; General Installer Settings
;------------------------------------------------------------------------------
OutFile       "${OUTPUT_DIRECTORY}\${SETUP_NAME}"
Name          "${PRODUCT_NAME} ${PRODUCT_VERSION}"

RequestExecutionLevel user      ; We are using UAC plugin to elevate to admin, outer
                                ; installer shell must run as regular user

!define MULTIUSER_EXECUTIONLEVEL Highest
!define MULTIUSER_MUI
!define MULTIUSER_INSTALLMODE_COMMANDLINE
!define MULTIUSER_INSTALLMODE_INSTDIR    "${PRODUCT_NAME}"

!define UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"


;------------------------------------------------------------------------------
; External Includes
;------------------------------------------------------------------------------
!include "LogicLib.nsh"
!include "MultiUser.nsh"
!include "MUI.nsh"
!include "FileFunc.nsh"


;------------------------------------------------------------------------------
; Functions
;------------------------------------------------------------------------------

;--------------------------------------------------------------------
Function .onInit
    !insertmacro MULTIUSER_INIT
FunctionEnd


;--------------------------------------------------------------------
Function un.onInit
    !insertmacro MULTIUSER_UNINIT
FunctionEnd


;--------------------------------------------------------------------
Function CreateStartMenuShortcut
    CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}.lnk "${APP_EXE_PATH}" "${APP_EXE_PATH}"
FunctionEnd


;--------------------------------------------------------------------
Function un.CreateStartMenuShortcut
    Delete "$SMPROGRAMS\${PRODUCT_NAME}.lnk"
FunctionEnd


;--------------------------------------------------------------------
; This function must be called after all the files are copied over to the production system
; because it uses the size of the $INSTDIR to report to windows how big the application is
Function WriteUninstallRegInfo
    WriteRegStr SHCTX "${UNINST_KEY}" "DisplayName" "${PRODUCT_NAME}"
    WriteRegStr SHCTX "${UNINST_KEY}" "UninstallString" \
                "$\"$INSTDIR\uninstall.exe$\" /$MultiUser.InstallMode"
    WriteRegStr SHCTX "${UNINST_KEY}" "QuietUninstallString" \
                "$\"$INSTDIR\uninstall.exe$\" /$MultiUser.InstallMode /S"
    WriteRegStr SHCTX "${UNINST_KEY}" "DisplayIcon" "${APP_EXE_PATH}"
    WriteRegStr SHCTX "${UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
                
    ${GetSize} "$INSTDIR" "/S=OK" $0 $1 $2
    IntFmt $0 "0x%08X" $0
    WriteRegStr SHCTX "${UNINST_KEY}" "EstimatedSize" "$0"
FunctionEnd

;--------------------------------------------------------------------
Function un.WriteUninstallRegInfo
    DeleteRegKey SHCTX "${UNINST_KEY}"
FunctionEnd


;------------------------------------------------------------------------------
; Pages
;------------------------------------------------------------------------------
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${PROJECT_ROOT_FOLDER}\LICENSE"
!insertmacro MULTIUSER_PAGE_INSTALLMODE
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_RUN "${APP_EXE_PATH}"
!define MUI_FINISHPAGE_RUN_TEXT "Run ${PRODUCT_NAME} now"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES


;------------------------------------------------------------------------------
; Language
;------------------------------------------------------------------------------
!insertmacro MUI_LANGUAGE "English"


;------------------------------------------------------------------------------
; Installer Sections
;------------------------------------------------------------------------------

;--------------------------------------------------------------------
Section "-Install"
    SetOutPath "$INSTDIR"
    
    File "${PROJECT_ROOT_FOLDER}\bin\Release\*.*"
    
    WriteUninstaller "$INSTDIR\Uninstall.exe"
    
    Call CreateStartMenuShortcut
    
    ; this call has to be made AFTER all the files were copied over
    Call WriteUninstallRegInfo

SectionEnd


;--------------------------------------------------------------------
Section "Uninstall"

    Delete "$INSTDIR\${APP_EXE_NAME}"
    Delete "$INSTDIR\${APP_EXE_NAME}.Config"
    Delete "$INSTDIR\LICENSE"
    
    Delete "$INSTDIR\Uninstall.exe"
    Delete "$INSTDIR\Install.log"
    
    RMDir "$INSTDIR"

    Call un.CreateStartMenuShortcut
    
    Call un.WriteUninstallRegInfo
    
SectionEnd

