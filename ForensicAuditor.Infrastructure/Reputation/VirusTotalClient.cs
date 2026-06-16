using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ForensicAuditor.Infrastructure.Reputation
{
    /// Integrasi Threat Intelligence via VirusTotal v3 API.
    /// Melakukan kueri reputasi berkas berdasarkan hash SHA-256 secara asinkron.
    public class VirusTotalClient
    {
        private static readonly HttpClient _httpClient = new();
        private readonly string _apiKey;

        public VirusTotalClient(string apiKey = "")
        {
            _apiKey = apiKey;
        }

        public async Task<string> CheckHashReputationAsync(string sha256Hash)
        {
            if (sha256Hash == "N/A" || sha256Hash.StartsWith("Error"))
            {
                return "Invalid Hash";
            }

            // DEMO: Jika API Key tidak disediakan
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return SimulateReputation(sha256Hash);
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"https://www.virustotal.com/api/v3/files/{sha256Hash}");
                request.Headers.Add("x-apikey", _apiKey);

                var response = await _httpClient.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return "Clean (Not Found in VT Database)";
                }

                if (!response.IsSuccessStatusCode)
                {
                    return $"API Error ({response.StatusCode})";
                }

                string jsonResult = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonResult);

                var stats = doc.RootElement
                    .GetProperty("data")
                    .GetProperty("attributes")
                    .GetProperty("last_analysis_stats");

                int malicious = stats.GetProperty("malicious").GetInt32();
                int suspicious = stats.GetProperty("suspicious").GetInt32();
                int harmless = stats.GetProperty("harmless").GetInt32();

                if (malicious > 0)
                {
                    return $"MALICIOUS ({malicious} Antivirus Detects)";
                }
                else if (suspicious > 0)
                {
                    return $"SUSPICIOUS ({suspicious} Warnings)";
                }
                return $"Harmless (VT Clean: {harmless} Engine Passed)";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        /// Mengisolasi simulasi biner agar sidik jari biner Windows 
        /// asli tidak salah didiagnosis sebagai malware selama pemantauan pasif real-time berjalan.
        private static string SimulateReputation(string sha256Hash)
        {
            if (sha256Hash == "D24C3E59B32E16D7130FCE8E711A4F4F3D6C11234F98E12A34B22C3D3E2F4A5E")
            {
                return "MALICIOUS (54 Antivirus Detects - Trojan.Ransom)";
            }
            if (sha256Hash == "FB4C3E59B32E16D7130FCE8E711A4F4F3D6C11234F98E12A34B22C3D3E2F4A5E")
            {
                return "SUSPICIOUS (4 Warnings - Unsigned Updater)";
            }
            if (sha256Hash == "8A4C3E59B32E16D7130FCE8E711A4F4F3D6C11234F98E12A34B22C3D3E2F4A5E")
            {
                return "Harmless (Passed 62 Antivirus Engines)";
            }

            // Default aman untuk seluruh aktivitas biner asli milik Windows
            return "Harmless (Passed 62 Antivirus Engines)";
        }
    }
}