using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users.Models.Responses;

namespace WFAI.Application.Features.Users.Queries.ExportUsers
{
    public class ExportUsersQuery : IQuery<IResponseWrapper<byte[]>>
    {
        public PagedFilterRequest PagedFilterRequest { get; set; } = new();
        public string ExportFormat { get; set; } = "excel";
    }

    public class ExportUsersQueryHandler(IUserService userService)
        : IQueryHandler<ExportUsersQuery, IResponseWrapper<byte[]>>
    {
        private readonly IUserService _userService = userService;

        public async ValueTask<IResponseWrapper<byte[]>> Handle(ExportUsersQuery request, CancellationToken ct)
        {
            var listResponse = await _userService.GetUsersListAsync(request.PagedFilterRequest, ct);

            if (!listResponse.IsSuccessful || listResponse.Data == null)
            {
                return ResponseWrapper<byte[]>.Fail(
                    listResponse.Messages ?? new List<string> { "Failed to retrieve users for export." },
                    listResponse.StatusCode);
            }

            var fileBytes = await _userService.ExportUsersAsync(listResponse.Data, request.ExportFormat, ct);

            return ResponseWrapper<byte[]>.Success(fileBytes);
        }
    }
}