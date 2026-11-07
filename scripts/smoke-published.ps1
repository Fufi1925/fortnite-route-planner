$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$executable = (Resolve-Path 'artifacts/win-x64/RoutePlanner.exe').Path
# Exercise only the application instance launched by this test on the CI runner.
$applicationProcess = Start-Process -FilePath $executable -PassThru
try {
    if (-not $applicationProcess.WaitForInputIdle(30000)) {
        throw 'Published application did not become ready within 30 seconds'
    }
    $applicationProcess.Refresh()
    if ($applicationProcess.HasExited -or $applicationProcess.MainWindowHandle -eq 0 `
        -or $applicationProcess.MainWindowTitle -notlike '*Routenatelier*Offline-Demo*') {
        throw 'Published application did not display the expected planner window'
    }
    Write-Output "PASS published self-contained EXE started: $($applicationProcess.MainWindowTitle)"
} finally {
    if (-not $applicationProcess.HasExited) {
        # The disposable runner has no user work; terminate only this exact test instance.
        $applicationProcess.Kill()
        $applicationProcess.WaitForExit()
    }
    $applicationProcess.Dispose()
}
