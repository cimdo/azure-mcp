# Azure MCP Evaluations

## Environment Setup

To run evaluations, you'll need to set up your OpenAI API key:

```bash
export OPENAI_API_KEY="your-openai-api-key"
```

Or if using Azure OpenAI:

```bash
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com"
export AZURE_OPENAI_API_KEY="your-azure-openai-key"
```

## Quick Start

1. Build the project:
```bash
dotnet build core/src/AzureMcp.Evals
```

2. Run evaluations:
```bash
dotnet run --project core/src/AzureMcp.Evals -- \
  --evaluations e2eTests/e2eTestPrompts.md \
  --output e2eTests/mcp-evals/results \
  --verbose
```

3. Use the PowerShell script for common scenarios:
```bash
# Run storage evaluations only
./eng/scripts/Run-Evaluations.ps1 -Category storage -Verbose

# Run all evaluations with GPT-3.5
./eng/scripts/Run-Evaluations.ps1 -Model gpt-3.5-turbo
```

## Sample Output

The tool will generate detailed reports in JSON format showing:
- Individual evaluation results with scores (1-5) for accuracy, completeness, relevance, clarity, and reasoning
- Overall statistics and averages
- Execution time and success rates

This enables continuous monitoring of MCP server quality and helps identify areas for improvement.
