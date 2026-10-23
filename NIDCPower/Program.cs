using Grpc.Core;
using Grpc.Net.Client;
using NationalInstruments.Grpc.DCPower;
using NationalInstruments.Grpc.Device;

// Usage: NIDCPower [server_address] [resource_name] [channel]
// Without resource_name the app runs against a simulated device on channel "0";
// with it, the app runs against the real device (channel defaults to "0").
var address = args.Length > 0 ? args[0] : "http://localhost:31763";
var simulated = args.Length < 2;
var resource = simulated ? "SimulatedDCPower" : args[1];
var channel = args.Length > 2 ? args[2] : "0";
var optionString = simulated ? "Simulate=1, DriverSetup=Model:4141; BoardType:PXIe" : "";
Console.WriteLine(simulated ? "Running with a simulated device." : "Running with a real device.");

using var grpcChannel = GrpcChannel.ForAddress(address);
var client = new NiDCPower.NiDCPowerClient(grpcChannel);

Session? vi = null;
try
{
    var init = client.InitializeWithIndependentChannels(new InitializeWithIndependentChannelsRequest
    {
        SessionName = "DcPowerSession",
        ResourceName = resource,
        Reset = false,
        OptionString = optionString,
        InitializationBehavior = SessionInitializationBehavior.InitializeNew
    });
    ThrowIfError(init.Status);
    vi = init.Vi;
    Console.WriteLine($"Initialized session '{vi.Name}' on '{resource}'.");

    ThrowIfError(client.ConfigureSourceMode(new ConfigureSourceModeRequest
    {
        Vi = vi,
        SourceMode = SourceMode.NidcpowerValSinglePoint
    }).Status);
    ThrowIfError(client.ConfigureOutputFunction(new ConfigureOutputFunctionRequest
    {
        Vi = vi,
        ChannelName = channel,
        Function = OutputFunction.NidcpowerValDcVoltage
    }).Status);
    ThrowIfError(client.ConfigureCurrentLimit(new ConfigureCurrentLimitRequest
    {
        Vi = vi,
        ChannelName = channel,
        Behavior = CurrentLimitBehavior.NidcpowerValCurrentRegulate,
        Limit = 0.1
    }).Status);
    ThrowIfError(client.ConfigureVoltageLevel(new ConfigureVoltageLevelRequest
    {
        Vi = vi,
        ChannelName = channel,
        Level = 3.0
    }).Status);
    ThrowIfError(client.ConfigureOutputEnabled(new ConfigureOutputEnabledRequest
    {
        Vi = vi,
        ChannelName = channel,
        Enabled = true
    }).Status);
    ThrowIfError(client.Initiate(new InitiateRequest { Vi = vi }).Status);

    var voltage = client.Measure(new MeasureRequest
    {
        Vi = vi,
        ChannelName = channel,
        MeasurementType = MeasurementTypes.NidcpowerValMeasureVoltage
    });
    ThrowIfError(voltage.Status);
    var current = client.Measure(new MeasureRequest
    {
        Vi = vi,
        ChannelName = channel,
        MeasurementType = MeasurementTypes.NidcpowerValMeasureCurrent
    });
    ThrowIfError(current.Status);

    Console.WriteLine($"Voltage: {voltage.Measurement:F4} V");
    Console.WriteLine($"Current: {current.Measurement:F6} A");
}
catch (RpcException ex)
{
    Console.Error.WriteLine($"gRPC error: {ex.Status.Detail} ({ex.StatusCode})");
    Environment.ExitCode = 1;
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}
finally
{
    if (vi != null)
    {
        try
        {
            client.ConfigureOutputEnabled(new ConfigureOutputEnabledRequest { Vi = vi, ChannelName = channel, Enabled = false });
            client.Close(new CloseRequest { Vi = vi });
            Console.WriteLine("Session closed.");
        }
        catch (RpcException ex)
        {
            Console.Error.WriteLine($"Cleanup failed: {ex.Status.Detail}");
        }
    }
}

// Negative status is an error, positive is a warning.
void ThrowIfError(int status)
{
    if (status == 0) return;
    string message = $"status {status}";
    try
    {
        message = client.ErrorMessage(new ErrorMessageRequest { Vi = vi, ErrorCode = status }).ErrorMessage;
    }
    catch (RpcException) { }

    if (status < 0)
        throw new InvalidOperationException($"NI-DCPower error {status}: {message}");
    Console.WriteLine($"NI-DCPower warning {status}: {message}");
}
