using Lakerfield.PingSweep.Models;
using Lakerfield.PingSweep.Services;
using Spectre.Console;

namespace Lakerfield.PingSweep.Ui;

public static class CliMode
{
  public static async Task RunAsync(string cidrArg)
  {
    if (!CidrRange.TryParse(cidrArg, out var range) || range is null)
    {
      AnsiConsole.MarkupLine($"[red]Error:[/] Invalid CIDR range: [yellow]{cidrArg}[/]");
      AnsiConsole.MarkupLine("Usage: ping-sweep [bold]<cidr>[/]  e.g. [dim]192.168.1.0/24[/]");
      Environment.Exit(1);
      return;
    }

    AnsiConsole.MarkupLine($"[bold cyan]Ping Sweep[/] [dim]|[/] Scanning [green]{range}[/] [dim]({range.FirstHost} - {range.LastHost},[/] [yellow]{range.HostCount}[/][dim] hosts)[/]");
    AnsiConsole.WriteLine();

    var onlineResults = new List<PingResult>();

    await foreach (var result in PingSweepService.SweepAsync(range!))
    {
      if (result.IsOnline)
      {
        onlineResults.Add(result);
        AnsiConsole.MarkupLine($"  [green]UP[/]  {result.IpAddress,-18} [dim]{result.RoundtripTime} ms[/]");
      }
    }

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[dim]Sorted results:[/]");
    foreach (var result in onlineResults.OrderBy(r => IpToUint(r.IpAddress)))
      AnsiConsole.MarkupLine($"  [green]UP[/]  {result.IpAddress,-18} [dim]{result.RoundtripTime} ms[/]");

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[bold]Done.[/] [green]{onlineResults.Count}[/]/[yellow]{range.HostCount}[/] hosts online.");
  }

  private static uint IpToUint(System.Net.IPAddress ip)
  {
    var b = ip.GetAddressBytes();
    return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
  }
}
