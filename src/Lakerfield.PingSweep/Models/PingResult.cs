using System.Net;

namespace Lakerfield.PingSweep.Models;

public class PingResult
{
  public IPAddress IpAddress { get; init; } = IPAddress.None;
  public bool IsOnline { get; init; }
  public long RoundtripTime { get; init; }
}
