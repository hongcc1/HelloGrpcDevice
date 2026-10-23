---
name: ni-grpc-csharp-wrapper
description: Create a documented, OOP-style C# wrapper with auto error capture, plus a runnable (simulated) example, for an NI driver (NI-DCPower, NI-FGEN, NI-DMM, NI-SCOPE, NI-DAQmx, ...) exposed through the NI gRPC Device Server, and verify it builds. Use when the user asks for a C# API/client/wrapper for an NI gRPC driver.
---

# Create a C# wrapper for an NI gRPC driver

Use this skill when the user asks for a C# API, client or wrapper for an NI driver via ni/grpc-device.

The user names an NI driver (for example NI-DCPower, NI-FGEN, NI-DMM, NI-SCOPE, NI-DAQmx) and may supply a documentation URL.
Produce a .NET console project containing a native-feeling OOP class that hides gRPC, plus an example.

## Bundled reference files (this skill is self-contained)

Everything needed is in the `reference\` folder next to this SKILL.md. **Do not assume any other project files exist** -
copy from `reference\` and adapt (replace `DCPower`/`NiDCPower`/`NIDCPOWER_`/`nidcpower` with the target driver's names):

| File | Use |
|---|---|
| `reference\NIDCPower.csproj` | Project file template (packages and `<Protobuf>` items) |
| `reference\DCPower.skeleton.cs` | Hand-written part of the wrapper: exception, constructors, `Close`, `Dispose`, `Check`, and an empty generated region |
| `reference\generate_dcpower_wrapper.py` | Generator: parses the `.proto`, fills the generated region (adapt paths/names/skipped RPCs) |
| `reference\extract_nidcpower_docs.py` | Downloads nimi-python metadata into a JSON docs file |
| `reference\dcpower_docs.py` | Converts the docs to C# XML comments (adapt the attribute mapping/manual summaries) |
| `reference\NOTICE-nimi-python.txt` | MIT notice to ship with generated docs |
| `reference\Program.cs` | Example app: simulated by default, real hardware when a resource name is given |

Place the tool scripts in `tools\` of the new project, and set the paths at the top of the generator (`ROOT`, `PROTO`, `OUT`).
If the target driver does not have a function the skeleton uses (e.g. `InitializeWithIndependentChannels`), use the driver's own
init RPC(s) instead.
Reference: https://github.com/ni/grpc-device/wiki/Creating-a-gRPC-Client

## Step 0 - Gather inputs (ask with `ask_user`, one question at a time)

1. **Driver**: infer from the prompt. Only ask if unclear.
2. **Documentation**:
   - If the user gave a URL (typically the Python API docs, e.g. `https://nifgen.readthedocs.io/en/latest/nifgen.html`), use it.
   - If not, search online for the Python docs (keyword pattern: `"Python NI-<Driver>"`, e.g. "Python NI-Daqmx", "NI-SCOPE Python API").
     **Confirm with the user before using what you found**, and ask whether to also port the Python usage examples to C#.
   - If the user declines documentation: write minimal docs (one-line summary per method from the proto/RPC name) and only the
     default example (initialize session, close session).
3. Do not ask about things you can decide (project name, namespace, folder layout). Follow the conventions below.

## Step 1 - Project setup

- Create a .NET 8 console project `NI<Driver>\` (e.g. `NIFgen`). Packages: `Google.Protobuf`, `Grpc.Net.Client`, `Grpc.Tools`
  (Google.Protobuf 3.28.3, Grpc.Net.Client 2.66.0, Grpc.Tools 2.67.0 with `PrivateAssets="All"`; see `reference\NIDCPower.csproj`). Use `GrpcServices="Client"` on the `<Protobuf>` items.
- Obtain the protos. `session.proto` is no longer on the grpc-device main branch. Take `<driver>.proto`, `nidevice.proto` and
  `session.proto` from the latest **client** release zip (`ni/grpc-device` GitHub Releases, e.g. `ni-grpc-device-client-*.zip`)
  and place them in the project folder. Keep the three files consistent from one release.
- Add `.gitignore` entries: `bin/`, `obj/`, `.vs/`, `__pycache__/`.
- Generated protobuf names strip the enum prefix (`NIDCPOWER_ATTRIBUTE_CHANNEL_COUNT` -> `NiDCPowerAttribute.ChannelCount`).
  Check the generated names in `obj\` before assuming; the casing of the driver prefix varies (`NiFgen`, `NiScope`, `NiDmm`).

## Step 2 - Wrapper class design (single file `<Driver>.cs`)

Start from `reference\DCPower.skeleton.cs`:

- `public sealed class <Driver> : IDisposable` owning the `GrpcChannel`/client and the driver `Session`.
- Exception type `<Driver>Exception` carrying the status code and the message. After **every** RPC call `Check(status)`:
  - negative status -> fetch the text via the driver's `ErrorMessage` RPC and throw;
  - positive status (warning) -> raise a `WarningRaised` event, do not throw.
- Constructors/factories hide the request structs: `Initialize`, `InitializeWithChannels`/`InitializeWithIndependentChannels`
  (whichever exist) as static factories or constructors. `Close` and `Dispose` are hand-written and idempotent; `Dispose` never throws.
- The user never sees `Request`/`Response` protobuf types. Methods take plain C# parameters and return plain values.
  - One output -> return it. Several outputs -> return a named tuple. No outputs -> `void`.
  - Parameters that are enums get an overload taking the proto enum and an overload taking the raw integer (for values not in the enum).
  - Repeated/array fields -> `IEnumerable<T>`/`double[]` as appropriate.
- Add a few hand-written convenience methods for the most common workflow (e.g. configure + measure) at the top of the class.
- Cover **all** RPCs in the service. Skip only those hand-written (`Close`, `Initialize*`). A generator is strongly preferred
  over hand-writing (see Step 3).

## Step 3 - Generate the bulk of the wrapper

Write `tools\generate_<driver>_wrapper.py` (copy and adapt `reference\generate_dcpower_wrapper.py`; run it with `python tools\generate_<driver>_wrapper.py`):

- Parse the `.proto` (rpc names, request/response messages, field names/types, enums, `repeated`, `oneof`).
- Emit methods into a `#region Generated RPC wrappers` ... `#endregion` block at the bottom of `<Driver>.cs`; the generator
  rewrites only that region, so hand-written code above is preserved. Preserve the file's existing line endings.
