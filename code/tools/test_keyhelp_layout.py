#!/usr/bin/env python3
"""Read-only verification of prompt recoloring offsets against the game executable."""
import re
import struct
import sys
from pathlib import Path

import capstone
import pefile

root = Path(__file__).resolve().parents[1] / 'p3rpc.femc/p3rpc.femc/Components'
source = (root / 'KeyHelp.cs').read_text(encoding='utf-8-sig')
layout = (root / 'KeyHelpColors.cs').read_text()
constants = {name: int(value, 0) for name, value in
             re.findall(r'const int (\w+) = (0x[0-9A-F]+|\d+);', layout)}
pe = pefile.PE(sys.argv[1], fast_load=True)
decoder = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
decoder.detail = True


def unique(name):
    signature = re.search(r'\b' + name + r' = "([^"]+)"', source)[1]
    pattern = b''.join(b'.' if t == '??' else re.escape(bytes.fromhex(t))
                       for t in signature.split())
    hits = [section.VirtualAddress + m.start() for section in pe.sections
            if section.Characteristics & 0x20000000
            for m in re.finditer(pattern, section.get_data(), re.DOTALL)]
    assert len(hits) == 1, (name, hits)
    return hits[0]


def instruction(rva):
    return next(decoder.disasm(pe.get_data(rva, 16), rva))


anchor = unique('PromptColorLayout_SIG')
assert instruction(anchor).operands[1].mem.disp == constants['SpriteColor']
assert instruction(anchor + 43).operands[1].mem.disp == constants['SpriteStride']
assert instruction(anchor + 49).operands[1].mem.disp == constants['SpriteCount']
assert instruction(anchor + 0x75).operands[0].mem.disp == constants['TextColor']
assert pe.get_data(anchor + 0x75, 6) == bytes.fromhex('89 83 F0 01 00 00')
# Each layout's color is 0x18 bytes into its 0x68-byte inline structure.
assert constants['SpriteColor'] + constants['SpriteCapacity'] * constants['SpriteStride'] == constants['TextColor']

constructor = unique('FKeyHelpButtonFastForward_UpdateState_SIG')
lea = instruction(constructor)
vtable = constructor + lea.size + lea.operands[1].mem.disp
update = struct.unpack('<Q', pe.get_data(vtable + 8, 8))[0] - pe.OPTIONAL_HEADER.ImageBase
assert pe.get_data(update + 0x123, 6) == bytes.fromhex('8B 8F 90 05 00 00')
assert instruction(update + 0x123).operands[1].mem.disp == constants['ActivationState']

movie = unique('FKeyHelpButtonMovie_UpdateState_SIG')
for offset, name, expected in [(0x8CD, 'MovieMainColor', '89 86 1C 04 00 00'),
                               (0xAE4, 'MoviePulseColor', '89 86 DC 04 00 00')]:
    assert pe.get_data(movie + offset, 6) == bytes.fromhex(expected)
    assert instruction(movie + offset).operands[0].mem.disp == constants[name]

assert 'self->SpriteCount' not in source
assert 'self->ActivationState' not in source
print('PASS: unique prompt signatures, runtime guards, all color/count/state offsets, and four-slot capacity')
