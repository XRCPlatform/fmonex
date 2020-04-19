using FreeMarketOne.DataStructure.Price;

namespace FreeMarketOne.Changelly
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