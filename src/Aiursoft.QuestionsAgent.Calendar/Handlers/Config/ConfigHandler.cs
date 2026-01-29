using Aiursoft.CommandFramework.Framework;

namespace Aiursoft.QuestionsAgent.Calendar.Handlers.Config;

public class ConfigHandler : NavigationCommandHandlerBuilder
{
    protected override string Name => "config";

    protected  override string Description => "Configuration management.";

    protected override CommandHandlerBuilder[] GetSubCommandHandlers()
    {
        return
        [
            new GetDbLocationHandler(),
            new SetDbLocationHandler()
        ];
    }
}
