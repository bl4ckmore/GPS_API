namespace ECommerceApp.Application.DTOs.WhatsGps
{
    public class ProxyDto
    {
        public string? Path { get; set; }                        // e.g. "position/queryHistory.do"
        public Dictionary<string, string?>? Params { get; set; } // query params (token is injected server-side)
    }
}
