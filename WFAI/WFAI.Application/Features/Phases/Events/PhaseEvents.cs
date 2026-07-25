namespace WFAI.Application.Features.Phases.Events
{
    public record PhaseCreatedEvent(int PhaseId) : INotification;
    public record PhaseUpdatedEvent(int PhaseId) : INotification;
    public record PhaseDeletedEvent(int PhaseId) : INotification;
}
