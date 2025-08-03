#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs Azure MCP evaluations with common configurations
.DESCRIPTION
    This script provides an easy way to run MCP evaluations with predefined configurations.
    It supports running against different categories and with different models.
.PARAMETER Category
    Filter evaluations by category (e.g., storage, compute, search)
.PARAMETER Model
    LLM model to use for scoring (default: gpt-4)
.PARAMETER Parallel
    Whether to run evaluations in parallel (default: true)
.PARAMETER Verbose
    Enable verbose logging
.EXAMPLE
    ./Run-Evaluations.ps1 -Category storage
.EXAMPLE
    ./Run-Evaluations.ps1 -Model gpt-3.5-turbo -Verbose
#>

param(
    [string]$Category = "",
    [string]$Model = "gpt-4",
    [switch]$Parallel = $true,
    [switch]$Verbose = $false
)

# Check if OpenAI API key is set
if (-not $env:OPENAI_API_KEY -and -not $env:AZURE_OPENAI_API_KEY) {
    Write-Error "Please set either OPENAI_API_KEY or AZURE_OPENAI_API_KEY environment variable"
    exit 1
}

# Build the project first
Write-Host "Building AzureMcp.Evals..." -ForegroundColor Green
dotnet build core/src/AzureMcp.Evals

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

# Prepare arguments
$args = @(
    "--evaluations", "e2eTests/e2eTestPrompts.md"
    "--server", "dotnet run --project core/src/AzureMcp.Cli"
    "--model", $Model
    "--output", "e2eTests/mcp-evals/results"
)

if ($Category) {
    $args += "--category", $Category
}

if (-not $Parallel) {
    $args += "--parallel", "false"
}

if ($Verbose) {
    $args += "--verbose"
}

# Run the evaluations
Write-Host "Running MCP evaluations..." -ForegroundColor Green
Write-Host "Arguments: $($args -join ' ')" -ForegroundColor Gray

dotnet run --project core/src/AzureMcp.Evals -- @args

if ($LASTEXITCODE -eq 0) {
    Write-Host "Evaluations completed successfully!" -ForegroundColor Green
    Write-Host "Results saved to: e2eTests/mcp-evals/results/" -ForegroundColor Yellow
} else {
    Write-Error "Evaluations failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}
