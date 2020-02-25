using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Rocket.Libraries.MicroServiceChannels
{
    public class MicroServiceChannel : IMicroServiceChannel
    {
        private readonly IMicroServicesRegistryReader microServiceRegistryReader;

        private readonly IHttpClientFactory httpClientFactory;

        public MicroServiceChannel(
            IMicroServicesRegistryReader microServiceRegistryReader,
            IHttpClientFactory httpClientFactory)
        {
            this.microServiceRegistryReader = microServiceRegistryReader;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<TResponse> CallAsync<TResponse>(string microService, string relativePath, Dictionary<string, string> headers, HttpMethod method, object payload)
        {
            using (var httpClient = httpClientFactory.CreateClient())
            {
                var absoluteUri = await GetAbsoluteUri(microService, relativePath);
                using (var requestMessage = new HttpRequestMessage(method, absoluteUri))
                {
                    InjectPayloadIfRequired(requestMessage, method, payload);
                    InjectHeadersIfRequired(requestMessage, headers);

                    using (var response = await httpClient.SendAsync(requestMessage))
                    {
                        var responseText = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<TResponse>(responseText);
                    }
                }
            }
        }

        private void InjectPayloadIfRequired(HttpRequestMessage requestMessage, HttpMethod method, object payload)
        {
            if (method == HttpMethod.Post || method == HttpMethod.Put)
            {
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            }
        }

        private void InjectHeadersIfRequired(HttpRequestMessage requestMessage, Dictionary<string, string> headers)
        {
            if (headers != default)
            {
                foreach (var singleHeader in headers)
                {
                    requestMessage.Headers.Add(singleHeader.Key, singleHeader.Value);
                }
            }
        }

        private async Task<Uri> GetAbsoluteUri(string microService, string relativePath)
        {
            var baseUrl = await microServiceRegistryReader.GetServiceBaseAddressAsync(microService);
            var urlBaseNotFound = string.IsNullOrEmpty(baseUrl);
            if (urlBaseNotFound)
            {
                throw new Exception($"Could not find base url for microservice '{microService}'");
            }
            return new Uri($"{baseUrl}{relativePath}", UriKind.Absolute);
        }
    }
}