using System.Net;

namespace Lakerfield.PingSweep.Models;

public class CidrRange
{
  public IPAddress NetworkAddress { get; }
  public int PrefixLength { get; }
  public int HostCount { get; }
  public IPAddress FirstHost { get; }
  public IPAddress LastHost { get; }

  private readonly uint _networkUint;
  private readonly uint _broadcastUint;

  private CidrRange(IPAddress networkAddress, int prefixLength)
  {
    NetworkAddress = networkAddress;
    PrefixLength = prefixLength;

    var bytes = networkAddress.GetAddressBytes();
    _networkUint = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];

    uint mask = prefixLength == 0 ? 0u : (0xFFFFFFFFu << (32 - prefixLength));
    _broadcastUint = _networkUint | ~mask;

    if (prefixLength >= 31)
    {
      HostCount = (int)(_broadcastUint - _networkUint + 1);
      FirstHost = UintToIp(_networkUint);
      LastHost = UintToIp(_broadcastUint);
    }
    else
    {
      HostCount = (int)(_broadcastUint - _networkUint - 1);
      FirstHost = UintToIp(_networkUint + 1);
      LastHost = UintToIp(_broadcastUint - 1);
    }
  }

  public static CidrRange Parse(string cidr)
  {
    if (!TryParse(cidr, out var result))
      throw new ArgumentException($"Invalid CIDR notation: {cidr}");
    return result!;
  }

  public static bool TryParse(string cidr, out CidrRange? result)
  {
    result = null;
    if (string.IsNullOrWhiteSpace(cidr)) return false;

    var parts = cidr.Split('/');
    if (parts.Length != 2) return false;

    if (!IPAddress.TryParse(parts[0], out var ip)) return false;
    if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
    if (!int.TryParse(parts[1], out var prefix)) return false;
    if (prefix < 0 || prefix > 32) return false;

    // Calculate network address by masking
    var ipBytes = ip.GetAddressBytes();
    uint ipUint = ((uint)ipBytes[0] << 24) | ((uint)ipBytes[1] << 16) | ((uint)ipBytes[2] << 8) | ipBytes[3];
    uint mask = prefix == 0 ? 0u : (0xFFFFFFFFu << (32 - prefix));
    uint networkUint = ipUint & mask;

    var networkBytes = new byte[]
    {
      (byte)(networkUint >> 24),
      (byte)(networkUint >> 16),
      (byte)(networkUint >> 8),
      (byte)(networkUint)
    };

    result = new CidrRange(new IPAddress(networkBytes), prefix);
    return true;
  }

  public static CidrRange FromInterface(NetworkInterfaceInfo iface)
  {
    var maskBytes = iface.SubnetMask.GetAddressBytes();
    uint maskUint = ((uint)maskBytes[0] << 24) | ((uint)maskBytes[1] << 16) | ((uint)maskBytes[2] << 8) | maskBytes[3];

    int prefix = 0;
    uint m = maskUint;
    while (m != 0)
    {
      prefix += (int)(m >> 31);
      m <<= 1;
    }

    return Parse($"{iface.IpAddress}/{prefix}");
  }

  public IEnumerable<IPAddress> GetAllHosts()
  {
    if (PrefixLength >= 31)
    {
      // /31 and /32: return all addresses
      for (uint i = _networkUint; i <= _broadcastUint; i++)
      {
        yield return UintToIp(i);
      }
    }
    else
    {
      // Skip network and broadcast
      for (uint i = _networkUint + 1; i < _broadcastUint; i++)
      {
        yield return UintToIp(i);
      }
    }
  }

  private static IPAddress UintToIp(uint value)
  {
    return new IPAddress(new byte[]
    {
      (byte)(value >> 24),
      (byte)(value >> 16),
      (byte)(value >> 8),
      (byte)(value)
    });
  }

  public override string ToString() => $"{NetworkAddress}/{PrefixLength}";
}
