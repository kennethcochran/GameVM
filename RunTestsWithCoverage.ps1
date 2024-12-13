# PowerShell script to run all tests with code coverage collection

# Define the output directory for coverage results
$outputDir = Join-Path $PSScriptRoot "TestResults"

# Ensure the output directory exists
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

# Run the tests with code coverage collection
& dotnet test --results-directory:"$outputDir" --collect:"XPlat Code Coverage" --verbosity:normal

# Find the coverage file in the results directory
$coverageFile = Get-ChildItem -Path $outputDir -Filter "*.cobertura.xml" -Recurse | Select-Object -First 1

if ($coverageFile) {
    # Get the full path to reportgenerator
    $reportGenPath = "$env:USERPROFILE\.dotnet\tools\reportgenerator.exe"
    
    # Convert the coverage report to HTML format
    & $reportGenPath -reports:$($coverageFile.FullName) -targetdir:"$outputDir/CoverageReport" -reporttypes:Html
    Write-Host "Coverage report generated at: $outputDir/CoverageReport/index.html"
} else {
    Write-Host "No coverage file found in $outputDir"
}
