namespace FreeMarketOne.DataStructure.Price
{
    public interface IItemPriceResponse
    {
        decimal MinAmount { get; set; }
        Currency Currency { get; set; }
        decimal Amount { get; set; }
        decimal Fee { get; set; }
        decimal Rate { get; set; }
    }
}