using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace Rocket.Libraries.MicroServiceChannels
{
    public interface IMicroServicesRegistryReader
    {
        Task<ImmutableList<string>> GetAllServiceNamesAsync();

        Task<string> GetServiceBaseAddressAsync(string serviceId);
    }
}