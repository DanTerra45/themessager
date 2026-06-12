using Application.Options;
using Application.UseCases;
using Domain.Database;
using Domain.Database.Fields;
using Domain.Dto.Response;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Application.Controller
{
    [ApiController]
    [Route("api/sales")]
    public class SaleController : ControllerBase
    {
        private readonly GetAllSalesUseCase _getAllSalesUseCase;
        private readonly GetSaleByUseCase _getSaleByUseCase;
        private readonly GetSalesReportUseCase _getSalesReportUseCase;

        public SaleController(GetAllSalesUseCase getAllSalesUseCase, GetSaleByUseCase getSaleByUseCase, GetSalesReportUseCase getSalesReportUseCase)
        {
            _getAllSalesUseCase = getAllSalesUseCase;
            _getSaleByUseCase = getSaleByUseCase;
            _getSalesReportUseCase = getSalesReportUseCase;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? limit = null,
            [FromQuery] int? offset = null,
            [FromQuery] SaleFields? orderBy = null,
            [FromQuery] bool orderDescending = false)
        {
            var options = new SaleOptions
            {
                Limit = limit,
                OrderBy = orderBy ?? SaleFields.Id,
                Offset = offset,
                OrderDescending = orderDescending
            };

            var result = await _getAllSalesUseCase.ExecuteAsync(options);
            return result.IsSuccess ? Ok(result.Value) : Problem(result.Errors.FirstOrDefault()?.Message);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSale(
            [FromRoute] int id)
        {
            var options = new SaleOptions();
            options.AddFilter(SaleFields.Id, FilterOperator.Equals, id);

            var result = await _getSaleByUseCase.ExecuteAsync(options);
            return result.IsSuccess ? Ok(result.Value) : Problem(result.Errors.FirstOrDefault()?.Message);
        }

        [HttpGet("reports/summary")]
        public async Task<IActionResult> GetReport()
        {
            var result = await _getSalesReportUseCase.ExecuteAsync();
            return result.IsSuccess ? Ok(result.Value) : Problem(result.Errors.FirstOrDefault()?.Message);
        }
    }
}