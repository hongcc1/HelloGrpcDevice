using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using NationalInstruments.Grpc.DCPower;
using NationalInstruments.Grpc.Device;

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

    public void ConfigureSourceModeSinglePoint() =>
        Check(_client.ConfigureSourceMode(new ConfigureSourceModeRequest
        {
            Vi = Vi,
            SourceMode = SourceMode.NidcpowerValSinglePoint
        }).Status);

    public void ConfigureDcVoltage(string channel, double level, double currentLimit)
    {
        Check(_client.ConfigureOutputFunction(new ConfigureOutputFunctionRequest
        {
            Vi = Vi,
            ChannelName = channel,
            Function = OutputFunction.NidcpowerValDcVoltage
        }).Status);
        Check(_client.ConfigureCurrentLimit(new ConfigureCurrentLimitRequest
        {
            Vi = Vi,
            ChannelName = channel,
            Behavior = CurrentLimitBehavior.NidcpowerValCurrentRegulate,
            Limit = currentLimit
        }).Status);
        Check(_client.ConfigureVoltageLevel(new ConfigureVoltageLevelRequest
        {
            Vi = Vi,
            ChannelName = channel,
            Level = level
        }).Status);
    }

    public void SetOutputEnabled(string channel, bool enabled) =>
        Check(_client.ConfigureOutputEnabled(new ConfigureOutputEnabledRequest
        {
            Vi = Vi,
            ChannelName = channel,
            Enabled = enabled
        }).Status);

    public double MeasureVoltage(string channel) => Measure(channel, MeasurementTypes.NidcpowerValMeasureVoltage);

    public double MeasureCurrent(string channel) => Measure(channel, MeasurementTypes.NidcpowerValMeasureCurrent);

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
    // Generated by tools/generate_dcpower_wrapper.py from nidcpower.proto. Do not edit by hand.

    /// <summary>Calls the NI-DCPower <c>Abort</c> RPC.</summary>
    public void Abort()
    {
        var request = new AbortRequest
        {
            Vi = Vi,
        };
        var response = _client.Abort(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>AbortWithChannels</c> RPC.</summary>
    public void AbortWithChannels(string channelName)
    {
        var request = new AbortWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.AbortWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>CalSelfCalibrate</c> RPC.</summary>
    public void CalSelfCalibrate(string channelName)
    {
        var request = new CalSelfCalibrateRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.CalSelfCalibrate(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ClearError</c> RPC.</summary>
    public void ClearError()
    {
        var request = new ClearErrorRequest
        {
            Vi = Vi,
        };
        var response = _client.ClearError(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ClearInterchangeWarnings</c> RPC.</summary>
    public void ClearInterchangeWarnings()
    {
        var request = new ClearInterchangeWarningsRequest
        {
            Vi = Vi,
        };
        var response = _client.ClearInterchangeWarnings(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ClearLatchedOutputCutoffState</c> RPC.</summary>
    public void ClearLatchedOutputCutoffState(string channelName, OutputCutoffReason outputCutoffReason)
    {
        var request = new ClearLatchedOutputCutoffStateRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            OutputCutoffReason = outputCutoffReason,
        };
        var response = _client.ClearLatchedOutputCutoffState(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ClearLatchedOutputCutoffState</c> RPC (raw integer value overload).</summary>
    public void ClearLatchedOutputCutoffState(string channelName, int outputCutoffReasonRaw)
    {
        var request = new ClearLatchedOutputCutoffStateRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            OutputCutoffReasonRaw = outputCutoffReasonRaw,
        };
        var response = _client.ClearLatchedOutputCutoffState(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>Commit</c> RPC.</summary>
    public void Commit()
    {
        var request = new CommitRequest
        {
            Vi = Vi,
        };
        var response = _client.Commit(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>CommitWithChannels</c> RPC.</summary>
    public void CommitWithChannels(string channelName)
    {
        var request = new CommitWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.CommitWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureApertureTime</c> RPC.</summary>
    public void ConfigureApertureTime(string channelName, double apertureTime, ApertureTimeUnits units)
    {
        var request = new ConfigureApertureTimeRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            ApertureTime = apertureTime,
            Units = units,
        };
        var response = _client.ConfigureApertureTime(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureApertureTime</c> RPC (raw integer value overload).</summary>
    public void ConfigureApertureTime(string channelName, double apertureTime, int unitsRaw)
    {
        var request = new ConfigureApertureTimeRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            ApertureTime = apertureTime,
            UnitsRaw = unitsRaw,
        };
        var response = _client.ConfigureApertureTime(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureAutoZero</c> RPC.</summary>
    public void ConfigureAutoZero(string channelName, AutoZero autoZero)
    {
        var request = new ConfigureAutoZeroRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            AutoZero = autoZero,
        };
        var response = _client.ConfigureAutoZero(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureAutoZero</c> RPC (raw integer value overload).</summary>
    public void ConfigureAutoZero(string channelName, int autoZeroRaw)
    {
        var request = new ConfigureAutoZeroRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            AutoZeroRaw = autoZeroRaw,
        };
        var response = _client.ConfigureAutoZero(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureCurrentLevel</c> RPC.</summary>
    public void ConfigureCurrentLevel(string channelName, double level)
    {
        var request = new ConfigureCurrentLevelRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Level = level,
        };
        var response = _client.ConfigureCurrentLevel(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureCurrentLevelRange</c> RPC.</summary>
    public void ConfigureCurrentLevelRange(string channelName, double range)
    {
        var request = new ConfigureCurrentLevelRangeRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Range = range,
        };
        var response = _client.ConfigureCurrentLevelRange(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureCurrentLimit</c> RPC.</summary>
    public void ConfigureCurrentLimit(string channelName, CurrentLimitBehavior behavior, double limit)
    {
        var request = new ConfigureCurrentLimitRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Behavior = behavior,
            Limit = limit,
        };
        var response = _client.ConfigureCurrentLimit(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureCurrentLimit</c> RPC (raw integer value overload).</summary>
    public void ConfigureCurrentLimit(string channelName, int behaviorRaw, double limit)
    {
        var request = new ConfigureCurrentLimitRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            BehaviorRaw = behaviorRaw,
            Limit = limit,
        };
        var response = _client.ConfigureCurrentLimit(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureCurrentLimitRange</c> RPC.</summary>
    public void ConfigureCurrentLimitRange(string channelName, double range)
    {
        var request = new ConfigureCurrentLimitRangeRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Range = range,
        };
        var response = _client.ConfigureCurrentLimitRange(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeMeasureTrigger</c> RPC.</summary>
    public void ConfigureDigitalEdgeMeasureTrigger(string inputTerminal, DigitalEdge edge)
    {
        var request = new ConfigureDigitalEdgeMeasureTriggerRequest
        {
            Vi = Vi,
            InputTerminal = inputTerminal,
            Edge = edge,
        };
        var response = _client.ConfigureDigitalEdgeMeasureTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeMeasureTrigger</c> RPC (raw integer value overload).</summary>
    public void ConfigureDigitalEdgeMeasureTrigger(string inputTerminal, int edgeRaw)
    {
        var request = new ConfigureDigitalEdgeMeasureTriggerRequest
        {
            Vi = Vi,
            InputTerminal = inputTerminal,
            EdgeRaw = edgeRaw,
        };
        var response = _client.ConfigureDigitalEdgeMeasureTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeMeasureTriggerWithChannels</c> RPC.</summary>
    public void ConfigureDigitalEdgeMeasureTriggerWithChannels(string channelName, string inputTerminal, DigitalEdge edge)
    {
        var request = new ConfigureDigitalEdgeMeasureTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            InputTerminal = inputTerminal,
            Edge = edge,
        };
        var response = _client.ConfigureDigitalEdgeMeasureTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeMeasureTriggerWithChannels</c> RPC (raw integer value overload).</summary>
    public void ConfigureDigitalEdgeMeasureTriggerWithChannels(string channelName, string inputTerminal, int edgeRaw)
    {
        var request = new ConfigureDigitalEdgeMeasureTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            InputTerminal = inputTerminal,
            EdgeRaw = edgeRaw,
        };
        var response = _client.ConfigureDigitalEdgeMeasureTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgePulseTrigger</c> RPC.</summary>
    public void ConfigureDigitalEdgePulseTrigger(string inputTerminal, DigitalEdge edge)
    {
        var request = new ConfigureDigitalEdgePulseTriggerRequest
        {
            Vi = Vi,
            InputTerminal = inputTerminal,
            Edge = edge,
        };
        var response = _client.ConfigureDigitalEdgePulseTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgePulseTrigger</c> RPC (raw integer value overload).</summary>
    public void ConfigureDigitalEdgePulseTrigger(string inputTerminal, int edgeRaw)
    {
        var request = new ConfigureDigitalEdgePulseTriggerRequest
        {
            Vi = Vi,
            InputTerminal = inputTerminal,
            EdgeRaw = edgeRaw,
        };
        var response = _client.ConfigureDigitalEdgePulseTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgePulseTriggerWithChannels</c> RPC.</summary>
    public void ConfigureDigitalEdgePulseTriggerWithChannels(string channelName, string inputTerminal, DigitalEdge edge)
    {
        var request = new ConfigureDigitalEdgePulseTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            InputTerminal = inputTerminal,
            Edge = edge,
        };
        var response = _client.ConfigureDigitalEdgePulseTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgePulseTriggerWithChannels</c> RPC (raw integer value overload).</summary>
    public void ConfigureDigitalEdgePulseTriggerWithChannels(string channelName, string inputTerminal, int edgeRaw)
    {
        var request = new ConfigureDigitalEdgePulseTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            InputTerminal = inputTerminal,
            EdgeRaw = edgeRaw,
        };
        var response = _client.ConfigureDigitalEdgePulseTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeSequenceAdvanceTrigger</c> RPC.</summary>
    public void ConfigureDigitalEdgeSequenceAdvanceTrigger(string inputTerminal, DigitalEdge edge)
    {
        var request = new ConfigureDigitalEdgeSequenceAdvanceTriggerRequest
        {
            Vi = Vi,
            InputTerminal = inputTerminal,
            Edge = edge,
        };
        var response = _client.ConfigureDigitalEdgeSequenceAdvanceTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeSequenceAdvanceTrigger</c> RPC (raw integer value overload).</summary>
    public void ConfigureDigitalEdgeSequenceAdvanceTrigger(string inputTerminal, int edgeRaw)
    {
        var request = new ConfigureDigitalEdgeSequenceAdvanceTriggerRequest
        {
            Vi = Vi,
            InputTerminal = inputTerminal,
            EdgeRaw = edgeRaw,
        };
        var response = _client.ConfigureDigitalEdgeSequenceAdvanceTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeSequenceAdvanceTriggerWithChannels</c> RPC.</summary>
    public void ConfigureDigitalEdgeSequenceAdvanceTriggerWithChannels(string channelName, string inputTerminal, DigitalEdge edge)
    {
        var request = new ConfigureDigitalEdgeSequenceAdvanceTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            InputTerminal = inputTerminal,
            Edge = edge,
        };
        var response = _client.ConfigureDigitalEdgeSequenceAdvanceTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeSequenceAdvanceTriggerWithChannels</c> RPC (raw integer value overload).</summary>
    public void ConfigureDigitalEdgeSequenceAdvanceTriggerWithChannels(string channelName, string inputTerminal, int edgeRaw)
    {
        var request = new ConfigureDigitalEdgeSequenceAdvanceTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            InputTerminal = inputTerminal,
            EdgeRaw = edgeRaw,
        };
        var response = _client.ConfigureDigitalEdgeSequenceAdvanceTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeShutdownTriggerWithChannels</c> RPC.</summary>
    public void ConfigureDigitalEdgeShutdownTriggerWithChannels(string channelName, string inputTerminal, DigitalEdge edge)
    {
        var request = new ConfigureDigitalEdgeShutdownTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            InputTerminal = inputTerminal,
            Edge = edge,
        };
        var response = _client.ConfigureDigitalEdgeShutdownTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeShutdownTriggerWithChannels</c> RPC (raw integer value overload).</summary>
    public void ConfigureDigitalEdgeShutdownTriggerWithChannels(string channelName, string inputTerminal, int edgeRaw)
    {
        var request = new ConfigureDigitalEdgeShutdownTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            InputTerminal = inputTerminal,
            EdgeRaw = edgeRaw,
        };
        var response = _client.ConfigureDigitalEdgeShutdownTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeSourceTrigger</c> RPC.</summary>
    public void ConfigureDigitalEdgeSourceTrigger(string inputTerminal, DigitalEdge edge)
    {
        var request = new ConfigureDigitalEdgeSourceTriggerRequest
        {
            Vi = Vi,
            InputTerminal = inputTerminal,
            Edge = edge,
        };
        var response = _client.ConfigureDigitalEdgeSourceTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeSourceTrigger</c> RPC (raw integer value overload).</summary>
    public void ConfigureDigitalEdgeSourceTrigger(string inputTerminal, int edgeRaw)
    {
        var request = new ConfigureDigitalEdgeSourceTriggerRequest
        {
            Vi = Vi,
            InputTerminal = inputTerminal,
            EdgeRaw = edgeRaw,
        };
        var response = _client.ConfigureDigitalEdgeSourceTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeSourceTriggerWithChannels</c> RPC.</summary>
    public void ConfigureDigitalEdgeSourceTriggerWithChannels(string channelName, string inputTerminal, DigitalEdge edge)
    {
        var request = new ConfigureDigitalEdgeSourceTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            InputTerminal = inputTerminal,
            Edge = edge,
        };
        var response = _client.ConfigureDigitalEdgeSourceTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeSourceTriggerWithChannels</c> RPC (raw integer value overload).</summary>
    public void ConfigureDigitalEdgeSourceTriggerWithChannels(string channelName, string inputTerminal, int edgeRaw)
    {
        var request = new ConfigureDigitalEdgeSourceTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            InputTerminal = inputTerminal,
            EdgeRaw = edgeRaw,
        };
        var response = _client.ConfigureDigitalEdgeSourceTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeStartTrigger</c> RPC.</summary>
    public void ConfigureDigitalEdgeStartTrigger(string inputTerminal, DigitalEdge edge)
    {
        var request = new ConfigureDigitalEdgeStartTriggerRequest
        {
            Vi = Vi,
            InputTerminal = inputTerminal,
            Edge = edge,
        };
        var response = _client.ConfigureDigitalEdgeStartTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeStartTrigger</c> RPC (raw integer value overload).</summary>
    public void ConfigureDigitalEdgeStartTrigger(string inputTerminal, int edgeRaw)
    {
        var request = new ConfigureDigitalEdgeStartTriggerRequest
        {
            Vi = Vi,
            InputTerminal = inputTerminal,
            EdgeRaw = edgeRaw,
        };
        var response = _client.ConfigureDigitalEdgeStartTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeStartTriggerWithChannels</c> RPC.</summary>
    public void ConfigureDigitalEdgeStartTriggerWithChannels(string channelName, string inputTerminal, DigitalEdge edge)
    {
        var request = new ConfigureDigitalEdgeStartTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            InputTerminal = inputTerminal,
            Edge = edge,
        };
        var response = _client.ConfigureDigitalEdgeStartTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureDigitalEdgeStartTriggerWithChannels</c> RPC (raw integer value overload).</summary>
    public void ConfigureDigitalEdgeStartTriggerWithChannels(string channelName, string inputTerminal, int edgeRaw)
    {
        var request = new ConfigureDigitalEdgeStartTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            InputTerminal = inputTerminal,
            EdgeRaw = edgeRaw,
        };
        var response = _client.ConfigureDigitalEdgeStartTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureLCRCustomCableCompensation</c> RPC.</summary>
    public void ConfigureLCRCustomCableCompensation(string channelName, ByteString customCableCompensationData)
    {
        var request = new ConfigureLCRCustomCableCompensationRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            CustomCableCompensationData = customCableCompensationData,
        };
        var response = _client.ConfigureLCRCustomCableCompensation(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureOutputEnabled</c> RPC.</summary>
    public void ConfigureOutputEnabled(string channelName, bool enabled)
    {
        var request = new ConfigureOutputEnabledRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Enabled = enabled,
        };
        var response = _client.ConfigureOutputEnabled(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureOutputFunction</c> RPC.</summary>
    public void ConfigureOutputFunction(string channelName, OutputFunction function)
    {
        var request = new ConfigureOutputFunctionRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Function = function,
        };
        var response = _client.ConfigureOutputFunction(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureOutputFunction</c> RPC (raw integer value overload).</summary>
    public void ConfigureOutputFunction(string channelName, int functionRaw)
    {
        var request = new ConfigureOutputFunctionRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            FunctionRaw = functionRaw,
        };
        var response = _client.ConfigureOutputFunction(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureOutputResistance</c> RPC.</summary>
    public void ConfigureOutputResistance(string channelName, double resistance)
    {
        var request = new ConfigureOutputResistanceRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Resistance = resistance,
        };
        var response = _client.ConfigureOutputResistance(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureOvp</c> RPC.</summary>
    public void ConfigureOvp(string channelName, bool enabled, double limit)
    {
        var request = new ConfigureOvpRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Enabled = enabled,
            Limit = limit,
        };
        var response = _client.ConfigureOvp(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigurePowerLineFrequency</c> RPC.</summary>
    public void ConfigurePowerLineFrequency(PowerLineFrequencies powerlineFrequency)
    {
        var request = new ConfigurePowerLineFrequencyRequest
        {
            Vi = Vi,
            PowerlineFrequency = powerlineFrequency,
        };
        var response = _client.ConfigurePowerLineFrequency(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigurePowerLineFrequency</c> RPC (raw integer value overload).</summary>
    public void ConfigurePowerLineFrequency(int powerlineFrequencyRaw)
    {
        var request = new ConfigurePowerLineFrequencyRequest
        {
            Vi = Vi,
            PowerlineFrequencyRaw = powerlineFrequencyRaw,
        };
        var response = _client.ConfigurePowerLineFrequency(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigurePulseBiasCurrentLevel</c> RPC.</summary>
    public void ConfigurePulseBiasCurrentLevel(string channelName, double level)
    {
        var request = new ConfigurePulseBiasCurrentLevelRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Level = level,
        };
        var response = _client.ConfigurePulseBiasCurrentLevel(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigurePulseBiasCurrentLimit</c> RPC.</summary>
    public void ConfigurePulseBiasCurrentLimit(string channelName, double limit)
    {
        var request = new ConfigurePulseBiasCurrentLimitRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Limit = limit,
        };
        var response = _client.ConfigurePulseBiasCurrentLimit(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigurePulseBiasVoltageLevel</c> RPC.</summary>
    public void ConfigurePulseBiasVoltageLevel(string channelName, double level)
    {
        var request = new ConfigurePulseBiasVoltageLevelRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Level = level,
        };
        var response = _client.ConfigurePulseBiasVoltageLevel(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigurePulseBiasVoltageLimit</c> RPC.</summary>
    public void ConfigurePulseBiasVoltageLimit(string channelName, double limit)
    {
        var request = new ConfigurePulseBiasVoltageLimitRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Limit = limit,
        };
        var response = _client.ConfigurePulseBiasVoltageLimit(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigurePulseCurrentLevel</c> RPC.</summary>
    public void ConfigurePulseCurrentLevel(string channelName, double level)
    {
        var request = new ConfigurePulseCurrentLevelRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Level = level,
        };
        var response = _client.ConfigurePulseCurrentLevel(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigurePulseCurrentLevelRange</c> RPC.</summary>
    public void ConfigurePulseCurrentLevelRange(string channelName, double range)
    {
        var request = new ConfigurePulseCurrentLevelRangeRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Range = range,
        };
        var response = _client.ConfigurePulseCurrentLevelRange(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigurePulseCurrentLimit</c> RPC.</summary>
    public void ConfigurePulseCurrentLimit(string channelName, double limit)
    {
        var request = new ConfigurePulseCurrentLimitRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Limit = limit,
        };
        var response = _client.ConfigurePulseCurrentLimit(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigurePulseCurrentLimitRange</c> RPC.</summary>
    public void ConfigurePulseCurrentLimitRange(string channelName, double range)
    {
        var request = new ConfigurePulseCurrentLimitRangeRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Range = range,
        };
        var response = _client.ConfigurePulseCurrentLimitRange(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigurePulseVoltageLevel</c> RPC.</summary>
    public void ConfigurePulseVoltageLevel(string channelName, double level)
    {
        var request = new ConfigurePulseVoltageLevelRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Level = level,
        };
        var response = _client.ConfigurePulseVoltageLevel(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigurePulseVoltageLevelRange</c> RPC.</summary>
    public void ConfigurePulseVoltageLevelRange(string channelName, double range)
    {
        var request = new ConfigurePulseVoltageLevelRangeRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Range = range,
        };
        var response = _client.ConfigurePulseVoltageLevelRange(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigurePulseVoltageLimit</c> RPC.</summary>
    public void ConfigurePulseVoltageLimit(string channelName, double limit)
    {
        var request = new ConfigurePulseVoltageLimitRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Limit = limit,
        };
        var response = _client.ConfigurePulseVoltageLimit(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigurePulseVoltageLimitRange</c> RPC.</summary>
    public void ConfigurePulseVoltageLimitRange(string channelName, double range)
    {
        var request = new ConfigurePulseVoltageLimitRangeRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Range = range,
        };
        var response = _client.ConfigurePulseVoltageLimitRange(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSense</c> RPC.</summary>
    public void ConfigureSense(string channelName, Sense sense)
    {
        var request = new ConfigureSenseRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Sense = sense,
        };
        var response = _client.ConfigureSense(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSense</c> RPC (raw integer value overload).</summary>
    public void ConfigureSense(string channelName, int senseRaw)
    {
        var request = new ConfigureSenseRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            SenseRaw = senseRaw,
        };
        var response = _client.ConfigureSense(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSoftwareEdgeMeasureTrigger</c> RPC.</summary>
    public void ConfigureSoftwareEdgeMeasureTrigger()
    {
        var request = new ConfigureSoftwareEdgeMeasureTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.ConfigureSoftwareEdgeMeasureTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSoftwareEdgeMeasureTriggerWithChannels</c> RPC.</summary>
    public void ConfigureSoftwareEdgeMeasureTriggerWithChannels(string channelName)
    {
        var request = new ConfigureSoftwareEdgeMeasureTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.ConfigureSoftwareEdgeMeasureTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSoftwareEdgePulseTrigger</c> RPC.</summary>
    public void ConfigureSoftwareEdgePulseTrigger()
    {
        var request = new ConfigureSoftwareEdgePulseTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.ConfigureSoftwareEdgePulseTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSoftwareEdgePulseTriggerWithChannels</c> RPC.</summary>
    public void ConfigureSoftwareEdgePulseTriggerWithChannels(string channelName)
    {
        var request = new ConfigureSoftwareEdgePulseTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.ConfigureSoftwareEdgePulseTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSoftwareEdgeSequenceAdvanceTrigger</c> RPC.</summary>
    public void ConfigureSoftwareEdgeSequenceAdvanceTrigger()
    {
        var request = new ConfigureSoftwareEdgeSequenceAdvanceTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.ConfigureSoftwareEdgeSequenceAdvanceTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSoftwareEdgeSequenceAdvanceTriggerWithChannels</c> RPC.</summary>
    public void ConfigureSoftwareEdgeSequenceAdvanceTriggerWithChannels(string channelName)
    {
        var request = new ConfigureSoftwareEdgeSequenceAdvanceTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.ConfigureSoftwareEdgeSequenceAdvanceTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSoftwareEdgeShutdownTriggerWithChannels</c> RPC.</summary>
    public void ConfigureSoftwareEdgeShutdownTriggerWithChannels(string channelName)
    {
        var request = new ConfigureSoftwareEdgeShutdownTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.ConfigureSoftwareEdgeShutdownTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSoftwareEdgeSourceTrigger</c> RPC.</summary>
    public void ConfigureSoftwareEdgeSourceTrigger()
    {
        var request = new ConfigureSoftwareEdgeSourceTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.ConfigureSoftwareEdgeSourceTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSoftwareEdgeSourceTriggerWithChannels</c> RPC.</summary>
    public void ConfigureSoftwareEdgeSourceTriggerWithChannels(string channelName)
    {
        var request = new ConfigureSoftwareEdgeSourceTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.ConfigureSoftwareEdgeSourceTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSoftwareEdgeStartTrigger</c> RPC.</summary>
    public void ConfigureSoftwareEdgeStartTrigger()
    {
        var request = new ConfigureSoftwareEdgeStartTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.ConfigureSoftwareEdgeStartTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSoftwareEdgeStartTriggerWithChannels</c> RPC.</summary>
    public void ConfigureSoftwareEdgeStartTriggerWithChannels(string channelName)
    {
        var request = new ConfigureSoftwareEdgeStartTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.ConfigureSoftwareEdgeStartTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSourceMode</c> RPC.</summary>
    public void ConfigureSourceMode(SourceMode sourceMode)
    {
        var request = new ConfigureSourceModeRequest
        {
            Vi = Vi,
            SourceMode = sourceMode,
        };
        var response = _client.ConfigureSourceMode(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSourceMode</c> RPC (raw integer value overload).</summary>
    public void ConfigureSourceMode(int sourceModeRaw)
    {
        var request = new ConfigureSourceModeRequest
        {
            Vi = Vi,
            SourceModeRaw = sourceModeRaw,
        };
        var response = _client.ConfigureSourceMode(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSourceModeWithChannels</c> RPC.</summary>
    public void ConfigureSourceModeWithChannels(string channelName, SourceMode sourceMode)
    {
        var request = new ConfigureSourceModeWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            SourceMode = sourceMode,
        };
        var response = _client.ConfigureSourceModeWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureSourceModeWithChannels</c> RPC (raw integer value overload).</summary>
    public void ConfigureSourceModeWithChannels(string channelName, int sourceModeRaw)
    {
        var request = new ConfigureSourceModeWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            SourceModeRaw = sourceModeRaw,
        };
        var response = _client.ConfigureSourceModeWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureVoltageLevel</c> RPC.</summary>
    public void ConfigureVoltageLevel(string channelName, double level)
    {
        var request = new ConfigureVoltageLevelRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Level = level,
        };
        var response = _client.ConfigureVoltageLevel(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureVoltageLevelRange</c> RPC.</summary>
    public void ConfigureVoltageLevelRange(string channelName, double range)
    {
        var request = new ConfigureVoltageLevelRangeRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Range = range,
        };
        var response = _client.ConfigureVoltageLevelRange(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureVoltageLimit</c> RPC.</summary>
    public void ConfigureVoltageLimit(string channelName, double limit)
    {
        var request = new ConfigureVoltageLimitRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Limit = limit,
        };
        var response = _client.ConfigureVoltageLimit(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ConfigureVoltageLimitRange</c> RPC.</summary>
    public void ConfigureVoltageLimitRange(string channelName, double range)
    {
        var request = new ConfigureVoltageLimitRangeRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Range = range,
        };
        var response = _client.ConfigureVoltageLimitRange(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>CreateAdvancedSequence</c> RPC.</summary>
    public void CreateAdvancedSequence(string sequenceName, IEnumerable<int> attributeIds, bool setAsActiveSequence)
    {
        var request = new CreateAdvancedSequenceRequest
        {
            Vi = Vi,
            SequenceName = sequenceName,
            SetAsActiveSequence = setAsActiveSequence,
        };
        request.AttributeIds.AddRange(attributeIds);
        var response = _client.CreateAdvancedSequence(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>CreateAdvancedSequenceCommitStepWithChannels</c> RPC.</summary>
    public void CreateAdvancedSequenceCommitStepWithChannels(string channelName, bool setAsActiveStep)
    {
        var request = new CreateAdvancedSequenceCommitStepWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            SetAsActiveStep = setAsActiveStep,
        };
        var response = _client.CreateAdvancedSequenceCommitStepWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>CreateAdvancedSequenceStep</c> RPC.</summary>
    public void CreateAdvancedSequenceStep(bool setAsActiveStep)
    {
        var request = new CreateAdvancedSequenceStepRequest
        {
            Vi = Vi,
            SetAsActiveStep = setAsActiveStep,
        };
        var response = _client.CreateAdvancedSequenceStep(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>CreateAdvancedSequenceStepWithChannels</c> RPC.</summary>
    public void CreateAdvancedSequenceStepWithChannels(string channelName, bool setAsActiveStep)
    {
        var request = new CreateAdvancedSequenceStepWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            SetAsActiveStep = setAsActiveStep,
        };
        var response = _client.CreateAdvancedSequenceStepWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>CreateAdvancedSequenceWithChannels</c> RPC.</summary>
    public void CreateAdvancedSequenceWithChannels(string channelName, string sequenceName, IEnumerable<int> attributeIds, bool setAsActiveSequence)
    {
        var request = new CreateAdvancedSequenceWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            SequenceName = sequenceName,
            SetAsActiveSequence = setAsActiveSequence,
        };
        request.AttributeIds.AddRange(attributeIds);
        var response = _client.CreateAdvancedSequenceWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>DeleteAdvancedSequence</c> RPC.</summary>
    public void DeleteAdvancedSequence(string sequenceName)
    {
        var request = new DeleteAdvancedSequenceRequest
        {
            Vi = Vi,
            SequenceName = sequenceName,
        };
        var response = _client.DeleteAdvancedSequence(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>DeleteAdvancedSequenceWithChannels</c> RPC.</summary>
    public void DeleteAdvancedSequenceWithChannels(string channelName, string sequenceName)
    {
        var request = new DeleteAdvancedSequenceWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            SequenceName = sequenceName,
        };
        var response = _client.DeleteAdvancedSequenceWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>Disable</c> RPC.</summary>
    public void Disable()
    {
        var request = new DisableRequest
        {
            Vi = Vi,
        };
        var response = _client.Disable(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>DisablePulseTrigger</c> RPC.</summary>
    public void DisablePulseTrigger()
    {
        var request = new DisablePulseTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.DisablePulseTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>DisablePulseTriggerWithChannels</c> RPC.</summary>
    public void DisablePulseTriggerWithChannels(string channelName)
    {
        var request = new DisablePulseTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.DisablePulseTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>DisableSequenceAdvanceTrigger</c> RPC.</summary>
    public void DisableSequenceAdvanceTrigger()
    {
        var request = new DisableSequenceAdvanceTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.DisableSequenceAdvanceTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>DisableSequenceAdvanceTriggerWithChannels</c> RPC.</summary>
    public void DisableSequenceAdvanceTriggerWithChannels(string channelName)
    {
        var request = new DisableSequenceAdvanceTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.DisableSequenceAdvanceTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>DisableShutdownTriggerWithChannels</c> RPC.</summary>
    public void DisableShutdownTriggerWithChannels(string channelName)
    {
        var request = new DisableShutdownTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.DisableShutdownTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>DisableSourceTrigger</c> RPC.</summary>
    public void DisableSourceTrigger()
    {
        var request = new DisableSourceTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.DisableSourceTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>DisableSourceTriggerWithChannels</c> RPC.</summary>
    public void DisableSourceTriggerWithChannels(string channelName)
    {
        var request = new DisableSourceTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.DisableSourceTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>DisableStartTrigger</c> RPC.</summary>
    public void DisableStartTrigger()
    {
        var request = new DisableStartTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.DisableStartTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>DisableStartTriggerWithChannels</c> RPC.</summary>
    public void DisableStartTriggerWithChannels(string channelName)
    {
        var request = new DisableStartTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.DisableStartTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ErrorMessage</c> RPC.</summary>
    public string ErrorMessage(int errorCode)
    {
        var request = new ErrorMessageRequest
        {
            Vi = Vi,
            ErrorCode = errorCode,
        };
        var response = _client.ErrorMessage(request);
        Check(response.Status);
        return response.ErrorMessage;
    }

    /// <summary>Calls the NI-DCPower <c>ErrorQuery</c> RPC.</summary>
    public (int ErrorCode, string ErrorMessage) ErrorQuery()
    {
        var request = new ErrorQueryRequest
        {
            Vi = Vi,
        };
        var response = _client.ErrorQuery(request);
        Check(response.Status);
        return (response.ErrorCode, response.ErrorMessage);
    }

    /// <summary>Calls the NI-DCPower <c>ExportAttributeConfigurationBuffer</c> RPC.</summary>
    public ByteString ExportAttributeConfigurationBuffer()
    {
        var request = new ExportAttributeConfigurationBufferRequest
        {
            Vi = Vi,
        };
        var response = _client.ExportAttributeConfigurationBuffer(request);
        Check(response.Status);
        return response.Configuration;
    }

    /// <summary>Calls the NI-DCPower <c>ExportAttributeConfigurationFile</c> RPC.</summary>
    public void ExportAttributeConfigurationFile(string filePath)
    {
        var request = new ExportAttributeConfigurationFileRequest
        {
            Vi = Vi,
            FilePath = filePath,
        };
        var response = _client.ExportAttributeConfigurationFile(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ExportSignal</c> RPC.</summary>
    public void ExportSignal(ExportSignal signal, string signalIdentifier, string outputTerminal)
    {
        var request = new ExportSignalRequest
        {
            Vi = Vi,
            Signal = signal,
            SignalIdentifier = signalIdentifier,
            OutputTerminal = outputTerminal,
        };
        var response = _client.ExportSignal(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ExportSignal</c> RPC (raw integer value overload).</summary>
    public void ExportSignal(int signalRaw, string signalIdentifier, string outputTerminal)
    {
        var request = new ExportSignalRequest
        {
            Vi = Vi,
            SignalRaw = signalRaw,
            SignalIdentifier = signalIdentifier,
            OutputTerminal = outputTerminal,
        };
        var response = _client.ExportSignal(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ExportSignalWithChannels</c> RPC.</summary>
    public void ExportSignalWithChannels(string channelName, ExportSignal signal, string signalIdentifier, string outputTerminal)
    {
        var request = new ExportSignalWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Signal = signal,
            SignalIdentifier = signalIdentifier,
            OutputTerminal = outputTerminal,
        };
        var response = _client.ExportSignalWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ExportSignalWithChannels</c> RPC (raw integer value overload).</summary>
    public void ExportSignalWithChannels(string channelName, int signalRaw, string signalIdentifier, string outputTerminal)
    {
        var request = new ExportSignalWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            SignalRaw = signalRaw,
            SignalIdentifier = signalIdentifier,
            OutputTerminal = outputTerminal,
        };
        var response = _client.ExportSignalWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>FetchMultiple</c> RPC.</summary>
    public (IReadOnlyList<double> VoltageMeasurements, IReadOnlyList<double> CurrentMeasurements, IReadOnlyList<bool> InCompliance, int ActualCount) FetchMultiple(string channelName, double timeout, int count)
    {
        var request = new FetchMultipleRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Timeout = timeout,
            Count = count,
        };
        var response = _client.FetchMultiple(request);
        Check(response.Status);
        return (response.VoltageMeasurements, response.CurrentMeasurements, response.InCompliance, response.ActualCount);
    }

    /// <summary>Calls the NI-DCPower <c>FetchMultipleLCR</c> RPC.</summary>
    public (IReadOnlyList<NILCRMeasurement> Measurements, int ActualCount) FetchMultipleLCR(string channelName, double timeout, int count)
    {
        var request = new FetchMultipleLCRRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Timeout = timeout,
            Count = count,
        };
        var response = _client.FetchMultipleLCR(request);
        Check(response.Status);
        return (response.Measurements, response.ActualCount);
    }

    /// <summary>Calls the NI-DCPower <c>GetAttributeViBoolean</c> RPC.</summary>
    public bool GetAttributeViBoolean(string channelName, NiDCPowerAttribute attributeId)
    {
        var request = new GetAttributeViBooleanRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            AttributeId = attributeId,
        };
        var response = _client.GetAttributeViBoolean(request);
        Check(response.Status);
        return response.AttributeValue;
    }

    /// <summary>Calls the NI-DCPower <c>GetAttributeViInt32</c> RPC.</summary>
    public int GetAttributeViInt32(string channelName, NiDCPowerAttribute attributeId)
    {
        var request = new GetAttributeViInt32Request
        {
            Vi = Vi,
            ChannelName = channelName,
            AttributeId = attributeId,
        };
        var response = _client.GetAttributeViInt32(request);
        Check(response.Status);
        return response.AttributeValue;
    }

    /// <summary>Calls the NI-DCPower <c>GetAttributeViInt64</c> RPC.</summary>
    public long GetAttributeViInt64(string channelName, NiDCPowerAttribute attributeId)
    {
        var request = new GetAttributeViInt64Request
        {
            Vi = Vi,
            ChannelName = channelName,
            AttributeId = attributeId,
        };
        var response = _client.GetAttributeViInt64(request);
        Check(response.Status);
        return response.AttributeValue;
    }

    /// <summary>Calls the NI-DCPower <c>GetAttributeViReal64</c> RPC.</summary>
    public double GetAttributeViReal64(string channelName, NiDCPowerAttribute attributeId)
    {
        var request = new GetAttributeViReal64Request
        {
            Vi = Vi,
            ChannelName = channelName,
            AttributeId = attributeId,
        };
        var response = _client.GetAttributeViReal64(request);
        Check(response.Status);
        return response.AttributeValue;
    }

    /// <summary>Calls the NI-DCPower <c>GetAttributeViSession</c> RPC.</summary>
    public Session GetAttributeViSession(string channelName, NiDCPowerAttribute attributeId)
    {
        var request = new GetAttributeViSessionRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            AttributeId = attributeId,
        };
        var response = _client.GetAttributeViSession(request);
        Check(response.Status);
        return response.AttributeValue;
    }

    /// <summary>Calls the NI-DCPower <c>GetAttributeViString</c> RPC.</summary>
    public string GetAttributeViString(string channelName, NiDCPowerAttribute attributeId)
    {
        var request = new GetAttributeViStringRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            AttributeId = attributeId,
        };
        var response = _client.GetAttributeViString(request);
        Check(response.Status);
        return response.AttributeValue;
    }

    /// <summary>Calls the NI-DCPower <c>GetChannelName</c> RPC.</summary>
    public string GetChannelName(int index)
    {
        var request = new GetChannelNameRequest
        {
            Vi = Vi,
            Index = index,
        };
        var response = _client.GetChannelName(request);
        Check(response.Status);
        return response.ChannelName;
    }

    /// <summary>Calls the NI-DCPower <c>GetChannelNameFromString</c> RPC.</summary>
    public string GetChannelNameFromString(string index)
    {
        var request = new GetChannelNameFromStringRequest
        {
            Vi = Vi,
            Index = index,
        };
        var response = _client.GetChannelNameFromString(request);
        Check(response.Status);
        return response.ChannelName;
    }

    /// <summary>Calls the NI-DCPower <c>GetError</c> RPC.</summary>
    public (int Code, string Description) GetError()
    {
        var request = new GetErrorRequest
        {
            Vi = Vi,
        };
        var response = _client.GetError(request);
        Check(response.Status);
        return (response.Code, response.Description);
    }

    /// <summary>Calls the NI-DCPower <c>GetExtCalLastDateAndTime</c> RPC.</summary>
    public (int Year, int Month, int Day, int Hour, int Minute) GetExtCalLastDateAndTime()
    {
        var request = new GetExtCalLastDateAndTimeRequest
        {
            Vi = Vi,
        };
        var response = _client.GetExtCalLastDateAndTime(request);
        Check(response.Status);
        return (response.Year, response.Month, response.Day, response.Hour, response.Minute);
    }

    /// <summary>Calls the NI-DCPower <c>GetExtCalLastTemp</c> RPC.</summary>
    public double GetExtCalLastTemp()
    {
        var request = new GetExtCalLastTempRequest
        {
            Vi = Vi,
        };
        var response = _client.GetExtCalLastTemp(request);
        Check(response.Status);
        return response.Temperature;
    }

    /// <summary>Calls the NI-DCPower <c>GetExtCalRecommendedInterval</c> RPC.</summary>
    public int GetExtCalRecommendedInterval()
    {
        var request = new GetExtCalRecommendedIntervalRequest
        {
            Vi = Vi,
        };
        var response = _client.GetExtCalRecommendedInterval(request);
        Check(response.Status);
        return response.Months;
    }

    /// <summary>Calls the NI-DCPower <c>GetLCRCompensationLastDateAndTime</c> RPC.</summary>
    public (int Year, int Month, int Day, int Hour, int Minute) GetLCRCompensationLastDateAndTime(string channelName, LCRCompensationType compensationType)
    {
        var request = new GetLCRCompensationLastDateAndTimeRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            CompensationType = compensationType,
        };
        var response = _client.GetLCRCompensationLastDateAndTime(request);
        Check(response.Status);
        return (response.Year, response.Month, response.Day, response.Hour, response.Minute);
    }

    /// <summary>Calls the NI-DCPower <c>GetLCRCompensationLastDateAndTime</c> RPC (raw integer value overload).</summary>
    public (int Year, int Month, int Day, int Hour, int Minute) GetLCRCompensationLastDateAndTime(string channelName, int compensationTypeRaw)
    {
        var request = new GetLCRCompensationLastDateAndTimeRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            CompensationTypeRaw = compensationTypeRaw,
        };
        var response = _client.GetLCRCompensationLastDateAndTime(request);
        Check(response.Status);
        return (response.Year, response.Month, response.Day, response.Hour, response.Minute);
    }

    /// <summary>Calls the NI-DCPower <c>GetLCRCustomCableCompensationData</c> RPC.</summary>
    public ByteString GetLCRCustomCableCompensationData(string channelName)
    {
        var request = new GetLCRCustomCableCompensationDataRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.GetLCRCustomCableCompensationData(request);
        Check(response.Status);
        return response.CustomCableCompensationData;
    }

    /// <summary>Calls the NI-DCPower <c>GetNextCoercionRecord</c> RPC.</summary>
    public string GetNextCoercionRecord()
    {
        var request = new GetNextCoercionRecordRequest
        {
            Vi = Vi,
        };
        var response = _client.GetNextCoercionRecord(request);
        Check(response.Status);
        return response.CoercionRecord;
    }

    /// <summary>Calls the NI-DCPower <c>GetNextInterchangeWarning</c> RPC.</summary>
    public string GetNextInterchangeWarning()
    {
        var request = new GetNextInterchangeWarningRequest
        {
            Vi = Vi,
        };
        var response = _client.GetNextInterchangeWarning(request);
        Check(response.Status);
        return response.InterchangeWarning;
    }

    /// <summary>Calls the NI-DCPower <c>GetSelfCalLastDateAndTime</c> RPC.</summary>
    public (int Year, int Month, int Day, int Hour, int Minute) GetSelfCalLastDateAndTime()
    {
        var request = new GetSelfCalLastDateAndTimeRequest
        {
            Vi = Vi,
        };
        var response = _client.GetSelfCalLastDateAndTime(request);
        Check(response.Status);
        return (response.Year, response.Month, response.Day, response.Hour, response.Minute);
    }

    /// <summary>Calls the NI-DCPower <c>GetSelfCalLastTemp</c> RPC.</summary>
    public double GetSelfCalLastTemp()
    {
        var request = new GetSelfCalLastTempRequest
        {
            Vi = Vi,
        };
        var response = _client.GetSelfCalLastTemp(request);
        Check(response.Status);
        return response.Temperature;
    }

    /// <summary>Calls the NI-DCPower <c>ImportAttributeConfigurationBuffer</c> RPC.</summary>
    public void ImportAttributeConfigurationBuffer(ByteString configuration)
    {
        var request = new ImportAttributeConfigurationBufferRequest
        {
            Vi = Vi,
            Configuration = configuration,
        };
        var response = _client.ImportAttributeConfigurationBuffer(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ImportAttributeConfigurationFile</c> RPC.</summary>
    public void ImportAttributeConfigurationFile(string filePath)
    {
        var request = new ImportAttributeConfigurationFileRequest
        {
            Vi = Vi,
            FilePath = filePath,
        };
        var response = _client.ImportAttributeConfigurationFile(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>InitializeWithChannels</c> RPC and returns the new session.</summary>
    public static DCPower InitializeWithChannels(string serverAddress, string sessionName, string resourceName, string channels, bool reset, string optionString, SessionInitializationBehavior initializationBehavior) =>
        InitializeWithChannels(GrpcChannel.ForAddress(serverAddress), sessionName, resourceName, channels, reset, optionString, initializationBehavior, ownsChannel: true);

    /// <summary>Calls the NI-DCPower <c>InitializeWithChannels</c> RPC on an existing channel and returns the new session.</summary>
    public static DCPower InitializeWithChannels(GrpcChannel channel, string sessionName, string resourceName, string channels, bool reset, string optionString, SessionInitializationBehavior initializationBehavior, bool ownsChannel = false)
    {
        var dcPower = new DCPower(channel, resourceName, ownsChannel);
        try
        {
            var request = new InitializeWithChannelsRequest
            {
                SessionName = sessionName,
                ResourceName = resourceName,
                Channels = channels,
                Reset = reset,
                OptionString = optionString,
                InitializationBehavior = initializationBehavior,
            };
            var response = dcPower._client.InitializeWithChannels(request);
            dcPower._vi = response.Vi;
            dcPower.Check(response.Status);
            return dcPower;
        }
        catch
        {
            dcPower._ownedChannel?.Dispose();
            throw;
        }
    }

    /// <summary>Calls the NI-DCPower <c>Initiate</c> RPC.</summary>
    public void Initiate()
    {
        var request = new InitiateRequest
        {
            Vi = Vi,
        };
        var response = _client.Initiate(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>InitiateWithChannels</c> RPC.</summary>
    public void InitiateWithChannels(string channelName)
    {
        var request = new InitiateWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.InitiateWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>InvalidateAllAttributes</c> RPC.</summary>
    public void InvalidateAllAttributes()
    {
        var request = new InvalidateAllAttributesRequest
        {
            Vi = Vi,
        };
        var response = _client.InvalidateAllAttributes(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>Measure</c> RPC.</summary>
    public double Measure(string channelName, MeasurementTypes measurementType)
    {
        var request = new MeasureRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            MeasurementType = measurementType,
        };
        var response = _client.Measure(request);
        Check(response.Status);
        return response.Measurement;
    }

    /// <summary>Calls the NI-DCPower <c>Measure</c> RPC (raw integer value overload).</summary>
    public double Measure(string channelName, int measurementTypeRaw)
    {
        var request = new MeasureRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            MeasurementTypeRaw = measurementTypeRaw,
        };
        var response = _client.Measure(request);
        Check(response.Status);
        return response.Measurement;
    }

    /// <summary>Calls the NI-DCPower <c>MeasureMultiple</c> RPC.</summary>
    public (IReadOnlyList<double> VoltageMeasurements, IReadOnlyList<double> CurrentMeasurements) MeasureMultiple(string channelName)
    {
        var request = new MeasureMultipleRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.MeasureMultiple(request);
        Check(response.Status);
        return (response.VoltageMeasurements, response.CurrentMeasurements);
    }

    /// <summary>Calls the NI-DCPower <c>MeasureMultipleLCR</c> RPC.</summary>
    public IReadOnlyList<NILCRMeasurement> MeasureMultipleLCR(string channelName)
    {
        var request = new MeasureMultipleLCRRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.MeasureMultipleLCR(request);
        Check(response.Status);
        return response.Measurements;
    }

    /// <summary>Calls the NI-DCPower <c>PerformLCRLoadCompensation</c> RPC.</summary>
    public void PerformLCRLoadCompensation(string channelName, IEnumerable<NILCRLoadCompensationSpot> compensationSpots)
    {
        var request = new PerformLCRLoadCompensationRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        request.CompensationSpots.AddRange(compensationSpots);
        var response = _client.PerformLCRLoadCompensation(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>PerformLCROpenCompensation</c> RPC.</summary>
    public void PerformLCROpenCompensation(string channelName, IEnumerable<double> additionalFrequencies)
    {
        var request = new PerformLCROpenCompensationRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        request.AdditionalFrequencies.AddRange(additionalFrequencies);
        var response = _client.PerformLCROpenCompensation(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>PerformLCROpenCustomCableCompensation</c> RPC.</summary>
    public void PerformLCROpenCustomCableCompensation(string channelName)
    {
        var request = new PerformLCROpenCustomCableCompensationRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.PerformLCROpenCustomCableCompensation(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>PerformLCRShortCompensation</c> RPC.</summary>
    public void PerformLCRShortCompensation(string channelName, IEnumerable<double> additionalFrequencies)
    {
        var request = new PerformLCRShortCompensationRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        request.AdditionalFrequencies.AddRange(additionalFrequencies);
        var response = _client.PerformLCRShortCompensation(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>PerformLCRShortCustomCableCompensation</c> RPC.</summary>
    public void PerformLCRShortCustomCableCompensation(string channelName)
    {
        var request = new PerformLCRShortCustomCableCompensationRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.PerformLCRShortCustomCableCompensation(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>QueryInCompliance</c> RPC.</summary>
    public bool QueryInCompliance(string channelName)
    {
        var request = new QueryInComplianceRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.QueryInCompliance(request);
        Check(response.Status);
        return response.InCompliance;
    }

    /// <summary>Calls the NI-DCPower <c>QueryLatchedOutputCutoffState</c> RPC.</summary>
    public bool QueryLatchedOutputCutoffState(string channelName, OutputCutoffReason outputCutoffReason)
    {
        var request = new QueryLatchedOutputCutoffStateRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            OutputCutoffReason = outputCutoffReason,
        };
        var response = _client.QueryLatchedOutputCutoffState(request);
        Check(response.Status);
        return response.OutputCutoffState;
    }

    /// <summary>Calls the NI-DCPower <c>QueryLatchedOutputCutoffState</c> RPC (raw integer value overload).</summary>
    public bool QueryLatchedOutputCutoffState(string channelName, int outputCutoffReasonRaw)
    {
        var request = new QueryLatchedOutputCutoffStateRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            OutputCutoffReasonRaw = outputCutoffReasonRaw,
        };
        var response = _client.QueryLatchedOutputCutoffState(request);
        Check(response.Status);
        return response.OutputCutoffState;
    }

    /// <summary>Calls the NI-DCPower <c>QueryMaxCurrentLimit</c> RPC.</summary>
    public double QueryMaxCurrentLimit(string channelName, double voltageLevel)
    {
        var request = new QueryMaxCurrentLimitRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            VoltageLevel = voltageLevel,
        };
        var response = _client.QueryMaxCurrentLimit(request);
        Check(response.Status);
        return response.MaxCurrentLimit;
    }

    /// <summary>Calls the NI-DCPower <c>QueryMaxVoltageLevel</c> RPC.</summary>
    public double QueryMaxVoltageLevel(string channelName, double currentLimit)
    {
        var request = new QueryMaxVoltageLevelRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            CurrentLimit = currentLimit,
        };
        var response = _client.QueryMaxVoltageLevel(request);
        Check(response.Status);
        return response.MaxVoltageLevel;
    }

    /// <summary>Calls the NI-DCPower <c>QueryMinCurrentLimit</c> RPC.</summary>
    public double QueryMinCurrentLimit(string channelName, double voltageLevel)
    {
        var request = new QueryMinCurrentLimitRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            VoltageLevel = voltageLevel,
        };
        var response = _client.QueryMinCurrentLimit(request);
        Check(response.Status);
        return response.MinCurrentLimit;
    }

    /// <summary>Calls the NI-DCPower <c>QueryOutputState</c> RPC.</summary>
    public bool QueryOutputState(string channelName, OutputStates outputState)
    {
        var request = new QueryOutputStateRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            OutputState = outputState,
        };
        var response = _client.QueryOutputState(request);
        Check(response.Status);
        return response.InState;
    }

    /// <summary>Calls the NI-DCPower <c>QueryOutputState</c> RPC (raw integer value overload).</summary>
    public bool QueryOutputState(string channelName, int outputStateRaw)
    {
        var request = new QueryOutputStateRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            OutputStateRaw = outputStateRaw,
        };
        var response = _client.QueryOutputState(request);
        Check(response.Status);
        return response.InState;
    }

    /// <summary>Calls the NI-DCPower <c>ReadCurrentTemperature</c> RPC.</summary>
    public double ReadCurrentTemperature()
    {
        var request = new ReadCurrentTemperatureRequest
        {
            Vi = Vi,
        };
        var response = _client.ReadCurrentTemperature(request);
        Check(response.Status);
        return response.Temperature;
    }

    /// <summary>Calls the NI-DCPower <c>Reset</c> RPC.</summary>
    public void Reset()
    {
        var request = new ResetRequest
        {
            Vi = Vi,
        };
        var response = _client.Reset(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ResetDevice</c> RPC.</summary>
    public void ResetDevice()
    {
        var request = new ResetDeviceRequest
        {
            Vi = Vi,
        };
        var response = _client.ResetDevice(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ResetInterchangeCheck</c> RPC.</summary>
    public void ResetInterchangeCheck()
    {
        var request = new ResetInterchangeCheckRequest
        {
            Vi = Vi,
        };
        var response = _client.ResetInterchangeCheck(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ResetWithChannels</c> RPC.</summary>
    public void ResetWithChannels(string channelName)
    {
        var request = new ResetWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        var response = _client.ResetWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>ResetWithDefaults</c> RPC.</summary>
    public void ResetWithDefaults()
    {
        var request = new ResetWithDefaultsRequest
        {
            Vi = Vi,
        };
        var response = _client.ResetWithDefaults(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>RevisionQuery</c> RPC.</summary>
    public (string InstrumentDriverRevision, string FirmwareRevision) RevisionQuery()
    {
        var request = new RevisionQueryRequest
        {
            Vi = Vi,
        };
        var response = _client.RevisionQuery(request);
        Check(response.Status);
        return (response.InstrumentDriverRevision, response.FirmwareRevision);
    }

    /// <summary>Calls the NI-DCPower <c>SelfTest</c> RPC.</summary>
    public (int SelfTestResult, string SelfTestMessage) SelfTest()
    {
        var request = new SelfTestRequest
        {
            Vi = Vi,
        };
        var response = _client.SelfTest(request);
        Check(response.Status);
        return (response.SelfTestResult, response.SelfTestMessage);
    }

    /// <summary>Calls the NI-DCPower <c>SendSoftwareEdgeTrigger</c> RPC.</summary>
    public void SendSoftwareEdgeTrigger(SendSoftwareEdgeTriggerType trigger)
    {
        var request = new SendSoftwareEdgeTriggerRequest
        {
            Vi = Vi,
            Trigger = trigger,
        };
        var response = _client.SendSoftwareEdgeTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>SendSoftwareEdgeTrigger</c> RPC (raw integer value overload).</summary>
    public void SendSoftwareEdgeTrigger(int triggerRaw)
    {
        var request = new SendSoftwareEdgeTriggerRequest
        {
            Vi = Vi,
            TriggerRaw = triggerRaw,
        };
        var response = _client.SendSoftwareEdgeTrigger(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>SendSoftwareEdgeTriggerWithChannels</c> RPC.</summary>
    public void SendSoftwareEdgeTriggerWithChannels(string channelName, SendSoftwareEdgeTriggerType trigger)
    {
        var request = new SendSoftwareEdgeTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            Trigger = trigger,
        };
        var response = _client.SendSoftwareEdgeTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>SendSoftwareEdgeTriggerWithChannels</c> RPC (raw integer value overload).</summary>
    public void SendSoftwareEdgeTriggerWithChannels(string channelName, int triggerRaw)
    {
        var request = new SendSoftwareEdgeTriggerWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            TriggerRaw = triggerRaw,
        };
        var response = _client.SendSoftwareEdgeTriggerWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>SetAttributeViBoolean</c> RPC.</summary>
    public void SetAttributeViBoolean(string channelName, NiDCPowerAttribute attributeId, bool attributeValue)
    {
        var request = new SetAttributeViBooleanRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            AttributeId = attributeId,
            AttributeValue = attributeValue,
        };
        var response = _client.SetAttributeViBoolean(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>SetAttributeViInt32</c> RPC.</summary>
    public void SetAttributeViInt32(string channelName, NiDCPowerAttribute attributeId, NiDCPowerInt32AttributeValues attributeValue)
    {
        var request = new SetAttributeViInt32Request
        {
            Vi = Vi,
            ChannelName = channelName,
            AttributeId = attributeId,
            AttributeValue = attributeValue,
        };
        var response = _client.SetAttributeViInt32(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>SetAttributeViInt32</c> RPC (raw integer value overload).</summary>
    public void SetAttributeViInt32(string channelName, NiDCPowerAttribute attributeId, int attributeValueRaw)
    {
        var request = new SetAttributeViInt32Request
        {
            Vi = Vi,
            ChannelName = channelName,
            AttributeId = attributeId,
            AttributeValueRaw = attributeValueRaw,
        };
        var response = _client.SetAttributeViInt32(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>SetAttributeViInt64</c> RPC.</summary>
    public void SetAttributeViInt64(string channelName, NiDCPowerAttribute attributeId, long attributeValue)
    {
        var request = new SetAttributeViInt64Request
        {
            Vi = Vi,
            ChannelName = channelName,
            AttributeId = attributeId,
            AttributeValueRaw = attributeValue,
        };
        var response = _client.SetAttributeViInt64(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>SetAttributeViReal64</c> RPC.</summary>
    public void SetAttributeViReal64(string channelName, NiDCPowerAttribute attributeId, NiDCPowerReal64AttributeValues attributeValue)
    {
        var request = new SetAttributeViReal64Request
        {
            Vi = Vi,
            ChannelName = channelName,
            AttributeId = attributeId,
            AttributeValue = attributeValue,
        };
        var response = _client.SetAttributeViReal64(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>SetAttributeViReal64</c> RPC (raw integer value overload).</summary>
    public void SetAttributeViReal64(string channelName, NiDCPowerAttribute attributeId, int attributeValueRaw)
    {
        var request = new SetAttributeViReal64Request
        {
            Vi = Vi,
            ChannelName = channelName,
            AttributeId = attributeId,
            AttributeValueRaw = attributeValueRaw,
        };
        var response = _client.SetAttributeViReal64(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>SetAttributeViSession</c> RPC.</summary>
    public void SetAttributeViSession(string channelName, NiDCPowerAttribute attributeId, Session attributeValue)
    {
        var request = new SetAttributeViSessionRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            AttributeId = attributeId,
            AttributeValue = attributeValue,
        };
        var response = _client.SetAttributeViSession(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>SetAttributeViString</c> RPC.</summary>
    public void SetAttributeViString(string channelName, NiDCPowerAttribute attributeId, string attributeValue)
    {
        var request = new SetAttributeViStringRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            AttributeId = attributeId,
            AttributeValueRaw = attributeValue,
        };
        var response = _client.SetAttributeViString(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>SetSequence</c> RPC.</summary>
    public void SetSequence(string channelName, IEnumerable<double> values, IEnumerable<double> sourceDelays)
    {
        var request = new SetSequenceRequest
        {
            Vi = Vi,
            ChannelName = channelName,
        };
        request.Values.AddRange(values);
        request.SourceDelays.AddRange(sourceDelays);
        var response = _client.SetSequence(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>WaitForEvent</c> RPC.</summary>
    public void WaitForEvent(Event eventId, double timeout)
    {
        var request = new WaitForEventRequest
        {
            Vi = Vi,
            EventId = eventId,
            Timeout = timeout,
        };
        var response = _client.WaitForEvent(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>WaitForEvent</c> RPC (raw integer value overload).</summary>
    public void WaitForEvent(int eventIdRaw, double timeout)
    {
        var request = new WaitForEventRequest
        {
            Vi = Vi,
            EventIdRaw = eventIdRaw,
            Timeout = timeout,
        };
        var response = _client.WaitForEvent(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>WaitForEventWithChannels</c> RPC.</summary>
    public void WaitForEventWithChannels(string channelName, Event eventId, double timeout)
    {
        var request = new WaitForEventWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            EventId = eventId,
            Timeout = timeout,
        };
        var response = _client.WaitForEventWithChannels(request);
        Check(response.Status);
    }

    /// <summary>Calls the NI-DCPower <c>WaitForEventWithChannels</c> RPC (raw integer value overload).</summary>
    public void WaitForEventWithChannels(string channelName, int eventIdRaw, double timeout)
    {
        var request = new WaitForEventWithChannelsRequest
        {
            Vi = Vi,
            ChannelName = channelName,
            EventIdRaw = eventIdRaw,
            Timeout = timeout,
        };
        var response = _client.WaitForEventWithChannels(request);
        Check(response.Status);
    }

    #endregion
}
