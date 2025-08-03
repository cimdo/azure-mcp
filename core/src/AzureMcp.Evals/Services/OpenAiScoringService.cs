using System.Text;
using System.Text.Json;
using AzureMcp.Evals.Models;
using Microsoft.Extensions.Logging;

namespace AzureMcp.Evals.Services;

/// <summary>
/// OpenAI-based implementation of LLM scoring service
/// </summary>
public class OpenAiScoringService(
    HttpClient httpClient,
    EvaluationConfig config,
    ILogger<OpenAiScoringService> logger) : ILlmScoringService
{
    private static readonly SemaphoreSlim RateLimitSemaphore = new(10, 10); // Allow max 10 concurrent requests
    private static DateTime LastRequestTime = DateTime.MinValue;
    private static readonly object LockObject = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task<EvaluationScore> ScoreAsync(
        EvaluationCase evaluationCase,
        string actualResponse,
        string? actualTool,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = BuildScoringPrompt(evaluationCase, actualResponse, actualTool);
            var response = await CallOpenAiAsync(prompt, cancellationToken);
            return ParseScoringResponse(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to score evaluation case {EvaluationId}", evaluationCase.Id);
            
            // Return default scores if LLM scoring fails
            return new EvaluationScore
            {
                Accuracy = 1,
                Completeness = 1,
                Relevance = 1,
                Clarity = 1,
                Reasoning = 1,
                Explanation = $"LLM scoring failed: {ex.Message}"
            };
        }
    }

    private static string BuildScoringPrompt(EvaluationCase evaluationCase, string actualResponse, string? actualTool)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("You are an expert evaluator for Model Context Protocol (MCP) tool interactions.");
        prompt.AppendLine("Please evaluate the following MCP interaction and provide scores from 1-5 for each criterion.");
        prompt.AppendLine();
        
        prompt.AppendLine("EVALUATION CASE:");
        prompt.AppendLine($"Name: {evaluationCase.Name}");
        prompt.AppendLine($"Description: {evaluationCase.Description}");
        prompt.AppendLine($"User Prompt: {evaluationCase.Prompt}");
        
        if (!string.IsNullOrEmpty(evaluationCase.ExpectedTool))
        {
            prompt.AppendLine($"Expected Tool: {evaluationCase.ExpectedTool}");
        }
        
        if (!string.IsNullOrEmpty(evaluationCase.ExpectedBehavior))
        {
            prompt.AppendLine($"Expected Behavior: {evaluationCase.ExpectedBehavior}");
        }
        
        prompt.AppendLine();
        prompt.AppendLine("ACTUAL RESPONSE:");
        prompt.AppendLine(actualResponse);
        
        if (!string.IsNullOrEmpty(actualTool))
        {
            prompt.AppendLine();
            prompt.AppendLine($"ACTUAL TOOL INVOKED: {actualTool}");
        }
        
        prompt.AppendLine();
        prompt.AppendLine("SCORING CRITERIA (1-5 scale):");
        prompt.AppendLine("1. Accuracy: How correct is the information provided?");
        prompt.AppendLine("2. Completeness: Does it provide all necessary information?");
        prompt.AppendLine("3. Relevance: Is the response appropriate for the query?");
        prompt.AppendLine("4. Clarity: Is the information presented clearly?");
        prompt.AppendLine("5. Reasoning: Does the model show sound reasoning in its use of tools?");
        prompt.AppendLine();
        prompt.AppendLine("Please respond with ONLY a JSON object in this exact format:");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"accuracy\": 4,");
        prompt.AppendLine("  \"completeness\": 3,");
        prompt.AppendLine("  \"relevance\": 5,");
        prompt.AppendLine("  \"clarity\": 4,");
        prompt.AppendLine("  \"reasoning\": 3,");
        prompt.AppendLine("  \"explanation\": \"Brief explanation of the scores\"");
        prompt.AppendLine("}");
        
        return prompt.ToString();
    }

    private async Task<string> CallOpenAiAsync(string prompt, CancellationToken cancellationToken)
    {
        // Rate limiting: ensure minimum delay between requests
        await RateLimitSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            await EnforceRateLimit();
            
            // Get API key from environment or config
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? config.OpenAiApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is required. Set OPENAI_API_KEY environment variable or configure in settings.");
            }

            var requestBody = new
            {
                model = config.ScoringModel,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.1,
                max_tokens = 500
            };

            // Use snake_case for OpenAI API
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = false
            };

            var json = JsonSerializer.Serialize(requestBody, jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            logger.LogDebug("Sending request to OpenAI API with model: {Model}", config.ScoringModel);

            // Set up authentication
            if (!string.IsNullOrEmpty(config.AzureOpenAiEndpoint))
            {
                // Azure OpenAI
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("api-key", config.AzureOpenAiApiKey);
                var url = $"{config.AzureOpenAiEndpoint}/openai/deployments/{config.ScoringModel}/chat/completions?api-version=2024-02-15-preview";
                var response = await httpClient.PostAsync(url, content, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogError("Azure OpenAI API error {StatusCode}: {Error}", response.StatusCode, errorContent);
                    
                    // Handle rate limiting with retry
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        await HandleRateLimitRetry(errorContent);
                        // Re-attempt the request after rate limit handling
                        response = await httpClient.PostAsync(url, content, cancellationToken);
                        if (!response.IsSuccessStatusCode)
                        {
                            errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                            throw new HttpRequestException($"Azure OpenAI API returned {response.StatusCode}: {errorContent}");
                        }
                    }
                    else
                    {
                        throw new HttpRequestException($"Azure OpenAI API returned {response.StatusCode}: {errorContent}");
                    }
                }
                
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return ExtractOpenAiResponse(responseJson);
            }
            else
            {
                // OpenAI
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogError("OpenAI API error {StatusCode}: {Error}", response.StatusCode, errorContent);
                    
                    // Handle rate limiting with retry
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        await HandleRateLimitRetry(errorContent);
                        // Re-attempt the request after rate limit handling
                        response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content, cancellationToken);
                        if (!response.IsSuccessStatusCode)
                        {
                            errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                            throw new HttpRequestException($"OpenAI API returned {response.StatusCode}: {errorContent}");
                        }
                    }
                    else
                    {
                        throw new HttpRequestException($"OpenAI API returned {response.StatusCode}: {errorContent}");
                    }
                }
                
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogDebug("Received response from OpenAI API");
                return ExtractOpenAiResponse(responseJson);
            }
        }
        finally
        {
            RateLimitSemaphore.Release();
        }
    }

    private async Task EnforceRateLimit()
    {
        // Dynamic rate limiting based on model
        var delayMs = GetModelRateLimit(config.ScoringModel);
        
        lock (LockObject)
        {
            var timeSinceLastRequest = DateTime.UtcNow - LastRequestTime;
            if (timeSinceLastRequest.TotalMilliseconds < delayMs)
            {
                var delayNeeded = delayMs - (int)timeSinceLastRequest.TotalMilliseconds;
                if (delayNeeded > 0)
                {
                    logger.LogDebug("Rate limiting: waiting {DelayMs}ms for model {Model}", delayNeeded, config.ScoringModel);
                    // Note: Using Task.Delay().Wait() in lock is not ideal, but ensures sequential access
                    Task.Delay(delayNeeded).Wait();
                }
            }
            LastRequestTime = DateTime.UtcNow;
        }
        
        await Task.CompletedTask; // Satisfy async requirement
    }

    private async Task HandleRateLimitRetry(string errorContent)
    {
        // Extract retry delay from error message if available
        var retryDelayMs = ExtractRetryDelay(errorContent);
        
        logger.LogWarning("Rate limit hit, waiting {DelayMs}ms before retry", retryDelayMs);
        await Task.Delay(retryDelayMs);
    }

    private static int ExtractRetryDelay(string errorContent)
    {
        // Try to extract "Please try again in 20s" from error message
        try
        {
            if (errorContent.Contains("try again in"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(errorContent, @"try again in (\d+)s");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var seconds))
                {
                    return seconds * 1000 + 1000; // Add 1 second buffer
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }
        
        // Default to 30 seconds for rate limit retry
        return 30000;
    }

    private static int GetModelRateLimit(string model)
    {
        // Conservative rate limiting based on known OpenAI model limits
        return model.ToLowerInvariant() switch
        {
            var m when m.Contains("gpt-4o-mini") || m.Contains("gpt-4.1-mini") => 25000, // 3 RPM = ~20 second delay
            var m when m.Contains("gpt-4") => 1000,  // Conservative for GPT-4 models (60 RPM)
            var m when m.Contains("gpt-3.5") => 150, // GPT-3.5-turbo (400 RPM)
            _ => 500 // Default conservative rate limiting
        };
    }

    private static string ExtractOpenAiResponse(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var choices = doc.RootElement.GetProperty("choices");
        var firstChoice = choices[0];
        var message = firstChoice.GetProperty("message");
        return message.GetProperty("content").GetString() ?? string.Empty;
    }

    private static EvaluationScore ParseScoringResponse(string response)
    {
        try
        {
            // Clean up the response - sometimes LLMs add extra text
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = response[jsonStart..(jsonEnd + 1)];
                return JsonSerializer.Deserialize<EvaluationScore>(jsonContent, JsonOptions) 
                       ?? throw new InvalidOperationException("Failed to deserialize scoring response");
            }
            
            throw new InvalidOperationException("No valid JSON found in LLM response");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse LLM scoring response: {ex.Message}");
        }
    }
}
