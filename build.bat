@echo off
setlocal enabledelayedexpansion

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set WPF=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF
if not exist "%CSC%" (
    echo [ERROR] Could not find .NET Framework csc.exe at %CSC%
    exit /b 1
)

if not exist bin mkdir bin

echo [1/3] Compiling NologGuard.exe...
"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 /win32manifest:src\app.manifest /win32icon:src\NologGuard.ico ^
    /out:bin\NologGuard.exe ^
    /reference:System.dll ^
    /reference:System.Core.dll ^
    /reference:System.Drawing.dll ^
    /reference:System.Runtime.Serialization.dll ^
    /reference:System.Windows.Forms.dll ^
    /reference:System.Xaml.dll ^
    /reference:"%WPF%\WindowsBase.dll" ^
    /reference:"%WPF%\PresentationCore.dll" ^
    /reference:"%WPF%\PresentationFramework.dll" ^
    src\AppConfig.cs ^
    src\DnsWireCodec.cs ^
    src\DohResolver.cs ^
    src\DomainBlocklist.cs ^
    src\DomainMatcher.cs ^
    src\MainWindow.cs ^
    src\Program.cs ^
    src\ProxyServer.cs ^
    src\Strings.cs ^
    src\TlsClientHelloParser.cs ^
    src\TrayHost.cs ^
    src\WindowsNetworkHelper.cs ^
    src\WinHttpHelper.cs ^
    src\WpfTheme.cs ^
    src\Properties\AssemblyInfo.cs

if errorlevel 1 (
    echo [ERROR] Build failed.
    exit /b 1
)

echo [2/3] Compiling NologGuardTests.exe...
"%CSC%" /nologo /target:exe /codepage:65001 ^
    /out:bin\NologGuardTests.exe ^
    /reference:System.dll ^
    /reference:System.Core.dll ^
    /reference:System.Runtime.Serialization.dll ^
    src\AppConfig.cs ^
    src\DnsWireCodec.cs ^
    src\DohResolver.cs ^
    src\DomainBlocklist.cs ^
    src\DomainMatcher.cs ^
    src\ProxyServer.cs ^
    src\Strings.cs ^
    src\TlsClientHelloParser.cs ^
    src\WinHttpHelper.cs ^
    tests\NologGuardTests.cs

if errorlevel 1 (
    echo [ERROR] Test compilation failed.
    exit /b 1
)

echo [3/3] Running automated tests...
bin\NologGuardTests.exe %1
exit /b %errorlevel%
