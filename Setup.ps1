param([string]$AppId, [string]$ProcessName, [string]$ShortcutPath, [switch]$NoShortcut)
$ErrorActionPreference = 'Stop'
$folder = $PSScriptRoot
if (!(Test-Path -LiteralPath (Join-Path $folder 'ChatGPTGraceLauncher.exe'))) { $folder = Join-Path $folder 'dist\AtGrace' }
$launcher = Join-Path $folder 'ChatGPTGraceLauncher.exe'
if (!(Test-Path -LiteralPath $launcher)) { throw 'Build the project first, or extract the Windows release ZIP.' }
if (![Environment]::Is64BitOperatingSystem) { throw 'This release requires 64-bit Windows.' }
if (!$AppId) {
    $apps = @(Get-StartApps | Where-Object { $_.Name -match 'Codex|ChatGPT' -and $_.AppID -match '!' })
    if ($apps.Count -eq 0) { throw 'No packaged Codex or ChatGPT app was found. Install and open the official Windows app first.' }
    if ($apps.Count -eq 1) { $selectedApp = $apps[0] }
    else {
        for ($i=0; $i -lt $apps.Count; $i++) { Write-Host ('[{0}] {1} ({2})' -f ($i+1),$apps[$i].Name,$apps[$i].AppID) }
        $choice = 0
        if (![int]::TryParse((Read-Host 'Choose the app number'),[ref]$choice) -or $choice -lt 1 -or $choice -gt $apps.Count) { throw 'Invalid app selection.' }
        $selectedApp = $apps[$choice-1]
    }
    $AppId = $selectedApp.AppID
}
if ($AppId -notmatch '\A[A-Za-z0-9_.-]+![A-Za-z0-9_.-]+\z') { throw 'Expected a packaged Windows application ID.' }
if (!$ProcessName) {
    $family,$application = $AppId -split '!',2
    try {
        $package = Get-AppxPackage | Where-Object { $_.PackageFamilyName -eq $family } | Select-Object -First 1
        if ($package) {
            $manifest = Get-AppxPackageManifest -Package $package.PackageFullName
            $entry = @($manifest.Package.Applications.Application) | Where-Object { $_.Id -eq $application } | Select-Object -First 1
            if ($entry.Executable) { $ProcessName = [IO.Path]::GetFileNameWithoutExtension([string]$entry.Executable) }
        }
    } catch { Write-Host 'Package manifest access is restricted; checking running app identities instead.' }
    if (!$ProcessName) {
        if (!('AtGraceSetupIdentity' -as [type])) {
            Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class AtGraceSetupIdentity {
 [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
 [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] static extern int GetApplicationUserModelId(IntPtr process, ref uint count, StringBuilder id);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
 public static string Get(uint pid) {
  IntPtr handle=OpenProcess(0x1000,false,pid); if(handle==IntPtr.Zero)return null;
  try { uint count=512; var id=new StringBuilder(512); return GetApplicationUserModelId(handle,ref count,id)==0?id.ToString():null; }
  finally { CloseHandle(handle); }
 }
}
'@
        }
        foreach ($candidate in Get-Process) {
            try { if ([AtGraceSetupIdentity]::Get([uint32]$candidate.Id) -eq $AppId) { $ProcessName=$candidate.ProcessName; break } }
            finally { $candidate.Dispose() }
        }
    }
    # This exact package/executable pair is the verified initial target. Prefer
    # the manifest or observed process on other installations.
    if (!$ProcessName -and $AppId -eq 'OpenAI.Codex_2p2nqsd0c76g0!App') { $ProcessName = 'ChatGPT' }
}
if (!$ProcessName) { throw 'Could not determine the app process. Run Setup.ps1 with -AppId and -ProcessName (without .exe).' }
if ($ProcessName -notmatch '\A[A-Za-z0-9_.-]{1,80}\z' -or $ProcessName -match '\.exe$') { throw 'Invalid process name.' }
$settings = @{ appId=$AppId; processName=$ProcessName; windowTitle='' } | ConvertTo-Json
$settings | Set-Content -LiteralPath (Join-Path $folder 'launcher.json') -Encoding UTF8
if (!$NoShortcut) {
    if (!$ShortcutPath) { $ShortcutPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'ChatGPT - Elden Ring.lnk' }
    if (Test-Path -LiteralPath $ShortcutPath) { throw ('A shortcut already exists. Choose another -ShortcutPath: ' + $ShortcutPath) }
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut([IO.Path]::GetFullPath($ShortcutPath))
    $shortcut.TargetPath = $launcher; $shortcut.WorkingDirectory = $folder
    $shortcut.Description = 'ChatGPT - Elden Ring entry animation'; $shortcut.Save()
    Write-Host ('Created shortcut: ' + $ShortcutPath)
}
Write-Host ('Target: ' + $AppId + ' / ' + $ProcessName)
Write-Host 'Setup complete. Keep this folder in its current location.'
Write-Host 'Optional music: copy your own MP3 to media\menu-music.mp3. Each pass starts at 00:15.'
