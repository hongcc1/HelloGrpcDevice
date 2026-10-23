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
    // Documentation text is adapted from the NI-DCPower Python API (nimi-python, MIT License,
    // Copyright (c) 2017-2022 National Instruments Corporation); see tools/NOTICE-nimi-python.txt.

    /// <summary>Transitions the specified channel(s) from the Running state to the Uncommitted state. If a
    ///   sequence is running, it is stopped. Any configuration functions called after this function are not
    ///   applied until the <c>InitiateWithChannels</c> function is called. If power output is enabled when
    ///   you call the <c>AbortWithChannels</c> function, the channels remain in their current state and
    ///   continue providing power.</summary>
    /// <remarks>
    /// <para>Use the <c>ConfigureOutputEnabled</c> function to disable power output on a per channel basis.
    ///   Use the <c>reset</c> function to disable output on all channels.</para>
    /// <para>Refer to the Programming States topic in the <i>NI DC Power Supplies and SMUs Help</i> for
    ///   information about the specific NI-DCPower software states.</para>
    /// <para><b>Note:</b></para>
    /// <para>NI-DCPower uses the terms "source" and "output". However, while sinking with electronic loads
    ///   and SMUs these correspond to "sinking" and "input", respectively.</para>
    /// <para>NI-DCPower Python API: <c>abort</c>.</para>
    /// </remarks>
    public void Abort()
    {
        var request = new AbortRequest
        {
            Vi = Vi,
        };
        var response = _client.Abort(request);
        Check(response.Status);
    }

    /// <summary>Transitions the specified channel(s) from the Running state to the Uncommitted state. If a
    ///   sequence is running, it is stopped. Any configuration functions called after this function are not
    ///   applied until the <c>InitiateWithChannels</c> function is called. If power output is enabled when
    ///   you call the <c>AbortWithChannels</c> function, the channels remain in their current state and
    ///   continue providing power.</summary>
    /// <remarks>
    /// <para>Use the <c>ConfigureOutputEnabled</c> function to disable power output on a per channel basis.
    ///   Use the <c>reset</c> function to disable output on all channels.</para>
    /// <para>Refer to the Programming States topic in the <i>NI DC Power Supplies and SMUs Help</i> for
    ///   information about the specific NI-DCPower software states.</para>
    /// <para><b>Note:</b></para>
    /// <para>NI-DCPower uses the terms "source" and "output". However, while sinking with electronic loads
    ///   and SMUs these correspond to "sinking" and "input", respectively.</para>
    /// <para>NI-DCPower Python API: <c>abort</c>.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Performs a self-calibration upon the specified channel(s).</summary>
    /// <remarks>
    /// <para>This function disables the output, performs several internal calculations, and updates
    ///   calibration values. The updated calibration values are written to the device hardware if the
    ///   <c>NiDCPowerAttribute.SelfCalibrationPersistence</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_WRITE_TO_EEPROM</c>. Refer to the
    ///   <c>NiDCPowerAttribute.SelfCalibrationPersistence</c> attribute topic for more information about the
    ///   settings for this attribute.</para>
    /// <para>When calling <c>CalSelfCalibrate</c> with the PXIe-4162/4163, specify all channels of your
    ///   PXIe-4162/4163 with the channelName input. You cannot self-calibrate a subset of PXIe-4162/4163
    ///   channels.</para>
    /// <para>Refer to the Self-Calibration topic for more information about this function.</para>
    /// <para><b>Note:</b></para>
    /// <para>NI-DCPower uses the terms "source" and "output". However, while sinking with electronic loads
    ///   and SMUs these correspond to "sinking" and "input", respectively.</para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>self_cal</c>.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Clears the error information for the current execution thread and the session.</summary>
    public void ClearError()
    {
        var request = new ClearErrorRequest
        {
            Vi = Vi,
        };
        var response = _client.ClearError(request);
        Check(response.Status);
    }

    /// <summary>Clears the list of current interchange warnings.</summary>
    public void ClearInterchangeWarnings()
    {
        var request = new ClearInterchangeWarningsRequest
        {
            Vi = Vi,
        };
        var response = _client.ClearInterchangeWarnings(request);
        Check(response.Status);
    }

    /// <summary>Clears the state of an output cutoff that was engaged. To clear the state for all output
    ///   cutoff reasons, use <c>NIDCPOWER_VAL_OUTPUT_CUTOFF_REASON_ALL</c>.</summary>
    /// <remarks>
    /// <para><b>Note:</b></para>
    /// <para>NI-DCPower uses the terms "source" and "output". However, while sinking with electronic loads
    ///   and SMUs these correspond to "sinking" and "input", respectively.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="outputCutoffReason">Specifies the reasons for which to clear the output cutoff
    ///   state.</param>
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

    /// <summary>Clears the state of an output cutoff that was engaged. To clear the state for all output
    ///   cutoff reasons, use <c>NIDCPOWER_VAL_OUTPUT_CUTOFF_REASON_ALL</c>.</summary>
    /// <remarks>
    /// <para><b>Note:</b></para>
    /// <para>NI-DCPower uses the terms "source" and "output". However, while sinking with electronic loads
    ///   and SMUs these correspond to "sinking" and "input", respectively.</para>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="outputCutoffReasonRaw">Specifies the reasons for which to clear the output cutoff
    ///   state.</param>
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

    /// <summary>Applies previously configured settings to the specified channel(s). Calling this function
    ///   moves the NI-DCPower session from the Uncommitted state into the Committed state. After calling this
    ///   function, modifying any attribute reverts the NI-DCPower session to the Uncommitted state. Use the
    ///   <c>InitiateWithChannels</c> function to transition to the Running state. Refer to the Programming
    ///   States topic in the <i>NI DC Power Supplies and SMUs Help</i> for details about the specific
    ///   NI-DCPower software states.</summary>
    /// <remarks>
    /// <para>NI-DCPower Python API: <c>commit</c>.</para>
    /// </remarks>
    public void Commit()
    {
        var request = new CommitRequest
        {
            Vi = Vi,
        };
        var response = _client.Commit(request);
        Check(response.Status);
    }

    /// <summary>Applies previously configured settings to the specified channel(s). Calling this function
    ///   moves the NI-DCPower session from the Uncommitted state into the Committed state. After calling this
    ///   function, modifying any attribute reverts the NI-DCPower session to the Uncommitted state. Use the
    ///   <c>InitiateWithChannels</c> function to transition to the Running state. Refer to the Programming
    ///   States topic in the <i>NI DC Power Supplies and SMUs Help</i> for details about the specific
    ///   NI-DCPower software states.</summary>
    /// <remarks>
    /// <para>NI-DCPower Python API: <c>commit</c>.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Configures the aperture time on the specified channel(s).</summary>
    /// <remarks>
    /// <para>The supported values depend on the <b>units</b>. Refer to the <i>Aperture Time</i> topic for
    ///   your device in the <i>NI DC Power Supplies and SMUs Help</i> for more information. In general,
    ///   devices support discrete <b>apertureTime</b> values, and if you configure <b>apertureTime</b> to
    ///   some unsupported value, NI-DCPower coerces it up to the next supported value.</para>
    /// <para>Refer to the <i>Measurement Configuration and Timing</i> or <i>DC Noise Rejection</i> topic
    ///   for your device in the <i>NI DC Power Supplies and SMUs Help</i> for more information about how to
    ///   configure your measurements.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="apertureTime">Specifies the aperture time. Refer to the <i>Aperture Time</i> topic for
    ///   your device in the <i>NI DC Power Supplies and SMUs Help</i> for more information.</param>
    /// <param name="units">Specifies the units for <b>apertureTime</b>. <b>Defined Values</b>:</param>
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

    /// <summary>Configures the aperture time on the specified channel(s).</summary>
    /// <remarks>
    /// <para>The supported values depend on the <b>units</b>. Refer to the <i>Aperture Time</i> topic for
    ///   your device in the <i>NI DC Power Supplies and SMUs Help</i> for more information. In general,
    ///   devices support discrete <b>apertureTime</b> values, and if you configure <b>apertureTime</b> to
    ///   some unsupported value, NI-DCPower coerces it up to the next supported value.</para>
    /// <para>Refer to the <i>Measurement Configuration and Timing</i> or <i>DC Noise Rejection</i> topic
    ///   for your device in the <i>NI DC Power Supplies and SMUs Help</i> for more information about how to
    ///   configure your measurements.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="apertureTime">Specifies the aperture time. Refer to the <i>Aperture Time</i> topic for
    ///   your device in the <i>NI DC Power Supplies and SMUs Help</i> for more information.</param>
    /// <param name="unitsRaw">Specifies the units for <b>apertureTime</b>. <b>Defined Values</b>:</param>
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

    /// <summary>Specifies the auto-zero method to use on the device. Refer to the NI PXI-4132 Measurement
    ///   Configuration and Timing and Auto Zero topics for more information about how to configure your
    ///   measurements. Default Value: The default value for the NI PXI-4132 is <c>NIDCPOWER_VAL_ON</c>. The
    ///   default value for all other devices is <c>NIDCPOWER_VAL_OFF</c>, which is the only supported value
    ///   for these devices.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the auto-zero method to use on the device. Refer to the NI PXI-4132 Measurement
    ///   Configuration and Timing and Auto Zero topics for more information about how to configure your
    ///   measurements. Default Value: The default value for the NI PXI-4132 is <c>NIDCPOWER_VAL_ON</c>. The
    ///   default value for all other devices is <c>NIDCPOWER_VAL_OFF</c>, which is the only supported value
    ///   for these devices.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the current level, in amps, that the device attempts to generate on the specified
    ///   channel(s). This attribute is applicable only if the <c>NiDCPowerAttribute.OutputFunction</c>
    ///   attribute is set to <c>NIDCPOWER_VAL_DC_CURRENT</c>.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>current_level</c>:</para>
    /// <para>Valid Values: The valid values for this attribute are defined by the values to which the
    ///   <c>NiDCPowerAttribute.CurrentLevelRange</c> attribute is set.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the current level range, in amps, for the specified channel(s). The range defines
    ///   the valid values to which you can set the current level. Use the
    ///   <c>NiDCPowerAttribute.CurrentLevelAutorange</c> attribute to enable automatic selection of the
    ///   current level range. The <c>NiDCPowerAttribute.CurrentLevelRange</c> attribute is applicable only if
    ///   the <c>NiDCPowerAttribute.OutputFunction</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_DC_CURRENT</c>.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>current_level_range</c>:</para>
    /// <para>For valid ranges, refer to the specifications for your instrument.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the current limit, in amps, that the output cannot exceed when generating the
    ///   desired voltage level on the specified channel(s). This attribute is applicable only if the
    ///   <c>NiDCPowerAttribute.OutputFunction</c> attribute is set to <c>NIDCPOWER_VAL_DC_VOLTAGE</c> and the
    ///   <c>NiDCPowerAttribute.ComplianceLimitSymmetry</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_COMPLIANCE_LIMIT_SYMMETRY_SYMMETRIC</c>.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>current_limit</c>:</para>
    /// <para>Valid Values: The valid values for this attribute are defined by the values to which
    ///   <c>NiDCPowerAttribute.CurrentLimitRange</c> attribute is set.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the current limit, in amps, that the output cannot exceed when generating the
    ///   desired voltage level on the specified channel(s). This attribute is applicable only if the
    ///   <c>NiDCPowerAttribute.OutputFunction</c> attribute is set to <c>NIDCPOWER_VAL_DC_VOLTAGE</c> and the
    ///   <c>NiDCPowerAttribute.ComplianceLimitSymmetry</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_COMPLIANCE_LIMIT_SYMMETRY_SYMMETRIC</c>.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>current_limit</c>:</para>
    /// <para>Valid Values: The valid values for this attribute are defined by the values to which
    ///   <c>NiDCPowerAttribute.CurrentLimitRange</c> attribute is set.</para>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the current limit range, in amps, for the specified channel(s). The range defines
    ///   the valid values to which you can set the current limit. Use the
    ///   <c>NiDCPowerAttribute.CurrentLimitAutorange</c> attribute to enable automatic selection of the
    ///   current limit range. The <c>NiDCPowerAttribute.CurrentLimitRange</c> attribute is applicable only if
    ///   the <c>NiDCPowerAttribute.OutputFunction</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_DC_VOLTAGE</c>.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>current_limit_range</c>:</para>
    /// <para>For valid ranges, refer to the specifications for your instrument.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the input terminal for the Measure trigger. This attribute is used only when the
    ///   <c>NiDCPowerAttribute.MeasureTriggerType</c> attribute is set to <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>.
    ///   for this attribute. You can specify any valid input terminal for this attribute. Valid terminals are
    ///   listed in Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be
    ///   specified in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can
    ///   specify the terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened
    ///   terminal name, PXI_Trig0. The input terminal can also be a terminal from another device. For
    ///   example, you can set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
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

    /// <summary>Specifies the input terminal for the Measure trigger. This attribute is used only when the
    ///   <c>NiDCPowerAttribute.MeasureTriggerType</c> attribute is set to <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>.
    ///   for this attribute. You can specify any valid input terminal for this attribute. Valid terminals are
    ///   listed in Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be
    ///   specified in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can
    ///   specify the terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened
    ///   terminal name, PXI_Trig0. The input terminal can also be a terminal from another device. For
    ///   example, you can set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
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

    /// <summary>Specifies the input terminal for the Measure trigger. This attribute is used only when the
    ///   <c>NiDCPowerAttribute.MeasureTriggerType</c> attribute is set to <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>.
    ///   for this attribute. You can specify any valid input terminal for this attribute. Valid terminals are
    ///   listed in Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be
    ///   specified in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can
    ///   specify the terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened
    ///   terminal name, PXI_Trig0. The input terminal can also be a terminal from another device. For
    ///   example, you can set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the input terminal for the Measure trigger. This attribute is used only when the
    ///   <c>NiDCPowerAttribute.MeasureTriggerType</c> attribute is set to <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>.
    ///   for this attribute. You can specify any valid input terminal for this attribute. Valid terminals are
    ///   listed in Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be
    ///   specified in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can
    ///   specify the terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened
    ///   terminal name, PXI_Trig0. The input terminal can also be a terminal from another device. For
    ///   example, you can set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the input terminal for the Pulse trigger. This attribute is used only when the
    ///   <c>NiDCPowerAttribute.PulseTriggerType</c> attribute is set to digital edge. You can specify any
    ///   valid input terminal for this attribute. Valid terminals are listed in Measurement &amp; Automation
    ///   Explorer under the Device Routes tab. Input terminals can be specified in one of two ways. If the
    ///   device is named Dev1 and your terminal is PXI_Trig0, you can specify the terminal with the fully
    ///   qualified terminal name, /Dev1/PXI_Trig0, or with the shortened terminal name, PXI_Trig0. The input
    ///   terminal can also be a terminal from another device. For example, you can set the input terminal on
    ///   Dev1 to be /Dev2/SourceCompleteEvent.</summary>
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

    /// <summary>Specifies the input terminal for the Pulse trigger. This attribute is used only when the
    ///   <c>NiDCPowerAttribute.PulseTriggerType</c> attribute is set to digital edge. You can specify any
    ///   valid input terminal for this attribute. Valid terminals are listed in Measurement &amp; Automation
    ///   Explorer under the Device Routes tab. Input terminals can be specified in one of two ways. If the
    ///   device is named Dev1 and your terminal is PXI_Trig0, you can specify the terminal with the fully
    ///   qualified terminal name, /Dev1/PXI_Trig0, or with the shortened terminal name, PXI_Trig0. The input
    ///   terminal can also be a terminal from another device. For example, you can set the input terminal on
    ///   Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
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

    /// <summary>Specifies the input terminal for the Pulse trigger. This attribute is used only when the
    ///   <c>NiDCPowerAttribute.PulseTriggerType</c> attribute is set to digital edge. You can specify any
    ///   valid input terminal for this attribute. Valid terminals are listed in Measurement &amp; Automation
    ///   Explorer under the Device Routes tab. Input terminals can be specified in one of two ways. If the
    ///   device is named Dev1 and your terminal is PXI_Trig0, you can specify the terminal with the fully
    ///   qualified terminal name, /Dev1/PXI_Trig0, or with the shortened terminal name, PXI_Trig0. The input
    ///   terminal can also be a terminal from another device. For example, you can set the input terminal on
    ///   Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the input terminal for the Pulse trigger. This attribute is used only when the
    ///   <c>NiDCPowerAttribute.PulseTriggerType</c> attribute is set to digital edge. You can specify any
    ///   valid input terminal for this attribute. Valid terminals are listed in Measurement &amp; Automation
    ///   Explorer under the Device Routes tab. Input terminals can be specified in one of two ways. If the
    ///   device is named Dev1 and your terminal is PXI_Trig0, you can specify the terminal with the fully
    ///   qualified terminal name, /Dev1/PXI_Trig0, or with the shortened terminal name, PXI_Trig0. The input
    ///   terminal can also be a terminal from another device. For example, you can set the input terminal on
    ///   Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the input terminal for the Sequence Advance trigger. Use this attribute only when
    ///   the <c>NiDCPowerAttribute.SequenceAdvanceTriggerType</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>. the NI DC Power Supplies and SMUs Help for information about
    ///   supported devices. You can specify any valid input terminal for this attribute. Valid terminals are
    ///   listed in Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be
    ///   specified in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can
    ///   specify the terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened
    ///   terminal name, PXI_Trig0. The input terminal can also be a terminal from another device. For
    ///   example, you can set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
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

    /// <summary>Specifies the input terminal for the Sequence Advance trigger. Use this attribute only when
    ///   the <c>NiDCPowerAttribute.SequenceAdvanceTriggerType</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>. the NI DC Power Supplies and SMUs Help for information about
    ///   supported devices. You can specify any valid input terminal for this attribute. Valid terminals are
    ///   listed in Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be
    ///   specified in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can
    ///   specify the terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened
    ///   terminal name, PXI_Trig0. The input terminal can also be a terminal from another device. For
    ///   example, you can set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
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

    /// <summary>Specifies the input terminal for the Sequence Advance trigger. Use this attribute only when
    ///   the <c>NiDCPowerAttribute.SequenceAdvanceTriggerType</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>. the NI DC Power Supplies and SMUs Help for information about
    ///   supported devices. You can specify any valid input terminal for this attribute. Valid terminals are
    ///   listed in Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be
    ///   specified in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can
    ///   specify the terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened
    ///   terminal name, PXI_Trig0. The input terminal can also be a terminal from another device. For
    ///   example, you can set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the input terminal for the Sequence Advance trigger. Use this attribute only when
    ///   the <c>NiDCPowerAttribute.SequenceAdvanceTriggerType</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>. the NI DC Power Supplies and SMUs Help for information about
    ///   supported devices. You can specify any valid input terminal for this attribute. Valid terminals are
    ///   listed in Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be
    ///   specified in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can
    ///   specify the terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened
    ///   terminal name, PXI_Trig0. The input terminal can also be a terminal from another device. For
    ///   example, you can set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the input terminal for the Shutdown trigger. This attribute is used only when the
    ///   <c>NiDCPowerAttribute.ShutdownTriggerType</c> attribute is set to digital edge. You can specify any
    ///   valid input terminal for this attribute. Valid terminals are listed in Measurement &amp; Automation
    ///   Explorer under the Device Routes tab. Input terminals can be specified in one of two ways. If the
    ///   device is named Dev1 and your terminal is PXI_Trig0, you can specify the terminal with the fully
    ///   qualified terminal name, /Dev1/PXI_Trig0, or with the shortened terminal name, PXI_Trig0. The input
    ///   terminal can also be a terminal from another device. For example, you can set the input terminal on
    ///   Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the input terminal for the Shutdown trigger. This attribute is used only when the
    ///   <c>NiDCPowerAttribute.ShutdownTriggerType</c> attribute is set to digital edge. You can specify any
    ///   valid input terminal for this attribute. Valid terminals are listed in Measurement &amp; Automation
    ///   Explorer under the Device Routes tab. Input terminals can be specified in one of two ways. If the
    ///   device is named Dev1 and your terminal is PXI_Trig0, you can specify the terminal with the fully
    ///   qualified terminal name, /Dev1/PXI_Trig0, or with the shortened terminal name, PXI_Trig0. The input
    ///   terminal can also be a terminal from another device. For example, you can set the input terminal on
    ///   Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the input terminal for the Source trigger. Use this attribute only when the
    ///   <c>NiDCPowerAttribute.SourceTriggerType</c> attribute is set to <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>.
    ///   You can specify any valid input terminal for this attribute. Valid terminals are listed in
    ///   Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be specified
    ///   in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can specify the
    ///   terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened terminal
    ///   name, PXI_Trig0. The input terminal can also be a terminal from another device. For example, you can
    ///   set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
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

    /// <summary>Specifies the input terminal for the Source trigger. Use this attribute only when the
    ///   <c>NiDCPowerAttribute.SourceTriggerType</c> attribute is set to <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>.
    ///   You can specify any valid input terminal for this attribute. Valid terminals are listed in
    ///   Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be specified
    ///   in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can specify the
    ///   terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened terminal
    ///   name, PXI_Trig0. The input terminal can also be a terminal from another device. For example, you can
    ///   set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
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

    /// <summary>Specifies the input terminal for the Source trigger. Use this attribute only when the
    ///   <c>NiDCPowerAttribute.SourceTriggerType</c> attribute is set to <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>.
    ///   You can specify any valid input terminal for this attribute. Valid terminals are listed in
    ///   Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be specified
    ///   in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can specify the
    ///   terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened terminal
    ///   name, PXI_Trig0. The input terminal can also be a terminal from another device. For example, you can
    ///   set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the input terminal for the Source trigger. Use this attribute only when the
    ///   <c>NiDCPowerAttribute.SourceTriggerType</c> attribute is set to <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>.
    ///   You can specify any valid input terminal for this attribute. Valid terminals are listed in
    ///   Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be specified
    ///   in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can specify the
    ///   terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened terminal
    ///   name, PXI_Trig0. The input terminal can also be a terminal from another device. For example, you can
    ///   set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the input terminal for the Start trigger. Use this attribute only when the
    ///   <c>NiDCPowerAttribute.StartTriggerType</c> attribute is set to <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>.
    ///   You can specify any valid input terminal for this attribute. Valid terminals are listed in
    ///   Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be specified
    ///   in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can specify the
    ///   terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened terminal
    ///   name, PXI_Trig0. The input terminal can also be a terminal from another device. For example, you can
    ///   set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
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

    /// <summary>Specifies the input terminal for the Start trigger. Use this attribute only when the
    ///   <c>NiDCPowerAttribute.StartTriggerType</c> attribute is set to <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>.
    ///   You can specify any valid input terminal for this attribute. Valid terminals are listed in
    ///   Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be specified
    ///   in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can specify the
    ///   terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened terminal
    ///   name, PXI_Trig0. The input terminal can also be a terminal from another device. For example, you can
    ///   set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
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

    /// <summary>Specifies the input terminal for the Start trigger. Use this attribute only when the
    ///   <c>NiDCPowerAttribute.StartTriggerType</c> attribute is set to <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>.
    ///   You can specify any valid input terminal for this attribute. Valid terminals are listed in
    ///   Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be specified
    ///   in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can specify the
    ///   terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened terminal
    ///   name, PXI_Trig0. The input terminal can also be a terminal from another device. For example, you can
    ///   set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the input terminal for the Start trigger. Use this attribute only when the
    ///   <c>NiDCPowerAttribute.StartTriggerType</c> attribute is set to <c>NIDCPOWER_VAL_DIGITAL_EDGE</c>.
    ///   You can specify any valid input terminal for this attribute. Valid terminals are listed in
    ///   Measurement &amp; Automation Explorer under the Device Routes tab. Input terminals can be specified
    ///   in one of two ways. If the device is named Dev1 and your terminal is PXI_Trig0, you can specify the
    ///   terminal with the fully qualified terminal name, /Dev1/PXI_Trig0, or with the shortened terminal
    ///   name, PXI_Trig0. The input terminal can also be a terminal from another device. For example, you can
    ///   set the input terminal on Dev1 to be /Dev2/SourceCompleteEvent.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>This function is deprecated. Use <c>ConfigureLCRCompensation</c> instead.</summary>
    /// <remarks>
    /// <para>Applies previously generated open and short custom cable compensation data to LCR
    ///   measurements.</para>
    /// <para>This function applies custom cable compensation data when you have set
    ///   <c>NiDCPowerAttribute.CableLength</c> property to <c>NIDCPOWER_VAL_CUSTOM_AS_CONFIGURED</c>.</para>
    /// <para>Call this function after you have obtained custom cable compensation data.</para>
    /// <para>If <c>NiDCPowerAttribute.LcrShortCustomCableCompensationEnabled</c> property is set to
    ///   VI_TRUE, you must generate data with both <c>PerformLCROpenCustomCableCompensation</c> and
    ///   <c>PerformLCRShortCustomCableCompensation</c>; if VI_FALSE, you must only use
    ///   <c>PerformLCROpenCustomCableCompensation</c>, and NI-DCPower uses default short data.</para>
    /// <para>Call <c>GetLCRCustomCableCompensationData</c> and pass the <b>custom cable compensation
    ///   data</b> to this function.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// </remarks>
    /// <param name="customCableCompensationData">The open and short custom cable compensation data to
    ///   apply.</param>
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

    /// <summary>Specifies whether the output is enabled (VI_TRUE) or disabled (VI_FALSE).</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>output_enabled</c>:</para>
    /// <para>Depending on the value you specify for the <c>NiDCPowerAttribute.OutputFunction</c> property,
    ///   you also must set the corresponding level properties or <c>NiDCPowerAttribute.OutputResistance</c>
    ///   in addition to enabling the output to generate the desired level. For more information about
    ///   configuring the output level, refer to the <c>NiDCPowerAttribute.OutputFunction</c> property.</para>
    /// <para>Default Value: The default value is VI_TRUE if you use the
    ///   <c>InitializeWithIndependentChannels</c> function to open the session. Otherwise the default value
    ///   is VI_FALSE, including when you use a calibration session or the deprecated programming
    ///   model.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Configures the function to generate on the specified channel(s).</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>output_function</c>:</para>
    /// <para>When <c>NIDCPOWER_VAL_DC_VOLTAGE</c> is selected, the device generates the desired voltage
    ///   level on the output as long as the output current is below the current limit. Use the following
    ///   attributes to configure the channel when <c>NIDCPOWER_VAL_DC_VOLTAGE</c> is selected:</para>
    /// <list type="bullet">
    ///   <item><description><c>NiDCPowerAttribute.VoltageLevel</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.CurrentLimit</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.CurrentLimitHigh</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.CurrentLimitLow</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.VoltageLevelRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.CurrentLimitRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.ComplianceLimitSymmetry</c></description></item>
    /// </list>
    /// <para>When <c>NIDCPOWER_VAL_DC_CURRENT</c> is selected, the device generates the desired current
    ///   level on the output as long as the output voltage is below the voltage limit. Use the following
    ///   attributes to configure the channel when <c>NIDCPOWER_VAL_DC_CURRENT</c> is selected:</para>
    /// <list type="bullet">
    ///   <item><description><c>NiDCPowerAttribute.CurrentLevel</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.VoltageLimit</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.VoltageLimitHigh</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.VoltageLimitLow</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.CurrentLevelRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.VoltageLimitRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.ComplianceLimitSymmetry</c></description></item>
    /// </list>
    /// <para>When <c>NIDCPOWER_VAL_PULSE_VOLTAGE</c> is selected, the device generates pulses at the
    ///   desired pulse voltage levels on the output as long as the output current is below the pulse current
    ///   limit. Use the following attributes to configure the channel when <c>NIDCPOWER_VAL_PULSE_VOLTAGE</c>
    ///   is selected:</para>
    /// <list type="bullet">
    ///   <item><description><c>NiDCPowerAttribute.PulseVoltageLevel</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasVoltageLevel</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseCurrentLimit</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseCurrentLimitHigh</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseCurrentLimitLow</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasCurrentLimit</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasCurrentLimitHigh</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasCurrentLimitLow</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseVoltageLevelRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseCurrentLimitRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.ComplianceLimitSymmetry</c></description></item>
    /// </list>
    /// <para>When <c>NIDCPOWER_VAL_PULSE_CURRENT</c> is selected, the device generates pulses at the
    ///   desired pulse current levels on the output as long as the output voltage is below the pulse voltage
    ///   limit. Use the following attributes to configure the channel when <c>NIDCPOWER_VAL_PULSE_CURRENT</c>
    ///   is selected:</para>
    /// <list type="bullet">
    ///   <item><description><c>NiDCPowerAttribute.PulseCurrentLevel</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasCurrentLevel</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseVoltageLimit</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseVoltageLimitHigh</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseVoltageLimitLow</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasVoltageLimit</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasVoltageLimitHigh</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasVoltageLimitLow</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseCurrentLevelRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseVoltageLimitRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.ComplianceLimitSymmetry</c></description></item>
    /// </list>
    /// <para>When <c>NIDCPOWER_VAL_CONSTANT_RESISTANCE</c> is selected, the device generates the desired
    ///   resistance level on the output as long as the output current is below the current limit. Use the
    ///   following attributes to configure the channel when <c>NIDCPOWER_VAL_CONSTANT_RESISTANCE</c> is
    ///   selected:</para>
    /// <list type="bullet">
    ///   <item><description><c>NIDCPOWER_ATTR_CONSTANT_RESISTANCE_LEVEL</c></description></item>
    ///   <item><description><c>NIDCPOWER_ATTR_CONSTANT_RESISTANCE_CURRENT_LIMIT</c></description></item>
    ///   <item><description><c>NIDCPOWER_ATTR_CONSTANT_RESISTANCE_LEVEL_RANGE</c></description></item>
    /// </list>
    /// <para>When <c>NIDCPOWER_VAL_CONSTANT_POWER</c> is selected, the device generates the desired power
    ///   level on the output as long as the output current is below the current limit. Use the following
    ///   attributes to configure the channel when <c>NIDCPOWER_VAL_CONSTANT_POWER</c> is selected:</para>
    /// <list type="bullet">
    ///   <item><description><c>NIDCPOWER_ATTR_CONSTANT_POWER_LEVEL</c></description></item>
    ///   <item><description><c>NIDCPOWER_ATTR_CONSTANT_POWER_CURRENT_LIMIT</c></description></item>
    ///   <item><description><c>NIDCPOWER_ATTR_CONSTANT_POWER_LEVEL_RANGE</c></description></item>
    /// </list>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Configures the function to generate on the specified channel(s).</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>output_function</c>:</para>
    /// <para>When <c>NIDCPOWER_VAL_DC_VOLTAGE</c> is selected, the device generates the desired voltage
    ///   level on the output as long as the output current is below the current limit. Use the following
    ///   attributes to configure the channel when <c>NIDCPOWER_VAL_DC_VOLTAGE</c> is selected:</para>
    /// <list type="bullet">
    ///   <item><description><c>NiDCPowerAttribute.VoltageLevel</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.CurrentLimit</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.CurrentLimitHigh</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.CurrentLimitLow</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.VoltageLevelRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.CurrentLimitRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.ComplianceLimitSymmetry</c></description></item>
    /// </list>
    /// <para>When <c>NIDCPOWER_VAL_DC_CURRENT</c> is selected, the device generates the desired current
    ///   level on the output as long as the output voltage is below the voltage limit. Use the following
    ///   attributes to configure the channel when <c>NIDCPOWER_VAL_DC_CURRENT</c> is selected:</para>
    /// <list type="bullet">
    ///   <item><description><c>NiDCPowerAttribute.CurrentLevel</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.VoltageLimit</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.VoltageLimitHigh</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.VoltageLimitLow</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.CurrentLevelRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.VoltageLimitRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.ComplianceLimitSymmetry</c></description></item>
    /// </list>
    /// <para>When <c>NIDCPOWER_VAL_PULSE_VOLTAGE</c> is selected, the device generates pulses at the
    ///   desired pulse voltage levels on the output as long as the output current is below the pulse current
    ///   limit. Use the following attributes to configure the channel when <c>NIDCPOWER_VAL_PULSE_VOLTAGE</c>
    ///   is selected:</para>
    /// <list type="bullet">
    ///   <item><description><c>NiDCPowerAttribute.PulseVoltageLevel</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasVoltageLevel</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseCurrentLimit</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseCurrentLimitHigh</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseCurrentLimitLow</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasCurrentLimit</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasCurrentLimitHigh</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasCurrentLimitLow</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseVoltageLevelRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseCurrentLimitRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.ComplianceLimitSymmetry</c></description></item>
    /// </list>
    /// <para>When <c>NIDCPOWER_VAL_PULSE_CURRENT</c> is selected, the device generates pulses at the
    ///   desired pulse current levels on the output as long as the output voltage is below the pulse voltage
    ///   limit. Use the following attributes to configure the channel when <c>NIDCPOWER_VAL_PULSE_CURRENT</c>
    ///   is selected:</para>
    /// <list type="bullet">
    ///   <item><description><c>NiDCPowerAttribute.PulseCurrentLevel</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasCurrentLevel</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseVoltageLimit</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseVoltageLimitHigh</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseVoltageLimitLow</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasVoltageLimit</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasVoltageLimitHigh</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseBiasVoltageLimitLow</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseCurrentLevelRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.PulseVoltageLimitRange</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.ComplianceLimitSymmetry</c></description></item>
    /// </list>
    /// <para>When <c>NIDCPOWER_VAL_CONSTANT_RESISTANCE</c> is selected, the device generates the desired
    ///   resistance level on the output as long as the output current is below the current limit. Use the
    ///   following attributes to configure the channel when <c>NIDCPOWER_VAL_CONSTANT_RESISTANCE</c> is
    ///   selected:</para>
    /// <list type="bullet">
    ///   <item><description><c>NIDCPOWER_ATTR_CONSTANT_RESISTANCE_LEVEL</c></description></item>
    ///   <item><description><c>NIDCPOWER_ATTR_CONSTANT_RESISTANCE_CURRENT_LIMIT</c></description></item>
    ///   <item><description><c>NIDCPOWER_ATTR_CONSTANT_RESISTANCE_LEVEL_RANGE</c></description></item>
    /// </list>
    /// <para>When <c>NIDCPOWER_VAL_CONSTANT_POWER</c> is selected, the device generates the desired power
    ///   level on the output as long as the output current is below the current limit. Use the following
    ///   attributes to configure the channel when <c>NIDCPOWER_VAL_CONSTANT_POWER</c> is selected:</para>
    /// <list type="bullet">
    ///   <item><description><c>NIDCPOWER_ATTR_CONSTANT_POWER_LEVEL</c></description></item>
    ///   <item><description><c>NIDCPOWER_ATTR_CONSTANT_POWER_CURRENT_LIMIT</c></description></item>
    ///   <item><description><c>NIDCPOWER_ATTR_CONSTANT_POWER_LEVEL_RANGE</c></description></item>
    /// </list>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the output resistance that the device attempts to generate for the specified
    ///   channel(s).</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>output_resistance</c>:</para>
    /// <para>Depending on the instrument, output resistance is configurable only if you set the
    ///   <c>NiDCPowerAttribute.OutputFunction</c> of the channel as follows:</para>
    /// <list type="bullet">
    ///   <item><description>PXIe-4141, PXIe-4143, PXIe-4145:
    ///     <c>NIDCPOWER_VAL_DC_VOLTAGE</c></description></item>
    ///   <item><description>PXIe-4135, PXIe-4137, PXIe-4139, PXIe-4147: <c>NIDCPOWER_VAL_DC_CURRENT</c> or
    ///     <c>NIDCPOWER_VAL_DC_VOLTAGE</c></description></item>
    /// </list>
    /// <para><b>Valid Values</b>: Vary by device. Refer to the device specifications for your device for
    ///   more information about supported values.</para>
    /// <para><b>Default Value:</b> Search ni.com for Supported Attributes by Device for the default value
    ///   by device.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Enables (VI_TRUE) or disables (VI_FALSE) overvoltage protection (OVP). Refer to the Output
    ///   Overvoltage Protection topic in the NI DC Power Supplies and SMUs Help for more information about
    ///   overvoltage protection. Default Value: VI_FALSE</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>ovp_limit</c>:</para>
    /// <para>Determines the voltage limit, in volts, beyond which overvoltage protection (OVP) engages. The
    ///   limit is specified as a positive value, but symmetric positive and negative limits are enforced
    ///   simultaneously. For example, setting the OVP Limit to 65 will configure the OVP feature to trigger
    ///   an OVP error if the output exceeds ±65 V.</para>
    /// <para>Valid Values: 2 V to 210 V Default Value: 210 V</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the power line frequency for specified channel(s). NI-DCPower uses this value to
    ///   select a timebase for setting the <c>NiDCPowerAttribute.ApertureTime</c> attribute in power line
    ///   cycles (PLCs). in the NI DC Power Supplies and SMUs Help for information about supported devices.
    ///   Default Value: <c>NIDCPOWER_VAL_60_HERTZ</c></summary>
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

    /// <summary>Specifies the power line frequency for specified channel(s). NI-DCPower uses this value to
    ///   select a timebase for setting the <c>NiDCPowerAttribute.ApertureTime</c> attribute in power line
    ///   cycles (PLCs). in the NI DC Power Supplies and SMUs Help for information about supported devices.
    ///   Default Value: <c>NIDCPOWER_VAL_60_HERTZ</c></summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
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

    /// <summary>Specifies the pulse bias current level, in amps, that the device attempts to generate on
    ///   the specified channel(s) during the off phase of a pulse. This attribute is applicable only if the
    ///   <c>NiDCPowerAttribute.OutputFunction</c> attribute is set to <c>NIDCPOWER_VAL_PULSE_CURRENT</c>.
    ///   Valid Values: The valid values for this attribute are defined by the values you specify for the
    ///   <c>NiDCPowerAttribute.PulseCurrentLevelRange</c> attribute.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the pulse bias current limit, in amps, that the output cannot exceed when
    ///   generating the desired pulse bias voltage on the specified channel(s) during the off phase of a
    ///   pulse. This attribute is applicable only if the <c>NiDCPowerAttribute.OutputFunction</c> attribute
    ///   is set to <c>NIDCPOWER_VAL_PULSE_VOLTAGE</c>. Valid Values: The valid values for this attribute are
    ///   defined by the values you specify for the <c>NiDCPowerAttribute.PulseCurrentLimitRange</c>
    ///   property.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the pulse bias voltage level, in volts, that the device attempts to generate on
    ///   the specified channel(s) during the off phase of a pulse. This attribute is applicable only if the
    ///   <c>NiDCPowerAttribute.OutputFunction</c> attribute is set to <c>NIDCPOWER_VAL_PULSE_VOLTAGE</c>.
    ///   Valid Values: The valid values for this attribute are defined by the values you specify for the
    ///   <c>NiDCPowerAttribute.PulseVoltageLevelRange</c> attribute.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the pulse voltage limit, in volts, that the output cannot exceed when generating
    ///   the desired current on the specified channel(s) during the off phase of a pulse. This attribute is
    ///   applicable only if the <c>NiDCPowerAttribute.OutputFunction</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_PULSE_CURRENT</c>. Valid Values: The valid values for this attribute are defined by
    ///   the values you specify for the <c>NiDCPowerAttribute.PulseVoltageLimitRange</c> attribute.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the pulse current level, in amps, that the device attempts to generate on the
    ///   specified channel(s) during the on phase of a pulse. This attribute is applicable only if the
    ///   <c>NiDCPowerAttribute.OutputFunction</c> attribute is set to <c>NIDCPOWER_VAL_PULSE_CURRENT</c>.
    ///   Valid Values: The valid values for this attribute are defined by the values you specify for the
    ///   <c>NiDCPowerAttribute.PulseCurrentLevelRange</c> attribute.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the pulse current level range, in amps, for the specified channel(s). The range
    ///   defines the valid values to which you can set the pulse current level and pulse bias current level.
    ///   This attribute is applicable only if the <c>NiDCPowerAttribute.OutputFunction</c> attribute is set
    ///   to <c>NIDCPOWER_VAL_PULSE_CURRENT</c>. For valid ranges, refer to the specifications for your
    ///   instrument.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the pulse current limit, in amps, that the output cannot exceed when generating
    ///   the desired pulse voltage on the specified channel(s) during the on phase of a pulse. This attribute
    ///   is applicable only if the <c>NiDCPowerAttribute.OutputFunction</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_PULSE_VOLTAGE</c> and the <c>NiDCPowerAttribute.ComplianceLimitSymmetry</c>
    ///   attribute is set to <c>NIDCPOWER_VAL_COMPLIANCE_LIMIT_SYMMETRY_SYMMETRIC</c>. Valid Values: The
    ///   valid values for this attribute are defined by the values you specify for the
    ///   <c>NiDCPowerAttribute.PulseCurrentLimitRange</c> attribute.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the pulse current limit range, in amps, for the specified channel(s). The range
    ///   defines the valid values to which you can set the pulse current limit and pulse bias current limit.
    ///   This attribute is applicable only if the <c>NiDCPowerAttribute.OutputFunction</c> attribute is set
    ///   to <c>NIDCPOWER_VAL_PULSE_VOLTAGE</c>. For valid ranges, refer to the specifications for your
    ///   instrument.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the pulse current limit, in amps, that the output cannot exceed when generating
    ///   the desired pulse voltage on the specified channel(s) during the on phase of a pulse. This attribute
    ///   is applicable only if the <c>NiDCPowerAttribute.OutputFunction</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_PULSE_VOLTAGE</c>. Valid Values: The valid values for this attribute are defined by
    ///   the values you specify for the <c>NiDCPowerAttribute.PulseCurrentLimitRange</c> attribute.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the pulse voltage level range, in volts, for the specified channel(s). The range
    ///   defines the valid values at which you can set the pulse voltage level and pulse bias voltage level.
    ///   This attribute is applicable only if the <c>NiDCPowerAttribute.OutputFunction</c> attribute is set
    ///   to <c>NIDCPOWER_VAL_PULSE_VOLTAGE</c>. For valid ranges, refer to the specifications for your
    ///   instrument.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the pulse voltage limit, in volts, that the output cannot exceed when generating
    ///   the desired pulse current on the specified channel(s) during the on phase of a pulse. This attribute
    ///   is applicable only if the <c>NiDCPowerAttribute.OutputFunction</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_PULSE_CURRENT</c> and the <c>NiDCPowerAttribute.ComplianceLimitSymmetry</c>
    ///   attribute is set to <c>NIDCPOWER_VAL_COMPLIANCE_LIMIT_SYMMETRY_SYMMETRIC</c>. Valid Values: The
    ///   valid values for this attribute are defined by the values you specify for the
    ///   <c>NiDCPowerAttribute.PulseVoltageLimitRange</c> attribute.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the pulse voltage limit range, in volts, for the specified channel(s). The range
    ///   defines the valid values to which you can set the pulse voltage limit and pulse bias voltage limit.
    ///   This attribute is applicable only if the <c>NiDCPowerAttribute.OutputFunction</c> attribute is set
    ///   to <c>NIDCPOWER_VAL_PULSE_CURRENT</c>. For valid ranges, refer to the specifications for your
    ///   instrument.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Selects either local or remote sensing of the output voltage for the specified channel(s).
    ///   Refer to the Local and Remote Sense topic in the NI DC Power Supplies and SMUs Help for more
    ///   information about sensing voltage on supported channels and about devices that support local and/or
    ///   remote sensing. Default Value: The default value is <c>NIDCPOWER_VAL_LOCAL</c> if the device
    ///   supports local sense. Otherwise, the default and only supported value is
    ///   <c>NIDCPOWER_VAL_REMOTE</c>.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Selects either local or remote sensing of the output voltage for the specified channel(s).
    ///   Refer to the Local and Remote Sense topic in the NI DC Power Supplies and SMUs Help for more
    ///   information about sensing voltage on supported channels and about devices that support local and/or
    ///   remote sensing. Default Value: The default value is <c>NIDCPOWER_VAL_LOCAL</c> if the device
    ///   supports local sense. Otherwise, the default and only supported value is
    ///   <c>NIDCPOWER_VAL_REMOTE</c>.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Configures the measure trigger to be a software edge trigger. Use
    ///   <c>SendSoftwareEdgeTrigger</c> to generate the trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>measure_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Measure trigger. Default Value:
    ///   <c>NIDCPOWER_VAL_DIGITAL_EDGE</c></para>
    /// </remarks>
    public void ConfigureSoftwareEdgeMeasureTrigger()
    {
        var request = new ConfigureSoftwareEdgeMeasureTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.ConfigureSoftwareEdgeMeasureTrigger(request);
        Check(response.Status);
    }

    /// <summary>Configures the measure trigger to be a software edge trigger. Use
    ///   <c>SendSoftwareEdgeTrigger</c> to generate the trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>measure_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Measure trigger. Default Value:
    ///   <c>NIDCPOWER_VAL_DIGITAL_EDGE</c></para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Configures the pulse trigger to be a software edge trigger. Use
    ///   <c>SendSoftwareEdgeTrigger</c> to generate the trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>pulse_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Pulse trigger. Default Value: <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    public void ConfigureSoftwareEdgePulseTrigger()
    {
        var request = new ConfigureSoftwareEdgePulseTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.ConfigureSoftwareEdgePulseTrigger(request);
        Check(response.Status);
    }

    /// <summary>Configures the pulse trigger to be a software edge trigger. Use
    ///   <c>SendSoftwareEdgeTrigger</c> to generate the trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>pulse_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Pulse trigger. Default Value: <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Configures the sequence advance trigger to be a software edge trigger. Use
    ///   <c>SendSoftwareEdgeTrigger</c> to generate the trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>sequence_advance_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Sequence Advance trigger. Default Value:
    ///   <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    public void ConfigureSoftwareEdgeSequenceAdvanceTrigger()
    {
        var request = new ConfigureSoftwareEdgeSequenceAdvanceTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.ConfigureSoftwareEdgeSequenceAdvanceTrigger(request);
        Check(response.Status);
    }

    /// <summary>Configures the sequence advance trigger to be a software edge trigger. Use
    ///   <c>SendSoftwareEdgeTrigger</c> to generate the trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>sequence_advance_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Sequence Advance trigger. Default Value:
    ///   <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Configures the shutdown trigger to be a software edge trigger. Use
    ///   <c>SendSoftwareEdgeTrigger</c> to generate the trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>shutdown_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Shutdown trigger. Default Value:
    ///   <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Configures the source trigger to be a software edge trigger. Use
    ///   <c>SendSoftwareEdgeTrigger</c> to generate the trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>source_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Source trigger. Default Value: <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    public void ConfigureSoftwareEdgeSourceTrigger()
    {
        var request = new ConfigureSoftwareEdgeSourceTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.ConfigureSoftwareEdgeSourceTrigger(request);
        Check(response.Status);
    }

    /// <summary>Configures the source trigger to be a software edge trigger. Use
    ///   <c>SendSoftwareEdgeTrigger</c> to generate the trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>source_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Source trigger. Default Value: <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Configures the start trigger to be a software edge trigger. Use
    ///   <c>SendSoftwareEdgeTrigger</c> to generate the trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>start_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Start trigger. Default Value: <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    public void ConfigureSoftwareEdgeStartTrigger()
    {
        var request = new ConfigureSoftwareEdgeStartTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.ConfigureSoftwareEdgeStartTrigger(request);
        Check(response.Status);
    }

    /// <summary>Configures the start trigger to be a software edge trigger. Use
    ///   <c>SendSoftwareEdgeTrigger</c> to generate the trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>start_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Start trigger. Default Value: <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies whether to run a single output point or a sequence. Refer to the Single Point
    ///   Source Mode and Sequence Source Mode topics in the NI DC Power Supplies and SMUs Help for more
    ///   information about source modes. Default value: <c>NIDCPOWER_VAL_SINGLE_POINT</c></summary>
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

    /// <summary>Specifies whether to run a single output point or a sequence. Refer to the Single Point
    ///   Source Mode and Sequence Source Mode topics in the NI DC Power Supplies and SMUs Help for more
    ///   information about source modes. Default value: <c>NIDCPOWER_VAL_SINGLE_POINT</c></summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
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

    /// <summary>Specifies whether to run a single output point or a sequence. Refer to the Single Point
    ///   Source Mode and Sequence Source Mode topics in the NI DC Power Supplies and SMUs Help for more
    ///   information about source modes. Default value: <c>NIDCPOWER_VAL_SINGLE_POINT</c></summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies whether to run a single output point or a sequence. Refer to the Single Point
    ///   Source Mode and Sequence Source Mode topics in the NI DC Power Supplies and SMUs Help for more
    ///   information about source modes. Default value: <c>NIDCPOWER_VAL_SINGLE_POINT</c></summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the voltage level, in volts, that the device attempts to generate on the
    ///   specified channel(s). This attribute is applicable only if the
    ///   <c>NiDCPowerAttribute.OutputFunction</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_DC_VOLTAGE</c>.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>voltage_level</c>:</para>
    /// <para>Valid Values: The valid values for this attribute are defined by the values you specify for
    ///   the <c>NiDCPowerAttribute.VoltageLevelRange</c> attribute.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the voltage level range, in volts, for the specified channel(s). The range
    ///   defines the valid values to which the voltage level can be set. Use the
    ///   <c>NiDCPowerAttribute.VoltageLevelAutorange</c> attribute to enable automatic selection of the
    ///   voltage level range. The <c>NiDCPowerAttribute.VoltageLevelRange</c> attribute is applicable only if
    ///   the <c>NiDCPowerAttribute.OutputFunction</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_DC_VOLTAGE</c>.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>voltage_level_range</c>:</para>
    /// <para>For valid ranges, refer to the specifications for your instrument.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the voltage limit, in volts, that the output cannot exceed when generating the
    ///   desired current level on the specified channels. This attribute is applicable only if the
    ///   <c>NiDCPowerAttribute.OutputFunction</c> attribute is set to <c>NIDCPOWER_VAL_DC_CURRENT</c> and the
    ///   <c>NiDCPowerAttribute.ComplianceLimitSymmetry</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_COMPLIANCE_LIMIT_SYMMETRY_SYMMETRIC</c>.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>voltage_limit</c>:</para>
    /// <para>Valid Values: The valid values for this attribute are defined by the values to which the
    ///   <c>NiDCPowerAttribute.VoltageLimitRange</c> attribute is set.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Specifies the voltage limit range, in volts, for the specified channel(s). The range
    ///   defines the valid values to which the voltage limit can be set. Use the
    ///   <c>NiDCPowerAttribute.VoltageLimitAutorange</c> attribute to enable automatic selection of the
    ///   voltage limit range. The <c>NiDCPowerAttribute.VoltageLimitRange</c> attribute is applicable only if
    ///   the <c>NiDCPowerAttribute.OutputFunction</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_DC_CURRENT</c>.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>voltage_limit_range</c>:</para>
    /// <para>For valid ranges, refer to the specifications for your instrument.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Creates an empty advanced sequence. Call the <c>CreateAdvancedSequenceStepWithChannels</c>
    ///   function to add steps to the active advanced sequence.</summary>
    /// <remarks>
    /// <para>You can create multiple advanced sequences for a channel.</para>
    /// <para><b>Support for this function</b></para>
    /// <para>You must set the source mode to Sequence to use this function.</para>
    /// <para>Using the <c>SetSequence</c> function with Advanced Sequence functions is unsupported.</para>
    /// <para>Use this function in the Uncommitted or Committed programming states. Refer to the Programming
    ///   States topic in the <i>NI DC Power Supplies and SMUs Help</i> for more information about NI-DCPower
    ///   programming states.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>create_advanced_sequence</c>.</para>
    /// </remarks>
    /// <param name="sequenceName">Specifies the name of the sequence to create.</param>
    /// <param name="attributeIds">Specifies the attributes you reconfigure per step in the advanced
    ///   sequence. For more information about which attributes can be configured in an advanced sequence for
    ///   each NI-DCPower device that supports advanced sequencing, search ni.com for Supported Properties by
    ///   Device.</param>
    /// <param name="setAsActiveSequence">Specifies that this current sequence is active.</param>
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

    /// <summary>Creates a Commit step in the Active advanced sequence. A Commit step configures channels to
    ///   a user-defined known state before starting the advanced sequence. When a Commit step exists in the
    ///   Active advanced sequence, you cannot set the output function to Pulse Voltage or Pulse Current in
    ///   either the Commit step (-1) or step 0. When you create an advanced sequence step, each attribute you
    ///   passed to the <c>CreateAdvancedSequenceWithChannels</c> function is reset to its default value for
    ///   that step unless otherwise specified.</summary>
    /// <remarks>
    /// <para><b>Support for this Function</b></para>
    /// <para>You must set the source mode to Sequence to use this function.</para>
    /// <para>Using the <c>SetSequence</c> function with Advanced Sequence functions is unsupported.</para>
    /// <para><b>Note:</b></para>
    /// <para>NI-DCPower uses the terms "source" and "output". However, while sinking with electronic loads
    ///   and SMUs these correspond to "sinking" and "input", respectively.</para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>create_advanced_sequence_commit_step</c>.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="setAsActiveStep">Specifies whether the step created with this function is active in the
    ///   Active advanced sequence.</param>
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

    /// <summary>Creates a new advanced sequence step in the advanced sequence specified by the Active
    ///   advanced sequence. When you create an advanced sequence step, each attribute you passed to the
    ///   <c>CreateAdvancedSequenceWithChannels</c> function is reset to its default value for that step
    ///   unless otherwise specified.</summary>
    /// <remarks>
    /// <para><b>Support for this Function</b></para>
    /// <para>You must set the source mode to Sequence to use this function.</para>
    /// <para>Using the <c>SetSequence</c> function with Advanced Sequence functions is unsupported.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>create_advanced_sequence_step</c>.</para>
    /// </remarks>
    /// <param name="setAsActiveStep">Specifies whether the step created with this function is active in the
    ///   Active advanced sequence.</param>
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

    /// <summary>Creates a new advanced sequence step in the advanced sequence specified by the Active
    ///   advanced sequence. When you create an advanced sequence step, each attribute you passed to the
    ///   <c>CreateAdvancedSequenceWithChannels</c> function is reset to its default value for that step
    ///   unless otherwise specified.</summary>
    /// <remarks>
    /// <para><b>Support for this Function</b></para>
    /// <para>You must set the source mode to Sequence to use this function.</para>
    /// <para>Using the <c>SetSequence</c> function with Advanced Sequence functions is unsupported.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>create_advanced_sequence_step</c>.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="setAsActiveStep">Specifies whether the step created with this function is active in the
    ///   Active advanced sequence.</param>
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

    /// <summary>Creates an empty advanced sequence. Call the <c>CreateAdvancedSequenceStepWithChannels</c>
    ///   function to add steps to the active advanced sequence.</summary>
    /// <remarks>
    /// <para>You can create multiple advanced sequences for a channel.</para>
    /// <para><b>Support for this function</b></para>
    /// <para>You must set the source mode to Sequence to use this function.</para>
    /// <para>Using the <c>SetSequence</c> function with Advanced Sequence functions is unsupported.</para>
    /// <para>Use this function in the Uncommitted or Committed programming states. Refer to the Programming
    ///   States topic in the <i>NI DC Power Supplies and SMUs Help</i> for more information about NI-DCPower
    ///   programming states.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>create_advanced_sequence</c>.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="sequenceName">Specifies the name of the sequence to create.</param>
    /// <param name="attributeIds">Specifies the attributes you reconfigure per step in the advanced
    ///   sequence. For more information about which attributes can be configured in an advanced sequence for
    ///   each NI-DCPower device that supports advanced sequencing, search ni.com for Supported Properties by
    ///   Device.</param>
    /// <param name="setAsActiveSequence">Specifies that this current sequence is active.</param>
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

    /// <summary>Deletes a previously created advanced sequence and all the advanced sequence steps in the
    ///   advanced sequence.</summary>
    /// <remarks>
    /// <para><b>Support for this Function</b></para>
    /// <para>You must set the source mode to Sequence to use this function.</para>
    /// <para>Using the <c>SetSequence</c> function with Advanced Sequence functions is unsupported.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>delete_advanced_sequence</c>.</para>
    /// </remarks>
    /// <param name="sequenceName">specifies the name of the sequence to delete.</param>
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

    /// <summary>Deletes a previously created advanced sequence and all the advanced sequence steps in the
    ///   advanced sequence.</summary>
    /// <remarks>
    /// <para><b>Support for this Function</b></para>
    /// <para>You must set the source mode to Sequence to use this function.</para>
    /// <para>Using the <c>SetSequence</c> function with Advanced Sequence functions is unsupported.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>delete_advanced_sequence</c>.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="sequenceName">specifies the name of the sequence to delete.</param>
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

    /// <summary>This function performs the same actions as the <c>ResetWithChannels</c> function, except
    ///   that this function also immediately sets the <c>NiDCPowerAttribute.OutputEnabled</c> attribute to
    ///   VI_FALSE.</summary>
    /// <remarks>
    /// <para>This function opens the output relay on devices that have an output relay.</para>
    /// <para><b>Note:</b></para>
    /// <para>NI-DCPower uses the terms "source" and "output". However, while sinking with electronic loads
    ///   and SMUs these correspond to "sinking" and "input", respectively.</para>
    /// </remarks>
    public void Disable()
    {
        var request = new DisableRequest
        {
            Vi = Vi,
        };
        var response = _client.Disable(request);
        Check(response.Status);
    }

    /// <summary>Disables the pulse trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>pulse_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Pulse trigger. Default Value: <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    public void DisablePulseTrigger()
    {
        var request = new DisablePulseTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.DisablePulseTrigger(request);
        Check(response.Status);
    }

    /// <summary>Disables the pulse trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>pulse_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Pulse trigger. Default Value: <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Disables the sequence advance trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>sequence_advance_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Sequence Advance trigger. Default Value:
    ///   <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    public void DisableSequenceAdvanceTrigger()
    {
        var request = new DisableSequenceAdvanceTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.DisableSequenceAdvanceTrigger(request);
        Check(response.Status);
    }

    /// <summary>Disables the sequence advance trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>sequence_advance_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Sequence Advance trigger. Default Value:
    ///   <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Disables the shutdown trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>shutdown_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Shutdown trigger. Default Value:
    ///   <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Disables the source trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>source_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Source trigger. Default Value: <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    public void DisableSourceTrigger()
    {
        var request = new DisableSourceTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.DisableSourceTrigger(request);
        Check(response.Status);
    }

    /// <summary>Disables the source trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>source_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Source trigger. Default Value: <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Disables the start trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>start_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Start trigger. Default Value: <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    public void DisableStartTrigger()
    {
        var request = new DisableStartTriggerRequest
        {
            Vi = Vi,
        };
        var response = _client.DisableStartTrigger(request);
        Check(response.Status);
    }

    /// <summary>Disables the start trigger.</summary>
    /// <remarks>
    /// <para>Corresponds to the Python property <c>start_trigger_type</c>:</para>
    /// <para>Specifies the behavior of the Start trigger. Default Value: <c>NIDCPOWER_VAL_NONE</c></para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Converts a status code returned by an instrument driver function into a user-readable
    ///   string.</summary>
    /// <param name="errorCode">Specifies the <b>status</b> parameter that is returned from any of the
    ///   NI-DCPower functions.</param>
    /// <returns>Returns the user-readable message string that corresponds to the status code you specify.
    ///   You must pass a ViChar array with at least 256 bytes.</returns>
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

    /// <summary>Reads an error code and message from the instrument's error queue.</summary>
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

    /// <summary>Exports the attribute configuration of the session to the specified configuration
    ///   buffer.</summary>
    /// <remarks>
    /// <para>You can export and import session attribute configurations only between devices with identical
    ///   model numbers and the same number of configured channels.</para>
    /// <para>This function verifies that the attributes you have configured for the session are valid. If
    ///   the configuration is invalid, NI‑DCPower returns an error.</para>
    /// <para><b>Support for this Function</b></para>
    /// <para>Calling this function in Sequence Source Mode is unsupported.</para>
    /// <para><b>Channel Mapping Behavior for Multichannel Sessions</b></para>
    /// <para>When importing and exporting session attribute configurations between NI‑DCPower sessions that
    ///   were initialized with different channels, the configurations of the exporting channels are mapped to
    ///   the importing channels in the order you specify in the <b>channelName</b> input to the
    ///   <c>InitializeWithChannels</c> function.</para>
    /// <para>For example, if your entry for <b>channelName</b> is 0,1 for the exporting session and 1,2 for
    ///   the importing session:</para>
    /// <list type="bullet">
    ///   <item><description>The configuration exported from channel 0 is imported into channel
    ///     1.</description></item>
    ///   <item><description>The configuration exported from channel 1 is imported into channel
    ///     2.</description></item>
    /// </list>
    /// <para><b>Note:</b></para>
    /// <para>This function will return an error if the total number of channels initialized for the
    ///   exporting session is not equal to the total number of channels initialized for the importing
    ///   session.</para>
    /// </remarks>
    /// <returns>Specifies the byte array buffer to be populated with the exported attribute
    ///   configuration.</returns>
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

    /// <summary>Exports the attribute configuration of the session to the specified file.</summary>
    /// <remarks>
    /// <para>You can export and import session attribute configurations only between devices with identical
    ///   model numbers and the same number of configured channels.</para>
    /// <para>This function verifies that the attributes you have configured for the session are valid. If
    ///   the configuration is invalid, NI‑DCPower returns an error.</para>
    /// <para><b>Support for this Function</b></para>
    /// <para>Calling this function in Sequence Source Mode is unsupported.</para>
    /// <para><b>Channel Mapping Behavior for Multichannel Sessions</b></para>
    /// <para>When importing and exporting session attribute configurations between NI‑DCPower sessions that
    ///   were initialized with different channels, the configurations of the exporting channels are mapped to
    ///   the importing channels in the order you specify in the <b>channelName</b> input to the
    ///   <c>InitializeWithChannels</c> function.</para>
    /// <para>For example, if your entry for <b>channelName</b> is 0,1 for the exporting session and 1,2 for
    ///   the importing session:</para>
    /// <list type="bullet">
    ///   <item><description>The configuration exported from channel 0 is imported into channel
    ///     1.</description></item>
    ///   <item><description>The configuration exported from channel 1 is imported into channel
    ///     2.</description></item>
    /// </list>
    /// <para><b>Note:</b></para>
    /// <para>This function will return an error if the total number of channels initialized for the
    ///   exporting session is not equal to the total number of channels initialized for the importing
    ///   session.</para>
    /// </remarks>
    /// <param name="filePath">Specifies the absolute path to the file to contain the exported attribute
    ///   configuration. If you specify an empty or relative path, this function returns an error. <b>Default
    ///   file extension:</b> .nidcpowerconfig</param>
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

    /// <summary>Routes the specified signal to the specified output terminal.</summary>
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

    /// <summary>Routes the specified signal to the specified output terminal.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
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

    /// <summary>Routes the specified signal to the specified output terminal.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Routes the specified signal to the specified output terminal.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Returns an array of voltage measurements, an array of current measurements, and an array of
    ///   compliance measurements that were previously taken and are stored in the NI-DCPower buffer. This
    ///   function should not be used when the <c>NiDCPowerAttribute.MeasureWhen</c> attribute is set to
    ///   <c>NIDCPOWER_VAL_ON_DEMAND</c>. You must first call <c>InitiateWithChannels</c> before calling this
    ///   function.</summary>
    /// <remarks>
    /// <para>Refer to the Acquiring Measurements and Compliance topics in the <i>NI DC Power Supplies and
    ///   SMUs Help</i> for more information about configuring this function.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>fetch_multiple</c>.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="timeout">Specifies the maximum time allowed for this function to complete, in seconds.
    ///   If the function does not complete within this time interval, NI-DCPower returns an error. Note: When
    ///   setting the timeout interval, ensure you take into account any triggers so that the timeout interval
    ///   is long enough for your application.</param>
    /// <param name="count">Specifies the number of measurements to fetch.</param>
    /// <returns>
    /// A tuple of:
    /// <list type="bullet">
    ///   <item><description><b>VoltageMeasurements</b>: Returns an array of voltage measurements. Ensure that
    ///     sufficient space has been allocated for the returned array.</description></item>
    ///   <item><description><b>CurrentMeasurements</b>: Returns an array of current measurements. Ensure that
    ///     sufficient space has been allocated for the returned array.</description></item>
    ///   <item><description><b>InCompliance</b>: Returns an array of Boolean values indicating whether the
    ///     output was in compliance at the time the measurement was taken. Ensure that sufficient space has
    ///     been allocated for the returned array.</description></item>
    ///   <item><description><b>ActualCount</b>: Indicates the number of measured values actually retrieved
    ///     from the device.</description></item>
    /// </list>
    /// </returns>
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

    /// <summary>Returns a list of previously measured LCRMeasurement instances on the specified channel
    ///   that have been taken and stored in a buffer.</summary>
    /// <remarks>
    /// <para>To use this function:</para>
    /// <list type="bullet">
    ///   <item><description>Set <c>NiDCPowerAttribute.MeasureWhen</c> property to
    ///     <c>NIDCPOWER_VAL_AUTOMATICALLY_AFTER_SOURCE_COMPLETE</c> or
    ///     <c>NIDCPOWER_VAL_ON_MEASURE_TRIGGER</c></description></item>
    ///   <item><description>Put the channel in the Running state (call
    ///     <c>InitiateWithChannels</c>)</description></item>
    /// </list>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="timeout">Specifies the maximum time allowed for this function to complete, in seconds.
    ///   If the function does not complete within this time interval, NI-DCPower returns an error. Note: When
    ///   setting the timeout interval, ensure you take into account any triggers so that the timeout interval
    ///   is long enough for your application.</param>
    /// <param name="count">Specifies the number of measurements to fetch.</param>
    /// <returns>
    /// A tuple of:
    /// <list type="bullet">
    ///   <item><description><b>Measurements</b>: A list of LCRMeasurement instances.</description></item>
    /// </list>
    /// </returns>
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

    /// <summary>Queries the value of a ViBoolean attribute. You can use this function to get the values of
    ///   device-specific attributes and inherent IVI attributes.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="attributeId">Specifies the ID of an attribute. From the function panel window, you can
    ///   use this control as follows. In the function panel window, click on the control or press
    ///   <b>Enter</b> or the spacebar to display a dialog box containing hierarchical list of the available
    ///   attributes. Help text is shown for each attribute. Select an attribute by double-clicking on it or
    ///   by selecting it and then pressing <b>Enter</b>.; A ring control at the top of the dialog box allows
    ///   you to see all IVI attributes or only the attributes of type ViBoolean. If you choose to see all IVI
    ///   attributes, the data types appear to the right of the attribute names in the list box. Attributes
    ///   with data types other than ViBoolean are dim. If you select an attribute data type that is dim,
    ///   LabWindows/CVI transfers you to the function panel for the corresponding function that is consistent
    ///   with the data type.; If you want to enter a variable name, press <b>Ctrl</b>\ +\ <b>T</b> to change
    ///   this ring control to a manual input box. If the attribute in this ring control has named constants
    ///   as valid values, you can view the constants by moving to the value control and pressing
    ///   <b>Enter</b>.</param>
    /// <returns>Returns the current value of the attribute. Passes the address of a ViBoolean variable. If
    ///   the attribute currently showing in the attribute ring control has constants as valid values, you can
    ///   view a list of the constants by pressing <b>Enter</b> on this control. Select a value by
    ///   double-clicking on it or by selecting it and then pressing <b>Enter</b>.</returns>
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

    /// <summary>Queries the value of a ViInt32 attribute. You can use this function to get the values of
    ///   device-specific attributes and inherent IVI attributes.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="attributeId">Specifies the ID of an attribute. From the function panel window, you can
    ///   use this control as follows. In the function panel window, click on the control or press
    ///   <b>Enter</b> or the spacebar to display a dialog box containing hierarchical list of the available
    ///   attributes. Help text is shown for each attribute. Select an attribute by double-clicking on it or
    ///   by selecting it and then pressing <b>Enter</b>.; A ring control at the top of the dialog box allows
    ///   you to see all IVI attributes or only the attributes of type ViInt32. If you choose to see all IVI
    ///   attributes, the data types appear to the right of the attribute names in the list box. Attributes
    ///   with data types other than ViInt32 are dim. If you select an attribute data type that is dim,
    ///   LabWindows/CVI transfers you to the function panel for the corresponding function that is consistent
    ///   with the data type.; If you want to enter a variable name, press <b>Ctrl</b>\ +\ <b>T</b> to change
    ///   this ring control to a manual input box. If the attribute in this ring control has named constants
    ///   as valid values, you can view the constants by moving to the value control and pressing
    ///   <b>Enter</b>.</param>
    /// <returns>Returns the current value of the attribute. Passes the address of a ViInt32 variable. If
    ///   the attribute currently showing in the attribute ring control has constants as valid values, you can
    ///   view a list of the constants by pressing <b>Enter</b> on this control. Select a value by
    ///   double-clicking on it or by selecting it and then pressing <b>Enter</b>.</returns>
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

    /// <summary>Queries the value of a ViInt64 attribute. You can use this function to get the values of
    ///   device-specific attributes and inherent IVI attributes.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="attributeId">Specifies the ID of an attribute. From the function panel window, you can
    ///   use this control as follows. In the function panel window, click on the control or press
    ///   <b>Enter</b> or the spacebar to display a dialog box containing hierarchical list of the available
    ///   attributes. Help text is shown for each attribute. Select an attribute by double-clicking on it or
    ///   by selecting it and then pressing <b>Enter</b>.; A ring control at the top of the dialog box allows
    ///   you to see all IVI attributes or only the attributes of type ViReal64. If you choose to see all IVI
    ///   attributes, the data types appear to the right of the attribute names in the list box. Attributes
    ///   with data types other than ViReal64 are dim. If you select an attribute data type that is dim,
    ///   LabWindows/CVI transfers you to the function panel for the corresponding function that is consistent
    ///   with the data type.; If you want to enter a variable name, press <b>Ctrl</b>\ +\ <b>T</b> to change
    ///   this ring control to a manual input box. If the attribute in this ring control has named constants
    ///   as valid values, you can view the constants by moving to the value control and pressing
    ///   <b>Enter</b>.</param>
    /// <returns>Returns the current value of the attribute. Passes the address of a ViReal64 variable. If
    ///   the attribute currently showing in the attribute ring control has constants as valid values, you can
    ///   view a list of the constants by pressing <b>Enter</b> on this control. Select a value by
    ///   double-clicking on it or by selecting it and then pressing <b>Enter</b>.</returns>
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

    /// <summary>Queries the value of a ViReal64 attribute. You can use this function to get the values of
    ///   device-specific attributes and inherent IVI attributes.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="attributeId">Specifies the ID of an attribute. From the function panel window, you can
    ///   use this control as follows. In the function panel window, click on the control or press
    ///   <b>Enter</b> or the spacebar to display a dialog box containing hierarchical list of the available
    ///   attributes. Help text is shown for each attribute. Select an attribute by double-clicking on it or
    ///   by selecting it and then pressing <b>Enter</b>.; A ring control at the top of the dialog box allows
    ///   you to see all IVI attributes or only the attributes of type ViReal64. If you choose to see all IVI
    ///   attributes, the data types appear to the right of the attribute names in the list box. Attributes
    ///   with data types other than ViReal64 are dim. If you select an attribute data type that is dim,
    ///   LabWindows/CVI transfers you to the function panel for the corresponding function that is consistent
    ///   with the data type.; If you want to enter a variable name, press <b>Ctrl</b>\ +\ <b>T</b> to change
    ///   this ring control to a manual input box. If the attribute in this ring control has named constants
    ///   as valid values, you can view the constants by moving to the value control and pressing
    ///   <b>Enter</b>.</param>
    /// <returns>Returns the current value of the attribute. Passes the address of a ViReal64 variable. If
    ///   the attribute currently showing in the attribute ring control has constants as valid values, you can
    ///   view a list of the constants by pressing <b>Enter</b> on this control. Select a value by
    ///   double-clicking on it or by selecting it and then pressing <b>Enter</b>.</returns>
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

    /// <summary>Queries the value of a session attribute.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Queries the value of a ViString attribute. You can use this function to get the values of
    ///   device-specific attributes and inherent IVI attributes.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="attributeId">Specifies the ID of an attribute. From the function panel window, you can
    ///   use this control as follows. In the function panel window, click on the control or press or the
    ///   spacebar to display a dialog box containing hierarchical list of the available attributes. Help text
    ///   is shown for each attribute. Select an attribute by double-clicking on it or by selecting it and
    ///   then pressing .; A ring control at the top of the dialog box allows you to see all IVI attributes or
    ///   only the attributes of type ViString. If you choose to see all IVI attributes, the data types appear
    ///   to the right of the attribute names in the list box. Attributes with data types other than ViString
    ///   are dimmed. If you select an attribute data type that is dimmed, LabWindows/CVI transfers you to the
    ///   function panel for the corresponding function that is consistent with the data type.; If you want to
    ///   enter a variable name, press to change this ring control to a manual input control. If the attribute
    ///   in this ring control has named constants as valid values, you can view the constants by moving to
    ///   the value control and pressing .</param>
    /// <returns>The buffer in which the function returns the current value of the attribute. The buffer
    ///   must be of type ViChar and have at least as many bytes as indicated in <b>bufSize</b>. If the
    ///   current value of the attribute, including the terminating NUL byte, contains more bytes that you
    ///   indicate in this attribute, the function copies (buffer size -1) bytes into the buffer, places an
    ///   ASCII NUL byte at the end of the buffer, and returns the buffer size you must pass to get the entire
    ///   value. For example, if the value is 123456 and the buffer size is 4, the function places 123 into
    ///   the buffer and returns 7. If you specify 0 for <b>bufSize</b>, you can pass VI_NULL for this
    ///   attribute. If the attribute currently showing in the attribute ring control has constants as valid
    ///   values, you can view a list of the constants by pressing on this control. Select a value by
    ///   double-clicking on it or by selecting it and then pressing .</returns>
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

    /// <summary>Retrieves the output <b>channelName</b> that corresponds to the requested <b>index</b>. Use
    ///   the <c>NiDCPowerAttribute.ChannelCount</c> attribute to determine the upper bound of valid values
    ///   for <b>index</b>.</summary>
    /// <param name="index">Specifies which channel name to return. The index values begin at 1.</param>
    /// <returns>Returns the channel name that corresponds to <b>index</b>.</returns>
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

    /// <summary>Returns a list of channel names for the given channel indices.</summary>
    /// <remarks>
    /// <para>NI-DCPower Python API: <c>_get_channel_names</c>.</para>
    /// </remarks>
    /// <param name="index">Index list for the channels in the session. Valid values are from zero to the
    ///   total number of channels in the session minus one. The index string can be one of the following
    ///   formats: A comma-separated list—for example, "0,2,3,1"; A range using a hyphen—for example, "0-3"; A
    ///   range using a colon—for example, "0:3 " You can combine comma-separated lists and ranges that use a
    ///   hyphen or colon. Both out-of-order and repeated indices are supported ("2,3,0", "1,2,2,3"). White
    ///   space characters, including spaces, tabs, feeds, and carriage returns, are allowed between
    ///   characters. Ranges can be incrementing or decrementing.</param>
    /// <returns>The channel name(s) at the specified indices.</returns>
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

    /// <summary>Retrieves and then clears the IVI error information for the session or the current
    ///   execution thread unless <b>bufferSize</b> is 0, in which case the function does not clear the error
    ///   information. By passing 0 for the buffer size, you can ascertain the buffer size required to get the
    ///   entire error description string and then call the function again with a sufficiently large buffer
    ///   size. If the user specifies a valid IVI session for <b>vi</b>, this function retrieves and then
    ///   clears the error information for the session. If the user passes VI_NULL for <b>vi</b>, this
    ///   function retrieves and then clears the error information for the current execution thread. If
    ///   <b>vi</b> is an invalid session, the function does nothing and returns an error. Normally, the error
    ///   information describes the first error that occurred since the user last called <c>GetError</c> or
    ///   <c>ClearError</c>.</summary>
    /// <returns>
    /// A tuple of:
    /// <list type="bullet">
    ///   <item><description><b>Code</b>: Returns the error code for the session or execution
    ///     thread.</description></item>
    ///   <item><description><b>Description</b>: Returns the error description for the IVI session or
    ///     execution thread. If there is no description, the function returns an empty string. The buffer must
    ///     contain at least as many elements as the value you specify with <b>bufferSize</b>. If the error
    ///     description, including the terminating NUL byte, contains more bytes than you indicate with
    ///     <b>bufferSize</b>, the function copies (buffer size - 1) bytes into the buffer, places an ASCII NUL
    ///     byte at the end of the buffer, and returns the buffer size you must pass to get the entire value.
    ///     For example, if the value is 123456 and the buffer size is 4, the function places 123 into the
    ///     buffer and returns 7. If you pass 0 for <b>bufferSize</b>, you can pass VI_NULL for this
    ///     attribute.</description></item>
    /// </list>
    /// </returns>
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

    /// <summary>Returns the date and time of the last successful calibration. The time returned is 24-hour
    ///   (military) local time; for example, if the device was calibrated at 2:30 PM, this function returns
    ///   14 for <b>hours</b> and 30 for <b>minutes</b>.</summary>
    /// <remarks>
    /// <para>NI-DCPower Python API: <c>get_ext_cal_last_date_and_time</c>.</para>
    /// </remarks>
    /// <returns>
    /// A tuple of:
    /// <list type="bullet">
    ///   <item><description><b>Year</b>: Returns the <b>year</b> the device was last
    ///     calibrated.</description></item>
    ///   <item><description><b>Month</b>: Returns the <b>month</b> in which the device was last
    ///     calibrated.</description></item>
    ///   <item><description><b>Day</b>: Returns the <b>day</b> on which the device was last
    ///     calibrated.</description></item>
    ///   <item><description><b>Hour</b>: Returns the <b>hour</b> (in 24-hour time) in which the device was
    ///     last calibrated.</description></item>
    ///   <item><description><b>Minute</b>: Returns the <b>minute</b> in which the device was last
    ///     calibrated.</description></item>
    /// </list>
    /// </returns>
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

    /// <summary>Returns the onboard <b>temperature</b> of the device, in degrees Celsius, during the last
    ///   successful external calibration.</summary>
    /// <returns>Returns the onboard <b>temperature</b> of the device, in degrees Celsius, during the last
    ///   successful external calibration.</returns>
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

    /// <summary>Returns the recommended maximum interval, in <b>months</b>, between external
    ///   calibrations.</summary>
    /// <returns>Specifies the recommended maximum interval, in <b>months</b>, between external
    ///   calibrations.</returns>
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

    /// <summary>Returns the date and time the specified type of compensation data for LCR measurements was
    ///   most recently generated. The time returned is 24-hour (military) local time; for example, if the
    ///   selected type of compensation data was generated at 2:30 PM, this function returns 14 for
    ///   <b>hours</b> and 30 for <b>minutes</b>.</summary>
    /// <remarks>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>get_lcr_compensation_last_date_and_time</c>.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="compensationType">Specifies the type of compensation for LCR measurements.</param>
    /// <returns>
    /// A tuple of:
    /// <list type="bullet">
    ///   <item><description><b>Year</b>: Returns the year of the relevant operation.</description></item>
    ///   <item><description><b>Month</b>: Returns the month of the relevant operation.</description></item>
    ///   <item><description><b>Day</b>: Returns the day of the relevant operation.</description></item>
    ///   <item><description><b>Hour</b>: Returns the hour (in 24-hour time) of the relevant
    ///     operation.</description></item>
    ///   <item><description><b>Minute</b>: Returns the minute of the relevant operation.</description></item>
    /// </list>
    /// </returns>
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

    /// <summary>Returns the date and time the specified type of compensation data for LCR measurements was
    ///   most recently generated. The time returned is 24-hour (military) local time; for example, if the
    ///   selected type of compensation data was generated at 2:30 PM, this function returns 14 for
    ///   <b>hours</b> and 30 for <b>minutes</b>.</summary>
    /// <remarks>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>get_lcr_compensation_last_date_and_time</c>.</para>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="compensationTypeRaw">Specifies the type of compensation for LCR measurements.</param>
    /// <returns>
    /// A tuple of:
    /// <list type="bullet">
    ///   <item><description><b>Year</b>: Returns the year of the relevant operation.</description></item>
    ///   <item><description><b>Month</b>: Returns the month of the relevant operation.</description></item>
    ///   <item><description><b>Day</b>: Returns the day of the relevant operation.</description></item>
    ///   <item><description><b>Hour</b>: Returns the hour (in 24-hour time) of the relevant
    ///     operation.</description></item>
    ///   <item><description><b>Minute</b>: Returns the minute of the relevant operation.</description></item>
    /// </list>
    /// </returns>
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

    /// <summary>This function is deprecated. Use <c>GetLCRCompensationData</c> instead.</summary>
    /// <remarks>
    /// <para>Collects previously generated open and short custom cable compensation data so you can then
    ///   apply it to LCR measurements with <c>ConfigureLCRCustomCableCompensation</c>.</para>
    /// <para>Call this function after you have obtained open and short custom cable compensation data. Pass
    ///   the <b>custom cable compensation data</b> to <c>ConfigureLCRCustomCableCompensation</c></para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <returns>The open and short custom cable compensation data to retrieve.</returns>
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

    /// <summary>Returns the next coercion record from the list of coercions the driver performed.</summary>
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

    /// <summary>Returns the next interchange warning from the list of current interchange
    ///   warnings.</summary>
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

    /// <summary>Returns the date and time of the oldest successful self-calibration from among the channels
    ///   in the session.</summary>
    /// <remarks>
    /// <para>The time returned is 24-hour (military) local time; for example, if you have a session using
    ///   channels 1 and 2, and a self-calibration was performed on channel 1 at 2:30 PM, and a
    ///   self-calibration was performed on channel 2 at 3:00 PM on the same day, this function returns 14 for
    ///   <b>hours</b> and 30 for <b>minutes</b>.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>get_self_cal_last_date_and_time</c>.</para>
    /// </remarks>
    /// <returns>
    /// A tuple of:
    /// <list type="bullet">
    ///   <item><description><b>Year</b>: Returns the <b>year</b> the device was last
    ///     calibrated.</description></item>
    ///   <item><description><b>Month</b>: Returns the <b>month</b> in which the device was last
    ///     calibrated.</description></item>
    ///   <item><description><b>Day</b>: Returns the <b>day</b> on which the device was last
    ///     calibrated.</description></item>
    ///   <item><description><b>Hour</b>: Returns the <b>hour</b> (in 24-hour time) in which the device was
    ///     last calibrated.</description></item>
    ///   <item><description><b>Minute</b>: Returns the <b>minute</b> in which the device was last
    ///     calibrated.</description></item>
    /// </list>
    /// </returns>
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

    /// <summary>Returns the onboard temperature of the device, in degrees Celsius, during the oldest
    ///   successful self-calibration from among the channels in the session.</summary>
    /// <remarks>
    /// <para>For example, if you have a session using channels 1 and 2, and you perform a self-calibration
    ///   on channel 1 with a device temperature of 25 degrees Celsius at 2:00, and a self-calibration was
    ///   performed on channel 2 at 27 degrees Celsius at 3:00 on the same day, this function returns 25 for
    ///   the <b>temperature</b> parameter.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// </remarks>
    /// <returns>Returns the onboard <b>temperature</b> of the device, in degrees Celsius, during the oldest
    ///   successful calibration.</returns>
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

    /// <summary>Imports an attribute configuration to the session from the specified configuration
    ///   buffer.</summary>
    /// <remarks>
    /// <para>You can export and import session attribute configurations only between devices with identical
    ///   model numbers and the same number of configured channels.</para>
    /// <para><b>Support for this Function</b></para>
    /// <para>Calling this function in Sequence Source Mode is unsupported.</para>
    /// <para><b>Channel Mapping Behavior for Multichannel Sessions</b></para>
    /// <para>When importing and exporting session attribute configurations between NI‑DCPower sessions that
    ///   were initialized with different channels, the configurations of the exporting channels are mapped to
    ///   the importing channels in the order you specify in the <b>channelName</b> input to the
    ///   <c>InitializeWithChannels</c> function.</para>
    /// <para>For example, if your entry for <b>channelName</b> is 0,1 for the exporting session and 1,2 for
    ///   the importing session:</para>
    /// <list type="bullet">
    ///   <item><description>The configuration exported from channel 0 is imported into channel
    ///     1.</description></item>
    ///   <item><description>The configuration exported from channel 1 is imported into channel
    ///     2.</description></item>
    /// </list>
    /// <para><b>Note:</b></para>
    /// <para>This function will return an error if the total number of channels initialized for the
    ///   exporting session is not equal to the total number of channels initialized for the importing
    ///   session.</para>
    /// </remarks>
    /// <param name="configuration">Specifies the byte array buffer that contains the attribute
    ///   configuration to import.</param>
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

    /// <summary>Imports an attribute configuration to the session from the specified file.</summary>
    /// <remarks>
    /// <para>You can export and import session attribute configurations only between devices with identical
    ///   model numbers and the same number of configured channels.</para>
    /// <para><b>Support for this Function</b></para>
    /// <para>Calling this function in Sequence Source Mode is unsupported.</para>
    /// <para><b>Channel Mapping Behavior for Multichannel Sessions</b></para>
    /// <para>When importing and exporting session attribute configurations between NI‑DCPower sessions that
    ///   were initialized with different channels, the configurations of the exporting channels are mapped to
    ///   the importing channels in the order you specify in the <b>channelName</b> input to the
    ///   <c>InitializeWithChannels</c> function.</para>
    /// <para>For example, if your entry for <b>channelName</b> is 0,1 for the exporting session and 1,2 for
    ///   the importing session:</para>
    /// <list type="bullet">
    ///   <item><description>The configuration exported from channel 0 is imported into channel
    ///     1.</description></item>
    ///   <item><description>The configuration exported from channel 1 is imported into channel
    ///     2.</description></item>
    /// </list>
    /// <para><b>Note:</b></para>
    /// <para>This function will return an error if the total number of channels initialized for the
    ///   exporting session is not equal to the total number of channels initialized for the importing
    ///   session.</para>
    /// </remarks>
    /// <param name="filePath">Specifies the absolute path to the file containing the attribute
    ///   configuration to import. If you specify an empty or relative path, this function returns an error.
    ///   <b>Default File Extension:</b> .nidcpowerconfig</param>
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

    /// <summary>Creates and returns a new NI-DCPower session to the instrument specified in <b>resource
    ///   name</b> to be used in all subsequent NI-DCPower function calls. With this function, you can
    ///   optionally set the initial state of the following session attributes:</summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description><c>NiDCPowerAttribute.Simulate</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.DriverSetup</c></description></item>
    /// </list>
    /// <para>After calling this function, the session will be in the Uncommitted state. Refer to the
    ///   Programming States topic for details about specific software states.</para>
    /// <para>To place the device in a known start-up state when creating a new session, set <b>reset</b> to
    ///   VI_TRUE. This action is equivalent to using the <c>ResetWithChannels</c> function immediately after
    ///   initializing the session.</para>
    /// <para>To open a session and leave the device in its existing configuration without passing through a
    ///   transitional output state, set <b>reset</b> to VI_FALSE. Then configure the device as in the
    ///   previous session, changing only the desired settings, and then call the <c>InitiateWithChannels</c>
    ///   function.</para>
    /// <para>NI-DCPower Python API: <c>__init__</c>.</para>
    /// </remarks>
    /// <param name="serverAddress">Address of the NI gRPC Device Server, for example
    ///   <c>http://localhost:31763</c>.</param>
    /// <param name="resourceName">Specifies the <b>resourceName</b> assigned by Measurement &amp;
    ///   Automation Explorer (MAX), for example "PXI1Slot3" where "PXI1Slot3" is an instrument's
    ///   <b>resourceName</b>. <b>resourceName</b> can also be a logical IVI name.</param>
    /// <param name="channels">Specifies which channel(s) to include in a new session. Specify multiple
    ///   channels by using a channel list or a channel range. A channel list is a comma (,) separated
    ///   sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range is a lower
    ///   bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel (for example,
    ///   0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel configurations are
    ///   performed sequentially based on the order specified in this parameter. If you do not specify any
    ///   channels, by default all channels on the device are included in the session.</param>
    /// <param name="reset">Specifies whether to reset the device during the initialization
    ///   procedure.</param>
    /// <param name="optionString">Specifies the initial value of certain attributes for the session. The
    ///   syntax for <b>optionString</b> is a list of attributes with an assigned value where 1 is VI_TRUE and
    ///   0 is VI_FALSE. For example: "Simulate=0" You do not have to specify a value for all the attributes.
    ///   If you do not specify a value for an attribute, the default value is used. For more information
    ///   about simulating a device, refer to Simulating an Instrument.</param>
    public static DCPower InitializeWithChannels(string serverAddress, string sessionName, string resourceName, string channels, bool reset, string optionString, SessionInitializationBehavior initializationBehavior) =>
        InitializeWithChannels(GrpcChannel.ForAddress(serverAddress), sessionName, resourceName, channels, reset, optionString, initializationBehavior, ownsChannel: true);

    /// <summary>Creates and returns a new NI-DCPower session to the instrument specified in <b>resource
    ///   name</b> to be used in all subsequent NI-DCPower function calls. With this function, you can
    ///   optionally set the initial state of the following session attributes:</summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description><c>NiDCPowerAttribute.Simulate</c></description></item>
    ///   <item><description><c>NiDCPowerAttribute.DriverSetup</c></description></item>
    /// </list>
    /// <para>After calling this function, the session will be in the Uncommitted state. Refer to the
    ///   Programming States topic for details about specific software states.</para>
    /// <para>To place the device in a known start-up state when creating a new session, set <b>reset</b> to
    ///   VI_TRUE. This action is equivalent to using the <c>ResetWithChannels</c> function immediately after
    ///   initializing the session.</para>
    /// <para>To open a session and leave the device in its existing configuration without passing through a
    ///   transitional output state, set <b>reset</b> to VI_FALSE. Then configure the device as in the
    ///   previous session, changing only the desired settings, and then call the <c>InitiateWithChannels</c>
    ///   function.</para>
    /// <para>NI-DCPower Python API: <c>__init__</c>.</para>
    /// </remarks>
    /// <param name="resourceName">Specifies the <b>resourceName</b> assigned by Measurement &amp;
    ///   Automation Explorer (MAX), for example "PXI1Slot3" where "PXI1Slot3" is an instrument's
    ///   <b>resourceName</b>. <b>resourceName</b> can also be a logical IVI name.</param>
    /// <param name="channels">Specifies which channel(s) to include in a new session. Specify multiple
    ///   channels by using a channel list or a channel range. A channel list is a comma (,) separated
    ///   sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range is a lower
    ///   bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel (for example,
    ///   0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel configurations are
    ///   performed sequentially based on the order specified in this parameter. If you do not specify any
    ///   channels, by default all channels on the device are included in the session.</param>
    /// <param name="reset">Specifies whether to reset the device during the initialization
    ///   procedure.</param>
    /// <param name="optionString">Specifies the initial value of certain attributes for the session. The
    ///   syntax for <b>optionString</b> is a list of attributes with an assigned value where 1 is VI_TRUE and
    ///   0 is VI_FALSE. For example: "Simulate=0" You do not have to specify a value for all the attributes.
    ///   If you do not specify a value for an attribute, the default value is used. For more information
    ///   about simulating a device, refer to Simulating an Instrument.</param>
    /// <param name="ownsChannel"><c>true</c> to dispose the gRPC channel together with this object.</param>
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

    /// <summary>Starts generation or acquisition, causing the specified channel(s) to leave the Uncommitted
    ///   state or Committed state and enter the Running state. To return to the Uncommitted state call the
    ///   <c>AbortWithChannels</c> function. Refer to the Programming States topic in the <i>NI DC Power
    ///   Supplies and SMUs Help</i> for information about the specific NI-DCPower software states.</summary>
    /// <remarks>
    /// <para>NI-DCPower Python API: <c>initiate</c>.</para>
    /// </remarks>
    public void Initiate()
    {
        var request = new InitiateRequest
        {
            Vi = Vi,
        };
        var response = _client.Initiate(request);
        Check(response.Status);
    }

    /// <summary>Starts generation or acquisition, causing the specified channel(s) to leave the Uncommitted
    ///   state or Committed state and enter the Running state. To return to the Uncommitted state call the
    ///   <c>AbortWithChannels</c> function. Refer to the Programming States topic in the <i>NI DC Power
    ///   Supplies and SMUs Help</i> for information about the specific NI-DCPower software states.</summary>
    /// <remarks>
    /// <para>NI-DCPower Python API: <c>initiate</c>.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Invalidates the cached value of all attributes in the session.</summary>
    public void InvalidateAllAttributes()
    {
        var request = new InvalidateAllAttributesRequest
        {
            Vi = Vi,
        };
        var response = _client.InvalidateAllAttributes(request);
        Check(response.Status);
    }

    /// <summary>Returns the measured value of either the voltage or current on the specified channel. Each
    ///   call to this function blocks other function calls until the hardware returns the <b>measurement</b>.
    ///   To measure multiple channels, use the <c>MeasureMultiple</c> function.</summary>
    /// <param name="channelName">Specifies the channel to measure. Only one measurement at a time may be
    ///   made with the <c>Measure</c> function. Use the <c>MeasureMultiple</c> function to measure multiple
    ///   channels.</param>
    /// <param name="measurementType">Specifies whether a voltage or current value is measured. <b>Defined
    ///   Values</b>:</param>
    /// <returns>Returns the value of the measurement, either in volts for voltage or amps for
    ///   current.</returns>
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

    /// <summary>Returns the measured value of either the voltage or current on the specified channel. Each
    ///   call to this function blocks other function calls until the hardware returns the <b>measurement</b>.
    ///   To measure multiple channels, use the <c>MeasureMultiple</c> function.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel to measure. Only one measurement at a time may be
    ///   made with the <c>Measure</c> function. Use the <c>MeasureMultiple</c> function to measure multiple
    ///   channels.</param>
    /// <param name="measurementTypeRaw">Specifies whether a voltage or current value is measured.
    ///   <b>Defined Values</b>:</param>
    /// <returns>Returns the value of the measurement, either in volts for voltage or amps for
    ///   current.</returns>
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

    /// <summary>Returns arrays of the measured voltage and current values on the specified channel(s). Each
    ///   call to this function blocks other function calls until the measurements are returned from the
    ///   device. The order of the measurements returned in the array corresponds to the order on the
    ///   specified channel(s).</summary>
    /// <remarks>
    /// <para>NI-DCPower Python API: <c>measure_multiple</c>.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channels to measure. You can specify multiple channels by
    ///   using a channel list or a channel range. A channel list is a comma (,) separated sequence of channel
    ///   names (e.g. 0,2 specifies channels 0 and 2). A channel range is a lower bound channel followed by a
    ///   hyphen (-) or colon (:) followed by an upper bound channel (e.g. 0-2 specifies channels 0, 1, and
    ///   2). If you do not specify a channel name, the function uses all channels in the session.</param>
    /// <returns>
    /// A tuple of:
    /// <list type="bullet">
    ///   <item><description><b>VoltageMeasurements</b>: Returns an array of voltage measurements. The
    ///     measurements in the array are returned in the same order as the channels specified in
    ///     <b>channelName</b>. Ensure that sufficient space has been allocated for the returned
    ///     array.</description></item>
    ///   <item><description><b>CurrentMeasurements</b>: Returns an array of current measurements. The
    ///     measurements in the array are returned in the same order as the channels specified in
    ///     <b>channelName</b>. Ensure that sufficient space has been allocated for the returned
    ///     array.</description></item>
    /// </list>
    /// </returns>
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

    /// <summary>Measures and returns a list of LCRMeasurement instances on the specified
    ///   channel(s).</summary>
    /// <remarks>
    /// <para>To use this function:</para>
    /// <list type="bullet">
    ///   <item><description>Set <c>NiDCPowerAttribute.InstrumentMode</c> property to
    ///     <c>NIDCPOWER_VAL_LCR</c></description></item>
    ///   <item><description>Set <c>NiDCPowerAttribute.MeasureWhen</c> property to
    ///     <c>NIDCPOWER_VAL_ON_DEMAND</c></description></item>
    ///   <item><description>Put the channel(s) in the Running state (call
    ///     <c>InitiateWithChannels</c>)</description></item>
    /// </list>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <returns>A list of LCRMeasurement instances.</returns>
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

    /// <summary>Generates load compensation data for LCR measurements for the test spots you
    ///   specify.</summary>
    /// <remarks>
    /// <para>You must physically configure your LCR circuit with an appropriate reference load to use this
    ///   function to generate valid load compensation data.</para>
    /// <para>When you call this function:</para>
    /// <list type="bullet">
    ///   <item><description>The load compensation data is written to the onboard storage of the instrument.
    ///     Onboard storage can contain only the most recent set of data.</description></item>
    ///   <item><description>Most NI-DCPower attributes in the session are reset to their default values.
    ///     Rewrite the values of any attributes you want to maintain.</description></item>
    /// </list>
    /// <para>To apply the load compensation data you generate with this function to your LCR measurements,
    ///   set the <c>NiDCPowerAttribute.LcrLoadCompensationEnabled</c> property to VI_TRUE.</para>
    /// <para>Load compensation data are generated only for those specific frequencies you define with this
    ///   function; load compensation is not interpolated from the specific frequencies you define and applied
    ///   to other frequencies.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sfequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="compensationSpots">Defines the frequencies and DUT specifications to use for LCR load
    ///   compensation. You can specify &lt;=1000 spot frequencies.</param>
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

    /// <summary>Generates open compensation data for LCR measurements based on a default set of test
    ///   frequencies and, optionally, additional frequencies you can specify.</summary>
    /// <remarks>
    /// <para>You must physically configure an open LCR circuit to use this function to generate valid open
    ///   compensation data.</para>
    /// <para>When you call this function:</para>
    /// <list type="bullet">
    ///   <item><description>The open compensation data is written to the onboard storage of the instrument.
    ///     Onboard storage can contain only the most recent set of data.</description></item>
    ///   <item><description>Most NI-DCPower attributes in the session are reset to their default values.
    ///     Rewrite the values of any attributes you want to maintain.</description></item>
    /// </list>
    /// <para>To apply the open compensation data you generate with this method to your LCR measurements,
    ///   set the <c>NiDCPowerAttribute.LcrOpenCompensationEnabled</c> property to VI_TRUE.</para>
    /// <para>Corrections for frequencies other than the default frequencies or any additional frequencies
    ///   you specify are interpolated.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="additionalFrequencies">Defines a further set of frequencies, in addition to the default
    ///   frequencies, to perform the compensation for. You can specify &lt;=200 additional
    ///   frequencies.</param>
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

    /// <summary>Generates open custom cable compensation data for LCR measurements.</summary>
    /// <remarks>
    /// <para>To use this function, you must physically configure an open LCR circuit to generate valid open
    ///   custom cable compensation data.</para>
    /// <para>When you call this function:</para>
    /// <list type="bullet">
    ///   <item><description>The open compensation data is written to the onboard storage of the instrument.
    ///     Onboard storage can contain only the most recent set of data.</description></item>
    ///   <item><description>Most NI-DCPower attributes in the session are reset to their default values.
    ///     Rewrite the values of any attributes you want to maintain.</description></item>
    /// </list>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Generates short compensation data for LCR measurements based on a default set of test
    ///   frequencies and, optionally, additional frequencies you can specify.</summary>
    /// <remarks>
    /// <para>You must physically configure your LCR circuit with a short to use this function to generate
    ///   valid short compensation data.</para>
    /// <para>When you call this function:</para>
    /// <list type="bullet">
    ///   <item><description>The short compensation data is written to the onboard storage of the instrument.
    ///     Onboard storage can contain only the most recent set of data.</description></item>
    ///   <item><description>Most NI-DCPower attributes in the session are reset to their default values.
    ///     Rewrite the values of any attributes you want to maintain.</description></item>
    /// </list>
    /// <para>To apply the short compensation data you generate with this method to your LCR measurements,
    ///   set the <c>NiDCPowerAttribute.LcrShortCompensationEnabled</c> property to VI_TRUE.</para>
    /// <para>Corrections for frequencies other than the default frequencies or any additional frequencies
    ///   you specify are interpolated.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="additionalFrequencies">Defines a further set of frequencies, in addition to the default
    ///   frequencies, to perform the compensation for. You can specify &lt;=200 additional
    ///   frequencies.</param>
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

    /// <summary>Generates short custom cable compensation data for LCR measurements.</summary>
    /// <remarks>
    /// <para>To use this function:</para>
    /// <list type="bullet">
    ///   <item><description>You must physically configure your LCR circuit with a short to generate valid
    ///     short custom cable compensation data.</description></item>
    ///   <item><description>Set <c>NiDCPowerAttribute.LcrShortCustomCableCompensationEnabled</c> property to
    ///     VI_TRUE</description></item>
    /// </list>
    /// <para>When you call this function:</para>
    /// <list type="bullet">
    ///   <item><description>The short compensation data is written to the onboard storage of the instrument.
    ///     Onboard storage can contain only the most recent set of data.</description></item>
    ///   <item><description>Most NI-DCPower properties in the session are reset to their default values.
    ///     Rewrite the values of any properties you want to maintain.</description></item>
    /// </list>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Queries the specified channel to determine if it is operating at the compliance
    ///   limit.</summary>
    /// <remarks>
    /// <para>The compliance limit is the current limit when the <c>NiDCPowerAttribute.OutputFunction</c>
    ///   property is set to <c>NIDCPOWER_VAL_DC_VOLTAGE</c>, <c>NIDCPOWER_VAL_PULSE_VOLTAGE</c>,
    ///   <c>NIDCPOWER_VAL_CONSTANT_RESISTANCE</c> or <c>NIDCPOWER_VAL_CONSTANT_POWER</c>. If the output is
    ///   operating at the compliance limit, the output reaches the current limit before the desired voltage,
    ///   resistance or power level.</para>
    /// <para>The compliance limit is the voltage limit when the <c>NiDCPowerAttribute.OutputFunction</c>
    ///   property is set to <c>NIDCPOWER_VAL_DC_CURRENT</c> or <c>NIDCPOWER_VAL_PULSE_CURRENT</c>. If the
    ///   output is operating at the compliance limit, the output reaches the voltage limit before the desired
    ///   current level.</para>
    /// <para>Refer to the <c>NiDCPowerAttribute.OutputFunction</c> property for more information about the
    ///   applicable properties to configure for each output function.</para>
    /// <para><b>Note:</b></para>
    /// <para>NI-DCPower uses the terms "source" and "output". However, while sinking with electronic loads
    ///   and SMUs these correspond to "sinking" and "input", respectively.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel to query. Compliance status can only be queried for
    ///   one channel at a time.</param>
    /// <returns>Returns whether the device channel is in compliance.</returns>
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

    /// <summary>Discovers if an output cutoff limit was exceeded for the specified reason. When an output
    ///   cutoff is engaged, the output of the channel(s) is disconnected. If a limit was exceeded, the state
    ///   is latched until you clear it with the <c>ClearLatchedOutputCutoffState</c> function or the
    ///   <c>ResetWithChannels</c> function.</summary>
    /// <remarks>
    /// <para>outputCutoffReason specifies the conditions for which an output is disconnected.</para>
    /// <para><b>Note:</b></para>
    /// <para>NI-DCPower uses the terms "source" and "output". However, while sinking with electronic loads
    ///   and SMUs these correspond to "sinking" and "input", respectively.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="outputCutoffReason">Specifies which output cutoff conditions to query.</param>
    /// <returns>Specifies whether an output cutoff has engaged.</returns>
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

    /// <summary>Discovers if an output cutoff limit was exceeded for the specified reason. When an output
    ///   cutoff is engaged, the output of the channel(s) is disconnected. If a limit was exceeded, the state
    ///   is latched until you clear it with the <c>ClearLatchedOutputCutoffState</c> function or the
    ///   <c>ResetWithChannels</c> function.</summary>
    /// <remarks>
    /// <para>outputCutoffReason specifies the conditions for which an output is disconnected.</para>
    /// <para><b>Note:</b></para>
    /// <para>NI-DCPower uses the terms "source" and "output". However, while sinking with electronic loads
    ///   and SMUs these correspond to "sinking" and "input", respectively.</para>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="outputCutoffReasonRaw">Specifies which output cutoff conditions to query.</param>
    /// <returns>Specifies whether an output cutoff has engaged.</returns>
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

    /// <summary>Queries the maximum current limit on a channel if the channel is set to the specified
    ///   <b>voltageLevel</b>.</summary>
    /// <param name="channelName">Specifies the channel to query. The maximum current limit may only be
    ///   queried for one channel at a time.</param>
    /// <param name="voltageLevel">Specifies the voltage level to use when calculating the
    ///   <b>maxCurrentLimit</b>.</param>
    /// <returns>Returns the maximum current limit that can be set with the specified
    ///   <b>voltageLevel</b>.</returns>
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

    /// <summary>Queries the maximum voltage level on a channel if the channel is set to the specified
    ///   <b>currentLimit</b>.</summary>
    /// <param name="channelName">Specifies the channel to query. The maximum voltage level may only be
    ///   queried for one channel at a time.</param>
    /// <param name="currentLimit">Specifies the current limit to use when calculating the
    ///   <b>maxVoltageLevel</b>.</param>
    /// <returns>Returns the maximum voltage level that can be set on a channel with the specified
    ///   <b>currentLimit</b>.</returns>
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

    /// <summary>Queries the minimum current limit on a channel if the channel is set to the specified
    ///   <b>voltageLevel</b>.</summary>
    /// <param name="channelName">Specifies the channel to query. The minimum current limit may only be
    ///   queried for one channel at a time.</param>
    /// <param name="voltageLevel">Specifies the voltage level to use when calculating the
    ///   <b>minCurrentLimit</b>.</param>
    /// <returns>Returns the minimum current limit that can be set on a channel with the specified
    ///   <b>voltageLevel</b>.</returns>
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

    /// <summary>Queries the specified channel to determine if the channel is currently in the state
    ///   specified by <b>outputState</b>.</summary>
    /// <remarks>
    /// <para><b>Note:</b></para>
    /// <para>NI-DCPower uses the terms "source" and "output". However, while sinking with electronic loads
    ///   and SMUs these correspond to "sinking" and "input", respectively.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel to query. The output state may only be queried for
    ///   one channel at a time.</param>
    /// <param name="outputState">Specifies the output state of the channel that is being queried.
    ///   <b>Defined Values</b>:</param>
    /// <returns>Returns whether the device channel is in the specified output state.</returns>
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

    /// <summary>Queries the specified channel to determine if the channel is currently in the state
    ///   specified by <b>outputState</b>.</summary>
    /// <remarks>
    /// <para><b>Note:</b></para>
    /// <para>NI-DCPower uses the terms "source" and "output". However, while sinking with electronic loads
    ///   and SMUs these correspond to "sinking" and "input", respectively.</para>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel to query. The output state may only be queried for
    ///   one channel at a time.</param>
    /// <param name="outputStateRaw">Specifies the output state of the channel that is being queried.
    ///   <b>Defined Values</b>:</param>
    /// <returns>Returns whether the device channel is in the specified output state.</returns>
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

    /// <summary>Returns the current onboard <b>temperature</b>, in degrees Celsius, of the
    ///   device.</summary>
    /// <returns>Returns the onboard <b>temperature</b>, in degrees Celsius, of the device.</returns>
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

    /// <summary>Resets the specified channel(s) to a known state. This function disables power generation,
    ///   resets session attributes to their default values, commits the session attributes, and leaves the
    ///   session in the Uncommitted state. Refer to the Programming States topic for more information about
    ///   NI-DCPower software states.</summary>
    /// <remarks>
    /// <para>NI-DCPower Python API: <c>reset</c>.</para>
    /// </remarks>
    public void Reset()
    {
        var request = new ResetRequest
        {
            Vi = Vi,
        };
        var response = _client.Reset(request);
        Check(response.Status);
    }

    /// <summary>Resets the device to a known state. The function disables power generation, resets session
    ///   attributes to their default values, clears errors such as overtemperature and unexpected loss of
    ///   auxiliary power, commits the session attributes, and leaves the session in the Uncommitted state.
    ///   This function also performs a hard reset on the device and driver software. This function has the
    ///   same functionality as using reset in Measurement &amp; Automation Explorer. Refer to the Programming
    ///   States topic for more information about NI-DCPower software states.</summary>
    /// <remarks>
    /// <para>This will also open the output relay on devices that have an output relay.</para>
    /// <para><b>Note:</b></para>
    /// <para>NI-DCPower uses the terms "source" and "output". However, while sinking with electronic loads
    ///   and SMUs these correspond to "sinking" and "input", respectively.</para>
    /// </remarks>
    public void ResetDevice()
    {
        var request = new ResetDeviceRequest
        {
            Vi = Vi,
        };
        var response = _client.ResetDevice(request);
        Check(response.Status);
    }

    /// <summary>Resets the interchangeability checking algorithm so that methods and attributes called
    ///   before this method have no effect on future interchangeability checks.</summary>
    public void ResetInterchangeCheck()
    {
        var request = new ResetInterchangeCheckRequest
        {
            Vi = Vi,
        };
        var response = _client.ResetInterchangeCheck(request);
        Check(response.Status);
    }

    /// <summary>Resets the specified channel(s) to a known state. This function disables power generation,
    ///   resets session attributes to their default values, commits the session attributes, and leaves the
    ///   session in the Uncommitted state. Refer to the Programming States topic for more information about
    ///   NI-DCPower software states.</summary>
    /// <remarks>
    /// <para>NI-DCPower Python API: <c>reset</c>.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Resets the device to a known state. This function disables power generation, resets session
    ///   attributes to their default values, commits the session attributes, and leaves the session in the
    ///   Running state. In addition to exhibiting the behavior of the <c>ResetWithChannels</c> function, this
    ///   function can assign user-defined default values for configurable attributes from the IVI
    ///   configuration.</summary>
    public void ResetWithDefaults()
    {
        var request = new ResetWithDefaultsRequest
        {
            Vi = Vi,
        };
        var response = _client.ResetWithDefaults(request);
        Check(response.Status);
    }

    /// <summary>Returns the revision numbers of the driver and the instrument firmware.</summary>
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

    /// <summary>Performs the device self-test routine and returns the test result(s). Calling this function
    ///   implicitly calls the <c>ResetWithChannels</c> function.</summary>
    /// <remarks>
    /// <para>When calling <c>self_test</c> with the PXIe-4162/4163, specify all channels of your
    ///   PXIe-4162/4163 with the channels input of <c>InitializeWithChannels</c>. You cannot self test a
    ///   subset of PXIe-4162/4163 channels.</para>
    /// <para>NI-DCPower Python API: <c>self_test</c>.</para>
    /// </remarks>
    /// <returns>
    /// A tuple of:
    /// <list type="bullet">
    ///   <item><description><b>SelfTestResult</b>: Returns the value result from the device
    ///     self-test.</description></item>
    ///   <item><description><b>SelfTestMessage</b>: Returns the self-test result message. The size of this
    ///     array must be at least 256 bytes.</description></item>
    /// </list>
    /// </returns>
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

    /// <summary>Asserts the specified trigger. This function can override an external edge
    ///   trigger.</summary>
    /// <remarks>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>send_software_edge_trigger</c>.</para>
    /// </remarks>
    /// <param name="trigger">Specifies which trigger to assert. <b>Defined Values:</b></param>
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

    /// <summary>Asserts the specified trigger. This function can override an external edge
    ///   trigger.</summary>
    /// <remarks>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>send_software_edge_trigger</c>.</para>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="triggerRaw">Specifies which trigger to assert. <b>Defined Values:</b></param>
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

    /// <summary>Asserts the specified trigger. This function can override an external edge
    ///   trigger.</summary>
    /// <remarks>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>send_software_edge_trigger</c>.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="trigger">Specifies which trigger to assert. <b>Defined Values:</b></param>
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

    /// <summary>Asserts the specified trigger. This function can override an external edge
    ///   trigger.</summary>
    /// <remarks>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>send_software_edge_trigger</c>.</para>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="triggerRaw">Specifies which trigger to assert. <b>Defined Values:</b></param>
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

    /// <summary>Sets the value of a ViBoolean attribute. This is a low-level function that you can use to
    ///   set the values of device-specific attributes and inherent IVI attributes.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="attributeId">Specifies the ID of an attribute. From the function panel window, you can
    ///   use this control as follows. In the function panel window, click on the control or press
    ///   <b>Enter</b> or the spacebar to display a dialog box containing hierarchical list of the available
    ///   attributes. Attributes whose value cannot be set are dim. Help text is shown for each attribute.
    ///   Select an attribute by double-clicking on it or by selecting it and then pressing <b>Enter</b>.;
    ///   Read-only attributes appear dim in the list box. If you select a read-only attribute, an error
    ///   message appears. A ring control at the top of the dialog box allows you to see all IVI attributes or
    ///   only the attributes of type ViBoolean. If you choose to see all IVI attributes, the data types
    ///   appear to the right of the attribute names in the list box. Attributes with data types other than
    ///   ViBoolean are dim. If you select an attribute data type that is dim, LabWindows/CVI transfers you to
    ///   the function panel for the corresponding function that is consistent with the data type.; If you
    ///   want to enter a variable name, press <b>Ctrl</b>\ +\ <b>T</b> to change this ring control to a
    ///   manual input box. If the attribute in this ring control has named constants as valid values, you can
    ///   view the constants by moving to the value control and pressing <b>Enter</b>.</param>
    /// <param name="attributeValue">Specifies the value to which you want to set the attribute. If the
    ///   attribute currently showing in the attribute ring control has constants as valid values, you can
    ///   view a list of the constants by pressing <b>Enter</b> on this control. Select a value by
    ///   double-clicking on it or by selecting it and then pressing <b>Enter</b>. Note: Some of the values
    ///   might not be valid depending upon the current settings of the device session.</param>
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

    /// <summary>Sets the value of a ViInt32 attribute. This is a low-level function that you can use to set
    ///   the values of device-specific attributes and inherent IVI attributes.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="attributeId">Specifies the ID of an attribute. From the function panel window, you can
    ///   use this control as follows. In the function panel window, click on the control or press
    ///   <b>Enter</b> or the spacebar to display a dialog box containing hierarchical list of the available
    ///   attributes. Attributes whose value cannot be set are dim. Help text is shown for each attribute.
    ///   Select an attribute by double-clicking on it or by selecting it and then pressing <b>Enter</b>.;
    ///   Read-only attributes appear dim in the list box. If you select a read-only attribute, an error
    ///   message appears. A ring control at the top of the dialog box allows you to see all IVI attributes or
    ///   only the attributes of type ViInt32. If you choose to see all IVI attributes, the data types appear
    ///   to the right of the attribute names in the list box. Attributes with data types other than ViInt32
    ///   are dim. If you select an attribute data type that is dim, LabWindows/CVI transfers you to the
    ///   function panel for the corresponding function that is consistent with the data type.; If you want to
    ///   enter a variable name, press <b>Ctrl</b>\ +\ <b>T</b> to change this ring control to a manual input
    ///   box. If the attribute in this ring control has named constants as valid values, you can view the
    ///   constants by moving to the value control and pressing <b>Enter</b>.</param>
    /// <param name="attributeValue">Specifies the value to which you want to set the attribute. If the
    ///   attribute currently showing in the attribute ring control has constants as valid values, you can
    ///   view a list of the constants by pressing <b>Enter</b> on this control. Select a value by
    ///   double-clicking on it or by selecting it and then pressing <b>Enter</b>. Note: Some of the values
    ///   might not be valid depending upon the current settings of the device session.</param>
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

    /// <summary>Sets the value of a ViInt32 attribute. This is a low-level function that you can use to set
    ///   the values of device-specific attributes and inherent IVI attributes.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="attributeId">Specifies the ID of an attribute. From the function panel window, you can
    ///   use this control as follows. In the function panel window, click on the control or press
    ///   <b>Enter</b> or the spacebar to display a dialog box containing hierarchical list of the available
    ///   attributes. Attributes whose value cannot be set are dim. Help text is shown for each attribute.
    ///   Select an attribute by double-clicking on it or by selecting it and then pressing <b>Enter</b>.;
    ///   Read-only attributes appear dim in the list box. If you select a read-only attribute, an error
    ///   message appears. A ring control at the top of the dialog box allows you to see all IVI attributes or
    ///   only the attributes of type ViInt32. If you choose to see all IVI attributes, the data types appear
    ///   to the right of the attribute names in the list box. Attributes with data types other than ViInt32
    ///   are dim. If you select an attribute data type that is dim, LabWindows/CVI transfers you to the
    ///   function panel for the corresponding function that is consistent with the data type.; If you want to
    ///   enter a variable name, press <b>Ctrl</b>\ +\ <b>T</b> to change this ring control to a manual input
    ///   box. If the attribute in this ring control has named constants as valid values, you can view the
    ///   constants by moving to the value control and pressing <b>Enter</b>.</param>
    /// <param name="attributeValueRaw">Specifies the value to which you want to set the attribute. If the
    ///   attribute currently showing in the attribute ring control has constants as valid values, you can
    ///   view a list of the constants by pressing <b>Enter</b> on this control. Select a value by
    ///   double-clicking on it or by selecting it and then pressing <b>Enter</b>. Note: Some of the values
    ///   might not be valid depending upon the current settings of the device session.</param>
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

    /// <summary>Sets the value of a ViInt64 attribute. This is a low-level function that you can use to set
    ///   the values of device-specific attributes and inherent IVI attributes.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="attributeId">Specifies the ID of an attribute. From the function panel window, you can
    ///   use this control as follows. In the function panel window, click on the control or press
    ///   <b>Enter</b> or the spacebar to display a dialog box containing hierarchical list of the available
    ///   attributes. Attributes whose value cannot be set are dim. Help text is shown for each attribute.
    ///   Select an attribute by double-clicking on it or by selecting it and then pressing <b>Enter</b>.;
    ///   Read-only attributes appear dim in the list box. If you select a read-only attribute, an error
    ///   message appears. A ring control at the top of the dialog box allows you to see all IVI attributes or
    ///   only the attributes of type ViReal64. If you choose to see all IVI attributes, the data types appear
    ///   to the right of the attribute names in the list box. Attributes with data types other than ViReal64
    ///   are dim. If you select an attribute data type that is dim, LabWindows/CVI transfers you to the
    ///   function panel for the corresponding function that is consistent with the data type.; If you want to
    ///   enter a variable name, press <b>Ctrl</b>\ +\ <b>T</b> to change this ring control to a manual input
    ///   box. If the attribute in this ring control has named constants as valid values, you can view the
    ///   constants by moving to the value control and pressing <b>Enter</b>.</param>
    /// <param name="attributeValue">Specifies the value to which you want to set the attribute. If the
    ///   attribute currently showing in the attribute ring control has constants as valid values, you can
    ///   view a list of the constants by pressing <b>Enter</b> on this control. Select a value by
    ///   double-clicking on it or by selecting it and then pressing <b>Enter</b>. Note: Some of the values
    ///   might not be valid depending upon the current settings of the device session.</param>
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

    /// <summary>Sets the value of a ViReal64 attribute. This is a low-level function that you can use to
    ///   set the values of device-specific attributes and inherent IVI attributes.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="attributeId">Specifies the ID of an attribute. From the function panel window, you can
    ///   use this control as follows. In the function panel window, click on the control or press
    ///   <b>Enter</b> or the spacebar to display a dialog box containing hierarchical list of the available
    ///   attributes. Attributes whose value cannot be set are dim. Help text is shown for each attribute.
    ///   Select an attribute by double-clicking on it or by selecting it and then pressing <b>Enter</b>.;
    ///   Read-only attributes appear dim in the list box. If you select a read-only attribute, an error
    ///   message appears. A ring control at the top of the dialog box allows you to see all IVI attributes or
    ///   only the attributes of type ViReal64. If you choose to see all IVI attributes, the data types appear
    ///   to the right of the attribute names in the list box. Attributes with data types other than ViReal64
    ///   are dim. If you select an attribute data type that is dim, LabWindows/CVI transfers you to the
    ///   function panel for the corresponding function that is consistent with the data type.; If you want to
    ///   enter a variable name, press <b>Ctrl</b>\ +\ <b>T</b> to change this ring control to a manual input
    ///   box. If the attribute in this ring control has named constants as valid values, you can view the
    ///   constants by moving to the value control and pressing <b>Enter</b>.</param>
    /// <param name="attributeValue">Specifies the value to which you want to set the attribute. If the
    ///   attribute currently showing in the attribute ring control has constants as valid values, you can
    ///   view a list of the constants by pressing <b>Enter</b> on this control. Select a value by
    ///   double-clicking on it or by selecting it and then pressing <b>Enter</b>. Note: Some of the values
    ///   might not be valid depending upon the current settings of the device session.</param>
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

    /// <summary>Sets the value of a ViReal64 attribute. This is a low-level function that you can use to
    ///   set the values of device-specific attributes and inherent IVI attributes.</summary>
    /// <remarks>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="attributeId">Specifies the ID of an attribute. From the function panel window, you can
    ///   use this control as follows. In the function panel window, click on the control or press
    ///   <b>Enter</b> or the spacebar to display a dialog box containing hierarchical list of the available
    ///   attributes. Attributes whose value cannot be set are dim. Help text is shown for each attribute.
    ///   Select an attribute by double-clicking on it or by selecting it and then pressing <b>Enter</b>.;
    ///   Read-only attributes appear dim in the list box. If you select a read-only attribute, an error
    ///   message appears. A ring control at the top of the dialog box allows you to see all IVI attributes or
    ///   only the attributes of type ViReal64. If you choose to see all IVI attributes, the data types appear
    ///   to the right of the attribute names in the list box. Attributes with data types other than ViReal64
    ///   are dim. If you select an attribute data type that is dim, LabWindows/CVI transfers you to the
    ///   function panel for the corresponding function that is consistent with the data type.; If you want to
    ///   enter a variable name, press <b>Ctrl</b>\ +\ <b>T</b> to change this ring control to a manual input
    ///   box. If the attribute in this ring control has named constants as valid values, you can view the
    ///   constants by moving to the value control and pressing <b>Enter</b>.</param>
    /// <param name="attributeValueRaw">Specifies the value to which you want to set the attribute. If the
    ///   attribute currently showing in the attribute ring control has constants as valid values, you can
    ///   view a list of the constants by pressing <b>Enter</b> on this control. Select a value by
    ///   double-clicking on it or by selecting it and then pressing <b>Enter</b>. Note: Some of the values
    ///   might not be valid depending upon the current settings of the device session.</param>
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

    /// <summary>Sets the value of a session attribute.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
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

    /// <summary>Sets the value of a ViString attribute. This is a low-level function that you can use to
    ///   set the values of device-specific attributes and inherent IVI attributes.</summary>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="attributeId">Specifies the ID of an attribute. From the function panel window, you can
    ///   use this control as follows. In the function panel window, click on the control or press
    ///   <b>Enter</b> or the spacebar to display a dialog box containing hierarchical list of the available
    ///   attributes. Attributes whose value cannot be set are dim. Help text is shown for each attribute.
    ///   Select an attribute by double-clicking on it or by selecting it and then pressing <b>Enter</b>.;
    ///   Read-only attributes appear dim in the list box. If you select a read-only attribute, an error
    ///   message appears. A ring control at the top of the dialog box allows you to see all IVI attributes or
    ///   only the attributes of type ViString. If you choose to see all IVI attributes, the data types appear
    ///   to the right of the attribute names in the list box. Attributes with data types other than ViString
    ///   are dim. If you select an attribute data type that is dim, LabWindows/CVI transfers you to the
    ///   function panel for the corresponding function that is consistent with the data type.; If you want to
    ///   enter a variable name, press <b>Ctrl</b>\ +\ <b>T</b> to change this ring control to a manual input
    ///   box. If the attribute in this ring control has named constants as valid values, you can view the
    ///   constants by moving to the value control and pressing <b>Enter</b>.</param>
    /// <param name="attributeValue">Specifies the value to which you want to set the attribute. If the
    ///   attribute currently showing in the attribute ring control has constants as valid values, you can
    ///   view a list of the constants by pressing <b>Enter</b> on this control. Select a value by
    ///   double-clicking on it or by selecting it and then pressing <b>Enter</b>. Note: Some of the values
    ///   might not be valid depending upon the current settings of the device session.</param>
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

    /// <summary>Configures a series of voltage, current, resistance or power outputs and corresponding
    ///   source delays. The source mode must be set to <c>NIDCPOWER_VAL_SEQUENCE</c> for this function to
    ///   take effect.</summary>
    /// <remarks>
    /// <para>Refer to the Configuring the Source Unit topic in the <i>NI DC Power Supplies and SMUs
    ///   Help</i> for more information about how to configure your device.</para>
    /// <para>Use this function in the Uncommitted or Committed programming states. Refer to the Programming
    ///   States topic in the <i>NI DC Power Supplies and SMUs Help</i> for more information about NI-DCPower
    ///   programming states.</para>
    /// <para>Using this function with Advanced Sequence functions for the same channel in the same session
    ///   is not supported.</para>
    /// <para><b>Note:</b></para>
    /// <para>NI-DCPower uses the terms "source" and "output". However, while sinking with electronic loads
    ///   and SMUs these correspond to "sinking" and "input", respectively.</para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel to which this configuration value applies. You can
    ///   only set a sequence for one channel at a time.</param>
    /// <param name="values">Specifies the series of voltage, current, resistance or power levels, depending
    ///   on the configured <c>NiDCPowerAttribute.OutputFunction</c>. <b>Valid values</b>: The valid values
    ///   for this parameter are defined by the voltage level range, current level range, constant resistance
    ///   level range or constant power level range.</param>
    /// <param name="sourceDelays">Specifies the source delay that follows the configuration of each value
    ///   in the sequence. <b>Valid Values</b>: The valid values are between 0 and 167 seconds.</param>
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

    /// <summary>Waits until the specified channel(s) have generated the specified event.</summary>
    /// <remarks>
    /// <para>The session monitors whether each type of event has occurred at least once since the last time
    ///   this function or the <c>InitiateWithChannels</c> function were called. If an event has only been
    ///   generated once and you call this function successively, the function times out. Individual events
    ///   must be generated between separate calls of this function.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>wait_for_event</c>.</para>
    /// </remarks>
    /// <param name="eventId">Specifies which event to wait for. <b>Defined Values:</b></param>
    /// <param name="timeout">Specifies the maximum time allowed for this function to complete, in seconds.
    ///   If the function does not complete within this time interval, NI-DCPower returns an error. Note: When
    ///   setting the timeout interval, ensure you take into account any triggers so that the timeout interval
    ///   is long enough for your application.</param>
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

    /// <summary>Waits until the specified channel(s) have generated the specified event.</summary>
    /// <remarks>
    /// <para>The session monitors whether each type of event has occurred at least once since the last time
    ///   this function or the <c>InitiateWithChannels</c> function were called. If an event has only been
    ///   generated once and you call this function successively, the function times out. Individual events
    ///   must be generated between separate calls of this function.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>wait_for_event</c>.</para>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="eventIdRaw">Specifies which event to wait for. <b>Defined Values:</b></param>
    /// <param name="timeout">Specifies the maximum time allowed for this function to complete, in seconds.
    ///   If the function does not complete within this time interval, NI-DCPower returns an error. Note: When
    ///   setting the timeout interval, ensure you take into account any triggers so that the timeout interval
    ///   is long enough for your application.</param>
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

    /// <summary>Waits until the specified channel(s) have generated the specified event.</summary>
    /// <remarks>
    /// <para>The session monitors whether each type of event has occurred at least once since the last time
    ///   this function or the <c>InitiateWithChannels</c> function were called. If an event has only been
    ///   generated once and you call this function successively, the function times out. Individual events
    ///   must be generated between separate calls of this function.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>wait_for_event</c>.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="eventId">Specifies which event to wait for. <b>Defined Values:</b></param>
    /// <param name="timeout">Specifies the maximum time allowed for this function to complete, in seconds.
    ///   If the function does not complete within this time interval, NI-DCPower returns an error. Note: When
    ///   setting the timeout interval, ensure you take into account any triggers so that the timeout interval
    ///   is long enough for your application.</param>
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

    /// <summary>Waits until the specified channel(s) have generated the specified event.</summary>
    /// <remarks>
    /// <para>The session monitors whether each type of event has occurred at least once since the last time
    ///   this function or the <c>InitiateWithChannels</c> function were called. If an event has only been
    ///   generated once and you call this function successively, the function times out. Individual events
    ///   must be generated between separate calls of this function.</para>
    /// <para><b>Note:</b></para>
    /// <para>This function is not supported on all devices. For more information about supported devices,
    ///   search ni.com for Supported Functions by Device.</para>
    /// <para>NI-DCPower Python API: <c>wait_for_event</c>.</para>
    /// <para>This overload takes the raw integer value instead of the enum member.</para>
    /// </remarks>
    /// <param name="channelName">Specifies the channel(s) to which this configuration value applies.
    ///   Specify multiple channels by using a channel list or a channel range. A channel list is a comma (,)
    ///   separated sequence of channel names (for example, 0,2 specifies channels 0 and 2). A channel range
    ///   is a lower bound channel followed by a hyphen (-) or colon (:) followed by an upper bound channel
    ///   (for example, 0-2 specifies channels 0, 1, and 2). In the Running state, multiple channel
    ///   configurations are performed sequentially based on the order specified in this parameter.</param>
    /// <param name="eventIdRaw">Specifies which event to wait for. <b>Defined Values:</b></param>
    /// <param name="timeout">Specifies the maximum time allowed for this function to complete, in seconds.
    ///   If the function does not complete within this time interval, NI-DCPower returns an error. Note: When
    ///   setting the timeout interval, ensure you take into account any triggers so that the timeout interval
    ///   is long enough for your application.</param>
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
