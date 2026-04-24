using System.Net;

namespace Lakerfield.PingSweep.Models;

public class NetworkInterfaceInfo
{
  public string Name { get; init; } = string.Empty;
  public IPAddress IpAddress { get; init; } = IPAddress.None;
  public IPAddress SubnetMask { get; init; } = IPAddress.None;
  public string Cidr { get; init; } = string.Empty;

  public override string ToString() => $"{Name} - {Cidr}";
}
