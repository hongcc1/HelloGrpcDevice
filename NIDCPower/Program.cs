using Grpc.Core;

// Usage: NIDCPower [server_address] [resource_name] [channel]
// Without resource_name the app runs against a simulated device on channel "0";
// with it, the app runs against the real device (channel defaults to "0").
var address = args.Length > 0 ? args[0] : "http://localhost:31763";
var simulated = args.Length < 2;
var resource = simulated ? "SimulatedDCPower" : args[1];
var channel = args.Length > 2 ? args[2] : "0";
var optionString = simulated ? "Simulate=1, DriverSetup=Model:4141; BoardType:PXIe" : "";
Console.WriteLine(simulated ? "Running with a simulated device." : "Running with a real device.");

try
{
    using var dcPower = new DCPower(address, resource, optionString);
    dcPower.WarningRaised += (status, message) => Console.WriteLine($"NI-DCPower warning {status}: {message}");
    Console.WriteLine($"Initialized session on '{resource}'.");

    dcPower.ConfigureSourceModeSinglePoint();
    dcPower.ConfigureDcVoltage(channel, level: 3.0, currentLimit: 0.1);
    dcPower.SetOutputEnabled(channel, true);    
    dcPower.Initiate();

    Console.WriteLine($"Voltage: {dcPower.MeasureVoltage(channel):F4} V");
    Console.WriteLine($"Current: {dcPower.MeasureCurrent(channel):F6} A");

    dcPower.SetOutputEnabled(channel, false);
}
catch (RpcException ex)
{
    Console.Error.WriteLine($"gRPC error: {ex.Status.Detail} ({ex.StatusCode})");
    Environment.ExitCode = 1;
}
catch (DCPowerException ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}
