using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Uptime.Services
{
    public class StatusService
    {
        private readonly HttpClient _http;

        private readonly IList<ServiceMetadata> _hosts;
        private readonly IList<ServiceStatus> _statuses;

        public StatusService(HttpClient http)
        {
            _http = http;

            _hosts = new List<ServiceMetadata>();
            _statuses = new List<ServiceStatus>();

            UpdateStatuses().Start();
        }

        /// <summary>
        /// Get the service status list.
        /// </summary>
        public IList<ServiceStatus> GetStatuses() => _statuses;

        /// <summary>
        /// Add a service to the service list.
        /// </summary>
        public void AddService(ServiceMetadata meta) => _hosts.Add(meta);

        /// <summary>
        /// Requests status information from the services in the configuration file.
        /// </summary>
        private async Task UpdateStatuses()
        {
            while (true)
            {
                foreach (var host in _hosts)
                {
                    string serviceName = host.ServiceName;

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
                        res = JObject.Parse(await _http.GetStringAsync(new Uri(host.Hostname)));
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
                await Task.Delay(180000);
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

    public struct ServiceMetadata
    {
        public string ServiceName;
        public string Hostname;
    }
}
