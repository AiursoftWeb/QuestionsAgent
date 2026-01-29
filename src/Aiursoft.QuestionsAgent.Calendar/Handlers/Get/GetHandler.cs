using Aiursoft.CommandFramework.Framework;

namespace Aiursoft.QuestionsAgent.Calendar.Handlers.Get;

public class GetHandler : NavigationCommandHandlerBuilder
{
    protected  override string Name => "get";

    protected  override string Description => "Database result fetcher.";

    protected override CommandHandlerBuilder[] GetSubCommandHandlers()
    {
        return
        [
            new ScoreHandler(),
            new HistoryHandler(),
            new CalendarHandler(),
            new FeelingHandler()
        ];
    }
}
