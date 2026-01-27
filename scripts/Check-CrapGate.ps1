#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory=$true)]
    [string]$ReportPath,
    
    [Parameter(Mandatory=$false)]
    [int]$Threshold = 30
)

$ErrorActionPreference = "Stop"

Write-Host "🔍 Checking CRAP Score Quality Gate..." -ForegroundColor Cyan
Write-Host "📊 Report: $ReportPath | 🎯 Threshold: $Threshold" -ForegroundColor Gray

if (-not (Test-Path $ReportPath)) {
    Write-Host "❌ Coverage report not found: $ReportPath" -ForegroundColor Red
    Write-Host "💡 Run tests with coverage first: dotnet test --collect:'XPlat Code Coverage'" -ForegroundColor Yellow
    exit 1
}

try {
    $report = Get-Content $ReportPath | ConvertFrom-Json
} catch {
    Write-Host "❌ Failed to parse coverage report: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$violations = @()
$totalMethods = 0

foreach ($class in $report.classes) {
    foreach ($method in $class.methods) {
        $totalMethods++
        $crapScore = $method.crapScore
        
        if ($crapScore -gt $Threshold) {
            $violations += [PSCustomObject]@{
                Class = $class.name
                Method = $method.name
                CrapScore = $crapScore
                Complexity = $method.cyclomaticComplexity
                Coverage = $method.coverage
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "`n❌ CRAP Score Quality Gate FAILED" -ForegroundColor Red
    Write-Host "🚨 Found $($violations.Count)/$totalMethods methods exceeding CRAP threshold of $Threshold" -ForegroundColor Red
    
    Write-Host "`n🔥 High-Risk Methods:" -ForegroundColor Yellow
    $violations | Sort-Object CrapScore -Descending | ForEach-Object {
        Write-Host "  • $($_.Class).$($_.Method): CRAP=$($_.CrapScore) (CC=$($_.Complexity), Coverage=$($_.Coverage)%)" -ForegroundColor Red
    }
    
    Write-Host "`n💡 To fix: Reduce complexity or increase test coverage" -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "✅ CRAP Score Quality Gate PASSED" -ForegroundColor Green
    Write-Host "📊 All $totalMethods methods have CRAP ≤ $Threshold" -ForegroundColor Gray
    exit 0
}
