param(
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $scriptDirectory))
$artifactRoot = [System.IO.Path]::GetFullPath((Join-Path $projectRoot 'artifacts'))

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $artifactRoot 'win-x64'
}

$outputFullPath = [System.IO.Path]::GetFullPath($OutputDirectory)
$stagingDirectory = Join-Path $projectRoot ("obj\publish-win-x64-{0}" -f [Guid]::NewGuid().ToString('N'))
$stagingExecutable = Join-Path $stagingDirectory 'MinecraftDoubleClicker.exe'
$finalExecutable = Join-Path $outputFullPath 'MinecraftDoubleClicker.exe'

try {
    dotnet publish `
        (Join-Path $projectRoot 'MinecraftDoubleClicker.csproj') `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        --output $stagingDirectory `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=None

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    if (-not (Test-Path -LiteralPath $stagingExecutable -PathType Leaf)) {
        throw "Publish completed without producing $stagingExecutable."
    }

    $artifactPrefix = $artifactRoot.TrimEnd('\') + '\'
    $isArtifactOutput = $outputFullPath.StartsWith(
        $artifactPrefix,
        [StringComparison]::OrdinalIgnoreCase)

    if ($isArtifactOutput -and (Test-Path -LiteralPath $outputFullPath)) {
        Remove-Item -LiteralPath $outputFullPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $outputFullPath -Force | Out-Null
    Copy-Item -LiteralPath $stagingExecutable -Destination $finalExecutable -Force
}
finally {
    $stagingRoot = [System.IO.Path]::GetFullPath((Join-Path $projectRoot 'obj')).TrimEnd('\') + '\'
    $resolvedStagingDirectory = [System.IO.Path]::GetFullPath($stagingDirectory)

    if ($resolvedStagingDirectory.StartsWith($stagingRoot, [StringComparison]::OrdinalIgnoreCase) `
        -and (Test-Path -LiteralPath $resolvedStagingDirectory)) {
        Remove-Item -LiteralPath $resolvedStagingDirectory -Recurse -Force
    }
}

if (-not (Test-Path -LiteralPath $finalExecutable -PathType Leaf)) {
    throw "Packaging completed without producing $finalExecutable."
}

Write-Host "Windows app: $finalExecutable"
