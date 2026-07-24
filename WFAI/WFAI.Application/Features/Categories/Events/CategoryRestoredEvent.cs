using Mediator;
using Microsoft.Extensions.Logging;

namespace WFAI.Application.Features.Categories.Events
{
    public class CategoryRestoredEvent : INotification
    {
        public CategoryRestoredEvent(int id)
        {
            CategoryId = id;
        }

        public int CategoryId { get; }
    }

    public class CategoryRestoredEventHandler(ILogger<CategoryRestoredEventHandler> logger) : INotificationHandler<CategoryRestoredEvent>
    {
        private readonly ILogger<CategoryRestoredEventHandler> _logger = logger;

        public async ValueTask Handle(CategoryRestoredEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("CategoryRestoredEvent received for CategoryId {CategoryId}", notification.CategoryId);
            await Task.CompletedTask;
        }
    }
}