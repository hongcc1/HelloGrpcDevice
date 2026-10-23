"""Builds C# XML documentation for the DCPower wrapper from tools/nidcpower_docs.json.

The text comes from the NI-DCPower Python API documentation sources (nimi-python, MIT License,
Copyright (c) 2017-2022 National Instruments Corporation); see tools/NOTICE-nimi-python.txt.
"""
import json
import os
import re
import textwrap

DOCS_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "nidcpower_docs.json")

# RPCs without an entry in the Python metadata (it exposes them as properties or private helpers).
MANUAL_SUMMARIES = {
    "ClearError": "Clears the error information for the current execution thread and the session.",
    "ClearInterchangeWarnings": "Clears the list of current interchange warnings.",
    "ErrorQuery": "Reads an error code and message from the instrument's error queue.",
    "ExportSignal": "Routes the specified signal to the specified output terminal.",
    "GetAttributeViSession": "Queries the value of a session attribute.",
    "SetAttributeViSession": "Sets the value of a session attribute.",
    "GetNextCoercionRecord": "Returns the next coercion record from the list of coercions the driver performed.",
    "GetNextInterchangeWarning": "Returns the next interchange warning from the list of current interchange warnings.",
    "InvalidateAllAttributes": "Invalidates the cached value of all attributes in the session.",
    "ResetInterchangeCheck": (
        "Resets the interchangeability checking algorithm so that methods and attributes called before "
        "this method have no effect on future interchangeability checks."
    ),
    "RevisionQuery": "Returns the revision numbers of the driver and the instrument firmware.",
}

# Attributes (Python properties) that back a Configure* RPC when the name does not match exactly.
ATTRIBUTE_OVERRIDES = {
    "ConfigureOvp": ["OVP_ENABLED", "OVP_LIMIT"],
    "ConfigureCurrentLimit": ["CURRENT_LIMIT_BEHAVIOR", "CURRENT_LIMIT"],
}

COMMON_PARAMS = ("channelname", "timeout")
OWNS_CHANNEL = "<c>true</c> to dispose the gRPC channel together with this object."
SERVER_ADDRESS = "Address of the NI gRPC Device Server, for example <c>http://localhost:31763</c>."


def snake(name):
    return re.sub(r"(?<!^)(?=[A-Z])", "_", re.sub(r"([A-Z]+)([A-Z][a-z])", r"\1_\2", name)).upper()


def pascal(name):
    return "".join(p[:1] + p[1:].lower() for p in name.split("_"))


def escape(text):
    return text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


