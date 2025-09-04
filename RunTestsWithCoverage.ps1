# PowerShell script to run all tests with code coverage collection

# Define the output directory for coverage results
$outputDir = Join-Path $PSScriptRoot "TestResults"
$coverageReportDir = "$outputDir/CoverageReport"

# Clean previous test results
if (Test-Path $outputDir) {
    Remove-Item -Path "$outputDir/*" -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
New-Item -ItemType Directory -Force -Path $coverageReportDir | Out-Null

# Install ReportGenerator as a local tool
Write-Host "Setting up ReportGenerator..."
if (-not (Test-Path "$PSScriptRoot/.config/dotnet-tools.json")) {
    & dotnet new tool-manifest
}

# Install ReportGenerator if not already installed
$reportGenInstalled = & dotnet tool list | Select-String "reportgenerator"
if (-not $reportGenInstalled) {
    & dotnet tool install dotnet-reportgenerator-globaltool
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install ReportGenerator"
        exit 1
    }
}

# Add coverlet.collector to test projects
$testProjects = @(
    (Get-ChildItem -Path "src" -Recurse -Filter "*.Tests.csproj").FullName,
    (Get-ChildItem -Path "src" -Recurse -Filter "*Specs.csproj").FullName
) | Where-Object { $_ -ne $null } | Select-Object -Unique

foreach ($project in $testProjects) {
    & dotnet add $project package coverlet.collector --version 6.0.0
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to add coverlet.collector to $project"
        exit 1
    }
}

# Find test projects again after package restore
$testProjects = @(
    (Get-ChildItem -Path "src" -Recurse -Filter "*.Tests.csproj").FullName,
    (Get-ChildItem -Path "src" -Recurse -Filter "*Specs.csproj").FullName
) | Where-Object { $_ -ne $null } | Select-Object -Unique

# Find test projects
$testProjects = @(
    (Get-ChildItem -Path "src" -Recurse -Filter "*.Tests.csproj").FullName,
    (Get-ChildItem -Path "src" -Recurse -Filter "*Specs.csproj").FullName
) | Where-Object { $_ -ne $null } | Select-Object -Unique

if ($testProjects.Count -eq 0) {
    Write-Error "No test projects found"
    exit 1
}

Write-Host "`nFound test projects:"
$testProjects | ForEach-Object { Write-Host "- $_" }

# Run tests with coverage
$coverageFiles = @()

foreach ($project in $testProjects) {
    $projectName = (Get-Item $project).BaseName
    $coveragePath = "$outputDir/coverage_$projectName.cobertura.xml"
    
    Write-Host "`nRunning tests for $projectName..."
    
    # Build the project first
    & dotnet build $project --configuration Debug --no-incremental
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $project"
        exit 1
    }
    
        # Run tests with coverage in Cobertura format
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
    $trxPath = Join-Path $outputDir "$projectName.trx"
    $coveragePath = "$outputDir/coverage_$projectName.cobertura.xml"
    
    # Build the project first to ensure everything is up to date
    & dotnet build $project --configuration Debug
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $project"
        exit 1
    }
    
    # Run tests with coverage collection using coverlet with CRAP metric support
    & dotnet test $project `
        --no-build `
        --configuration Debug `
        --logger "trx;LogFileName=$trxPath" `
        --collect:"XPlat Code Coverage" `
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura `
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Exclude="[xunit*]*,[*]Coverlet*,[*]Tests*" `
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.IncludeTestAssembly=true `
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.IncludeDirectory=src
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tests failed for $project"
        exit 1
    }
    
    # Check if coverage file was generated in the TestResults directory
    $testResultsDir = Join-Path (Split-Path $project) "TestResults"
    $coverageFile = Get-ChildItem -Path $testResultsDir -Recurse -Filter "coverage.cobertura.xml" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if ($coverageFile) {
        $destinationPath = Join-Path $outputDir "coverage_$projectName.cobertura.xml"
        Copy-Item -Path $coverageFile.FullName -Destination $destinationPath -Force
        $coverageFiles += $destinationPath
        Write-Host "Coverage report generated at: $destinationPath"
    } else {
        # Fallback to the original path
        if (Test-Path $coveragePath) {
            $coverageFiles += $coveragePath
            Write-Host "Coverage report generated at: $coveragePath"
        } else {
            # Try one more time with a broader search
            $coverageFile = Get-ChildItem -Path (Split-Path $project) -Recurse -Filter "coverage.cobertura.xml" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            if ($coverageFile) {
                $destinationPath = Join-Path $outputDir "coverage_$projectName.cobertura.xml"
                Copy-Item -Path $coverageFile.FullName -Destination $destinationPath -Force
                $coverageFiles += $destinationPath
                Write-Host "Coverage report found and copied to: $destinationPath"
            } else {
                Write-Warning "No coverage file was generated for $project"
                exit 1
            }
        }
    }
}

# Generate HTML report
if ($coverageFiles.Count -gt 0) {
    $reports = $coverageFiles -join ';'
    Write-Host    # Generate HTML report with CRAP metric
    Write-Host "Generating HTML report..."
    
    # Ensure the CustomReports directory exists
    if (-not (Test-Path "CustomReports")) {
        New-Item -ItemType Directory -Path "CustomReports" | Out-Null
    }
    
    # Use the .NET Core global tool directly
    try {
        & dotnet reportgenerator `
            "-reports:$reports" `
            "-targetdir:$coverageReportDir" `
            "-reporttypes:Html;HtmlInline;Badges;Cobertura;HtmlSummary" `
            "-sourcedirs:src" `
            "-classfilters:-*Test*" `
            "-verbosity:Info" `
            "-title:GameVM Test Coverage" `
            "-tag:$(Get-Date -Format 'yyyy-MM-dd')" `
            "-historydir:$coverageReportDir/history" `
            "-assemblyfilters:-*Tests;-*Test;-*Specs;-*Test.*;-*Tests.*" `
            "-enablesourcelink:true" `
            "-verbosity:Verbose" `
            "-filefilters:-*Test*;-*Tests*"
            
        if ($LASTEXITCODE -ne 0) {
            throw "ReportGenerator failed with exit code $LASTEXITCODE"
        }
    } catch {
        Write-Warning "Failed to generate report with dotnet tool, trying direct execution..."
        
        # Try direct execution as fallback
        $reportGeneratorPath = (Get-Command -Name reportgenerator -ErrorAction SilentlyContinue).Source
        if ($reportGeneratorPath) {
            & $reportGeneratorPath `
                "-reports:$reports" `
                "-targetdir:$coverageReportDir" `
                "-reporttypes:Html;Badges;Cobertura" `
                "-sourcedirs:src" `
                "-classfilters:-*Test*" `
                "-verbosity:Info" `
                "-title:GameVM Test Coverage" `
                "-tag:$(Get-Date -Format 'yyyy-MM-dd')" `
                "-plugins:CustomReports/CRAPScore.dll"
        } else {
            Write-Error "ReportGenerator not found. Please install it using: dotnet tool install -g dotnet-reportgenerator-globaltool"
            exit 1
        }
    }
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to generate coverage report"
        exit 1
    }
} else {
    Write-Error "No coverage files were generated"
    exit 1
}
