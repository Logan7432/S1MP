using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamGameServerMod.Client.Models;
using SteamGameServerMod.Logging;

namespace SteamGameServerMod.Client
{
    public class ServerFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverUrl;

        public ServerFetcher(string baseUrl)
        {
            _httpClient = new HttpClient();
            _serverUrl = baseUrl + "/api/servers";
        }

        public async Task<List<ServerData>> GetServers()
        {
            try
            {
                Log.LogInfo($"Fetching servers from: {_serverUrl}");

                var response = await _httpClient.GetAsync(_serverUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var servers = JsonConvert.DeserializeObject<List<ServerData>>(content);

                Log.LogInfo($"Retrieved {servers.Count} servers");
                return servers;
            }
            catch (Exception ex)
            {
                Log.LogError($"Error fetching servers: {ex.Message}");
                return new List<ServerData>();
            }
        }
    }
}