using Microsoft.Extensions.Logging;

namespace WFAI.Application.Features.Categories.Events
{
    public class CategoryDeletedEvent : INotification
    {
        public CategoryDeletedEvent(int id)
        {
            CategoryId = id;
        }
        public int CategoryId { get; }

    }

    public class CategoryDeletedEventHandler(ILogger<CategoryDeletedEventHandler> logger) : INotificationHandler<CategoryDeletedEvent>
    {
        private readonly ILogger<CategoryDeletedEventHandler> _logger = logger;

        public async ValueTask Handle(CategoryDeletedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("CategoryDeletedEvent received for CategoryId {CategoryId}", notification.CategoryId);
            await Task.CompletedTask;
        }
    }
}