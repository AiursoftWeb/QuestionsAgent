using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aiursoft.QuestionsAgent.PluginFramework.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aiursoft.QuestionsAgent.PluginFramework.Services;

public class OllamaClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OllamaClient> _logger;
    private readonly OllamaOptions _options;

    public OllamaClient(
        IHttpClientFactory httpClientFactory,
        ILogger<OllamaClient> logger,
        IOptions<OllamaOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    public virtual async Task<T?> CallOllamaJson<T>(string prompt)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.Token}");
        client.Timeout = TimeSpan.FromMinutes(2);

        var payload = new
        {
            model = _options.Model,
            messages = new[] { new { role = "user", content = prompt } },
            stream = false,
            format = "json",
            options = new { temperature = 0.1, num_ctx = 4096 }
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        const int maxAttempts = 3;

        for (var i = 1; i <= maxAttempts; i++)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(_options.Instance, content, cts.Token);
                response.EnsureSuccessStatusCode();

                var jsonStr = await response.Content.ReadAsStringAsync(cts.Token);
                using var doc = JsonDocument.Parse(jsonStr);
                var aiText = doc.RootElement.GetProperty("message").GetProperty("content").GetString();

                if (string.IsNullOrWhiteSpace(aiText))
                {
                    return default;
                }

                // Clean the response if AI wrapped it in markdown blocks
                aiText = Regex.Replace(aiText, @"^```json\s*|\s*```$", "", RegexOptions.Multiline);

                return JsonSerializer.Deserialize<T>(aiText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                });
            }
            catch (Exception ex)
            {
                if (i == maxAttempts)
                {
                    _logger.LogError(ex, "AI call failed after {MaxAttempts} attempts.", maxAttempts);
                    throw;
                }
                _logger.LogWarning(ex, "AI call failed (attempt {CurrentAttempt}), retrying...", i);
            }
        }

        return default;
    }
}
