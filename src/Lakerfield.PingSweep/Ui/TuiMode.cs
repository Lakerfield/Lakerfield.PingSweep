using System.Text;
using Lakerfield.PingSweep.Models;
using Lakerfield.PingSweep.Services;
using Spectre.Console;

namespace Lakerfield.PingSweep.Ui;

public static class TuiMode
{
  public static async Task RunAsync()
  {
    AnsiConsole.MarkupLine("[bold cyan]Ping Sweep[/]");
    AnsiConsole.WriteLine();

    // Step 1: Select network interface
    var interfaces = NetworkInterfaceService.GetInterfaces();

    if (interfaces.Count == 0)
    {
      AnsiConsole.MarkupLine("[red]No active network interfaces found.[/]");
      return;
    }

    NetworkInterfaceInfo selectedInterface;
    if (interfaces.Count == 1)
    {
      selectedInterface = interfaces[0];
      AnsiConsole.MarkupLine($"Using interface: [green]{selectedInterface}[/]");
    }
    else
    {
      selectedInterface = AnsiConsole.Prompt(
        new SelectionPrompt<NetworkInterfaceInfo>()
          .Title("Select a [green]network interface[/] to scan:")
          .PageSize(10)
          .AddChoices(interfaces));
    }

    // Step 2: Confirm/edit CIDR range
    var defaultCidr = CidrRange.FromInterface(selectedInterface).ToString();
    var cidrInput = PromptEditable("CIDR range to scan: ", defaultCidr);
    var range = IpRange.Parse(cidrInput);

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"Scanning [green]{range}[/] [dim]({range.FirstHost} - {range.LastHost},[/] [yellow]{range.HostCount}[/][dim] hosts)[/]");
    AnsiConsole.WriteLine();

    // Step 3: Live table
    var table = new Table()
      .RoundedBorder()
      .BorderColor(Color.Grey)
      .AddColumn(new TableColumn("[bold]IP Address[/]").Width(20))
      .AddColumn(new TableColumn("[bold]Status[/]").Width(10))
      .AddColumn(new TableColumn("[bold]RTT[/]").Width(10));

    var onlineResults = new List<PingResult>();

    await AnsiConsole.Live(table)
      .AutoClear(false)
      .Overflow(VerticalOverflow.Ellipsis)
      .StartAsync(async ctx =>
      {
        await foreach (var result in PingSweepService.SweepAsync(range))
        {
          if (result.IsOnline)
          {
            onlineResults.Add(result);
            table.AddRow(
                    result.IpAddress.ToString(),
                    "[green]UP[/]",
                    $"[dim]{result.RoundtripTime} ms[/]");
            ctx.Refresh();
          }
        }
      });

    // Rebuild table sorted by IP
    var sortedTable = new Table()
      .RoundedBorder()
      .BorderColor(Color.Grey)
      .AddColumn(new TableColumn("[bold]IP Address[/]").Width(20))
      .AddColumn(new TableColumn("[bold]Status[/]").Width(10))
      .AddColumn(new TableColumn("[bold]RTT[/]").Width(10));

    foreach (var result in onlineResults.OrderBy(r => IpToUint(r.IpAddress)))
    {
      sortedTable.AddRow(
          result.IpAddress.ToString(),
          "[green]UP[/]",
          $"[dim]{result.RoundtripTime} ms[/]");
    }

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[dim]Sorted results:[/]");
    AnsiConsole.Write(sortedTable);
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[bold]Sweep complete.[/] [green]{onlineResults.Count}[/]/[yellow]{range.HostCount}[/] hosts online.");
  }

  // Prints the prompt with the default value pre-filled so the user can edit it
  // directly (e.g. backspace "24" and type "28") rather than retyping from scratch.
  private static string PromptEditable(string prompt, string defaultValue)
  {
    while (true)
    {
      AnsiConsole.Markup($"[green]?[/] {prompt}");
      var input = new StringBuilder(defaultValue);
      Console.Write(defaultValue);

      while (true)
      {
        var key = Console.ReadKey(intercept: true);

        if (key.Key == ConsoleKey.Enter)
        {
          Console.WriteLine();
          break;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
          if (input.Length > 0)
          {
            input.Remove(input.Length - 1, 1);
            Console.Write("\b \b");
          }
        }
        else if (!char.IsControl(key.KeyChar))
        {
          input.Append(key.KeyChar);
          Console.Write(key.KeyChar);
        }
      }

      var value = input.ToString().Trim();
      if (IpRange.TryParse(value, out _))
        return value;

      AnsiConsole.MarkupLine("[red]Invalid range.[/] Examples: [dim]192.168.1.0/24[/]  or  [dim]192.168.1.1-100[/]");
    }
  }

  private static uint IpToUint(System.Net.IPAddress ip)
  {
    var b = ip.GetAddressBytes();
    return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
  }
}
