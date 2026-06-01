using System.Drawing;
using ServicioFrontend.Dtos.Sales;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;

namespace ServicioFrontend.Pages.Sales;

public sealed class SalesListingExcelExporter : ISalesListingExcelExporter
{
    private static readonly Color Ink = Color.FromArgb(13, 31, 21);
    private static readonly Color SurfaceAlt = Color.FromArgb(233, 232, 226);
    private static readonly Color Accent = Color.FromArgb(0, 229, 255);

    public byte[] Export(IReadOnlyList<SaleSummaryDto> sales, SalesListingExcelExportContext context)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Mercadito");

        using var package = new ExcelPackage();
        package.Workbook.Properties.Title = "Reporte listado de ventas";
        package.Workbook.Properties.Author = "Mercadito";

        var sheet = package.Workbook.Worksheets.Add("Listado de ventas");
        BuildSheet(sheet, sales, context);

        return package.GetAsByteArray();
    }

    private static void BuildSheet(ExcelWorksheet sheet, IReadOnlyList<SaleSummaryDto> sales, SalesListingExcelExportContext context)
    {
        sheet.View.ShowGridLines = false;
        sheet.Cells.Style.Font.Name = "Arial";
        sheet.Cells.Style.Font.Size = 11;

        sheet.Cells["A1:H1"].Merge = true;
        sheet.Cells["A1"].Value = "MERCADITO";
        sheet.Cells["A1"].Style.Font.Bold = true;
        sheet.Cells["A1"].Style.Font.Size = 22;
        sheet.Cells["A1"].Style.Font.Color.SetColor(Ink);

        sheet.Cells["A2:H2"].Merge = true;
        sheet.Cells["A2"].Value = "REPORTE DE VENTAS";
        sheet.Cells["A2"].Style.Font.Bold = true;
        sheet.Cells["A2"].Style.Font.Size = 14;
        sheet.Cells["A2"].Style.Font.Color.SetColor(Ink);

        sheet.Cells["A4"].Value = "Desde";
        sheet.Cells["B4"].Value = context.FromDate?.ToString("dd/MM/yyyy") ?? "Todos";
        sheet.Cells["D4"].Value = "Hasta";
        sheet.Cells["E4"].Value = context.ToDate?.ToString("dd/MM/yyyy") ?? "Todos";
        sheet.Cells["G4"].Value = "Generado";
        sheet.Cells["H4"].Value = context.GeneratedAt.ToString("dd/MM/yyyy HH:mm");

        sheet.Cells["A5"].Value = "Estado";
        sheet.Cells["B5"].Value = string.IsNullOrWhiteSpace(context.Status) ? "Todos" : context.Status;
        sheet.Cells["D5"].Value = "Metodo";
        sheet.Cells["E5"].Value = string.IsNullOrWhiteSpace(context.PaymentMethod) ? "Todos" : context.PaymentMethod;

        var tableStartRow = 7;
        sheet.Cells[tableStartRow, 1].LoadFromArrays(new[]
        {
            new object[] { "Codigo", "Fecha/Hora", "Cliente", "Canal", "Metodo", "Total Bs.", "Estado" }
        });
        StyleHeader(sheet.Cells[tableStartRow, 1, tableStartRow, 7]);

        var row = tableStartRow + 1;
        foreach (var sale in sales)
        {
            sheet.Cells[row, 1].Value = sale.Code;
            sheet.Cells[row, 2].Value = sale.CreatedAt;
            sheet.Cells[row, 2].Style.Numberformat.Format = "dd/mm/yyyy hh:mm";
            sheet.Cells[row, 3].Value = sale.CustomerName;
            sheet.Cells[row, 4].Value = sale.Channel;
            sheet.Cells[row, 5].Value = sale.PaymentMethod;
            sheet.Cells[row, 6].Value = sale.Total;
            sheet.Cells[row, 6].Style.Numberformat.Format = "#,##0.00";
            sheet.Cells[row, 7].Value = sale.Status;
            row++;
        }

        if (row == tableStartRow + 1)
        {
            sheet.Cells[row, 1].Value = "Sin ventas para los filtros seleccionados.";
            row++;
        }

        StyleTable(sheet.Cells[tableStartRow, 1, row - 1, 7]);

        var footerRow = row + 2;
        sheet.Cells[footerRow, 1].Value = "Registros";
        sheet.Cells[footerRow, 2].Value = sales.Count;
        sheet.Cells[footerRow, 4].Value = "Total listado Bs.";
        sheet.Cells[footerRow, 5].Value = sales.Sum(sale => sale.Total);
        sheet.Cells[footerRow, 5].Style.Numberformat.Format = "#,##0.00";
        StyleSummary(sheet.Cells[footerRow, 1, footerRow, 5]);

        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        sheet.Column(1).Width = Math.Max(sheet.Column(1).Width, 16);
        sheet.Column(3).Width = Math.Max(sheet.Column(3).Width, 28);
        sheet.Column(6).Width = Math.Max(sheet.Column(6).Width, 14);
    }

    private static void StyleHeader(ExcelRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
        range.Style.Fill.BackgroundColor.SetColor(SurfaceAlt);
        range.Style.Border.BorderAround(ExcelBorderStyle.Medium, Ink);
    }

    private static void StyleTable(ExcelRange range)
    {
        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
        range.Style.Border.Top.Color.SetColor(Ink);
        range.Style.Border.Bottom.Color.SetColor(Ink);
        range.Style.Border.Left.Color.SetColor(Ink);
        range.Style.Border.Right.Color.SetColor(Ink);

        var table = range.Worksheet.Tables.Add(range, $"Ventas{range.Start.Row}{range.Start.Column}");
        table.TableStyle = TableStyles.Light1;
        table.ShowFilter = true;
    }

    private static void StyleSummary(ExcelRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
        range.Style.Fill.BackgroundColor.SetColor(Accent);
        range.Style.Border.BorderAround(ExcelBorderStyle.Medium, Ink);
    }
}
