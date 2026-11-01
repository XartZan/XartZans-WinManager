[CmdletBinding()]
param(
    [string]$PublishDirectory
)

$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$appProject = Join-Path $projectRoot 'src\XartZans.WinManager.App\XartZans.WinManager.App.csproj'
$installerScript = Join-Path $projectRoot 'installer\XartZans-WinManager.iss'

if ([string]::IsNullOrWhiteSpace($PublishDirectory)) {
    $PublishDirectory = Join-Path $env:TEMP 'XartZans-WinManager-win-x64'
}
elseif (![System.IO.Path]::IsPathRooted($PublishDirectory)) {
    $PublishDirectory = Join-Path $projectRoot $PublishDirectory
}

$PublishDirectory = [System.IO.Path]::GetFullPath($PublishDirectory)
$appExe = Join-Path $PublishDirectory 'XartZans.WinManager.exe'
if (!(Test-Path -LiteralPath $appExe)) {
    Write-Host 'Publishing the self-contained Windows x64 application...'
    dotnet publish $appProject -c Release -r win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true -o $PublishDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }
}

$compiler = (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
if ([string]::IsNullOrWhiteSpace($compiler)) {
    $compilerCandidates = @(
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 7\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 7\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
    )
    $compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($compiler)) {
    throw 'Inno Setup compiler was not found. Install it from https://jrsoftware.org/isdl.php, then run this script again.'
}

[xml]$projectFile = Get-Content -Raw -Encoding utf8 $appProject
$appVersion = $projectFile.Project.PropertyGroup |
    ForEach-Object { $_.Version } |
    Where-Object { ![string]::IsNullOrWhiteSpace($_) } |
    Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($appVersion)) {
    throw 'Could not read the application version from the app project file.'
}

$outputDirectory = Join-Path $projectRoot 'artifacts\installer'
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$compilerArguments = @(
    "-dAppVersion=$appVersion",
    "-dPublishDir=$PublishDirectory",
    "-o$outputDirectory",
    $installerScript
)

Write-Host "Building the Setup.exe installer for version $appVersion..."
& $compiler @compilerArguments
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE."
}

$setupExe = Join-Path $outputDirectory "XartZans-WinManager-Setup-$appVersion.exe"
if (!(Test-Path -LiteralPath $setupExe)) {
    throw "Inno Setup completed but the expected installer was not found: $setupExe"
}

Write-Output "Installer created: $setupExe"
