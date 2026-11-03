$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'src\native'
$tests = Join-Path $root 'tests\native'
$out = Join-Path $root 'artifacts\native'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$cases = @(
    @{ Test='LauncherPolicyTests'; Sources=@('LauncherPolicy.cs') },
    @{ Test='WindowRevealTests'; Sources=@('WindowReveal.cs') },
    @{ Test='ForegroundHandoffTests'; Sources=@('WindowReveal.cs') },
    @{ Test='WindowEdgeLayoutTests'; Sources=@('WindowEdgeLayout.cs') },
    @{ Test='LaunchSettingsTests'; Sources=@('LaunchSettings.cs') }
)
foreach ($case in $cases) {
    $executable = Join-Path $out ($case.Test + '.exe')
    $arguments = @('/nologo',('/out:' + $executable),'/reference:System.Drawing.dll','/reference:System.Web.Extensions.dll',(Join-Path $tests ($case.Test + '.cs')))
    $arguments += $case.Sources | ForEach-Object { Join-Path $source $_ }
    & $compiler $arguments
    if ($LASTEXITCODE -ne 0) { throw ('Compilation failed: ' + $case.Test) }
    & $executable
    if ($LASTEXITCODE -ne 0) { throw ('Test failed: ' + $case.Test) }
}
