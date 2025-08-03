# Azure MCP Evaluation Framework

The Azure MCP Evaluation Framework is a .NET tool that provides automated testing and scoring of Azure MCP server implementations using LLM-based evaluation. This tool is inspired by the Node.js [mcp-evals](https://github.com/mclenhard/mcp-evals) package and implements similar functionality for the .NET ecosystem.

## Overview

The evaluation framework helps ensure MCP tool implementations work correctly by:

1. Running predefined test prompts against the MCP server
2. Capturing the server's responses and tool invocations
3. Using LLMs (like GPT-4) to score the responses on multiple criteria
4. Generating detailed reports with scores and recommendations

## Key Features

- **Automated Testing**: Execute evaluation cases against MCP servers
- **LLM-based Scoring**: Use GPT-4 or other models to evaluate response quality
- **Multiple Criteria**: Score on accuracy, completeness, relevance, clarity, and reasoning
- **Parallel Execution**: Run evaluations concurrently for faster results
- **Flexible Configuration**: Support for OpenAI API and Azure OpenAI
- **Detailed Reporting**: Generate comprehensive JSON reports
- **Category Filtering**: Focus on specific areas (storage, compute, etc.)

## Quick Start

### Prerequisites

1. .NET 9.0 or later
2. OpenAI API key or Azure OpenAI access
3. Azure MCP Server project

### Installation

The evaluation tool is included as part of the Azure MCP project:

```bash
git clone https://github.com/Azure/azure-mcp.git
cd azure-mcp
dotnet build core/src/AzureMcp.Evals
```

### Basic Usage

1. Set up your API key:
```bash
export OPENAI_API_KEY="your-openai-api-key"
```

2. Run evaluations:
```bash
dotnet run --project core/src/AzureMcp.Evals -- \
  --evaluations e2eTests/e2eTestPrompts.md \
  --output ./results \
  --verbose
```

3. Or use the PowerShell script:
```powershell
./eng/scripts/Run-Evaluations.ps1 -Category storage -Verbose
```

## Configuration

### Configuration File

Create a `config.json` file:

```json
{
  "mcpServerPath": "dotnet run --project core/src/AzureMcp.Cli",
  "evaluationCasesPath": "e2eTests/evaluation-cases.json",
  "scoringModel": "gpt-4",
  "outputDirectory": "./results",
  "parallel": true,
  "maxParallelism": 4,
  "timeoutSeconds": 60
}
```

### Evaluation Cases

Define test cases in JSON format:

```json
[
  {
    "id": "storage-list-001",
    "name": "List Storage Accounts",
    "description": "Test ability to list all storage accounts in the subscription",
    "prompt": "List all storage accounts in my subscription",
    "expectedTool": "azmcp-storage-list",
    "expectedBehavior": "Should invoke the storage list tool and return account information",
    "category": "storage",
    "tags": ["azure", "storage", "list"]
  }
]
```

## Scoring Criteria

Each evaluation is scored on a 1-5 scale across five dimensions:

| Criterion | Description |
|-----------|-------------|
| **Accuracy** | How correct is the information provided? |
| **Completeness** | Does it provide all necessary information? |
| **Relevance** | Is the response appropriate for the query? |
| **Clarity** | Is the information presented clearly? |
| **Reasoning** | Does the model show sound reasoning in tool use? |

## Command Line Options

| Option | Description | Default |
|--------|-------------|---------|
| `--config` | Path to configuration file | - |
| `--evaluations` | Path to evaluation cases file | Required |
| `--server` | Path to MCP server executable | - |
| `--model` | LLM model for scoring | gpt-4 |
| `--output` | Output directory | ./results |
| `--parallel` | Run evaluations in parallel | true |
| `--category` | Filter by category | - |
| `--verbose` | Enable verbose logging | false |

## Output Format

The tool generates:

### Results File
```json
{
  "evaluationCase": {
    "id": "storage-list-001",
    "name": "List Storage Accounts",
    // ... evaluation case details
  },
  "actualResponse": "Found 3 storage accounts: ...",
  "actualTool": "azmcp-storage-list",
  "score": {
    "accuracy": 4,
    "completeness": 3,
    "relevance": 5,
    "clarity": 4,
    "reasoning": 4,
    "overall": 4.0,
    "explanation": "Response correctly identified storage accounts..."
  },
  "durationMs": 1250,
  "success": true
}
```

### Summary Report
```json
{
  "timestamp": "2025-01-01T12:00:00Z",
  "totalEvaluations": 10,
  "successfulEvaluations": 9,
  "failedEvaluations": 1,
  "averageScores": {
    "accuracy": 4.2,
    "completeness": 3.8,
    "relevance": 4.5,
    "clarity": 4.0,
    "reasoning": 3.9,
    "overall": 4.1
  }
}
```

## Integration with CI/CD

Add to your GitHub Actions workflow:

```yaml
- name: Run MCP Evaluations
  run: |
    dotnet run --project core/src/AzureMcp.Evals -- \
      --evaluations e2eTests/evaluation-cases.json \
      --server "dotnet run --project core/src/AzureMcp.Cli" \
      --output ./evaluation-results
  env:
    OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}

- name: Upload Results
  uses: actions/upload-artifact@v3
  with:
    name: evaluation-results
    path: ./evaluation-results/
```

## Best Practices

1. **Comprehensive Test Cases**: Cover all major tool categories and common use patterns
2. **Regular Evaluation**: Run evaluations on every significant change
3. **Score Thresholds**: Set minimum score requirements for releases
4. **Category Organization**: Group evaluations by functional area for focused testing
5. **Iterative Improvement**: Use evaluation feedback to enhance tool implementations

## Troubleshooting

### Common Issues

1. **API Key Not Set**: Ensure `OPENAI_API_KEY` or Azure OpenAI credentials are configured
2. **Build Failures**: Verify .NET 9.0 is installed and project builds successfully
3. **Low Scores**: Review LLM explanations for specific improvement guidance
4. **Timeout Issues**: Increase `timeoutSeconds` for complex evaluations

### Debugging

Enable verbose logging to see detailed execution information:

```bash
dotnet run --project core/src/AzureMcp.Evals -- \
  --evaluations e2eTests/test-evaluation.json \
  --verbose
```

## Contributing

To add new evaluation cases:

1. Create evaluation cases in the appropriate category
2. Test with the evaluation framework
3. Ensure consistent scoring patterns
4. Document expected behaviors clearly

## Architecture

The evaluation framework consists of:

- **`Program.cs`**: CLI interface and orchestration
- **`Models/`**: Data structures for evaluations and results
- **`Services/`**: Core evaluation and scoring logic
- **Configuration**: JSON-based configuration system
- **Reporting**: Detailed JSON output generation

This design enables easy extension and customization for different evaluation scenarios.
