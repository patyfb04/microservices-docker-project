using System.Collections.Generic;

namespace Play.Common.Settings
{
    public class ClientServiceSettings
    {
        public string ServiceName { get; init; }
        public string ServiceUrl { get; init; }

    }

    public class ClientServicesSettings
    {
        public List<ClientServiceSettings> ClientServices { get; init; }

    }
}