- Write files with the editor tools (create/edit), not shell redirection or inline scripts - PowerShell mangles backticks and `\n`.
- Print the RPCs that were skipped or have no documentation so gaps are visible.

## Step 4 - Documentation (if the user approved it)

Python docs map well onto the RPCs. Source of truth is the MIT-licensed **nimi-python** metadata
(`https://github.com/ni/nimi-python`, `src/<driver>/metadata/functions.py` and `attributes.py`) - far easier to parse than the
JS-rendered web pages. Pipeline (copy and adapt the `reference\` scripts into `tools\`; the extractor's metadata URL uses `src/<driver>/metadata/`):

1. `extract_<driver>_docs.py` - download the metadata, write `tools\<driver>_docs.json`.
2. `<driver>_docs.py` - convert reStructuredText to C# XML doc comments (`<summary>`, `<param>`, `<returns>`, `<remarks>`).
3. Wire into the generator via a `doc_lines()` helper.
4. Add `tools\NOTICE-nimi-python.txt` (MIT notice) and attribution in the generator header.

Matching rules learned from NI-DCPower:

- Function keys can be lowercase and carry a `grpc_name` field (`error_message`, `self_test`, `close`). Try the exact RPC name,
  the `+WithChannels` form, and the name with `WithChannels` stripped.
- Python exposes many `Configure*` RPCs as **properties**; for these use the matching attribute's documentation
  (e.g. `ConfigureVoltageLevel` -> `VOLTAGE_LEVEL`) and note the Python property name.
- Some metadata `documentation` values are sets, not dicts - guard with `isinstance(d, dict)`.
- Convert: RST links -> `<see href>` or plain text, `**bold**` -> `<b>`, bullet lists -> `<list>`,
  `NIDRIVER_ATTR_X` -> `<c>Attribute.X</c>`, `niDriver_Foo` -> `<c>Foo</c>`; drop "Related Topics" blocks; escape `& < >`.
- For RPCs with no Python equivalent, write a short manual summary in a `MANUAL_SUMMARIES` dict and report which ones to the user
  (these come from your own knowledge, not NI docs - say so).
- Goal: zero methods left with a generic "Calls the X RPC" summary.
- If the user declined docs, still emit a one-line `<summary>` for every method.

## Step 5 - Example (`Program.cs`)

- Usage: `<App> [server_address] [resource_name] [channel...]` (template: `reference\Program.cs`). Default server `localhost:31763`.
- **No resource name -> simulated device**; otherwise real hardware with an empty option string. Use the driver's simulation
  option string (NI-DCPower: `Simulate=1, DriverSetup=Model:4141; BoardType:PXIe`; for other drivers use a valid simulated model
  from the driver docs/Python examples, e.g. `Simulate=1, DriverSetup=Model:<model>; BoardType:PXIe`).
- If the documentation was approved and the Python docs contain usage examples (the "Example" / `.. code-block:: python`
  sections or the nimi-python `examples\` folder), port them to C# using the wrapper API - one example per scenario, selectable
  by a command-line verb or separate classes. Keep them runnable in simulation where the driver supports it.
- Otherwise the example is just: initialize session -> (optionally print driver/revision info) -> close session.
- Always use `using`/`try-finally` so the session is closed and outputs are disabled even on failure.

## Step 6 - Verify (required before reporting done)

1. Run the generator, then `dotnet build` - must give **0 errors and 0 warnings**.
2. Doc check: `dotnet build -p:GenerateDocumentationFile=true -p:NoWarn=1591` - no malformed-XML warnings (CS1570/CS1587 etc.).
   CS1573 (partial `<param>` tags) is acceptable.
3. Run the example in simulated mode. If a gRPC Device Server is reachable (ask the user for address/resource), also run it
   against real hardware. If the server is unreachable, say clearly that the generated methods were only compile-checked.
4. Clean up `__pycache__` and temporary files.

## Rules

- Never commit or push unless the user asks. When asked, commit only (no push).
- Keep the final report short: what was generated, how many RPCs wrapped/documented, what is manual, what was tested.

## Example user prompts

1. "Create an NI gRPC device client in C# for NI-FGEN. Include the documentation from
   https://nifgen.readthedocs.io/en/latest/nifgen.html"
   -> Generate wrapper, docs and ported examples without asking about docs.
2. "Create a C# API for NI-DMM by referring to https://github.com/ni/grpc-device/wiki/Creating-a-gRPC-Client"
   -> No docs supplied: search online for the NI-DMM Python docs, ask the user whether to use them and port the examples.
   If declined: minimal docs + open/close example.
3. "Create a C# API for NI-SCOPE"
   -> Generate the wrapper; suggest https://niscope.readthedocs.io/en/latest/niscope.html and offer to implement its usage examples.
