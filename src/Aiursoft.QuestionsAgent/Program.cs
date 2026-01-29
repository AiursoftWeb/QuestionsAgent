using Aiursoft.CommandFramework;
using Aiursoft.CommandFramework.Models;
using Aiursoft.QuestionsAgent.Handlers;

return await new NestedCommandApp()
    .WithFeature(new ProcessHandler())
    .WithGlobalOptions(CommonOptionsProvider.DryRunOption)
    .WithGlobalOptions(CommonOptionsProvider.VerboseOption)
    .RunAsync(args);
