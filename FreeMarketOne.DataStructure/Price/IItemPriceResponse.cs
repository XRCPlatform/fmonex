namespace FreeMarketOne.DataStructure.Price
{
    public interface IItemPriceResponse
    {
        double MinAmount { get; set; }
        Currency Currency { get; set; }
        double Amount { get; set; }
        double Fee { get; set; }
        double Rate { get; set; }
    }
}