using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WFAI.Application.Features.Phases;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Infrastructure.Services
{
    public class PhaseExportService : IPhaseExportService
    {
        public async Task<byte[]> ExportPhasesAsync(List<PhaseDto> data, string format, CancellationToken ct)
        {
            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                return GeneratePdfExport(data);
            }
            else
            {
                return GenerateExcelExport(data);
            }
        }

        private byte[] GenerateExcelExport(List<PhaseDto> data)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Phases");

            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Title";
            worksheet.Cell(1, 3).Value = "Description";
            worksheet.Cell(1, 4).Value = "Sort Order";
            worksheet.Cell(1, 5).Value = "Status";

            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.Id;
                worksheet.Cell(row, 2).Value = item.Title;
                worksheet.Cell(row, 3).Value = item.Description ?? string.Empty;
                worksheet.Cell(row, 4).Value = item.SortOrder;
                worksheet.Cell(row, 5).Value = item.IsActive ? "Active" : "Inactive";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private byte[] GeneratePdfExport(List<PhaseDto> data)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Helvetica"));

                    page.Header()
                        .PaddingBottom(10)
                        .Text("Phases Report")
                        .SemiBold().FontSize(16).FontColor(Colors.Indigo.Medium);

                    page.Content()
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40); // ID
                                columns.RelativeColumn(3f); // Title
                                columns.RelativeColumn(4f); // Description
                                columns.RelativeColumn(1.5f); // Sort Order
                                columns.RelativeColumn(1.5f); // Status
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("ID").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("Title").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("Description").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("Sort Order").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("Status").SemiBold().FontColor(Colors.White);

                                static IContainer HeaderStyle(IContainer container)
                                {
                                    return container
                                        .Background(Colors.Indigo.Medium)
                                        .Padding(6)
                                        .AlignMiddle();
                                }
                            });

                            foreach (var item in data)
                            {
                                table.Cell().Element(CellStyle).Text(item.Id.ToString());
                                table.Cell().Element(CellStyle).Text(item.Title);
                                table.Cell().Element(CellStyle).Text(item.Description ?? "");
                                table.Cell().Element(CellStyle).Text(item.SortOrder.ToString());
                                table.Cell().Element(CellStyle).Text(item.IsActive ? "Active" : "Inactive");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container
                                        .BorderBottom(0.5f)
                                        .BorderColor(Colors.Grey.Lighten3)
                                        .Padding(6)
                                        .AlignMiddle();
                                }
                            }
                        });

                    page.Footer()
                        .PaddingTop(10)
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }
    }
}
