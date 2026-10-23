"""Regenerates the "Generated RPC wrappers" region of NIDCPower/DCPower.cs (a wrapper method per RPC) from nidcpower.proto.

Usage (from the repository root): python tools/generate_dcpower_wrapper.py
"""
import os
import re

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "NIDCPower")
PROTO = os.path.join(ROOT, "nidcpower.proto")
OUT = os.path.join(ROOT, "DCPower.cs")
BEGIN = "#region Generated RPC wrappers"
END = "#endregion"

# Implemented by hand in DCPower.cs.
SKIP_RPCS = {"Close", "InitializeWithIndependentChannels"}
FACTORY_RPCS = {"InitializeWithChannels"}

SCALARS = {
    "int32": "int", "sint32": "int", "int64": "long", "uint32": "uint",
    "double": "double", "bool": "bool", "string": "string", "bytes": "ByteString",
}
KEYWORDS = {
    "event", "params", "out", "ref", "in", "lock", "object", "string", "base", "checked",
    "default", "delegate", "fixed", "namespace", "new", "operator", "internal", "is",
}


def pascal(name):
    return "".join(p[:1].upper() + p[1:] for p in name.split("_"))


def camel(name):
    p = pascal(name)
    p = p[:1].lower() + p[1:]
    return "@" + p if p in KEYWORDS else p


def cs_type(proto_type):
    if proto_type in SCALARS:
        return SCALARS[proto_type]
    if proto_type.startswith("nidevice_grpc."):
        return proto_type.split(".", 1)[1]
    return proto_type


class Field:
    def __init__(self, name, ptype, repeated, oneof, deprecated):
        self.name, self.ptype, self.repeated = name, ptype, repeated
        self.oneof, self.deprecated = oneof, deprecated
        self.type = cs_type(ptype)
        self.prop = pascal(name)


def parse_messages(text):
    messages = {}
    for m in re.finditer(r"^message (\w+) \{(.*?)^\}", text, re.M | re.S):
        fields, oneof = [], None
        for line in m.group(2).splitlines():
            line = line.strip()
            om = re.match(r"oneof (\w+) \{", line)
            if om:
                oneof = om.group(1)
                continue
            if line == "}":
                oneof = None
                continue
            fm = re.match(r"(repeated )?([\w.]+) (\w+) = \d+(.*);", line)
            if fm:
                fields.append(Field(fm.group(3), fm.group(2), bool(fm.group(1)), oneof, "deprecated" in fm.group(4)))
        messages[m.group(1)] = fields
    return messages


def params_for(request_fields, raw):
    """Returns [(field, param_name, param_type)] for the request fields."""
    params = []
    for f in request_fields:
        if f.name == "vi" and f.type == "Session":
            continue
        if f.oneof:
            if raw and f.name.endswith("_raw"):
                params.append((f, camel(f.name), "int"))
            elif not raw and not f.name.endswith("_raw"):
                params.append((f, camel(f.name), f.type))
            continue
        name = camel(f.name[:-4]) if f.name.endswith("_raw") else camel(f.name)
        ptype = f"IEnumerable<{f.type}>" if f.repeated else f.type
        params.append((f, name, ptype))
    return params


def return_info(response_fields):
    outs = [f for f in response_fields if f.name not in ("status", "vi") and not f.deprecated]

    def t(f):
        return f"IReadOnlyList<{f.type}>" if f.repeated else f.type

    if not outs:
        return "void", None
    if len(outs) == 1:
        return t(outs[0]), f"response.{outs[0].prop}"
    rtype = "(" + ", ".join(f"{t(f)} {f.prop}" for f in outs) + ")"
    return rtype, "(" + ", ".join(f"response.{f.prop}" for f in outs) + ")"


