using Microsoft.Extensions.Logging;

namespace WFAI.Application.Features.Categories.Events
{
    public class CategoryUpdatedEvent : INotification
    {
        public CategoryUpdatedEvent(int id)
        {
            CategoryId = id;
        }
        public int CategoryId { get; }
    }
    public class CategoryUpdatedEventHandler(ILogger<CategoryUpdatedEventHandler> logger) : INotificationHandler<CategoryUpdatedEvent>
    {
        private readonly ILogger<CategoryUpdatedEventHandler> _logger = logger;

        public async ValueTask Handle(CategoryUpdatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("CategoryUpdatedEvent received for CategoryId {CategoryId}", notification.CategoryId);
            await Task.CompletedTask;
        }
    }
}