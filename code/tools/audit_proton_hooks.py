#!/usr/bin/env python3
"""Inventory UI signatures against a local P3R executable (requires pefile, capstone).

Read-only: emits JSON for review; does not patch the game. A matching signature
alone does not prove that a hook's register/stack assumptions are correct.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

import capstone
import pefile


def audit(executable, components):
    data = executable.read_bytes()
    pe = pefile.PE(data=data, fast_load=True)
    sections = [(s.VirtualAddress, s.get_data()) for s in pe.sections
                if s.Characteristics & 0x20000000]
    decoder = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
    results = []
    for source in sorted(components.glob('*.cs')):
        text = source.read_text(encoding='utf-8-sig')
        # Keep string literals, but exclude disabled/commented-out signatures.
        text = re.sub(r'"(?:\\.|[^"\\])*"|//[^\n]*|/\*.*?\*/',
                      lambda m: re.sub(r'[^\n]', ' ', m[0]) if m[0].startswith('/') else m[0],
                      text, flags=re.DOTALL)
        entries = [(m[1], m[2], m.start()) for m in re.finditer(
            r'(?:private|public)\s+(?:static\s+)?(?:readonly\s+)?string\s+(\w+)\s*=\s*"([^"\n]+)"', text)]
        for array in re.finditer(r'private\s+string\[\]\s+(\w+)\s*=\s*\[(.*?)\];', text, re.DOTALL):
            entries.extend((f'{array[1]}[{i}]', m[1], array.start())
                           for i, m in enumerate(re.finditer(r'"([^"\n]+)"', array[2])))
        for name, signature, position in entries:
            if not signature.strip():
                continue
            tokens = signature.split()
            if any(t != '??' and not re.fullmatch(r'[0-9A-Fa-f]{2}', t) for t in tokens):
                results.append({'file': source.name, 'name': name, 'signature': signature,
                                'error': 'invalid signature token', 'matches': []})
                continue
            pattern = re.compile(b''.join(b'.' if t == '??' else re.escape(bytes.fromhex(t)) for t in tokens), re.DOTALL)
            # Search a literal anchor first, rather than regex-scanning hundreds
            # of megabytes for each signature beginning with wildcards/calls.
            runs = list(re.finditer(r'(?:[0-9A-Fa-f]{2} ?)+', signature))
            anchor_run = max(runs, key=lambda m: len(m[0].split()))
            anchor = bytes.fromhex(anchor_run[0])
            anchor_offset = len(signature[:anchor_run.start()].split())
            hits = []
            for base, section in sections:
                found = section.find(anchor)
                while found >= 0:
                    offset = found - anchor_offset
                    found = section.find(anchor, found + 1)
                    if offset < 0 or not pattern.match(section, offset):
                        continue
                    rva = base + offset
                    instructions = [f'{i.address:#x}: {i.mnemonic} {i.op_str}'
                                    for i in decoder.disasm(section[offset:offset + 64], rva)]
                    hits.append({'rva': hex(rva), 'instructions': instructions})
            results.append({'file': source.name, 'line': text.count('\n', 0, position) + 1,
                            'name': name, 'signature': signature, 'matches': hits})
    return {'exe_sha256': hashlib.sha256(data).hexdigest(), 'signatures': results}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('executable', type=Path)
    parser.add_argument('--components', type=Path,
                        default=Path(__file__).resolve().parents[1] / 'p3rpc.femc/p3rpc.femc/Components')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = audit(args.executable, args.components)
    args.output.write_text(json.dumps(result, indent=2) + '\n')
    counts = {k: sum(len(s['matches']) == k for s in result['signatures']) for k in (0, 1)}
    print(f"{len(result['signatures'])} signatures: {counts[1]} unique, {counts[0]} absent, "
          f"{len(result['signatures']) - sum(counts.values())} ambiguous")
