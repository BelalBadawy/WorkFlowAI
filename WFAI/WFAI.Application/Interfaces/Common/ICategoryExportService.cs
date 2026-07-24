using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WFAI.Application.Features.Categories.Queries.GetCategoriesPaged;

namespace WFAI.Application.Interfaces.Common
{
    public interface ICategoryExportService
    {
        Task<byte[]> ExportCategoriesAsync(List<CategoryResponse> data, string format, CancellationToken ct);
    }
}