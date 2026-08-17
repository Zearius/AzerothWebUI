using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace AzerothWebUI.Core.Soap;

public class SoapCommandException(string message) : Exception(message);

/// <summary>
/// Minimal client for AzerothCore's SOAP command API (worldserver, SOAP.Enabled).
/// Sends the same commands the in-game/console GM interface accepts, over a raw
/// XML-over-HTTP envelope with HTTP Basic auth — no SOAP library needed for this.
/// </summary>
public class SoapClient(HttpClient httpClient, string soapUrl, string soapUsername, string soapPassword)
{
    private const string Namespace = "urn:AC";

    public async Task<string> ExecuteCommandAsync(string command)
    {
        var envelope = new XElement(XName.Get("Envelope", "http://schemas.xmlsoap.org/soap/envelope/"),
            new XElement(XName.Get("Body", "http://schemas.xmlsoap.org/soap/envelope/"),
                new XElement(XName.Get("executeCommand", Namespace),
                    new XElement(XName.Get("command", Namespace), command))));

        using var request = new HttpRequestMessage(HttpMethod.Post, soapUrl)
        {
            Content = new StringContent(envelope.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "text/xml"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{soapUsername}:{soapPassword}")));

        using var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new SoapCommandException(ExtractFaultString(responseBody) ?? $"SOAP request failed with status {(int)response.StatusCode}.");
        }

        return ExtractResult(responseBody);
    }

    private static string ExtractResult(string responseBody)
    {
        var doc = XDocument.Parse(responseBody);
        var result = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "result");
        return result?.Value.Trim() ?? string.Empty;
    }

    private static string? ExtractFaultString(string responseBody)
    {
        try
        {
            var doc = XDocument.Parse(responseBody);
            return doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value;
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }
    }
}
