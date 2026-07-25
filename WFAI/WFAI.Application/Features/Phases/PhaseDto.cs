namespace WFAI.Application.Features.Phases
{
    public record PhaseDto(
        int Id,
        string Title,
        string? Description,
        int SortOrder,
        bool IsActive,
        bool SoftDeleted,
        byte[] RowVersion
    );
}
