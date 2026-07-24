using Microsoft.Extensions.Logging;

namespace WFAI.Application.Features.Categories.Events
{
    public class CategoryCreatedEvent : INotification
    {
        public CategoryCreatedEvent(int id)
        {
            CategoryId = id;
        }

        public int CategoryId { get; }

    }

    public class CategoryCreatedEventHandler(ILogger<CategoryCreatedEventHandler> logger) : INotificationHandler<CategoryCreatedEvent>
    {
        private readonly ILogger<CategoryCreatedEventHandler> _logger = logger;

        public async ValueTask Handle(CategoryCreatedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("CategoryCreatedEvent received for CategoryId {CategoryId}", notification.CategoryId);
            await Task.CompletedTask;
        }
    }



}