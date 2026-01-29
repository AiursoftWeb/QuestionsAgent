using Aiursoft.CommandFramework;
using Aiursoft.CommandFramework.Models;
using Aiursoft.QuestionsAgent.Calendar.Handlers.Config;
using Aiursoft.QuestionsAgent.Calendar.Handlers.Get;
using Aiursoft.QuestionsAgent.Calendar.Handlers.Mark;

return await new NestedCommandApp()
    .WithFeature(new GetHandler())
    .WithFeature(new MarkHandler())
    .WithFeature(new ConfigHandler())
    .WithGlobalOptions(CommonOptionsProvider.DryRunOption)
    .WithGlobalOptions(CommonOptionsProvider.VerboseOption)
    .RunAsync(args);
