using System.Text.RegularExpressions;
using AzureMcp.Evals.Models;

namespace AzureMcp.Evals.Services;

/// <summary>
/// Service for parsing evaluation cases from the e2eTestPrompts.md file
/// </summary>
public class E2eTestPromptsParser
{
    public static async Task<IReadOnlyList<EvaluationCase>> ParseFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"E2E test prompts file not found: {filePath}");
        }

        var content = await File.ReadAllTextAsync(filePath);
        return ParseFromContent(content);
    }

    public static IReadOnlyList<EvaluationCase> ParseFromContent(string content)
    {
        var evaluationCases = new List<EvaluationCase>();
        var lines = content.Split('\n');
        
        string? currentCategory = null;
        bool inTable = false;
        int caseCounter = 1;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Check for category headers (## Azure ...)
            if (line.StartsWith("## ") && line.Contains("Azure"))
            {
                currentCategory = ExtractCategoryFromHeader(line);
                inTable = false;
                continue;
            }

            // Check for table headers
            if (line.StartsWith("| Tool Name | Test Prompt |"))
            {
                inTable = true;
                i++; // Skip the separator line
                continue;
            }

            // Parse table rows
            if (inTable && line.StartsWith("|") && !line.StartsWith("| Tool Name"))
            {
                var parts = line.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var toolName = parts[0].Trim();
                    var prompt = parts[1].Trim();

                    if (!string.IsNullOrEmpty(toolName) && !string.IsNullOrEmpty(prompt))
                    {
                        var evaluationCase = CreateEvaluationCase(
                            toolName, 
                            prompt, 
                            currentCategory ?? "unknown", 
                            caseCounter++);
                        
                        evaluationCases.Add(evaluationCase);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(line) && !line.StartsWith("|"))
            {
                inTable = false;
            }
        }

        return evaluationCases;
    }

    private static string ExtractCategoryFromHeader(string header)
    {
        // Convert "## Azure Storage" -> "storage"
        // Convert "## Azure App Configuration" -> "appconfig"
        // Convert "## Azure AI Search" -> "search"
        
        var categoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Azure AI Foundry"] = "foundry",
            ["Azure AI Search"] = "search",
            ["Azure App Configuration"] = "appconfig",
            ["Azure CLI"] = "cli",
            ["Azure Cosmos DB"] = "cosmos",
            ["Azure Data Explorer"] = "kusto",
            ["Azure Database for PostgreSQL"] = "postgres",
            ["Azure Developer CLI"] = "azd",
            ["Azure Key Vault"] = "keyvault",
            ["Azure Kubernetes Service (AKS)"] = "aks",
            ["Azure Load Testing"] = "loadtesting",
            ["Azure Managed Grafana"] = "grafana",
            ["Azure Marketplace"] = "marketplace",
            ["Azure MCP Best Practices"] = "bestpractices",
            ["Azure MCP Tools"] = "tools",
            ["Azure Monitor"] = "monitor",
            ["Azure Native ISV"] = "isv",
            ["Azure Quick Review CLI"] = "azqr",
            ["Azure RBAC"] = "rbac",
            ["Azure Redis"] = "redis",
            ["Azure Resource Group"] = "resourcegroup",
            ["Azure Service Bus"] = "servicebus",
            ["Azure SQL Database"] = "sql",
            ["Azure SQL Elastic Pool Operations"] = "sql",
            ["Azure SQL Server Operations"] = "sql",
            ["Azure Storage"] = "storage",
            ["Azure Subscription Management"] = "subscription",
            ["Azure Terraform Best Practices"] = "terraform",
            ["Azure Workbooks"] = "workbooks",
            ["Bicep"] = "bicep"
        };

        var headerText = header.Replace("##", "").Trim();
        
        if (categoryMap.TryGetValue(headerText, out var category))
        {
            return category;
        }

        // Fallback: extract last word and lowercase
        var words = headerText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 0 ? words.Last().ToLowerInvariant() : "unknown";
    }

    private static EvaluationCase CreateEvaluationCase(
        string toolName, 
        string prompt, 
        string category, 
        int counter)
    {
        // Clean up the prompt by removing placeholders like <resource-name>
        var cleanPrompt = CleanPrompt(prompt);
        
        // Create a meaningful ID
        var id = $"{category}-{counter:D3}";
        
        // Create a human-readable name
        var name = GenerateName(toolName, prompt);
        
        // Generate description
        var description = $"Test the {toolName} tool with prompt: {cleanPrompt}";
        
        // Extract tags
        var tags = GenerateTags(toolName, category);

        return new EvaluationCase
        {
            Id = id,
            Name = name,
            Description = description,
            Prompt = cleanPrompt,
            ExpectedTool = toolName,
            ExpectedBehavior = $"Should invoke the {toolName} tool and provide appropriate response",
            Category = category,
            Tags = tags
        };
    }

    private static string CleanPrompt(string prompt)
    {
        // Replace common placeholders with example values
        var cleaned = prompt;
        
        var replacements = new Dictionary<string, string>
        {
            [@"\<resource-name\>"] = "my-resource",
            [@"\<resource_name\>"] = "my-resource",
            [@"\<account-name\>"] = "my-account",
            [@"\<account_name\>"] = "my-account",
            [@"\<storage_account_name\>"] = "mystorageaccount",
            [@"\<service-name\>"] = "my-service",
            [@"\<service_name\>"] = "my-service",
            [@"\<index-name\>"] = "my-index",
            [@"\<index_name\>"] = "my-index",
            [@"\<search_term\>"] = "example",
            [@"\<key_name\>"] = "MyKey",
            [@"\<app_config_store_name\>"] = "my-config-store",
            [@"\<value\>"] = "example-value",
            [@"\<cluster-name\>"] = "my-cluster",
            [@"\<cluster_name\>"] = "my-cluster",
            [@"\<database-name\>"] = "my-database",
            [@"\<database_name\>"] = "my-database",
            [@"\<table-name\>"] = "my-table",
            [@"\<table_name\>"] = "my-table",
            [@"\<server\>"] = "my-server",
            [@"\<workspace_name\>"] = "my-workspace",
            [@"\<resource-group\>"] = "my-resource-group",
            [@"\<resource_group\>"] = "my-resource-group",
            [@"\<container_name\>"] = "my-container",
            [@"\<time_period\>"] = "24 hours",
            [@"\<metric_name\>"] = "CPU",
            [@"\<resource_type\>"] = "storage account",
            [@"\<aggregation_type\>"] = "average"
        };

        foreach (var replacement in replacements)
        {
            cleaned = Regex.Replace(cleaned, replacement.Key, replacement.Value, RegexOptions.IgnoreCase);
        }

        return cleaned;
    }

    private static string GenerateName(string toolName, string prompt)
    {
        // Extract action from tool name (e.g., "azmcp-storage-list" -> "List Storage")
        var parts = toolName.Split('-');
        if (parts.Length >= 3)
        {
            var action = parts[^1]; // Last part
            var service = parts[^2]; // Second to last part
            
            return $"{CapitalizeFirst(action)} {CapitalizeFirst(service)}";
        }

        // Fallback: use the first few words of the prompt
        var words = prompt.Split(' ').Take(4);
        return string.Join(" ", words);
    }

    private static string CapitalizeFirst(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        return char.ToUpper(input[0]) + input[1..].ToLower();
    }

    private static IReadOnlyList<string> GenerateTags(string toolName, string category)
    {
        var tags = new List<string> { "azure", category };
        
        // Extract additional tags from tool name
        var parts = toolName.Split('-');
        foreach (var part in parts.Skip(1)) // Skip "azmcp"
        {
            if (!tags.Contains(part, StringComparer.OrdinalIgnoreCase))
            {
                tags.Add(part.ToLowerInvariant());
            }
        }

        return tags;
    }
}
