using Lakerfield.PingSweep.Ui;

if (args.Length == 0)
  await TuiMode.RunAsync();
else
  await CliMode.RunAsync(args[0]);
