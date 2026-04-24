# Lakerfield.PingSweep

A fast .NET global tool for sweeping a network range and discovering online hosts.

<img width="550" height="324" alt="image" src="https://github.com/user-attachments/assets/7e49fb40-b0cb-4740-b0ac-31379f9eb987" />

## Installation

```bash
dotnet tool install --global Lakerfield.PingSweep
```

## Run without installing

```bash
dnx Lakerfield.PingSweep
```

## Usage

### Interactive mode

Run without arguments to get an interactive prompt. Select a network interface and confirm the CIDR range — results appear live as hosts respond, then a sorted summary is shown when the sweep finishes.

```bash
ping-sweep
```

### CLI mode

Pass a range directly to sweep non-interactively. Online hosts are printed as they respond, followed by a sorted list.

Two range formats are supported:

```bash
# CIDR notation
ping-sweep 192.168.1.0/24

# Dash notation (last-octet range)
ping-sweep 192.168.1.1-100
```

## Requirements

- .NET 10 runtime
- On Linux/macOS, raw ICMP may require running as root or granting the binary `cap_net_raw`
