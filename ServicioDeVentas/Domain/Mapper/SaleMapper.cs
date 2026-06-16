using System.Data;
using Domain.Dto.Response;
using Domain.Entities;
using Domain.Entities.Enums;

namespace Domain.Mapper
{
    public static class SaleMapper
    {
        public static IEnumerable<SaleWithDetails> ToSalesWithDetails(this IDataReader reader)
        {
            var saleDictionary = new Dictionary<int, SaleWithDetails>();
            var detailsDictionary = new Dictionary<int, List<SaleDetails>>();

            while (reader.Read())
            {
                int saleId = Convert.ToInt32(reader["Id"]);
                if (!saleDictionary.TryGetValue(saleId, out var sale))
                {
                    var detailsList = new List<SaleDetails>();
                    
                    sale = new SaleWithDetails(
                        Id: saleId,
                        CustomerId: Convert.ToInt32(reader["CustomerId"]),
                        OperatorId: Convert.ToInt32(reader["OperatorId"]),
                        TotalPrice: Convert.ToDecimal(reader["TotalPrice"]),
                        CreatedAt: Convert.ToDateTime(reader["CreatedAt"]),
                        State: Enum.Parse<SaleState>(reader["State"].ToString()!), 
                        Details: detailsList
                    );

                    saleDictionary.Add(saleId, sale);
                    detailsDictionary.Add(saleId, detailsList);
                }
                if (reader["DetailId"] != DBNull.Value)
                {
                    var detail = new SaleDetails(
                        DetailId: Convert.ToInt32(reader["DetailId"]),
                        SaleId: Convert.ToInt32(reader["SaleId"]),
                        ProductId: Convert.ToInt32(reader["ProductId"]),
                        Quantity: Convert.ToInt32(reader["Quantity"]),
                        UnitPrice: Convert.ToDecimal(reader["UnitPrice"]),
                        SubTotal: reader["SubTotal"] != DBNull.Value ? Convert.ToDecimal(reader["SubTotal"]) : null
                    );

                    detailsDictionary[saleId].Add(detail);
                }
            }

            return saleDictionary.Values;
        }

        public static SaleDetailResponseDto ToResponseDto(this SaleDetails detail)
        {
            return new SaleDetailResponseDto(
                ProductId: detail.ProductId,
                Quantity: detail.Quantity,
                UnitPrice: detail.UnitPrice,
                SubTotal: detail.SubTotal?? 0
            );
        }

        public static SaleResponseDto ToResponseDto(this SaleWithDetails sale, CustomerResponseDto customer)
        {
            return new SaleResponseDto(
                SaleId: sale.Id,
                Customer: customer,
                CreatedAt: sale.CreatedAt,
                State: sale.State,
                Details: sale.Details.Select(d => d.ToResponseDto())
            );
        }
    }
}