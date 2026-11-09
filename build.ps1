$ErrorActionPreference = "Stop"

$csc = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
if (-not (Test-Path $csc)) { $csc = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe" }
if (-not (Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
}
$fw = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$wpf = "$fw\WPF"
$dir = $PSScriptRoot

Copy-Item "$dir\FanControl.CorsairLink.dll" "$dir\FanControl.CorsairLink.merged.dll" -Force -ErrorAction SilentlyContinue

$iconArg = @()
if (Test-Path "$dir\hyteflow.ico") {
    $iconArg = @("/win32icon:$dir\hyteflow.ico")
}

& $csc /nologo /target:winexe /platform:x64 /optimize+ @iconArg `
    /r:"$fw\mscorlib.dll" `
    /r:"$fw\System.dll" `
    /r:"$fw\System.Core.dll" `
    /r:"$fw\System.Drawing.dll" `
    /r:"$fw\System.Windows.Forms.dll" `
    /r:"$fw\System.Web.Extensions.dll" `
    /r:"$fw\System.Management.dll" `
    /r:"$fw\System.Net.Http.dll" `
    /r:"$fw\System.Xaml.dll" `
    /r:"$fw\Microsoft.CSharp.dll" `
    /r:"$fw\netstandard.dll" `
    /r:"$wpf\WindowsBase.dll" `
    /r:"$wpf\PresentationCore.dll" `
    /r:"$wpf\PresentationFramework.dll" `
    /r:"$dir\LibreHardwareMonitorLib.dll" `
    /r:"$dir\HidSharp.dll" `
    /r:"$dir\FanControl.Plugins.dll" `
    /r:"$dir\FanControl.CorsairLink.dll" `
    /r:"$dir\Newtonsoft.Json.dll" `
    /out:"$dir\HyteFlow.exe" `
    "$dir\src\Models.cs" `
    "$dir\src\CaseCatalog.cs" `
    "$dir\src\StartupManager.cs" `
    "$dir\src\HardwareEngine.cs" `
    "$dir\src\CustomControls.cs" `
    "$dir\src\MainWindow.cs" `
    "$dir\src\Program.cs"

Write-Host "Successfully compiled $dir\HyteFlow.exe"
