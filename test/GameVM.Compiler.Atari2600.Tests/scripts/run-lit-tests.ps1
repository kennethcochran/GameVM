<#
.SYNOPSIS
    Sets up and runs LLVM lit tests for the GameVM compiler.
.DESCRIPTION
    This script:
    1. Checks for Python 3 installation
    2. Installs lit if not present
    3. Sets up a basic test structure
    4. Runs the tests using lit
#>

# Configuration
$LitTestDir = Join-Path $PSScriptRoot "../lit-tests"
$CompileExe = Join-Path $PSScriptRoot "../../../src/GameVM.Compile/bin/Debug/net8.0/compile.exe"
$VenvDir = Join-Path $PSScriptRoot ".venv"
$PythonExe = "python3"
$PipExe = "pip3"

function Test-CommandExists {
    param($command)
    $exists = $null -ne (Get-Command $command -ErrorAction SilentlyContinue)
    return $exists
}

function Initialize-Venv {
    Write-Host "Creating Python virtual environment..."
    & $PythonExe -m venv $VenvDir
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create Python virtual environment"
        exit 1
    }

    $script:PythonExe = Join-Path $VenvDir "bin" "python"
    $script:PipExe = Join-Path $VenvDir "bin" "pip"
    
    # Upgrade pip
    & $PythonExe -m pip install --upgrade pip
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to upgrade pip"
        exit 1
    }
}

function Install-Lit {
    Write-Host "Installing lit in virtual environment..."
    & $PipExe install lit
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install lit"
        exit 1
    }
}

function Initialize-TestStructure {
    param(
        [string]$testDir
    )

    # Create test directories
    $testDirs = @("features", "regression", "unit")
    foreach ($dir in $testDirs) {
        $fullPath = Join-Path $testDir $dir
        if (-not (Test-Path $fullPath)) {
            New-Item -ItemType Directory -Path $fullPath | Out-Null
            Write-Host "Created test directory: $fullPath"
        }
    }

    # Create a sample test
    $sampleTest = @"
// RUN: %compile %s -o %t
// RUN: %t | FileCheck %s
// CHECK: Hello, Atari 2600!

program HelloWorld;
begin
    WriteLn('Hello, Atari 2600!');
end.
"@

    $sampleTestPath = Join-Path $testDir "features" "hello_world.pas"
    if (-not (Test-Path $sampleTestPath)) {
        $sampleTest | Out-File -FilePath $sampleTestPath -Encoding utf8
        Write-Host "Created sample test: $sampleTestPath"
    }

    # Create lit.site.cfg
    $litConfig = @"
import os
import lit.llvm
from lit.llvm import llvm_config

# Set up paths
config.name = 'GameVM Compiler Tests'
config.test_format = lit.formats.ShTest(not llvm_config.use_lit_shell)

# Set file patterns to recognize as tests
config.suffixes = ['.pas', '.txt']

# Set the root directories
config.test_source_root = os.path.dirname(os.path.abspath(__file__))
config.test_exec_root = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'test_output')

# Ensure the output directory exists
os.makedirs(config.test_exec_root, exist_ok=True)

# Set up substitutions
compile_cmd = r'$([System.IO.Path]::GetFullPath("$($CompileExe -replace '\\', '\\\\')") -replace '\\', '\\\\')'
config.substitutions = [
    ('%{compile}', compile_cmd),
    ('%{FileCheck}', 'FileCheck'),
    ('%{test_exec_root}', config.test_exec_root)
]

# Set available features
config.available_features = ['host-run-tool']
"@

    $litConfigPath = Join-Path $testDir "lit.site.cfg.py"
    
    # Remove old configuration if it exists
    if (Test-Path $litConfigPath) {
        Remove-Item -Path $litConfigPath -Force
    }
    
    $litConfig | Out-File -FilePath $litConfigPath -Encoding utf8
    Write-Host "Created lit configuration: $litConfigPath"
}

# Main script execution
Write-Host "=== GameVM Compiler Test Runner ==="
Write-Host "Test Directory: $LitTestDir"
Write-Host "Compiler Path: $CompileExe"

# Check Python installation
if (-not (Test-CommandExists $PythonExe)) {
    Write-Error "Python 3 is required but not found. Please install Python 3 and ensure it's in your PATH."
    exit 1
}

# Check pip installation
if (-not (Test-CommandExists $PipExe)) {
    Write-Error "pip is required but not found. Please ensure pip is installed and in your PATH."
    exit 1
}

# Ensure virtual environment exists
if (-not (Test-Path $VenvDir)) {
    Initialize-Venv
    Install-Lit
}

# Activate virtual environment
$activateScript = Join-Path $VenvDir "bin" "Activate.ps1"
if (Test-Path $activateScript) {
    . $activateScript
}

# Verify lit is installed
try {
    $litVersion = & $PythonExe -c "import lit; print(lit.__version__)" 2>&1
    if (-not $?) {
        throw "lit not found"
    }
    Write-Host "Found lit version: $litVersion"
} catch {
    Write-Host "lit not found. Installing..."
    Install-Lit
}

# Ensure test directory exists
if (-not (Test-Path $LitTestDir)) {
    Write-Host "Creating test directory: $LitTestDir"
    New-Item -ItemType Directory -Path $LitTestDir | Out-Null
}

# Initialize test structure if it doesn't exist
if (-not (Test-Path (Join-Path $LitTestDir "lit.site.cfg.py"))) {
    Write-Host "Initializing test structure in $LitTestDir"
    Initialize-TestStructure -testDir $LitTestDir
}

# Compile the compiler if needed
if (-not (Test-Path $CompileExe)) {
    Write-Host "Compiling the compiler..."
    $solutionDir = Join-Path $PSScriptRoot "..\..\.." -Resolve
    dotnet build (Join-Path $solutionDir "GameVM.sln") -c Debug
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build the compiler"
        exit 1
    }
}

# Run the tests
Write-Host "`nRunning tests..."
# Make sure we're using the virtual environment's Python
$venvPython = Join-Path $VenvDir "bin" "python"
if (-not (Test-Path $venvPython)) {
    $venvPython = Join-Path $VenvDir "Scripts" "python.exe"
}

# Find and run lit executable from the virtual environment
$litExecutable = Join-Path $VenvDir "bin" "lit"
if (-not (Test-Path $litExecutable)) {
    $litExecutable = Join-Path $VenvDir "Scripts" "lit.exe"
}

if (-not (Test-Path $litExecutable)) {
    Write-Error "Could not find lit executable in virtual environment"
    exit 1
}

# Run lit with the test directory
Push-Location (Split-Path $LitTestDir)
$testDirName = Split-Path $LitTestDir -Leaf
& $litExecutable -v $testDirName
$testResult = $LASTEXITCODE
Pop-Location

if ($testResult -ne 0) {
    Write-Error "`nSome tests failed"
    exit $testResult
}

Write-Host "`nAll tests passed!"
exit 0
