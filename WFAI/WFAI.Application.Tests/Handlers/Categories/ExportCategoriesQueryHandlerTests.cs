using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using WFAI.Application.Dtos.Wrappers;
using WFAI.Application.Features.Categories.Queries.ExportCategories;
using WFAI.Application.Features.Categories.Queries.GetCategoriesPaged;
using WFAI.Application.Interfaces.Common;
using WFAI.Application.Tests.Support.Categories;
using Xunit;

namespace WFAI.Application.Tests.Handlers.Categories
{
    public class ExportCategoriesQueryHandlerTests
    {
        private readonly Mock<ICurrentUserService> _currentUserService = new();
        private readonly Mock<ICategoryExportService> _categoryExportService = new();

        public ExportCategoriesQueryHandlerTests()
        {
            _currentUserService.Setup(u => u.IsAuthenticated()).Returns(true);
            _currentUserService.Setup(u => u.HasClaim(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        }

        [Fact]
        public async Task Handle_should_return_file_bytes_when_successful()
        {
            await using var scope = await CategoryHandlerTestScope.CreateAsync();

            await scope.SeedCategoryAsync("Test 1", "test-1", 1, isActive: true);
            await scope.SeedCategoryAsync("Test 2", "test-2", 2, isActive: true);

            var query = new ExportCategoriesQuery
            {
                SearchTerm = "test",
                IsActive = true,
                SortBy = "name",
                SortDirection = "asc",
                ExportFormat = "excel"
            };

            var fileBytes = new byte[] { 1, 2, 3 };

            _categoryExportService
                .Setup(s => s.ExportCategoriesAsync(
                    It.Is<List<CategoryResponse>>(list => list.Count == 2 && list.Any(c => c.Name == "Test 1") && list.Any(c => c.Name == "Test 2")),
                    "excel",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(fileBytes);

            var handler = new ExportCategoriesQueryHandler(scope.DbContext, _currentUserService.Object, _categoryExportService.Object);

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccessful.Should().BeTrue();
            result.Data.Should().BeEquivalentTo(fileBytes);
            _categoryExportService.Verify(s => s.ExportCategoriesAsync(
                It.Is<List<CategoryResponse>>(list => list.Count == 2),
                "excel",
                CancellationToken.None), Times.Once);
        }
    }
}