using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Lakerfield.PingSweep.Models;

namespace Lakerfield.PingSweep.Services;

public static class NetworkInterfaceService
{
  public static List<NetworkInterfaceInfo> GetInterfaces()
  {
    var results = new List<NetworkInterfaceInfo>();

    foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
    {
      if (nic.OperationalStatus != OperationalStatus.Up) continue;
      if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
      if (nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

      var ipProps = nic.GetIPProperties();
      foreach (var unicast in ipProps.UnicastAddresses)
      {
        if (unicast.Address.AddressFamily != AddressFamily.InterNetwork) continue;

        var ip = unicast.Address;
        var mask = unicast.IPv4Mask;

        // Skip link-local addresses (169.254.x.x)
        var bytes = ip.GetAddressBytes();
        if (bytes[0] == 169 && bytes[1] == 254) continue;

        // Calculate prefix length for display
        var maskBytes = mask.GetAddressBytes();
        uint maskUint = ((uint)maskBytes[0] << 24) | ((uint)maskBytes[1] << 16) | ((uint)maskBytes[2] << 8) | maskBytes[3];
        int prefix = 0;
        uint m = maskUint;
        while (m != 0)
        {
          prefix += (int)(m >> 31);
          m <<= 1;
        }

        results.Add(new NetworkInterfaceInfo
        {
          Name = nic.Name,
          IpAddress = ip,
          SubnetMask = mask,
          Cidr = $"{ip}/{prefix}"
        });
      }
    }

    return results;
  }
}
