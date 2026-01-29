using Aiursoft.CommandFramework.Abstracts;
using Aiursoft.QuestionsAgent.PluginFramework.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.QuestionsAgent;

public class Startup : IStartUp
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddTransient<TextCleaner>();
        services.AddTransient<OllamaClient>();
        services.AddTransient<SectionSplitter>();
        services.AddTransient<Extractor>();
        services.AddTransient<Matcher>();
        services.AddTransient<ResultSaver>();
        services.AddTransient<Processor>();
    }
}
