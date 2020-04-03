namespace FreeMarketOne.DataStructure.Price
{
    public class ItemPriceResponse : IItemPriceResponse
    {
        public double MinAmount { get; set; }
        public Currency Currency { get; set; }
        public double Amount { get; set; }
        public double Fee { get; set; }
        public double Rate { get; set; }
    }
}