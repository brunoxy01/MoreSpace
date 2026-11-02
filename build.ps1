param([switch]$SkipTests)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$dist = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null
# Keep the file name ASCII so both Windows PowerShell 5 and PowerShell 7 can build.
$target = Join-Path $dist 'MoreSpace.exe'
$sources = @(Get-ChildItem (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
& $compiler /nologo /target:winexe /optimize+ /platform:x64 "/out:$target" "/win32icon:$PSScriptRoot\assets\app.ico" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
if (-not $SkipTests) {
    foreach ($suite in @('--self-test', '--management-test', '--space-test', '--ui-test')) {
        $test = Start-Process -FilePath $target -ArgumentList $suite -PassThru -Wait
        if ($test.ExitCode -ne 0) { throw "Test failed: $suite (exit $($test.ExitCode))" }
        Write-Output "$suite passed"
    }
}
Write-Output "Built: $target"
