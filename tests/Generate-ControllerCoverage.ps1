$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$targets = @(
    @{ Layer = "Backend"; Path = Join-Path $root "backend/Controllers"; TestPath = "tests/backend/UbhayaFashions.Backend.Tests/Controllers" },
    @{ Layer = "Frontend"; Path = Join-Path $root "frontend/Controllers"; TestPath = "tests/frontend/UbhayaFashions.Frontend.Tests/Controllers" }
)
$rows = [System.Collections.Generic.List[string]]::new()
$rows.Add("# Controller and action coverage matrix")
$rows.Add("")
$rows.Add("> Generated from the current controller source. Status is deliberately conservative: IN PROGRESS means a behavioral test has not yet been verified for every applicable scenario. Route/method metadata tests do not count.")
$rows.Add("")
$rows.Add("| Layer | Controller | Public action | Test file exists | Status |")
$rows.Add("|---|---|---|---|---|")
foreach ($target in $targets) {
    if (-not (Test-Path $target.Path)) { throw "Controller source directory missing: $($target.Path)" }
    foreach ($file in Get-ChildItem $target.Path -Filter "*.cs" -File | Sort-Object Name) {
        $controller = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $testFile = Join-Path $root (Join-Path $target.TestPath ($controller + "Tests.cs"))
        $source = Get-Content -LiteralPath $file.FullName -Raw
        $matches = [regex]::Matches($source, '(?m)^\s*public\s+(?:(?:async|static|virtual|override|new)\s+)*(?:[\w<>,\[\]?.]+\s+)+(?<method>\w+)\s*\(')
        $actions = @($matches | ForEach-Object { $_.Groups["method"].Value } | Sort-Object -Unique)
        if ($actions.Count -eq 0) {
            $rows.Add("| $($target.Layer) | $controller | (no public methods detected) | $(if (Test-Path $testFile) {'Yes'} else {'No'}) | IN PROGRESS |")
        }
        foreach ($action in $actions) {
            $rows.Add("| $($target.Layer) | $controller | $action | $(if (Test-Path $testFile) {'Yes'} else {'No'}) | IN PROGRESS |")
        }
    }
}
$rows.Add("")
$rows.Add("## Completion policy")
$rows.Add("")
$rows.Add("- Change an action to COMPLETE only after meaningful behavioral NUnit tests for all applicable positive, negative, boundary and security scenarios have been implemented and executed.")
$rows.Add("- Mark a scenario N/A only with a short reason.")
$rows.Add("- Do not mark a controller complete because its test class exists.")
$rows.Add("- This generated inventory is an audit checklist, not evidence that coverage is complete.")
$destination = Join-Path $PSScriptRoot "CONTROLLER-COVERAGE.md"
$rows | Set-Content -LiteralPath $destination -Encoding UTF8
Write-Host "Generated $destination"
