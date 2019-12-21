using System.Net.Http.Headers;

namespace FreeMarketOne.Extensions.Http.Models
{
	public class HttpResponseContentHeaders
	{
		public HttpResponseHeaders ResponseHeaders { get; set; }
		public HttpContentHeaders ContentHeaders { get; set; }
	}
}
