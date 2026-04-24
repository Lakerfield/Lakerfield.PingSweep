using System.Net;

namespace Lakerfield.PingSweep.Models;

public class IpRange
{
  public IPAddress FirstHost { get; }
  public IPAddress LastHost { get; }
  public int HostCount { get; }

  private readonly uint _firstUint;
  private readonly uint _lastUint;
  private readonly string _display;

  private IpRange(IPAddress first, IPAddress last, string display)
  {
    FirstHost = first;
    LastHost = last;
    _display = display;

    var fb = first.GetAddressBytes();
    _firstUint = ((uint)fb[0] << 24) | ((uint)fb[1] << 16) | ((uint)fb[2] << 8) | fb[3];
    var lb = last.GetAddressBytes();
    _lastUint = ((uint)lb[0] << 24) | ((uint)lb[1] << 16) | ((uint)lb[2] << 8) | lb[3];

    HostCount = (int)(_lastUint - _firstUint + 1);
  }

  /// <summary>
  /// Parses a CIDR range ("192.168.1.0/24") or a dash range ("192.168.1.1-100").
  /// </summary>
  public static IpRange Parse(string input)
  {
    if (!TryParse(input, out var result))
      throw new ArgumentException($"Invalid range: {input}");
    return result!;
  }

  public static bool TryParse(string input, out IpRange? result)
  {
    result = null;
    if (string.IsNullOrWhiteSpace(input)) return false;

    input = input.Trim();

    if (input.Contains('/'))
      return TryParseCidr(input, out result);

    if (input.Contains('-'))
      return TryParseDash(input, out result);

    return false;
  }

  private static bool TryParseCidr(string input, out IpRange? result)
  {
    result = null;
    if (!CidrRange.TryParse(input, out var cidr) || cidr is null) return false;
    result = new IpRange(cidr.FirstHost, cidr.LastHost, input);
    return true;
  }

  private static bool TryParseDash(string input, out IpRange? result)
  {
    result = null;

    // Format: a.b.c.d-e  (last octet range, e.g. 192.168.1.1-100)
    var dashIdx = input.LastIndexOf('-');
    var ipPart = input[..dashIdx];
    var endPart = input[(dashIdx + 1)..];

    if (!IPAddress.TryParse(ipPart, out var firstIp)) return false;
    if (firstIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
    if (!int.TryParse(endPart, out var endOctet)) return false;
    if (endOctet < 0 || endOctet > 255) return false;

    var firstBytes = firstIp.GetAddressBytes();
    if (endOctet < firstBytes[3]) return false;

    var lastIp = new IPAddress(new byte[] { firstBytes[0], firstBytes[1], firstBytes[2], (byte)endOctet });
    result = new IpRange(firstIp, lastIp, input);
    return true;
  }

  public IEnumerable<IPAddress> GetAllHosts()
  {
    for (uint i = _firstUint; i <= _lastUint; i++)
    {
      yield return new IPAddress(new byte[]
      {
                (byte)(i >> 24), (byte)(i >> 16), (byte)(i >> 8), (byte)i
      });
    }
  }

  public override string ToString() => _display;
}
