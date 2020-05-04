namespace FreeMarketOne.Changelly
{
    public class CurrencyFullResponse
    {
        public string jsonrpc { get; set; }
        public int id { get; set; }
        public CurrencyFull[] result { get; set; }
    }    

}
