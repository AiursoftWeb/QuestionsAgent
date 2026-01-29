using Aiursoft.CommandFramework.Abstracts;
using Aiursoft.QuestionsAgent.PluginFramework.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.QuestionsAgent.Calendar;

public class Startup : IStartUp
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<Database>();
        services.AddScoped<DatabaseManager>();
        services.AddScoped<CalendarRenderer>();
        services.AddScoped<Algorithm>();
    }
}
