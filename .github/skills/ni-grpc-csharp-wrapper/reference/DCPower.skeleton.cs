// Skeleton of the hand-written part of a wrapper (NI-DCPower flavour). Replace NiDCPower/DCPower/
// namespaces, request types and convenience methods with the target driver's. Everything between
// "#region Generated RPC wrappers" and "#endregion" is rewritten by the generator script.
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using NationalInstruments.Grpc.DCPower;   // namespace = proto "csharp_namespace" option
using NationalInstruments.Grpc.Device;    // Session type from session.proto

/// <summary>Raised when an NI-DCPower call returns a negative (error) status.</summary>
public class DCPowerException : Exception
{
    public int Status { get; }

    public DCPowerException(int status, string message) : base($"NI-DCPower error {status}: {message}")
    {
        Status = status;
    }
}

/// <summary>
/// Object-oriented wrapper over the NI-DCPower gRPC client. Hides request construction and
/// converts non-zero driver statuses into exceptions (errors) or <see cref="WarningRaised"/> (warnings).
/// </summary>
public sealed class DCPower : IDisposable
{
    private readonly GrpcChannel? _ownedChannel;
    private readonly NiDCPower.NiDCPowerClient _client;
    private Session? _vi;

    /// <summary>Raised with (status, message) when the driver returns a positive status.</summary>
    public event Action<int, string>? WarningRaised;

    public string ResourceName { get; }
    public bool IsOpen => _vi != null;

    public DCPower(string serverAddress, string resourceName, string optionString = "",
        string sessionName = "DcPowerSession", bool reset = false)
        : this(GrpcChannel.ForAddress(serverAddress), resourceName, optionString, sessionName, reset, ownsChannel: true)
    {
    }

    public DCPower(GrpcChannel channel, string resourceName, string optionString = "",
        string sessionName = "DcPowerSession", bool reset = false, bool ownsChannel = false)
        : this(channel, resourceName, ownsChannel)
    {
        try
        {
            var response = _client.InitializeWithIndependentChannels(new InitializeWithIndependentChannelsRequest
            {
                SessionName = sessionName,
                ResourceName = resourceName,
                Reset = reset,
                OptionString = optionString,
                InitializationBehavior = SessionInitializationBehavior.InitializeNew
            });
            _vi = response.Vi;
            Check(response.Status);
        }
        catch
        {
            _ownedChannel?.Dispose();
            throw;
        }
    }

    // Creates an unopened wrapper; used by the initialize factories.
    private DCPower(GrpcChannel channel, string resourceName, bool ownsChannel)
    {
        _ownedChannel = ownsChannel ? channel : null;
        _client = new NiDCPower.NiDCPowerClient(channel);
        ResourceName = resourceName;
    }

    // ---- hand-written convenience methods for the common workflow go here ----

    /// <summary>Closes the session. Safe to call more than once.</summary>
    public void Close()
    {
        var vi = _vi;
        if (vi == null) return;
        _vi = null;
        Check(_client.Close(new CloseRequest { Vi = vi }).Status, vi);
    }

    public void Dispose()
    {
        try
        {
            Close();
        }
        catch (RpcException)
        {
        }
        catch (DCPowerException)
        {
        }
        finally
        {
            _ownedChannel?.Dispose();
        }
    }

    private Session Vi => _vi ?? throw new ObjectDisposedException(nameof(DCPower), "The session is closed.");

    // Negative status is an error, positive is a warning.
    private void Check(int status, Session? vi = null)
    {
        if (status == 0) return;

        var message = $"status {status}";
        try
        {
            message = _client.ErrorMessage(new ErrorMessageRequest { Vi = vi ?? _vi, ErrorCode = status }).ErrorMessage;
        }
        catch (RpcException)
        {
        }

        if (status < 0) throw new DCPowerException(status, message);
        WarningRaised?.Invoke(status, message);
    }

    #region Generated RPC wrappers
    #endregion
}
