using Application.Options;
using Application.UseCases;
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

        public SaleController(GetAllSalesUseCase getAllSalesUseCase)
        {
            _getAllSalesUseCase = getAllSalesUseCase;
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
    }
}