def emit_method(rpc, req_fields, resp_fields, raw, lines):
    params = params_for(req_fields, raw)
    rtype, expr = return_info(resp_fields)
    sig = ", ".join(f"{pt} {pn}" for _, pn, pt in params)
    variant = " (raw integer value overload)" if raw else ""
    lines.append(f"    /// <summary>Calls the NI-DCPower <c>{rpc}</c> RPC{variant}.</summary>")
    lines.append(f"    public {rtype} {rpc}({sig})")
    lines.append("    {")
    lines.append(f"        var request = new {rpc}Request")
    lines.append("        {")
    lines.append("            Vi = Vi,")
    for f, pn, _ in params:
        if not f.repeated:
            lines.append(f"            {f.prop} = {pn},")
    lines.append("        };")
    for f, pn, _ in params:
        if f.repeated:
            lines.append(f"        request.{f.prop}.AddRange({pn});")
    lines.append(f"        var response = _client.{rpc}(request);")
    lines.append("        Check(response.Status);")
    if expr:
        lines.append(f"        return {expr};")
    lines.append("    }")
    lines.append("")


def emit_factory(rpc, req_fields, lines):
    params = params_for(req_fields, False)
    sig = ", ".join(f"{pt} {pn}" for _, pn, pt in params)
    args = ", ".join(pn for _, pn, _ in params)
    resource = next(pn for f, pn, _ in params if f.name == "resource_name")
    lines.append(f"    /// <summary>Calls the NI-DCPower <c>{rpc}</c> RPC and returns the new session.</summary>")
    lines.append(f"    public static DCPower {rpc}(string serverAddress, {sig}) =>")
    lines.append(f"        {rpc}(GrpcChannel.ForAddress(serverAddress), {args}, ownsChannel: true);")
    lines.append("")
    lines.append(f"    /// <summary>Calls the NI-DCPower <c>{rpc}</c> RPC on an existing channel and returns the new session.</summary>")
    lines.append(f"    public static DCPower {rpc}(GrpcChannel channel, {sig}, bool ownsChannel = false)")
    lines.append("    {")
    lines.append(f"        var dcPower = new DCPower(channel, {resource}, ownsChannel);")
    lines.append("        try")
    lines.append("        {")
    lines.append(f"            var request = new {rpc}Request")
    lines.append("            {")
    for f, pn, _ in params:
        lines.append(f"                {f.prop} = {pn},")
    lines.append("            };")
    lines.append(f"            var response = dcPower._client.{rpc}(request);")
    lines.append("            dcPower._vi = response.Vi;")
    lines.append("            dcPower.Check(response.Status);")
    lines.append("            return dcPower;")
    lines.append("        }")
    lines.append("        catch")
    lines.append("        {")
    lines.append("            dcPower._ownedChannel?.Dispose();")
    lines.append("            throw;")
    lines.append("        }")
    lines.append("    }")
    lines.append("")


def main():
    text = open(PROTO, encoding="utf-8").read()
    messages = parse_messages(text)
    rpcs = re.findall(r"rpc (\w+)\((\w+)\) returns\s*\((\w+)\)", text)
    lines = [
        '    ' + BEGIN,
        '    // Generated by tools/generate_dcpower_wrapper.py from nidcpower.proto. Do not edit by hand.',
        '',
    ]
    count = 0
    for rpc, req, resp in rpcs:
        if rpc in SKIP_RPCS:
            continue
        if rpc in FACTORY_RPCS:
            emit_factory(rpc, messages[req], lines)
        else:
            emit_method(rpc, messages[req], messages[resp], False, lines)
            if any(f.oneof for f in messages[req]):
                emit_method(rpc, messages[req], messages[resp], True, lines)
        count += 1
    lines.append("    " + END)
    source = open(OUT, encoding="utf-8", newline="").read()
    eol = "\r\n" if "\r\n" in source else "\n"
    pattern = re.compile(r"[ \t]*" + re.escape(BEGIN) + r".*?" + re.escape(END), re.S)
    if not pattern.search(source):
        raise SystemExit(f"Markers not found in {OUT}")
    updated = pattern.sub(lambda _: eol.join(lines), source, count=1)
    with open(OUT, "w", encoding="utf-8", newline="") as f:
        f.write(updated)
    print(f"Generated {count} RPC wrappers -> {OUT}")

main()
