using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TennisPhone
{
    /// <summary>
    /// Trouve les adresses IPv4 du PC sur le réseau local.
    /// </summary>
    public static class LanAddress
    {
        /// <summary>
        /// Adresses IPv4 utilisables par un téléphone du même réseau.
        /// Les interfaces qui ont une passerelle sont placées en premier.
        /// </summary>
        public static List<string> GetLanIPv4()
        {
            var withGateway = new List<string>();
            var withoutGateway = new List<string>();

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up
                    || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback
                    || nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                IPInterfaceProperties properties = nic.GetIPProperties();
                bool hasGateway = false;
                foreach (GatewayIPAddressInformation gateway in properties.GatewayAddresses)
                {
                    if (gateway.Address.AddressFamily == AddressFamily.InterNetwork
                        && gateway.Address.ToString() != "0.0.0.0")
                    {
                        hasGateway = true;
                        break;
                    }
                }

                foreach (UnicastIPAddressInformation unicast in properties.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    string address = unicast.Address.ToString();
                    if (address.StartsWith("127.") || address.StartsWith("169.254."))
                    {
                        continue;
                    }

                    List<string> target = hasGateway ? withGateway : withoutGateway;
                    if (!target.Contains(address))
                    {
                        target.Add(address);
                    }
                }
            }

            withGateway.AddRange(withoutGateway);
            return withGateway;
        }
    }
}
