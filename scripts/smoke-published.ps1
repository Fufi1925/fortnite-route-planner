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
        -or $applicationProcess.MainWindowTitle -notlike '*Route Planner*Lokal*') {
        throw 'Published application did not display the expected planner window'
    }
    Write-Output "PASS published self-contained EXE started: $($applicationProcess.MainWindowTitle)"
    if ($env:ROUTE_VERIFY_FORTNITE_GG -eq '1') {
        Add-Type -AssemblyName UIAutomationClient
        Add-Type -AssemblyName UIAutomationTypes
        $planner = [System.Windows.Automation.AutomationElement]::FromHandle($applicationProcess.MainWindowHandle)
        $buttonCondition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'OpenFortniteGg')
        $button = $planner.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $buttonCondition)
        if ($null -eq $button) { throw 'Published EXE does not expose the Fortnite.GG action' }
        $invoke = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $invoke.Invoke()
        $deadline = [DateTime]::UtcNow.AddSeconds(60)
        $loaded = $false
        $statusCondition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'WebMapStatus')
        $processCondition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $applicationProcess.Id)
        do {
            $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $processCondition)
            foreach ($window in $windows) {
                $status = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $statusCondition)
                if ($null -ne $status -and $status.Current.Name -like 'https://fortnite.gg/*') { $loaded = $true; break }
            }
            if (-not $loaded) { Start-Sleep -Milliseconds 500 }
        } while (-not $loaded -and [DateTime]::UtcNow -lt $deadline)
        if (-not $loaded) { throw 'Published single-file EXE did not load Fortnite.GG in WebView2' }
        Write-Output 'PASS published EXE loaded Fortnite.GG in WebView2 (including bundled native loader)'
    }
} finally {
    if (-not $applicationProcess.HasExited) {
        # The disposable runner has no user work; terminate only this exact test instance.
        $applicationProcess.Kill()
        $applicationProcess.WaitForExit()
    }
    $applicationProcess.Dispose()
}
