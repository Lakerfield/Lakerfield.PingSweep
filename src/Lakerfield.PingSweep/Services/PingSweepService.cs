using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Lakerfield.PingSweep.Models;

namespace Lakerfield.PingSweep.Services;

public static class PingSweepService
{
  public static async IAsyncEnumerable<PingResult> SweepAsync(
    IpRange range,
    int timeoutMs = 1000,
    int maxConcurrency = 100,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var channel = Channel.CreateUnbounded<PingResult>(new UnboundedChannelOptions
    {
      SingleWriter = false,
      SingleReader = true
    });

    var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    var hosts = range.GetAllHosts().ToList();

    var producerTask = Task.Run(async () =>
    {
      try
      {
        var tasks = hosts.Select(ip => PingHostAsync(ip, timeoutMs, semaphore, channel.Writer, cancellationToken));
        await Task.WhenAll(tasks);
      }
      finally
      {
        channel.Writer.Complete();
      }
    }, cancellationToken);

    await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken))
    {
      yield return result;
    }

    await producerTask;
  }

  private static async Task PingHostAsync(
    IPAddress ip,
    int timeoutMs,
    SemaphoreSlim semaphore,
    ChannelWriter<PingResult> writer,
    CancellationToken cancellationToken)
  {
    await semaphore.WaitAsync(cancellationToken);
    try
    {
      using var ping = new Ping();
      PingReply reply;
      try
      {
        reply = await ping.SendPingAsync(ip, timeoutMs);
      }
      catch
      {
        await writer.WriteAsync(new PingResult { IpAddress = ip, IsOnline = false }, cancellationToken);
        return;
      }

      await writer.WriteAsync(new PingResult
      {
        IpAddress = ip,
        IsOnline = reply.Status == IPStatus.Success,
        RoundtripTime = reply.Status == IPStatus.Success ? reply.RoundtripTime : 0
      }, cancellationToken);
    }
    finally
    {
      semaphore.Release();
    }
  }
}
