#!/usr/bin/env python3
"""Verify prompt color patches against P3R and execute its patched palette branch.

Requires pefile/capstone and Linux x86-64 for the native branch probe.
"""
import ctypes
import mmap
import re
import sys
from pathlib import Path

import capstone
import pefile

root = Path(__file__).resolve().parents[1] / 'p3rpc.femc/p3rpc.femc/Components'
source = (root / 'KeyHelp.cs').read_text(encoding='utf-8-sig')
pe = pefile.PE(sys.argv[1], fast_load=True)
decoder = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
decoder.detail = True


def unique(name):
    signature = re.search(r'\b' + name + r' = "([^"]+)"', source)[1]
    pattern = b''.join(b'.' if t == '??' else re.escape(bytes.fromhex(t)) for t in signature.split())
    hits = [section.VirtualAddress + m.start() for section in pe.sections
            if section.Characteristics & 0x20000000
            for m in re.finditer(pattern, section.get_data(), re.DOTALL)]
    assert len(hits) == 1, (name, hits)
    return hits[0]


# Read production patch declarations, checking full instructions, RGB channels,
# native calls, and untouched alpha calculations against the actual executable.
for signature, block in re.findall(r'SigScan\((\w+_SIG),[^\n]*?addr =>(.*?)(?=\n            _context|\n        })', source, re.S):
    anchor = unique(signature)
    for offset, expected, channel in re.findall(r'\((-?(?:0x[0-9A-F]+|\d+)), "([0-9A-F]+)", (\d)\)', block):
        address = anchor + int(offset, 0)
        raw = bytes.fromhex(expected)
        assert pe.get_data(address, len(raw)) == raw, (signature, offset)
        old = list(decoder.disasm(raw, address))
        assert len(old) == 1 and old[0].size == len(raw)
        channel = int(channel)
        replacement = (bytes([0xB1 + channel, 0x57]) if channel < 2 else bytes([0x41, 0xB0, 0x57]))
        replacement += b'\x90' * (len(raw) - len(replacement))
        new = list(decoder.disasm(replacement, address))
        assert new[0].mnemonic == 'mov'
        assert new[0].op_str == ['cl, 0x57', 'dl, 0x57', 'r8b, 0x57'][channel]
        assert all(i.mnemonic == 'nop' for i in new[1:])
    print('PASS: native color inputs', signature)

anchor = unique('PromptPalette_SIG')
expected = bytes.fromhex('440FB6B3550200000FB6F0440FB6BB56020000')
assert pe.get_data(anchor, len(expected)) == expected
# Ensure preceding white/disabled branches and their targets have not changed.
start = anchor - 0x38
original = pe.get_data(start, 0x4b)
assert original == bytes.fromhex(
    '4038AB5702000075094038AB5A020000740340B6800FB683540200003CFF7518'
    '3883550200007510440FB6F6440FB6FE3883560200007413') + expected
assert 'IHook<' not in source and 'OriginalFunction' not in source
assert 'KeyHelpColors.' not in source and 'GetIndirectAddressLong' not in source

# Execute the real native branch with the production replacement opcodes. Guard
# bytes detect writes into the object (including its vtable); flags/alpha inputs
# and white/disabled states retain their native behavior.
for red, green, blue in [(0x12, 0x34, 0x56), (0, 0, 0), (255, 255, 255), (0xa6, 6, 0x52)]:
    branch = bytearray(original)
    for offset_text, length, rex, opcode, channel in re.findall(r'PatchPaletteByte\(address( \+ \d+)?, (\d+), (0x[0-9A-F]+), (0x[0-9A-F]+), \(\) => _context._config.ButtonPromptHighlightColor.([RGB])', source):
        offset = int(offset_text.split('+')[1]) if offset_text else 0
        length, rex, opcode = int(length), int(rex, 0), int(opcode, 0)
        value = {'R': red, 'G': green, 'B': blue}[channel]
        branch[0x38 + offset:0x38 + offset + length] = bytes([rex, opcode, value]) + b'\x90' * (length - 3)
    # SysV function(input pointer): save nonvolatile registers; prepare native
    # inputs; run branch; pack SIL/R14B/R15B into a return value; restore.
    prologue = bytes.fromhex('535556415641574889FB31EDBEFF000000')
    epilogue = bytes.fromhex('400FB6C6410FB6D6C1E20809D0410FB6D7C1E21009D0415F415E5E5D5BC3')
    code = prologue + branch + epilogue
    with mmap.mmap(-1, len(code), prot=mmap.PROT_READ | mmap.PROT_WRITE | mmap.PROT_EXEC) as executable:
        executable.write(code)
        probe = ctypes.CFUNCTYPE(ctypes.c_uint32, ctypes.c_void_p)(ctypes.addressof(ctypes.c_char.from_buffer(executable)))
        for native in [(0x24, 255, 255), (255, 255, 255)]:
            for disabled in [0, 1]:
                for fading in [0, 1]:
                    memory = (ctypes.c_ubyte * 0x600)(*([0xa5] * 0x600))
                    memory[0x254:0x257] = native
                    memory[0x257], memory[0x25a] = disabled, fading
                    before = bytes(memory)
                    actual = probe(ctypes.addressof(memory))
                    white = 0x808080 if disabled or fading else 0xffffff
                    wanted = white if native == (255, 255, 255) else red | green << 8 | blue << 16
                    assert actual == wanted, (native, disabled, fading, hex(actual), hex(wanted))
                    assert bytes(memory) == before
print('PASS: 32 native palette cases; white/disabled states preserved; no object memory writes')
