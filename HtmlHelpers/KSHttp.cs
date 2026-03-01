using System;
using System.IO;
using System.Net;
using System.Text;

namespace KS.Foundation.HtmlHelpers
{    
    public static class KSHttp
    {
        // HttpClient sollte in .NET einmalig instanziiert und wiederverwendet werden!
        // Das verhindert das Erschöpfen von Sockets (Socket Exhaustion).
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler 
        { 
            AllowAutoRedirect = true, 
            MaxAutomaticRedirections = 10 
        }) 
        { 
            Timeout = TimeSpan.FromSeconds(15) 
        };

        public static async Task<string> GetHtmlPageAsync(string url, string userAgent = "Mozilla/5.0 (compatible; KS.Foundation/8.0)")
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd(userAgent);

                // SendAsync ist der moderne Standard
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                
                // Wirft eine Exception bei 4xx oder 5xx Fehlern
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                // Hier spezifisches Logging oder Re-Throw
                throw new Exception($"Fehler beim Abrufen der URL: {url}", ex);
            }
        }

        public static async Task<string> PostHtmlPageAsync(string url, string rparams, string userAgent = "KS.Foundation/8.0")
        {
            if (string.IsNullOrWhiteSpace(url)) 
                throw new ArgumentNullException(nameof(url));

            try
            {
                // Wir erstellen den Content aus dem String. 
                // Standardmäßig ist das bei altem Code meist "application/x-www-form-urlencoded".
                using var content = new StringContent(rparams ?? string.Empty, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };
                
                request.Headers.UserAgent.ParseAdd(userAgent);

                // _httpClient ist die statische Instanz aus dem vorherigen Schritt
                using var response = await _httpClient.SendAsync(request);
                
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                // Sauberer Re-throw ohne Stacktrace-Verlust
                throw new Exception($"POST-Fehler bei URL: {url}", ex);
            }
        }
    }
}
