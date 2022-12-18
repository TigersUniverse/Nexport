using System;
using Nexport.Transports;
using Nexport.Transports.kcp2k;
using Nexport.Transports.Telepathy;

namespace Nexport
{
    public class Instantiator
    {
        public static Server InstantiateServer(TransportType transportType, ServerSettings settings)
        {
            switch (transportType)
            {
                case TransportType.KCP:
                    return new KCPServer(settings);
                case TransportType.Telepathy:
                    return new TelepathyServer(settings);
            }
            throw new Exception("Unknown Server");
        }

        public static Client InstantiateClient(TransportType transportType, ClientSettings settings)
        {
            switch (transportType)
            {
                case TransportType.KCP:
                    return new KCPClient(settings);
                case TransportType.Telepathy:
                    return new TelepathyClient(settings);
            }
            throw new Exception("Unknown Server");
        }

        public static TransportType GetTransportTypeFromString(string transportString)
        {
            if (transportString.ToLower().Contains("kcp"))
                return TransportType.KCP;
            if (transportString.ToLower().Contains("telepathy"))
                return TransportType.Telepathy;
            throw new Exception("Unknown TransportType " + transportString);
        }
    }
}