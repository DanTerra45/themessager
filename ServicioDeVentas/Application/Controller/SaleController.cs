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

        public SaleController(GetAllSalesUseCase getAllSalesUseCase, GetSaleByUseCase getSaleByUseCase)
        {
            _getAllSalesUseCase = getAllSalesUseCase;
            _getSaleByUseCase = getSaleByUseCase;
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
    }
}