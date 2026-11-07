param([Parameter(Mandatory)][string]$RunId, [Parameter(Mandatory)][string]$Tag)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if ($RunId -notmatch '^[1-9][0-9]{0,19}$' -or $Tag -notmatch '^v[0-9]+\.[0-9]+\.[0-9]+(-[A-Za-z0-9.-]+)?$') {
    throw 'Invalid build run ID or release tag'
}
$repository = $env:GITHUB_REPOSITORY
if ([string]::IsNullOrWhiteSpace($repository)) { throw 'GITHUB_REPOSITORY is required' }
function Assert-GhSuccess {
    if ($LASTEXITCODE -ne 0) { throw "GitHub command failed with exit code $LASTEXITCODE" }
}
$runJson = gh api "repos/$repository/actions/runs/$RunId"
Assert-GhSuccess
$buildRun = $runJson | ConvertFrom-Json
if ($buildRun.status -ne 'completed' -or $buildRun.conclusion -ne 'success' `
    -or $buildRun.path -ne '.github/workflows/windows.yml' -or $buildRun.head_branch -ne 'main') {
    throw 'Release requires a successful Windows verification run on main'
}
$notes = "docs/releases/$Tag.md"
if (-not (Test-Path $notes -PathType Leaf)) { throw "Missing release notes: $notes" }
$releaseJson = gh release view $Tag --repo $repository --json isDraft,targetCommitish
if ($LASTEXITCODE -ne 0) {
    gh release create $Tag --repo $repository --draft --target $buildRun.head_sha `
        --title "$Tag - Windows Routenplaner" --notes-file $notes
    Assert-GhSuccess
    $releaseJson = gh release view $Tag --repo $repository --json isDraft,targetCommitish
    Assert-GhSuccess
}
$release = $releaseJson | ConvertFrom-Json
if (-not $release.isDraft -or $release.targetCommitish -ne $buildRun.head_sha) {
    throw 'Only a draft targeting the verified commit can be published; existing public releases are preserved'
}
$output = Join-Path $PWD "artifacts/release-$Tag"
New-Item $output -ItemType Directory -Force | Out-Null
gh run download $RunId --repo $repository --name Routenatelier-win-x64 --dir $output
Assert-GhSuccess
$zip = Join-Path $output 'Routenatelier-win-x64.zip'
$expectedZipHash = ((Get-Content "$zip.sha256" -Raw).Trim() -split '\s+')[0]
if ((Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedZipHash) {
    throw 'Build ZIP checksum mismatch'
}
$unpacked = Join-Path $output 'package'
Expand-Archive $zip -DestinationPath $unpacked
foreach ($line in Get-Content (Join-Path $unpacked 'SHA256SUMS.txt')) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $parts = $line -split '  ', 2
    $file = Join-Path $unpacked $parts[1]
    if ((Get-FileHash $file -Algorithm SHA256).Hash.ToLowerInvariant() -ne $parts[0]) {
        throw "Package checksum mismatch: $($parts[1])"
    }
}
Copy-Item (Join-Path $unpacked 'RoutePlanner.exe') $output
$licenses = Join-Path $output 'licenses'
New-Item $licenses -ItemType Directory | Out-Null
Copy-Item (Join-Path $unpacked 'LICENSE') $licenses
Copy-Item (Join-Path $unpacked 'third-party') $licenses -Recurse
Compress-Archive "$licenses/*" -DestinationPath (Join-Path $output 'Lizenzen.zip')
$verification = Join-Path $output 'verification'
gh run download $RunId --repo $repository --name offline-verification --dir $verification
Assert-GhSuccess
if ((Get-Content (Join-Path $verification 'core-tests.txt') -Raw) -notmatch 'RESULT [1-9][0-9]* passed; 0 failed') { throw 'Core verification failed' }
$checks = @{
    'ui-smoke.txt' = 'RESULT Windows WPF smoke passed'
    'published-exe-smoke.txt' = 'PASS published self-contained EXE started'
}
if (-not (Get-Content (Join-Path $verification 'ui-smoke.txt') -Raw).Contains('PASS Original-Fortnite.GG tatsächlich in WebView2 geladen')) { throw 'Fortnite.GG integration verification missing' }
if (-not (Get-Content (Join-Path $verification 'published-exe-smoke.txt') -Raw).Contains('PASS published EXE loaded Fortnite.GG in WebView2')) { throw 'Published WebView2 loader verification missing' }
foreach ($name in $checks.Keys) {
    if (-not (Get-Content (Join-Path $verification $name) -Raw).Contains($checks[$name])) {
        throw "Verification result missing: $name"
    }
}
Compress-Archive "$verification/*" -DestinationPath (Join-Path $output 'offline-verification.zip')
Copy-Item (Join-Path $verification 'ui-smoke/planner.png') $output
Copy-Item (Join-Path $verification 'ui-smoke/compact.png') $output
Copy-Item (Join-Path $verification 'ui-smoke/fortnite-gg.png') $output
Copy-Item (Join-Path $verification 'ui-smoke/desktop-hud.png') $output
if (-not (Get-Content (Join-Path $verification 'ui-smoke.txt') -Raw).Contains('PASS Routenfenster ist im echten Desktopbild über aktivem neutralen Fenster sichtbar')) { throw 'Desktop window visibility verification missing' }
$assets = @('RoutePlanner.exe', 'Routenatelier-win-x64.zip', 'Lizenzen.zip', 'offline-verification.zip', 'planner.png', 'compact.png', 'fortnite-gg.png', 'desktop-hud.png')
$hashes = foreach ($name in $assets) {
    '{0}  {1}' -f (Get-FileHash (Join-Path $output $name) -Algorithm SHA256).Hash.ToLowerInvariant(), $name
}
$hashes | Set-Content (Join-Path $output 'SHA256SUMS.txt') -Encoding ascii
$assets += 'SHA256SUMS.txt'
foreach ($name in $assets) {
    gh release upload $Tag (Join-Path $output $name) --repo $repository --clobber
    Assert-GhSuccess
}
$uploadedJson = gh release view $Tag --repo $repository --json assets
Assert-GhSuccess
$uploaded = ($uploadedJson | ConvertFrom-Json).assets
foreach ($name in $assets) {
    $asset = @($uploaded | Where-Object name -eq $name)
    if ($asset.Count -ne 1 -or $asset[0].state -ne 'uploaded' `
        -or $asset[0].size -ne (Get-Item (Join-Path $output $name)).Length) {
        throw "Release asset verification failed: $name"
    }
}
gh release edit $Tag --repo $repository --notes-file $notes --draft=false --latest
Assert-GhSuccess
$url = "https://github.com/$repository/releases/tag/$Tag"
Write-Output "Published verified release: $url"
if ($env:GITHUB_STEP_SUMMARY) {
    "[Release]($url) | [EXE](https://github.com/$repository/releases/download/$Tag/RoutePlanner.exe)" |
        Add-Content $env:GITHUB_STEP_SUMMARY
}
