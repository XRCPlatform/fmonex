namespace FreeMarketOne.Changelly
{
    public class ExchangeAmountResponse
    {
        public string jsonrpc { get; set; }
        public int id { get; set; }
        public ExchangeAmount[] result { get; set; }
    }    

}
