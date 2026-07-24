using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using WFAI.Application.Features.Categories.Queries.GetCategoriesPaged;
using WFAI.Infrastructure.Services;
using Xunit;

namespace WFAI.Infrastructure.Tests.Services
{
    public class CategoryExportTest
    {
        public CategoryExportTest()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        }

        [Fact]
        public async Task ExportCategoriesAsync_Excel_should_not_throw()
        {
            var data = new List<CategoryResponse>
            {
                new(1, "Test 1", "test-1", null, 1, true, false, System.Array.Empty<byte>()),
                new(2, "Test 2", "test-2", null, 2, true, false, System.Array.Empty<byte>())
            };

            var service = new CategoryExportService();
            
            var bytes = await service.ExportCategoriesAsync(data, "excel", CancellationToken.None);
            bytes.Should().NotBeNull();
            bytes.Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ExportCategoriesAsync_Pdf_should_not_throw()
        {
            var data = new List<CategoryResponse>
            {
                new(1, "Test 1", "test-1", null, 1, true, false, System.Array.Empty<byte>()),
                new(2, "Test 2", "test-2", null, 2, true, false, System.Array.Empty<byte>())
            };

            var service = new CategoryExportService();
            
            var bytes = await service.ExportCategoriesAsync(data, "pdf", CancellationToken.None);
            bytes.Should().NotBeNull();
            bytes.Length.Should().BeGreaterThan(0);
        }
    }
}