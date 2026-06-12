using Domain.Entities.Enums;

namespace Domain.Dto.Update{
  public record UpdateSale(
     int SaleId
      ){

  }
  public record UpdateSaleState(
      int SaleId,
      SaleState State
      ) : UpdateSale(SaleId:SaleId){
    
  }
}
