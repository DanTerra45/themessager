using Mercadito.src.application.sales.models;

namespace Mercadito.src.application.sales.facades
{
    internal static class SaleAuditSnapshotFactory
    {
        public static object BuildCreatedSnapshot(SaleReceiptDto receipt, RegisterSaleDto request)
        {
            ArgumentNullException.ThrowIfNull(receipt);
            ArgumentNullException.ThrowIfNull(request);

            return new Dictionary<string, object?>
            {
                ["CodigoVenta"] = receipt.Code,
                ["Cliente"] = BuildCustomerLabel(receipt.CustomerDocumentNumber, receipt.CustomerName),
                ["MetodoPago"] = request.PaymentMethod,
                ["Canal"] = request.Channel,
                ["CantidadLineas"] = request.Lines.Count,
                ["Total"] = receipt.Total
            };
        }

        public static (object PreviousData, object NewData)? BuildUpdatedSnapshot(
            SaleDetailDto previousSale,
            SaleDetailDto updatedSale)
        {
            ArgumentNullException.ThrowIfNull(previousSale);
            ArgumentNullException.ThrowIfNull(updatedSale);

            var previousData = new Dictionary<string, object?>();
            var newData = new Dictionary<string, object?>();

            AddChange(
                BuildCustomerLabel(previousSale.CustomerDocumentNumber, previousSale.CustomerName),
                BuildCustomerLabel(updatedSale.CustomerDocumentNumber, updatedSale.CustomerName),
                "Cliente",
                previousData,
                newData);

            AddChange(previousSale.PaymentMethod, updatedSale.PaymentMethod, "MetodoPago", previousData, newData);
            AddChange(previousSale.Channel, updatedSale.Channel, "Canal", previousData, newData);
            AddChange(previousSale.Lines.Count, updatedSale.Lines.Count, "CantidadLineas", previousData, newData);
            AddChange(previousSale.Total, updatedSale.Total, "Total", previousData, newData);

            var previousLines = BuildLineItems(previousSale.Lines);
            var updatedLines = BuildLineItems(updatedSale.Lines);
            if (!previousLines.SequenceEqual(updatedLines, StringComparer.Ordinal))
            {
                previousData["Lineas"] = previousLines;
                newData["Lineas"] = updatedLines;
            }

            if (previousData.Count == 0)
            {
                return null;
            }

            return (previousData, newData);
        }

        public static (object PreviousData, object NewData) BuildCancelledSnapshot(
            SaleDetailDto previousSale,
            SaleDetailDto? updatedSale,
            string reason)
        {
            ArgumentNullException.ThrowIfNull(previousSale);

            var previousData = new Dictionary<string, object?>
            {
                ["Estado"] = previousSale.Status,
                ["MotivoAnulacion"] = NormalizeEmptyToNull(previousSale.CancellationReason),
                ["FechaAnulacion"] = previousSale.CancelledAt
            };

            var newData = new Dictionary<string, object?>
            {
                ["Estado"] = updatedSale?.Status ?? "Anulada",
                ["MotivoAnulacion"] = string.IsNullOrWhiteSpace(reason)
                    ? NormalizeEmptyToNull(updatedSale?.CancellationReason)
                    : reason,
                ["FechaAnulacion"] = updatedSale?.CancelledAt ?? DateTime.UtcNow
            };

            return (previousData, newData);
        }

        private static string BuildCustomerLabel(string documentNumber, string customerName)
        {
            return string.Concat(documentNumber, " - ", customerName);
        }

        private static string? NormalizeEmptyToNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value;
        }

        private static void AddChange<T>(
            T previousValue,
            T currentValue,
            string fieldName,
            Dictionary<string, object?> previousData,
            Dictionary<string, object?> newData)
        {
            if (EqualityComparer<T>.Default.Equals(previousValue, currentValue))
            {
                return;
            }

            previousData[fieldName] = previousValue;
            newData[fieldName] = currentValue;
        }

        private static string[] BuildLineItems(IReadOnlyList<SaleDetailLineDto> lines)
        {
            ArgumentNullException.ThrowIfNull(lines);

            return lines
                .Select(line => string.Concat(
                    line.ProductId.ToString(),
                    ":",
                    line.ProductName,
                    "|",
                    line.Quantity.ToString(),
                    "|",
                    line.UnitPrice.ToString("0.00"),
                    "|",
                    line.Amount.ToString("0.00")))
                .ToArray();
        }
    }
}
