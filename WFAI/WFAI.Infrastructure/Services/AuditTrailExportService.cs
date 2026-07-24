using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WFAI.Application.Features.AuditTrails.Queries.GetAuditTrailsPaged;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Infrastructure.Services
{
    public class AuditTrailExportService : IAuditTrailExportService
    {
        public async Task<byte[]> ExportAuditTrailsAsync(List<AuditTrailResponse> data, string format, CancellationToken ct)
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

        private byte[] GenerateExcelExport(List<AuditTrailResponse> data)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Audit Logs");

            // Define headers
            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "User Email";
            worksheet.Cell(1, 3).Value = "IP Address";
            worksheet.Cell(1, 4).Value = "Type";
            worksheet.Cell(1, 5).Value = "Table Name";
            worksheet.Cell(1, 6).Value = "DateTime (UTC)";
            worksheet.Cell(1, 7).Value = "PrimaryKey";
            worksheet.Cell(1, 8).Value = "Affected Columns";
            worksheet.Cell(1, 9).Value = "Old Values";
            worksheet.Cell(1, 10).Value = "New Values";

            // Format Header Row
            var headerRange = worksheet.Range(1, 1, 1, 10);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5"); // Indigo-600
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Populate rows
            int row = 2;
            foreach (var log in data)
            {
                worksheet.Cell(row, 1).Value = log.Id;
                worksheet.Cell(row, 2).Value = log.UserEmail ?? "System / Guest";
                worksheet.Cell(row, 3).Value = log.IpAddress ?? "N/A";
                worksheet.Cell(row, 4).Value = log.Type;
                worksheet.Cell(row, 5).Value = log.TableName ?? "N/A";
                worksheet.Cell(row, 6).Value = log.DateTime;
                worksheet.Cell(row, 6).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                worksheet.Cell(row, 7).Value = log.PrimaryKey ?? "N/A";
                worksheet.Cell(row, 8).Value = log.AffectedColumns ?? "N/A";
                worksheet.Cell(row, 9).Value = log.OldValues ?? "";
                worksheet.Cell(row, 10).Value = log.NewValues ?? "";
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private byte[] GeneratePdfExport(List<AuditTrailResponse> data)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Helvetica"));

                    // Header
                    page.Header()
                        .PaddingBottom(10)
                        .Text("Audit Trails Report")
                        .SemiBold().FontSize(16).FontColor(Colors.Indigo.Medium);

                    // Content
                    page.Content()
                        .Table(table =>
                        {
                            // Columns definition
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(35); // ID
                                columns.RelativeColumn(2.5f); // User Email
                                columns.RelativeColumn(1.2f); // Type
                                columns.RelativeColumn(2f); // Table Name
                                columns.RelativeColumn(1.5f); // IP Address
                                columns.RelativeColumn(2.5f); // DateTime
                                columns.RelativeColumn(1.5f); // Primary Key
                            });

                            // Table Header
                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("ID").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("User Email").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("Type").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("Table Name").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("IP Address").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("DateTime (UTC)").SemiBold().FontColor(Colors.White);
                                header.Cell().Element(HeaderStyle).Text("Primary Key").SemiBold().FontColor(Colors.White);

                                static IContainer HeaderStyle(IContainer container)
                                {
                                    return container
                                        .Background(Colors.Indigo.Medium)
                                        .Padding(6)
                                        .AlignMiddle();
                                }
                            });

                            // Table Rows
                            foreach (var log in data)
                            {
                                table.Cell().Element(CellStyle).Text(log.Id.ToString());
                                table.Cell().Element(CellStyle).Text(log.UserEmail ?? "System / Guest");
                                table.Cell().Element(CellStyle).Text(log.Type);
                                table.Cell().Element(CellStyle).Text(log.TableName ?? "N/A");
                                table.Cell().Element(CellStyle).Text(log.IpAddress ?? "N/A");
                                table.Cell().Element(CellStyle).Text(log.DateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                                table.Cell().Element(CellStyle).Text(log.PrimaryKey ?? "N/A");

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

                    // Footer
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