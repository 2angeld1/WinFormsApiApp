; Script de Inno Setup para ECM Desktop - RUTAS COMPLETAMENTE CORREGIDAS
; Aplicación de gestión documental con impresora virtual y escáner

[Setup]
; Información básica de la aplicación
AppId={{ECM-Desktop-B8A2F3C4-D5E6-7F89-A0B1-C2D3E4F5A6B7}
AppName=ECM Desktop
AppVersion=1.0.0
AppVerName=ECM Desktop 1.0.0
AppPublisher=ECM Central
AppPublisherURL=https://ecmcentral.com
AppSupportURL=https://ecmcentral.com/support
AppUpdatesURL=https://ecmcentral.com/updates
AppCopyright=Copyright (C) 2025 ECM Central

; Configuración de instalación
DefaultDirName={autopf}\ECM Desktop
DefaultGroupName=ECM Desktop
AllowNoIcons=yes
LicenseFile=License.txt
InfoBeforeFile=ReadMe.txt
OutputDir=Output
OutputBaseFilename=ECMDesktop_Setup_v1.0.0
SetupIconFile=icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

; Requisitos del sistema
MinVersion=6.1sp1
ArchitecturesAllowed=x86 x64
ArchitecturesInstallIn64BitMode=x64

; Privilegios administrativos (necesario para instalar impresora virtual)
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

; Configuración del asistente
WizardImageFile=WizardImage.bmp
WizardSmallImageFile=WizardSmallImage.bmp
WizardImageStretch=no
WizardImageBackColor=clWhite

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode
Name: "autostart"; Description: "Iniciar ECM Desktop automáticamente con Windows"; GroupDescription: "Opciones de inicio"; Flags: unchecked

[Files]
; ✅ EJECUTABLE PRINCIPAL Y CONFIGURACIÓN - RUTAS CORREGIDAS
Source: "WinFormsApiClient\bin\Release\WinFormsApiClient.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "WinFormsApiClient\bin\Release\WinFormsApiClient.exe.config"; DestDir: "{app}"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\bin\Release\WinFormsApiClient.exe.config'))
Source: "WinFormsApiClient\App.config"; DestDir: "{app}"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\App.config'))

; ✅ TODAS LAS DLLs DEL RELEASE - RUTA CORREGIDA
Source: "WinFormsApiClient\bin\Release\*.dll"; DestDir: "{app}"; Flags: ignoreversion

; ✅ LIBRERÍAS ESPECÍFICAS - RUTAS CORREGIDAS
Source: "WinFormsApiClient\bin\Release\MaterialSkin.dll"; DestDir: "{app}"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\bin\Release\MaterialSkin.dll'))
Source: "WinFormsApiClient\bin\Release\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\bin\Release\Newtonsoft.Json.dll'))
Source: "WinFormsApiClient\bin\Release\FontAwesome.Sharp.dll"; DestDir: "{app}"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\bin\Release\FontAwesome.Sharp.dll'))
Source: "WinFormsApiClient\obj\Release\Interop.WIA.dll"; DestDir: "{app}"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\bin\Release\Interop.WIA.dll'))

; ✅ SCRIPTS DE AUTOMATIZACIÓN - RUTAS CORREGIDAS
Source: "WinFormsApiClient\Scripts\AutoIt\BullzipSaveDialog.au3"; DestDir: "{app}\Scripts\AutoIt"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\Scripts\AutoIt\BullzipSaveDialog.au3'))
Source: "WinFormsApiClient\Scripts\Batch\IniciarAutomatizacionBullzip.bat"; DestDir: "{app}\Scripts\Batch"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\Scripts\Batch\IniciarAutomatizacionBullzip.bat'))

; ✅ RECURSOS DE FONTAWESOME - RUTAS CORREGIDAS
Source: "WinFormsApiClient\Content\*.css"; DestDir: "{app}\Content"; Flags: ignoreversion
Source: "WinFormsApiClient\Content\all.css"; DestDir: "{app}\Content"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\Content\all.css'))
Source: "WinFormsApiClient\Content\all.min.css"; DestDir: "{app}\Content"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\Content\all.min.css'))
Source: "WinFormsApiClient\Content\brands.css"; DestDir: "{app}\Content"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\Content\brands.css'))
Source: "WinFormsApiClient\Content\brands.min.css"; DestDir: "{app}\Content"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\Content\brands.min.css'))
Source: "WinFormsApiClient\Content\fontawesome.css"; DestDir: "{app}\Content"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\Content\fontawesome.css'))
Source: "WinFormsApiClient\Content\fontawesome.min.css"; DestDir: "{app}\Content"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\Content\fontawesome.min.css'))
Source: "WinFormsApiClient\Content\regular.css"; DestDir: "{app}\Content"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\Content\regular.css'))
Source: "WinFormsApiClient\Content\regular.min.css"; DestDir: "{app}\Content"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\Content\regular.min.css'))
Source: "WinFormsApiClient\Content\solid.min.css"; DestDir: "{app}\Content"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\Content\solid.min.css'))
Source: "WinFormsApiClient\Content\svg-with-js.css"; DestDir: "{app}\Content"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\Content\svg-with-js.css'))
Source: "WinFormsApiClient\Content\svg-with-js.min.css"; DestDir: "{app}\Content"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\Content\svg-with-js.min.css'))
Source: "WinFormsApiClient\Content\v4-shims.css"; DestDir: "{app}\Content"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\Content\v4-shims.css'))
Source: "WinFormsApiClient\Content\v4-shims.min.css"; DestDir: "{app}\Content"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\Content\v4-shims.min.css'))

; ✅ IMÁGENES DE LA APLICACIÓN (SI EXISTEN) - RUTAS CORREGIDAS
Source: "WinFormsApiClient\images\*"; DestDir: "{app}\images"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: DirExists(ExpandConstant('{src}\WinFormsApiClient\images'))

; ✅ ICONOS Y ARCHIVOS DE DOCUMENTACIÓN
Source: "icon.ico"; DestDir: "{app}"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\icon.ico'))
Source: "WinFormsApiClient\ecmicon.ico"; DestDir: "{app}"; DestName: "ecmicon.ico"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{src}\WinFormsApiClient\ecmicon.ico'))
Source: "License.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "ReadMe.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\ECM Desktop"; Filename: "{app}\WinFormsApiClient.exe"; IconFilename: "{app}\icon.ico"
Name: "{group}\Instrucciones de Instalación"; Filename: "{app}\ReadMe.txt"
Name: "{group}\{cm:ProgramOnTheWeb,ECM Desktop}"; Filename: "https://ecmcentral.com"
Name: "{group}\{cm:UninstallProgram,ECM Desktop}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ECM Desktop"; Filename: "{app}\WinFormsApiClient.exe"; IconFilename: "{app}\icon.ico"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\ECM Desktop"; Filename: "{app}\WinFormsApiClient.exe"; IconFilename: "{app}\icon.ico"; Tasks: quicklaunchicon

[Registry]
; Registro de aplicación para autostart (opcional)
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ECM Desktop"; ValueData: """{app}\WinFormsApiClient.exe"" -minimized"; Flags: uninsdeletevalue; Tasks: autostart

; Configuración de la aplicación ECM Desktop
Root: HKLM; Subkey: "SOFTWARE\ECM Desktop"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\ECM Desktop"; ValueType: string; ValueName: "Version"; ValueData: "1.0.0"
Root: HKLM; Subkey: "SOFTWARE\ECM Desktop"; ValueType: string; ValueName: "OutputFolder"; ValueData: "C:\Temp\ECM Central"
Root: HKLM; Subkey: "SOFTWARE\ECM Desktop"; ValueType: string; ValueName: "SupportContact"; ValueData: "support@ecmcentral.com"

; Configuración para Microsoft Print to PDF (impresora virtual)
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows"; ValueType: string; ValueName: "Device"; ValueData: "Microsoft Print to PDF,winspool,Ne00:"

[Run]
; Configurar carpetas necesarias del sistema
Filename: "{cmd}"; Parameters: "/c mkdir ""C:\Temp\ECM Central"""; StatusMsg: "Creando carpetas del sistema..."; Flags: runhidden
Filename: "{cmd}"; Parameters: "/c mkdir ""C:\Temp\ECM Central\logs"""; StatusMsg: "Creando carpetas de logs..."; Flags: runhidden

; Configurar permisos en la carpeta de trabajo
Filename: "{cmd}"; Parameters: "/c icacls ""C:\Temp\ECM Central"" /grant Everyone:(OI)(CI)F"; StatusMsg: "Configurando permisos..."; Flags: runhidden

; Configurar impresora virtual Microsoft Print to PDF
Filename: "powershell.exe"; Parameters: "-Command ""Add-Printer -Name 'ECM Central Printer' -DriverName 'Microsoft Print To PDF' -PortName 'PORTPROMPT:'"""; StatusMsg: "Configurando impresora virtual..."; Flags: runhidden; Check: not IsPrinterInstalled

; Ejecutar la aplicación al finalizar
Filename: "{app}\WinFormsApiClient.exe"; Description: "{cm:LaunchProgram,ECM Desktop}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Detener procesos antes de desinstalar
Filename: "{cmd}"; Parameters: "/c taskkill /f /im WinFormsApiClient.exe"; Flags: runhidden

[UninstallDelete]
; Eliminar archivos de logs y temporales
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\temp"
Type: filesandordirs; Name: "C:\Temp\ECM Central\logs"

[Dirs]
; Crear directorios necesarios en la aplicación
Name: "{app}\Scripts\AutoIt"
Name: "{app}\Scripts\Batch"
Name: "{app}\Content"
Name: "{app}\Resources"; Permissions: everyone-full
Name: "{app}\images"; Permissions: everyone-full
Name: "{app}\logs"; Permissions: everyone-full

; Crear directorios del sistema
Name: "C:\Temp\ECM Central"; Permissions: everyone-full
Name: "C:\Temp\ECM Central\logs"; Permissions: everyone-full

[Code]
// ✅ FUNCIÓN CORRECTA para verificar .NET Framework 4.8
function IsNet48Installed: Boolean;
var
  ReleaseKey: Cardinal;
begin
  Result := False;
  // Verificar en el registro la versión de .NET Framework 4.x
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', ReleaseKey) then
  begin
    // .NET 4.8 tiene Release >= 528040
    // .NET 4.7.2 tiene Release >= 461808  
    // .NET 4.7.1 tiene Release >= 461308
    // Aceptamos 4.7.1 o superior
    Result := ReleaseKey >= 461308;
  end;
end;

// ✅ FUNCIÓN CORREGIDA para verificar .NET Framework (legacy)
function IsDotNetDetected(version: string; service: cardinal): boolean;
var
    key: string;
    install, release, serviceCount: cardinal;
    versionStr: string;
begin
    Result := false;
    
    // Remover 'v' del inicio si existe
    versionStr := version;
    if Copy(versionStr, 1, 1) = 'v' then
        versionStr := Copy(versionStr, 2, Length(versionStr) - 1);
    
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + versionStr;
    
    if RegQueryDWordValue(HKLM, key, 'Install', install) then begin
        if install = 1 then begin
            if RegQueryDWordValue(HKLM, key, 'SP', serviceCount) then begin
                if serviceCount >= service then begin
                    Result := true;
                end;
            end else begin
                if service = 0 then begin
                    Result := true;
                end;
            end;
        end;
    end;
end;

// Función de inicialización del setup
function InitializeSetup(): Boolean;
begin
    Result := true;
    
    // Verificar .NET Framework 4.8
    if not IsNet48Installed then begin
        if MsgBox('ECM Desktop requiere Microsoft .NET Framework 4.8 o superior.' + #13#10 + #13#10 +
                  'CARACTERÍSTICAS DE ECM DESKTOP:' + #13#10 +
                  '• Impresora virtual integrada' + #13#10 +
                  '• Sistema de escaneo' + #13#10 +
                  '• Conexión automática con ECM Central' + #13#10 +
                  '• Monitoreo automático de impresiones' + #13#10 + #13#10 +
                  'REQUISITOS DEL SISTEMA:' + #13#10 +
                  '• Windows 7 SP1 o superior' + #13#10 +
                  '• .NET Framework 4.8 o superior' + #13#10 +
                  '• 100 MB de espacio libre en disco' + #13#10 + #13#10 +
                  'Descargue .NET Framework desde:' + #13#10 +
                  'https://dotnet.microsoft.com/download/dotnet-framework' + #13#10 + #13#10 +
                  'Para soporte técnico: support@ecmcentral.com' + #13#10 + #13#10 +
                  '¿Desea continuar con la instalación?', 
                  mbConfirmation, MB_YESNO) = IDNO then
        begin
            Result := false;
        end;
    end;
end;

// Función para verificar si la impresora ECM Central está instalada
function IsPrinterInstalled: Boolean;
var
  Printers: TArrayOfString;
  I: Integer;
begin
  Result := False;
  if RegGetSubkeyNames(HKLM, 'SYSTEM\CurrentControlSet\Control\Print\Printers', Printers) then
  begin
    for I := 0 to GetArrayLength(Printers) - 1 do
    begin
      if Printers[I] = 'ECM Central Printer' then
      begin
        Result := True;
        Break;
      end;
    end;
  end;
end;

// Función para verificar si un archivo existe (helper para Check:)
function FileExists(FileName: string): Boolean;
begin
  Result := FileOrDirExists(FileName);
end;

// Evento que se ejecuta después de la instalación
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // Configuración adicional después de la instalación
    Exec(ExpandConstant('{cmd}'), '/c echo ECM Desktop instalado correctamente el ' + GetDateTimeString('dd/mm/yyyy hh:nn:ss', #0, #0) + ' > "' + ExpandConstant('{app}') + '\install.log"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    
    // Configurar la aplicación para que use la carpeta correcta
    RegWriteStringValue(HKCU, 'SOFTWARE\ECM Desktop', 'LastInstallDate', GetDateTimeString('dd/mm/yyyy hh:nn:ss', #0, #0));
    RegWriteStringValue(HKCU, 'SOFTWARE\ECM Desktop', 'SupportContact', 'support@ecmcentral.com');
    RegWriteStringValue(HKCU, 'SOFTWARE\ECM Desktop', 'InstallVersion', '1.0.0');
  end;
end;

// Evento que se ejecuta durante la desinstalación
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // Preguntar si eliminar archivos de configuración
    if MsgBox('¿Desea eliminar también los archivos de configuración, logs y documentos temporales de ECM Desktop?'#13#13 +
              'Esto incluye:'#13 +
              '• Archivos de configuración de usuario'#13 +
              '• Logs del sistema'#13 +
              '• Documentos en C:\Temp\ECM Central'#13#13 +
              'Seleccione "Sí" para eliminar todo o "No" para conservar los datos.',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      Exec(ExpandConstant('{cmd}'), '/c rmdir /s /q "C:\Temp\ECM Central"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      
      // Eliminar entradas del registro
      RegDeleteKeyIncludingSubkeys(HKCU, 'SOFTWARE\ECM Desktop');
    end;
  end;
end;

[Messages]
spanish.WelcomeLabel1=Bienvenido al Asistente de Instalación de ECM Desktop
spanish.WelcomeLabel2=Este programa instalará ECM Desktop versión 1.0.0 en su equipo.%n%nECM Desktop es una solución completa para la gestión documental que incluye:%n%n• Impresora virtual integrada%n• Sistema de escaneo%n• Conexión automática con ECM Central%n• Monitoreo automático de impresiones%n• Interfaz moderna con Material Design%n%nREQUISITOS DEL SISTEMA:%n• Windows 7 SP1 o superior%n• .NET Framework 4.8 o superior%n• 100 MB de espacio libre en disco%n%nSe recomienda cerrar todas las demás aplicaciones antes de continuar.

english.WelcomeLabel1=Welcome to the ECM Desktop Setup Wizard
english.WelcomeLabel2=This will install ECM Desktop version 1.0.0 on your computer.%n%nECM Desktop is a complete document management solution that includes:%n%n• Integrated virtual printer%n• Document scanning system%n• Automatic connection to ECM Central%n• Automatic print monitoring%n• Modern interface with Material Design%n%nSYSTEM REQUIREMENTS:%n• Windows 7 SP1 or higher%n• .NET Framework 4.8 or higher%n• 100 MB free disk space%n%nIt is recommended that you close all other applications before continuing.

spanish.FinishedLabel=ECM Desktop se ha instalado correctamente en su equipo.%n%nLa aplicación está lista para usar. Al ejecutarla por primera vez:%n%n1. Se configurará automáticamente la impresora virtual%n2. Se crearán las carpetas necesarias del sistema%n3. Se iniciará el monitor de impresión en segundo plano%n%nPara soporte técnico: support@ecmcentral.com%n%nPara comenzar, haga clic en el icono de ECM Desktop en su escritorio o en el menú Inicio.

english.FinishedLabel=ECM Desktop has been successfully installed on your computer.%n%nThe application is ready to use. When you run it for the first time:%n%n1. The virtual printer will be automatically configured%n2. Necessary system folders will be created%n3. The background print monitor will start%n%nFor technical support: support@ecmcentral.com%n%nTo get started, click on the ECM Desktop icon on your desktop or in the Start menu.