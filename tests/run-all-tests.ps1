param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$results = Join-Path $PSScriptRoot "TestResults"
New-Item -ItemType Directory -Force -Path $results | Out-Null

Get-ChildItem -Path $results -Force | Remove-Item -Recurse -Force
$projects = @(
    @{ Name = "Backend NUnit"; Path = "tests/backend/UbhayaFashions.Backend.Tests/UbhayaFashions.Backend.Tests.csproj" },
    @{ Name = "Frontend NUnit"; Path = "tests/frontend/UbhayaFashions.Frontend.Tests/UbhayaFashions.Frontend.Tests.csproj" },
    @{ Name = "Legacy regression"; Path = "tests/UbhayaFashions.Tests/UbhayaFashions.Tests.csproj" }
)

$runResults = @()
$frontendBuildExit = 1
Push-Location $root
try {
    foreach ($project in $projects) {
        Write-Host ""
        Write-Host "========== $($project.Name) ==========" -ForegroundColor Cyan
        $trxName = ($project.Name -replace '[^A-Za-z0-9]+', '-') + ".trx"
        dotnet test $project.Path --configuration $Configuration --logger "trx;LogFileName=$trxName" --collect:"XPlat Code Coverage" --results-directory $results
        $exitCode = $LASTEXITCODE
        $runResults += [pscustomobject]@{ Name = $project.Name; ExitCode = $exitCode }
        if ($exitCode -ne 0) {
            Write-Host "$($project.Name) FAILED." -ForegroundColor Red
        }
    }

    Write-Host ""
    Write-Host "========== Frontend application build ==========" -ForegroundColor Cyan
    dotnet build "frontend/LadiesDressStore.Web.csproj" --configuration $Configuration --no-restore
    $frontendBuildExit = $LASTEXITCODE
}
finally {
    Pop-Location
}

$total = 0; $passed = 0; $failed = 0; $skipped = 0
$trxFiles = @(Get-ChildItem -Path $results -Filter "*.trx" -Recurse -ErrorAction SilentlyContinue)
foreach ($trx in $trxFiles) {
    try {
        [xml]$doc = Get-Content -LiteralPath $trx.FullName
        $counter = $doc.SelectSingleNode("//*[local-name()='Counters']")
        if ($null -ne $counter) {
            $total += [int]$counter.total
            $passed += [int]$counter.passed
            $failed += [int]$counter.failed
            $skipped += [int]$counter.notExecuted
        }
    }
    catch {
        Write-Warning "Could not parse test report $($trx.Name): $_"
    }
}

$projectFailures = @($runResults | Where-Object { $_.ExitCode -ne 0 }).Count
$overallPass = ($projectFailures -eq 0 -and $frontendBuildExit -eq 0 -and $trxFiles.Count -eq $projects.Count -and $failed -eq 0 -and $skipped -eq 0)
$status = if ($overallPass) { "PASS" } else { "FAIL" }

$projectSummary = ($runResults | ForEach-Object {
    $state = if ($_.ExitCode -eq 0) { "PASS" } else { "FAIL" }
    "$($_.Name): $state"
}) -join [Environment]::NewLine

$summary = @"
UBHAYA FASHIONS - PRE-DEPLOYMENT TEST REPORT
=============================================
Backend NUnit / Frontend NUnit / Legacy regression

Total tests : $total
Passed      : $passed
Failed      : $failed
Skipped     : $skipped
TRX reports : $($trxFiles.Count) / $($projects.Count)
Frontend build exit code: $frontendBuildExit

Project execution:
$projectSummary

FINAL RESULT: $status
"@

$summaryPath = Join-Path $results "summary.txt"
$summary | Set-Content -Path $summaryPath -Encoding UTF8
$coverageFiles = @(Get-ChildItem -Path $results -Filter "coverage.cobertura.xml" -Recurse)
$coverageList = ($coverageFiles | ForEach-Object { "<li>$([System.Net.WebUtility]::HtmlEncode($_.FullName))</li>" }) -join ""
$html = "<html><head><title>Ubhaya Fashions Test Report</title></head><body><h1>Ubhaya Fashions - Pre-deployment Test Report</h1><pre>$([System.Net.WebUtility]::HtmlEncode($summary))</pre><h2>Cobertura coverage files</h2><ul>$coverageList</ul></body></html>"
$html | Set-Content -Path (Join-Path $results "summary.html") -Encoding UTF8

Write-Host $summary
Write-Host "Reports: $results"
if (-not $overallPass) { exit 1 }
exit 0
