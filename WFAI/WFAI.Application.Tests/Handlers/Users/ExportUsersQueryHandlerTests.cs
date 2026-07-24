using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using WFAI.Application.Dtos.Pagination;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Users;
using WFAI.Application.Features.Users.Models.Responses;
using WFAI.Application.Features.Users.Queries.ExportUsers;
using Xunit;

namespace WFAI.Application.Tests.Handlers.Users
{
    public class ExportUsersQueryHandlerTests
    {
        private readonly Mock<IUserService> _userService = new();

        [Fact]
        public async Task Handle_should_return_file_bytes_when_successful()
        {
            var filter = new PagedFilterRequest
            {
                SearchTerm = "john",
                SortBy = "email"
            };

            var query = new ExportUsersQuery
            {
                PagedFilterRequest = filter,
                ExportFormat = "pdf"
            };

            var users = new List<UserExportResponse>
            {
                new() { Id = 1, FullName = "John Doe", Email = "john@doe.com", Roles = new List<string> { "Admin" } }
            };

            var fileBytes = new byte[] { 4, 5, 6 };

            _userService
                .Setup(s => s.GetUsersListAsync(filter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseWrapper<List<UserExportResponse>>.Success(users));

            _userService
                .Setup(s => s.ExportUsersAsync(users, "pdf", It.IsAny<CancellationToken>()))
                .ReturnsAsync(fileBytes);

            var handler = new ExportUsersQueryHandler(_userService.Object);

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccessful.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(fileBytes);
            _userService.Verify(s => s.GetUsersListAsync(filter, CancellationToken.None), Times.Once);
            _userService.Verify(s => s.ExportUsersAsync(users, "pdf", CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Handle_should_return_failure_when_list_retrieval_fails()
        {
            var filter = new PagedFilterRequest();
            var query = new ExportUsersQuery { PagedFilterRequest = filter };

            _userService
                .Setup(s => s.GetUsersListAsync(filter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResponseWrapper<List<UserExportResponse>>.Fail("Error retrieving list", 500));

            var handler = new ExportUsersQueryHandler(_userService.Object);

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Messages.Should().Contain("Error retrieving list");
            _userService.Verify(s => s.GetUsersListAsync(filter, CancellationToken.None), Times.Once);
            _userService.Verify(s => s.ExportUsersAsync(It.IsAny<List<UserExportResponse>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}