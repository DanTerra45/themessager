using System.Drawing;
using Mercadito.Frontend.Dtos.Sales;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;

namespace Mercadito.Frontend.Pages.Sales;

public sealed class DailyCashClosingExcelExporter : IDailyCashClosingExcelExporter
{
    private static readonly Color Ink = Color.FromArgb(13, 31, 21);
    private static readonly Color SurfaceAlt = Color.FromArgb(233, 232, 226);
    private static readonly Color Accent = Color.FromArgb(0, 229, 255);
    private static readonly Color Success = Color.FromArgb(0, 230, 118);

    public byte[] Export(DailyCashClosingReportDto report)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Mercadito");

        using var package = new ExcelPackage();
        package.Workbook.Properties.Title = $"Cierre de caja {report.BusinessDate:yyyy-MM-dd}";
        package.Workbook.Properties.Author = "Mercadito";

        var sheet = package.Workbook.Worksheets.Add("Cierre diario");
        BuildSummarySheet(sheet, report);

        return package.GetAsByteArray();
    }

    private static void BuildSummarySheet(ExcelWorksheet sheet, DailyCashClosingReportDto report)
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
        sheet.Cells["A2"].Value = "CIERRE DIARIO DE CAJA";
        sheet.Cells["A2"].Style.Font.Bold = true;
        sheet.Cells["A2"].Style.Font.Size = 14;
        sheet.Cells["A2"].Style.Font.Color.SetColor(Ink);

        sheet.Cells["A4"].Value = "Fecha";
        sheet.Cells["B4"].Value = report.BusinessDate.ToString("dd/MM/yyyy");
        sheet.Cells["D4"].Value = "Generado";
        sheet.Cells["E4"].Value = report.GeneratedAt.ToString("dd/MM/yyyy HH:mm");

        var metricsStartRow = 6;
        WriteMetric(sheet, metricsStartRow, 1, "Ventas registradas", report.RegisteredSalesCount);
        WriteMetric(sheet, metricsStartRow, 3, "Ventas anuladas", report.CancelledSalesCount);
        WriteMetric(sheet, metricsStartRow, 5, "Monto registrado", report.RegisteredAmountTotal);
        WriteMetric(sheet, metricsStartRow, 7, "Ticket promedio", report.AverageTicket);

        var paymentStartRow = 10;
        sheet.Cells[paymentStartRow, 1].Value = "Resumen por método de pago";
        sheet.Cells[paymentStartRow, 1, paymentStartRow, 3].Merge = true;
        StyleSectionTitle(sheet.Cells[paymentStartRow, 1, paymentStartRow, 3]);
        sheet.Cells[paymentStartRow + 1, 1].LoadFromArrays(new[]
        {
            new object[] { "Método", "Ventas", "Total Bs." }
        });
        StyleHeader(sheet.Cells[paymentStartRow + 1, 1, paymentStartRow + 1, 3]);

        var paymentRow = paymentStartRow + 2;
        foreach (var paymentMethod in report.PaymentMethods)
        {
            sheet.Cells[paymentRow, 1].Value = paymentMethod.PaymentMethod;
            sheet.Cells[paymentRow, 2].Value = paymentMethod.SalesCount;
            sheet.Cells[paymentRow, 3].Value = paymentMethod.TotalAmount;
            sheet.Cells[paymentRow, 3].Style.Numberformat.Format = "#,##0.00";
            paymentRow++;
        }

        if (paymentRow == paymentStartRow + 2)
        {
            sheet.Cells[paymentRow, 1].Value = "Sin ventas registradas para la fecha seleccionada.";
            paymentRow++;
        }

        StyleTable(sheet.Cells[paymentStartRow + 1, 1, paymentRow - 1, 3]);

        var productStartRow = paymentRow + 2;
        sheet.Cells[productStartRow, 1].Value = "Productos con mayor ingreso";
        sheet.Cells[productStartRow, 1, productStartRow, 5].Merge = true;
        StyleSectionTitle(sheet.Cells[productStartRow, 1, productStartRow, 5]);
        sheet.Cells[productStartRow + 1, 1].LoadFromArrays(new[]
        {
            new object[] { "Producto", "Cantidad", "Precio promedio", "Total Bs.", "Participación" }
        });
        StyleHeader(sheet.Cells[productStartRow + 1, 1, productStartRow + 1, 5]);

        var productRow = productStartRow + 2;
        foreach (var product in report.Products)
        {
            sheet.Cells[productRow, 1].Value = product.ProductName;
            sheet.Cells[productRow, 2].Value = product.QuantitySold;
            sheet.Cells[productRow, 3].Value = product.AverageUnitPrice;
            sheet.Cells[productRow, 4].Value = product.TotalAmount;
            sheet.Cells[productRow, 5].Formula = report.RegisteredAmountTotal > 0
                ? $"D{productRow}/{report.RegisteredAmountTotal.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                : "0";
            sheet.Cells[productRow, 3, productRow, 4].Style.Numberformat.Format = "#,##0.00";
            sheet.Cells[productRow, 5].Style.Numberformat.Format = "0.00%";
            productRow++;
        }

        if (productRow == productStartRow + 2)
        {
            sheet.Cells[productRow, 1].Value = "Sin productos vendidos para la fecha seleccionada.";
            productRow++;
        }

        StyleTable(sheet.Cells[productStartRow + 1, 1, productRow - 1, 5]);

        if (report.Products.Count > 0)
        {
            AddProductsChart(sheet, productStartRow + 2, productRow - 1);
        }

        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        sheet.Column(1).Width = Math.Max(sheet.Column(1).Width, 28);
        sheet.Column(4).Width = Math.Max(sheet.Column(4).Width, 14);
        sheet.Column(5).Width = Math.Max(sheet.Column(5).Width, 14);
    }

    private static void WriteMetric(ExcelWorksheet sheet, int row, int column, string label, decimal value)
    {
        sheet.Cells[row, column, row, column + 1].Merge = true;
        sheet.Cells[row, column].Value = label;
        sheet.Cells[row + 1, column, row + 1, column + 1].Merge = true;
        sheet.Cells[row + 1, column].Value = value;
        sheet.Cells[row + 1, column].Style.Numberformat.Format = "#,##0.00";
        StyleMetric(sheet.Cells[row, column, row + 1, column + 1]);
    }

    private static void WriteMetric(ExcelWorksheet sheet, int row, int column, string label, int value)
    {
        sheet.Cells[row, column, row, column + 1].Merge = true;
        sheet.Cells[row, column].Value = label;
        sheet.Cells[row + 1, column, row + 1, column + 1].Merge = true;
        sheet.Cells[row + 1, column].Value = value;
        StyleMetric(sheet.Cells[row, column, row + 1, column + 1]);
    }

    private static void AddProductsChart(ExcelWorksheet sheet, int firstDataRow, int lastDataRow)
    {
        var chart = sheet.Drawings.AddChart("IngresosPorProducto", eChartType.Pie);
        chart.Title.Text = "Ingresos por producto";
        chart.SetPosition(9, 0, 6, 0);
        chart.SetSize(520, 320);
        chart.Series.Add(sheet.Cells[firstDataRow, 4, lastDataRow, 4], sheet.Cells[firstDataRow, 1, lastDataRow, 1]);
    }

    private static void StyleMetric(ExcelRange range)
    {
        range.Style.Border.BorderAround(ExcelBorderStyle.Medium, Ink);
        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
        range.Style.Fill.BackgroundColor.SetColor(SurfaceAlt);
        range.Style.Font.Color.SetColor(Ink);
        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        range.Style.Font.Bold = true;
    }

    private static void StyleSectionTitle(ExcelRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Font.Size = 13;
        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
        range.Style.Fill.BackgroundColor.SetColor(Accent);
        range.Style.Border.BorderAround(ExcelBorderStyle.Medium, Ink);
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

        var table = range.Worksheet.Tables.Add(range, $"Tabla{range.Start.Row}{range.Start.Column}");
        table.TableStyle = TableStyles.Light1;
        table.ShowFilter = true;

        if (range.End.Row > range.Start.Row)
        {
            range.Worksheet.Cells[range.Start.Row + 1, range.Start.Column, range.End.Row, range.End.Column]
                .Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Worksheet.Cells[range.Start.Row + 1, range.Start.Column, range.End.Row, range.End.Column]
                .Style.Fill.BackgroundColor.SetColor(Color.White);
        }
    }
}
