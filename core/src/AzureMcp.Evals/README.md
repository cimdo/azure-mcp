# Azure MCP Evaluations

A .NET tool for automatically testing and scoring Azure MCP server implementations using LLM-based evaluation.

## Overview

This tool provides automated evaluation of Model Context Protocol (MCP) implementations, similar to the Node.js [mcp-evals](https://github.com/mclenhard/mcp-evals) package. It runs evaluation cases against an MCP server and uses Large Language Models (LLMs) to score the responses on multiple criteria.

## Features

- **Automated Testing**: Run predefined evaluation cases against your MCP server
- **LLM-based Scoring**: Use GPT-4 or other models to score responses on accuracy, completeness, relevance, clarity, and reasoning
- **Parallel Execution**: Run evaluations in parallel for faster results
- **Flexible Configuration**: Support for OpenAI API or Azure OpenAI
- **Detailed Reporting**: Generate comprehensive reports with scores and explanations
- **Category Filtering**: Filter evaluations by category or tags

## Usage

### Basic Usage

```bash
# Run evaluations directly from e2eTestPrompts.md
dotnet run --project core/src/AzureMcp.Evals -- \
  --evaluations e2eTests/e2eTestPrompts.md \
  --output ./results \
  --verbose
```

### With Configuration File

```bash
# Use a configuration file (defaults to e2eTestPrompts.md)
dotnet run --project core/src/AzureMcp.Evals -- \
  --config e2eTests/mcp-evals/config.json
```

### Environment Variables

Set your API key:

```bash
export OPENAI_API_KEY="your-openai-api-key"
# or for Azure OpenAI
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com"
export AZURE_OPENAI_API_KEY="your-azure-openai-key"
```

### Command Line Options

- `--config`: Path to configuration file (JSON)
- `--evaluations`: Path to evaluation cases file (JSON) [Required]
- `--server`: Path to MCP server executable
- `--model`: LLM model to use for scoring (default: gpt-4)
- `--output`: Output directory for results (default: ./results)
- `--parallel`: Run evaluations in parallel (default: true)
- `--category`: Filter evaluations by category
- `--verbose`: Enable verbose logging

## Configuration

### Configuration File Format

```json
### Configuration File Format

```json
{
  "mcpServerPath": "dotnet run --project core/src/AzureMcp.Cli",
  "mcpServerArgs": [],
  "evaluationCasesPath": "e2eTests/e2eTestPrompts.md",
  "scoringModel": "gpt-4",
  "outputDirectory": "./results",
  "parallel": true,
  "maxParallelism": 4,
  "timeoutSeconds": 60
}
```

### Evaluation Sources

The tool supports two input formats:

1. **Markdown format** (`e2eTestPrompts.md`): Automatically parsed from the existing e2e test prompts
2. **JSON format**: Custom evaluation cases in structured format
```

### Evaluation Cases Format

```json
[
  {
    "id": "storage-list-001",
    "name": "List Storage Accounts",
    "description": "Test ability to list all storage accounts in the subscription",
    "prompt": "List all storage accounts in my subscription",
    "expectedTool": "azmcp-storage-list",
    "expectedBehavior": "Should invoke the storage list tool and return a list of storage accounts with their names and basic information",
    "category": "storage",
    "tags": ["azure", "storage", "list"]
  }
]
```

## Scoring Criteria

Each evaluation is scored on a 1-5 scale across five criteria:

1. **Accuracy**: How correct is the information provided?
2. **Completeness**: Does it provide all necessary information?
3. **Relevance**: Is the response appropriate for the query?
4. **Clarity**: Is the information presented clearly?
5. **Reasoning**: Does the model show sound reasoning in its use of tools?

## Output

The tool generates:

- **Detailed Results**: Complete evaluation results with scores and explanations
- **Summary Report**: Aggregated statistics and average scores
- **JSON Files**: Machine-readable results for further analysis

Example output structure:
```
results/
├── evaluation-results-2025-01-01T12-00-00.json
└── evaluation-summary-2025-01-01T12-00-00.json
```

## Integration with CI/CD

You can integrate this tool into your continuous integration pipeline to automatically test MCP implementations:

```yaml
- name: Run MCP Evaluations
  run: |
    dotnet run --project core/src/AzureMcp.Evals -- \
      --evaluations e2eTests/evaluation-cases.json \
      --server "dotnet run --project core/src/AzureMcp.Cli" \
      --output ./evaluation-results
  env:
    OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
```

## Development

### Building

```bash
dotnet build core/src/AzureMcp.Evals
```

### Running Tests

```bash
# Run with sample evaluation cases
dotnet run --project core/src/AzureMcp.Evals -- \
  --evaluations e2eTests/evaluation-cases.json \
  --verbose
```

## Contributing

Contributions are welcome! Please ensure that:

1. New evaluation cases follow the established format
2. Code includes appropriate error handling
3. Documentation is updated for new features

## License

This project is licensed under the same terms as the Azure MCP project.
