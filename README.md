# HelloGrpcDevice - NI gRPC C# wrapper skill

This repository contains a Copilot **skill**, `ni-grpc-csharp-wrapper`, that generates a C# client for an NI driver exposed by the
[NI gRPC Device Server](https://github.com/ni/grpc-device). Following the official
[Creating a gRPC Client](https://github.com/ni/grpc-device/wiki/Creating-a-gRPC-Client) wiki, it produces:

- A .NET 8 console project with the `.proto` files and gRPC packages (protos come from the official
  [ni/grpc-device releases](https://github.com/ni/grpc-device/releases)).
- A single-file, native-feeling OOP wrapper class (for example `DCPower.cs`) that hides the gRPC request/response types and
  automatically captures driver errors (exception on error, `WarningRaised` event on warning).
- XML documentation on every method, adapted from the equivalent NI Python API docs
  ([nimi-python](https://github.com/ni/nimi-python), MIT licence).
- A runnable example that uses a simulated device by default, and ports the Python usage examples to C# when documentation is used.
- A verified build (0 errors, 0 warnings).

## Layout

```
.github/skills/ni-grpc-csharp-wrapper/
  SKILL.md        # the skill instructions
  reference/      # templates the skill copies and adapts (csproj, wrapper skeleton, generator and doc scripts, example app)
```

The skill is self-contained; copy the whole `ni-grpc-csharp-wrapper` folder to reuse it elsewhere.

## How to use

1. Put the skill folder where your Copilot agent loads skills (this repo's `.github/skills/` works when the repo is the workspace,
   or copy it to your personal skills folder).
2. Install prerequisites: .NET 8+ SDK and Python 3 (for the generator scripts). To run the example you need an NI gRPC Device Server
   (default `localhost:31763`); simulated mode needs no hardware.
3. Ask Copilot for a wrapper, for example:
   - `Create an NI gRPC device client in C# for NI-FGEN. Include the documentation from https://nifgen.readthedocs.io/en/latest/nifgen.html`
     - generates the wrapper, documentation and examples.
   - `Create a C# API for NI-DMM by referring to https://github.com/ni/grpc-device/wiki/Creating-a-gRPC-Client`
     - no documentation given, so Copilot searches for the Python docs and asks whether to use them.
   - `Create a C# API for NI-SCOPE` - generates the wrapper and suggests adding the online documentation and examples.
4. Run the generated app: `dotnet run -- [server_address] [resource_name] [channel...]`.
   With no resource name it runs on a simulated device; with a resource name (for example `SMU1`) it runs on real hardware.

## License

Released under the [MIT License](LICENSE). The software is provided "as is", without warranty of any kind; use it at your own risk,
including when driving real hardware.

## Notes

- Generated documentation text is adapted from nimi-python (MIT licence); the notice is shipped in `reference/NOTICE-nimi-python.txt`.
- Trademarks: NI and the driver names belong to their respective owners; this project is not affiliated with NI.
