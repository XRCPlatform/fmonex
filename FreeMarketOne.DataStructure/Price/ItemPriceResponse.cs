namespace FreeMarketOne.DataStructure.Price
{
    public class ItemPriceResponse : IItemPriceResponse
    {
        public decimal MinAmount { get; set; }
        public Currency Currency { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal Rate { get; set; }
    }
}