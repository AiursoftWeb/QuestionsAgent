using System.CommandLine;
using System.CommandLine.Parsing;
using Aiursoft.CommandFramework.Framework;
using Aiursoft.CommandFramework.Services;
using Aiursoft.QuestionsAgent.PluginFramework.Models;
using Aiursoft.QuestionsAgent.PluginFramework.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.QuestionsAgent.Handlers;

public class ProcessHandler : ExecutableCommandHandlerBuilder
{
    private static readonly Option<string> InputOption = new(
        name: "--input",
        aliases: new[] { "-i" })
    {
        Description = "The input markdown file to process.",
        Required = true
    };

    private static readonly Option<string> OutputOption = new(
        name: "--output",
        aliases: new[] { "-o" })
    {
        Description = "The output directory for JSON files.",
        DefaultValueFactory = _ => "FinalOutput"
    };

    private static readonly Option<string> OllamaInstanceOption = new(
        name: "--instance")
    {
        Description = "The Ollama instance to use.",
        Required = true
    };

    private static readonly Option<string> OllamaModelOption = new(
        name: "--model")
    {
        Description = "The Ollama model to use.",
        Required = true
    };

    private static readonly Option<string> OllamaTokenOption = new(
        name: "--token")
    {
        Description = "The Ollama token to use.",
        Required = true
    };

    protected override string Name => "process";

    protected override string Description => "Process a markdown file containing questions and output JSON files.";

    protected override IEnumerable<Option> GetCommandOptions() => new Option[]
    {
        InputOption,
        OutputOption,
        OllamaInstanceOption,
        OllamaModelOption,
        OllamaTokenOption
    };

    protected override async Task Execute(ParseResult parseResult)
    {
        var input = parseResult.GetValue(InputOption)!;
        var output = parseResult.GetValue(OutputOption)!;
        var instance = parseResult.GetValue(OllamaInstanceOption)!;
        var model = parseResult.GetValue(OllamaModelOption)!;
        var token = parseResult.GetValue(OllamaTokenOption)!;

        var host = ServiceBuilder
            .CreateCommandHostBuilder<Startup>(false)
            .ConfigureServices((_, services) =>
            {
                services.Configure<OllamaOptions>(options =>
                {
                    options.Instance = instance;
                    options.Model = model;
                    options.Token = token;
                });
            })
            .Build();

        var processor = host.Services.GetRequiredService<Processor>();
        await processor.RunAsync(input, output);
    }
}
