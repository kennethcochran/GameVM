$ErrorActionPreference = "Stop"

# Create tools directory if it doesn't exist
$toolsDir = Join-Path $PSScriptRoot "tools"
if (-not (Test-Path $toolsDir)) {
    New-Item -ItemType Directory -Path $toolsDir | Out-Null
}

# Get latest ANTLR version from GitHub
Write-Host "Fetching latest ANTLR version from GitHub..."
$releases = Invoke-RestMethod -Uri "https://api.github.com/repos/antlr/antlr4/releases/latest"
$latestVersion = $releases.tag_name -replace '^v', ''
$antlrJar = "antlr-$latestVersion-complete.jar"
$antlrPath = Join-Path $toolsDir $antlrJar

# Download ANTLR if not present or if version is different
$currentVersion = $null
if (Test-Path $toolsDir) {
    $existingJar = Get-ChildItem $toolsDir -Filter "antlr-*-complete.jar" | Select-Object -First 1
    if ($existingJar) {
        $currentVersion = [regex]::Match($existingJar.Name, 'antlr-(.+?)-complete\.jar').Groups[1].Value
    }
}

if ($currentVersion -ne $latestVersion) {
    Write-Host "Downloading ANTLR v$latestVersion..."
    $downloadUrl = "https://www.antlr.org/download/$antlrJar"
    Invoke-WebRequest -Uri $downloadUrl -OutFile $antlrPath
    
    # Clean up old versions
    Get-ChildItem $toolsDir -Filter "antlr-*-complete.jar" | 
        Where-Object { $_.Name -ne $antlrJar } | 
        ForEach-Object { 
            Write-Host "Removing old version: $($_.Name)"
            Remove-Item $_.FullName 
        }
}
else {
    Write-Host "Using existing ANTLR v$currentVersion"
}

# Configuration
$lexerGrammar = "PythonLexer.g4"
$parserGrammar = "PythonParser.g4"
$namespace = "GameVM.Compiler.Python.ANTLR"

# Generate lexer first
Write-Host "Generating lexer from $lexerGrammar..."
java -jar $antlrPath -Dlanguage=CSharp -package $namespace -visitor -listener $lexerGrammar

# Then generate parser
Write-Host "Generating parser from $parserGrammar..."
java -jar $antlrPath -Dlanguage=CSharp -package $namespace -visitor -listener $parserGrammar

Write-Host "Parser generation complete!"
