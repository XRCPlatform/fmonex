using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FreeMarketOne.Extensions.Helpers
{
    public static class EndPointHelper
    {
        public static IPEndPoint ParseIPEndPoint(string text)
        {
            Uri uri;
            if (Uri.TryCreate(text, UriKind.Absolute, out uri))
                return new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);
            if (Uri.TryCreate(String.Concat("tcp://", text), UriKind.Absolute, out uri))
                return new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);
            if (Uri.TryCreate(String.Concat("tcp://", String.Concat("[", text, "]")), UriKind.Absolute, out uri))
                return new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);
            throw new FormatException("Failed to parse text to IPEndPoint");
        }

        public static EndPoint ParseEndPoint(string text)
        {
            Uri uri;
            if (Uri.TryCreate(text, UriKind.Absolute, out uri))
                return new DnsEndPoint(uri.Host, uri.Port < 0 ? 0 : uri.Port);
            if (Uri.TryCreate(String.Concat("tcp://", text), UriKind.Absolute, out uri))
                return new DnsEndPoint(uri.Host, uri.Port < 0 ? 0 : uri.Port);
            if (Uri.TryCreate(String.Concat("tcp://", String.Concat("[", text, "]")), UriKind.Absolute, out uri))
                return new DnsEndPoint(uri.Host, uri.Port < 0 ? 0 : uri.Port);
            throw new FormatException("Failed to parse text to DnsEndPoint");
        }
    }
}
