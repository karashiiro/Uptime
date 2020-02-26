using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Uptime.Services
{
    public class StatusService
    {
        private readonly HttpClient _http;
        private readonly IConfigurationRoot _config;

        private readonly IList<ServiceStatus> _statuses;

        public StatusService(HttpClient http)
        {
            _http = http;
            _config = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "services.json"), optional: false, reloadOnChange: true)
                .Build();

            _statuses = new List<ServiceStatus>();

            UpdateStatuses().Start();
        }

        /// <summary>
        /// Get the service status list.
        /// </summary>
        public IList<ServiceStatus> GetStatuses() => _statuses;

        /// <summary>
        /// Requests status information from the services in the configuration file.
        /// </summary>
        private async Task UpdateStatuses()
        {
            while (true)
            {
                foreach (var child in _config.GetChildren())
                {
                    string serviceName = child.GetSection("ServiceName").Value;

                    // Add the service to the List if it doesn't already exist.
                    if (!_statuses.Where(status => status.ServiceName == serviceName).Any())
                    {
                        _statuses.Add(new ServiceStatus
                        {
                            ServiceName = serviceName
                        });
                    }

                    // Get the service.
                    var service = _statuses.Single(status => status.ServiceName == serviceName);

                    // Request service information.
                    JObject res;
                    try
                    {
                        res = JObject.Parse(await _http.GetStringAsync(new Uri(child.GetSection("Hostname").Value)));
                    }
                    catch (HttpRequestException) // Service is unavailable.
                    {
                        service = new ServiceStatus
                        {
                            ServiceName = serviceName
                        };
                        continue;
                    }
                    var timeInitialized = res["timeInitialized"].Value<long>();
                    var flavorText = res["flavorText"].Value<string>();

                    // Set service information.
                    service.IsOnline = true;
                    service.TimeInitialized = timeInitialized;
                    service.FlavorText = flavorText;
                }
                await Task.Delay(60000);
            }
        }
    }

    public class ServiceStatus
    {
        public string ServiceName;
        public bool IsOnline = false;
        public long TimeInitialized = -1;
        public string FlavorText = string.Empty;
    }
}
