param([string]$OutputDirectory, [string]$SdkPackage)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (!$OutputDirectory) { $OutputDirectory = Join-Path $root 'dist\AtGrace' }
$out = [IO.Path]::GetFullPath($OutputDirectory)
$cache = Join-Path $root '.cache'
New-Item -ItemType Directory -Path $out,$cache -Force | Out-Null
$version = '1.0.4314.35'
$expectedHash = '90778DEB1B77E36292828B4B5833C9255C27533F40E0DDD55891AE718944EDC4'
if (!$SdkPackage) {
    $SdkPackage = Join-Path $cache ('webview2-' + $version + '.nupkg')
    if (!(Test-Path -LiteralPath $SdkPackage)) {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -UseBasicParsing -Uri ('https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/' + $version + '/microsoft.web.webview2.' + $version + '.nupkg') -OutFile $SdkPackage
    }
}
if ((Get-FileHash -LiteralPath $SdkPackage -Algorithm SHA256).Hash -ne $expectedHash) { throw 'WebView2 SDK checksum mismatch.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($SdkPackage))
try {
    foreach ($name in @('lib/net462/Microsoft.Web.WebView2.Core.dll','lib/net462/Microsoft.Web.WebView2.WinForms.dll','runtimes/win-x64/native/WebView2Loader.dll')) {
        $entry = $archive.GetEntry($name)
        if (!$entry) { throw ('Missing SDK entry: ' + $name) }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $out ([IO.Path]::GetFileName($name))), $true)
    }
} finally { $archive.Dispose() }
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$source = Join-Path $root 'src\native'
$arguments = @('/nologo','/target:winexe','/platform:x64','/optimize+',('/out:' + (Join-Path $out 'ChatGPTGraceLauncher.exe')),('/win32manifest:' + (Join-Path $source 'launcher.manifest')))
foreach ($reference in @('System.dll','System.Core.dll','System.Windows.Forms.dll','System.Drawing.dll','System.Web.Extensions.dll','System.Management.dll')) { $arguments += '/reference:' + $reference }
foreach ($reference in @('UIAutomationClient.dll','UIAutomationTypes.dll','WindowsBase.dll')) { $arguments += '/reference:' + (Join-Path $framework ('WPF\' + $reference)) }
foreach ($reference in @('Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll')) { $arguments += '/reference:' + (Join-Path $out $reference) }
$arguments += Get-ChildItem -LiteralPath $source -Filter '*.cs' | ForEach-Object { $_.FullName }
& (Join-Path $framework 'csc.exe') $arguments
if ($LASTEXITCODE -ne 0) { throw 'Native compilation failed.' }
Copy-Item -LiteralPath (Join-Path $source 'launcher.config') -Destination (Join-Path $out 'ChatGPTGraceLauncher.exe.config') -Force
& python (Join-Path $PSScriptRoot 'build_scene.py') --output $out
if ($LASTEXITCODE -ne 0) { throw 'Scene build failed.' }
foreach ($name in @('README.md','RELEASE_NOTES.md','LICENSE','THIRD_PARTY_NOTICES.md','Setup.ps1','Setup.cmd')) { Copy-Item -LiteralPath (Join-Path $root $name) -Destination $out -Force }
Copy-Item -LiteralPath (Join-Path $root 'vendor\WebView2-LICENSE.txt'),(Join-Path $root 'vendor\html2canvas-LICENSE.txt') -Destination $out -Force
$docs = Join-Path $out 'docs'
New-Item -ItemType Directory -Path $docs -Force | Out-Null
Copy-Item -Path (Join-Path $root 'docs\*') -Destination $docs -Force
Write-Output ('Build complete: ' + $out)
