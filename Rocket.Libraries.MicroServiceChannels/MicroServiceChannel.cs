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
        private readonly IHttpClientFactory httpClientFactory;

        private readonly IMicroServicesRegistryReader microServiceRegistryReader;

        public MicroServiceChannel(
            IMicroServicesRegistryReader microServiceRegistryReader,
            IHttpClientFactory httpClientFactory)
        {
            this.microServiceRegistryReader = microServiceRegistryReader;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<TResponse> CallAsync<TResponse>(Uri absoluteUri, Dictionary<string, string> headers, HttpMethod method, object payload, Action<string> onResponseReceived = null)
        {
            using (var httpClient = httpClientFactory.CreateClient())
            {
                using (var requestMessage = new HttpRequestMessage(method, absoluteUri))
                {
                    InjectPayloadIfRequired(requestMessage, method, payload);
                    InjectHeadersIfRequired(requestMessage, headers);

                    using (var response = await httpClient.SendAsync(requestMessage))
                    {
                        var responseText = await response.Content.ReadAsStringAsync();
                        LogResponseText(onResponseReceived, responseText);
                        return JsonConvert.DeserializeObject<TResponse>(responseText);
                    }
                }
            }
        }

        public async Task<TResponse> CallAsync<TResponse>(string microService, string relativePath, Dictionary<string, string> headers, HttpMethod method, object payload, Action<string> onResponseReceived = null)
        {
            using (var httpClient = httpClientFactory.CreateClient())
            {
                var absoluteUri = await GetAbsoluteUri(microService, relativePath);
                return await CallAsync<TResponse>(absoluteUri, headers, method, payload, onResponseReceived);
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

        private void InjectPayloadIfRequired(HttpRequestMessage requestMessage, HttpMethod method, object payload)
        {
            if (method == HttpMethod.Post || method == HttpMethod.Put)
            {
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            }
        }

        private void LogResponseText(Action<string> onResponseReceived, string responseText)
        {
            try
            {
                if (onResponseReceived == null)
                {
                    return;
                }
                onResponseReceived(responseText);
            }
            catch
            {
                // Non critical. We can ignore errors from this callback.
            }
        }
    }
}