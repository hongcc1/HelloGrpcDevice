"""Extracts NI-DCPower documentation text from the nimi-python metadata into tools/nidcpower_docs.json.

nimi-python (https://github.com/ni/nimi-python) is MIT licensed, Copyright (c) 2017-2022 National Instruments
Corporation. Its metadata is the source of the NI-DCPower Python API documentation
(https://nidcpower.readthedocs.io/en/latest/).

Usage: python tools/extract_nidcpower_docs.py
"""
import json
import os
import urllib.request

BASE = "https://raw.githubusercontent.com/ni/nimi-python/master/src/nidcpower/metadata/"
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "nidcpower_docs.json")


def load(name):
    with urllib.request.urlopen(BASE + name + ".py") as response:
        source = response.read().decode("utf-8")
    namespace = {}
    exec(source, namespace)
    return namespace[name]


def doc(entry):
    d = entry.get("documentation", {})
    if not isinstance(d, dict):
        return {}
    return {k: d[k].strip() for k in ("description", "note") if isinstance(d.get(k), str) and d[k].strip()}


def main():
    functions, attributes = load("functions"), load("attributes")
    out = {
        "_source": "nimi-python src/nidcpower/metadata (MIT License, Copyright (c) 2017-2022 National Instruments Corporation)",
        "functions": {},
        "attributes": {},
    }
    for key, fn in functions.items():
        if "documentation" not in fn:
            continue
        entry = doc(fn)
        entry["python_name"] = fn.get("python_name") or fn.get("method_name_for_documentation")
        entry["params"] = {
            p["name"].lower(): {"direction": p["direction"], **doc(p)}
            for p in fn["parameters"]
            if "documentation" in p
        }
        out["functions"][fn.get("grpc_name", key)] = entry
    for attr in attributes.values():
        if "documentation" in attr:
            out["attributes"][attr["name"]] = doc(attr)
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(out, f, indent=1, sort_keys=True)
        f.write("\n")
    print(f"{len(out['functions'])} functions, {len(out['attributes'])} attributes -> {OUT}")


main()