class Docs:
    def __init__(self, rpcs, attribute_members):
        with open(DOCS_PATH, encoding="utf-8") as f:
            data = json.load(f)
        self.functions = data["functions"]
        self.attributes = data["attributes"]
        self.rpcs = set(rpcs)
        self.attribute_members = set(attribute_members)
        self.common = {}
        for fn in self.functions.values():
            for key in COMMON_PARAMS:
                if key in fn["params"] and key not in self.common:
                    self.common[key] = fn["params"][key]

    # ---- text conversion -------------------------------------------------------------------------------

    def inline(self, text):
        links = []

        def keep_link(m):
            label = " ".join(m.group(1).split())
            target = m.group(2).strip()
            links.append((label, target if target.startswith("http") else None))
            return f"\x00{len(links) - 1}\x00"

        text = re.sub(r"`([^`<]+?)\s*<([^>]*)>`_{1,2}", keep_link, text)
        text = escape(" ".join(text.split()))
        text = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", text)
        text = re.sub(r"(?<![\w*])\*([^*\s][^*]*?)\*(?![\w*])", r"<i>\1</i>", text)
        text = re.sub(r"NIDCPOWER_ATTR_(\w+)", self._attribute_ref, text)
        text = re.sub(r"\bniDCPower_(\w+)", self._function_ref, text)
        text = re.sub(r"(?<![\w.>])(NIDCPOWER_VAL_\w+)", r"<c>\1</c>", text)
        text = re.sub(r"``(.+?)``", r"<c>\1</c>", text)

        def restore(m):
            label, target = links[int(m.group(1))]
            label = escape(label)
            return f'<see href="{escape(target)}">{label}</see>' if target else label

        return re.sub(r"\x00(\d+)\x00", restore, text)

    def _attribute_ref(self, m):
        member = pascal(m.group(1))
        return f"<c>NiDCPowerAttribute.{member}</c>" if member in self.attribute_members else f"<c>{m.group(0)}</c>"

    def _function_ref(self, m):
        name = m.group(1)
        for candidate in (name, name + "WithChannels"):
            if candidate in self.rpcs:
                return f"<c>{candidate}</c>"
        return f"<c>{name}</c>"

    @staticmethod
    def blocks(text):
        """Splits reStructuredText-ish text into [('p', text) | ('ul', [items])] blocks."""
        blocks, para, items = [], [], []

        def flush_para():
            if para:
                blocks.append(("p", " ".join(para)))
                para.clear()

        def flush_items():
            if items:
                blocks.append(("ul", list(items)))
                items.clear()

        for raw in text.split("\n"):
            line = raw.rstrip()
            bullet = re.match(r"^\s*[-*]\s+(.*)", line)
            if not line.strip():
                flush_para()
            elif bullet:
                flush_para()
                items.append(bullet.group(1))
            elif items and line[:1].isspace():
                items[-1] += " " + line.strip()
            else:
                flush_items()
                para.append(line.strip().lstrip("|").strip())
        flush_para()
        flush_items()

        for i, (kind, value) in enumerate(blocks):
            if kind == "p" and value.startswith("**Related Topics"):
                return blocks[:i]
        return blocks

    def render_blocks(self, blocks):
        lines = []
        for kind, value in blocks:
            if kind == "p":
                lines += wrap(f"<para>{self.inline(value)}</para>")
            else:
                lines.append('<list type="bullet">')
                for item in value:
                    lines += wrap(f"<item><description>{self.inline(item)}</description></item>", indent="  ")
                lines.append("</list>")
        return lines

    def flat(self, entry):
        """Single-line text for <param>/<returns> from a description (+ note)."""
        parts = []
        for key, label in (("description", ""), ("note", "Note: ")):
            for kind, value in self.blocks(entry.get(key, "")):
                parts.append(label + ("; ".join(value) if kind == "ul" else value))
                label = ""
        return self.inline(" ".join(parts))

    # ---- lookups ---------------------------------------------------------------------------------------

    def find_function(self, rpc):
        base = rpc.replace("WithChannels", "")
        for key in (rpc, rpc + "WithChannels", base, base + "WithChannels"):
            if key in self.functions:
                return self.functions[key]
        return None

    def find_attributes(self, rpc):
        names = ATTRIBUTE_OVERRIDES.get(rpc)
        if not names:
            base = rpc.replace("WithChannels", "")
            m = re.match(r"Configure(DigitalEdge)(\w+)Trigger$", base)
            if m:
                x = snake(m.group(2))
                names = [f"DIGITAL_EDGE_{x}_TRIGGER_INPUT_TERMINAL", f"DIGITAL_EDGE_{x}_TRIGGER_EDGE"]
            else:
                m = re.match(r"(?:ConfigureSoftwareEdge|Disable)(\w+)Trigger$", base)
                names = [f"{snake(m.group(1))}_TRIGGER_TYPE"] if m else [snake(re.sub(r"^Configure", "", base))]
        return [(n, self.attributes[n]) for n in names if n in self.attributes]

    def trigger_lead(self, rpc):
        base = rpc.replace("WithChannels", "")
        m = re.match(r"(ConfigureSoftwareEdge|Disable)(\w+)Trigger$", base)
        if not m:
            return None
        name = " ".join(snake(m.group(2)).lower().split("_"))
        if m.group(1) == "Disable":
            return f"Disables the {name} trigger."
        send = "SendSoftwareEdgeTrigger" in self.rpcs
        suffix = " Use <c>SendSoftwareEdgeTrigger</c> to generate the trigger." if send else ""
        return f"Configures the {name} trigger to be a software edge trigger.{suffix}"

    # ---- public ----------------------------------------------------------------------------------------

    def for_rpc(self, rpc, params, outputs, raw=False):
        """Returns XML doc lines (without the leading ///) for an RPC wrapper.

        params: names of the generated C# parameters; outputs: [response field names].
        Returns (lines, documented) where documented is False if only the generic summary was available.
        """
        fn = self.find_function(rpc)
        summary, remarks, param_docs, out_docs = [], [], {}, {}

        if fn:
            blocks = self.blocks(fn.get("description", ""))
            first = blocks[0] if blocks and blocks[0][0] == "p" else None
            summary = [self.inline(first[1])] if first else []
            remarks += self.render_blocks(blocks[1:] if first else blocks)
            if fn.get("note"):
                remarks += ["<para><b>Note:</b></para>"] + self.render_blocks(self.blocks(fn["note"]))
            if fn.get("python_name"):
                remarks.append(f"<para>NI-DCPower Python API: <c>{fn['python_name']}</c>.</para>")
            for name, info in fn["params"].items():
                (out_docs if info["direction"] == "out" else param_docs)[name] = info
        else:
            attrs = self.find_attributes(rpc)
            lead = MANUAL_SUMMARIES.get(rpc.replace("WithChannels", "")) or self.trigger_lead(rpc)
            attr_blocks = [(n, self.blocks(a.get("description", ""))) for n, a in attrs]
            if lead:
                summary = [lead]
            elif attr_blocks and attr_blocks[0][1] and attr_blocks[0][1][0][0] == "p":
                summary = [self.inline(attr_blocks[0][1][0][1])]
                attr_blocks[0] = (attr_blocks[0][0], attr_blocks[0][1][1:])
            for name, blocks in attr_blocks:
                if blocks:
                    remarks.append(f"<para>Corresponds to the Python property <c>{name.lower()}</c>:</para>")
                    remarks += self.render_blocks(blocks)
                elif lead:
                    remarks.append(f"<para>Corresponds to the Python property <c>{name.lower()}</c>.</para>")

        if raw:
            remarks.append("<para>This overload takes the raw integer value instead of the enum member.</para>")

        documented = bool(summary)
        if not summary:
            summary = [f"Calls the NI-DCPower <c>{rpc}</c> RPC."]

        lines = wrap("<summary>" + summary[0] + "</summary>")
        if remarks:
            lines += ["<remarks>"] + remarks + ["</remarks>"]
        for name in params:
            key = name.lstrip("@")
            key = (key[:-3] if key.endswith("Raw") and raw else key).lower()
            if name == "ownsChannel":
                text = OWNS_CHANNEL
            elif name == "serverAddress":
                text = SERVER_ADDRESS
            else:
                info = param_docs.get(key) or self.common.get(key)
                text = self.flat(info) if info else ""
            if text:
                lines += wrap(f'<param name="{name}">{text}</param>')
        out_texts = [(o, self.flat(out_docs[o.replace("_", "").lower()])) for o in outputs
                     if o.replace("_", "").lower() in out_docs]
        if len(outputs) == 1 and out_texts:
            lines += wrap(f"<returns>{out_texts[0][1]}</returns>")
        elif len(outputs) > 1 and out_texts:
            lines.append("<returns>")
            lines.append("A tuple of:")
            lines.append('<list type="bullet">')
            for o, text in out_texts:
                title = "".join(p[:1].upper() + p[1:] for p in o.split("_"))
                lines += wrap(f"<item><description><b>{title}</b>: {text}</description></item>", indent="  ")
            lines += ["</list>", "</returns>"]
        return lines, documented


def wrap(text, indent="", width=100):
    if len(text) <= width:
        return [indent + text]
    # Wrap on spaces; tags such as <see href="..."> may span lines, which is valid XML.
    first, *rest = textwrap.wrap(text, width=width, break_long_words=False, break_on_hyphens=False)
    return [indent + first] + [indent + "  " + r for r in rest]
