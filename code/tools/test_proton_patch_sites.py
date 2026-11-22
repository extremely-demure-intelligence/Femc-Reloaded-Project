#!/usr/bin/env python3
"""Verify the new immediate patches against a local, unmodified P3R executable.

Requires pefile and capstone. Does not modify the executable. Checks unique
signature matches and that each write stays within its intended immediate.
"""
import argparse
import re
from pathlib import Path

import capstone
import pefile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('executable', type=Path)
args = parser.parse_args()
pe = pefile.PE(str(args.executable), fast_load=True)
sections = [(s.VirtualAddress, s.get_data()) for s in pe.sections if s.Characteristics & 0x20000000]
root = Path(__file__).resolve().parents[1] / 'p3rpc.femc/p3rpc.femc/Components'
decoder = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
decoder.detail = True


def unique(file, name):
    source = (root / file).read_text(encoding='utf-8-sig')
    sig = re.search(r'\b' + name + r'\s*=\s*"([^"]+)"', source)[1]
    pattern = b''.join(b'.' if t == '??' else re.escape(bytes.fromhex(t)) for t in sig.split())
    hits = [(rva + m.start(), data[m.start():m.start() + len(sig.split())])
            for rva, data in sections for m in re.finditer(b'(?=(' + pattern + b'))', data, re.DOTALL)]
    assert len(hits) == 1, (file, name, len(hits))
    return hits[0]


patches = [
    ('Camp.cs', 'UUICmpCalendarDraw_DrawMonthDisplay_PrevMonthColor_SIG', [(7, 2), (15, 1)]),
    ('Camp.cs', 'UUICmpCalendarDraw_DrawMonthDisplay_CurrMonthColor_SIG', [(7, 2), (15, 1)]),
    ('Camp.cs', 'UUICmpCalendarDraw_DrawMonthDisplay_NextMonthColor_SIG', [(7, 2), (19, 1)]),
    ('Tutorial.cs', 'AUITutorialDraw_ListEntryDotColor_SIG', [(7, 2), (15, 1)]),
    ('SocialStats.cs', 'UCmpHeroHumanStatusDraw_DrawStatUpRings_SIG', [(5, 2), (11, 1)]),
    ('Camp.cs', 'UUICmpCalendar_DrawDayEvents_DateBg_SIG', [(4, 2), (12, 1)]),
    ('Camp.cs', 'UCmpSkillDraw_DrawNoUsableSkillNoneGraphic_SIG', [(2, 4)]),
    ('Camp.cs', 'UCmpSystemDraw_GetMenuColorNoSelect_SIG', [(2, 4)]),
    ('MsgWindowMind.cs', 'UMsgProcWindow_Select_Mind_SelectedTextColor_SIG', [(2, 4)]),
]
for file, name, writes in patches:
    rva, data = unique(file, name)
    instructions = list(decoder.disasm(data, rva))
    for offset, size in writes:
        assert any(i.address + i.imm_offset == rva + offset and i.imm_size == size
                   for i in instructions), (name, offset, size)
    print('PASS:', name)

# The system menu's indexed read uses an image-relative table displacement.
rva, data = unique('Camp.cs', 'UCmpSystemDraw_GetMenuColors_SIG')
instruction = next(decoder.disasm(data, rva))
assert instruction.mnemonic == 'mov' and instruction.op_str.startswith('edx, dword ptr [')
assert instruction.disp_offset == 4 and instruction.disp_size == 4
table_rva = instruction.disp
assert pe.get_section_by_rva(table_rva).Characteristics & 0x80000000
assert len(pe.get_data(table_rva, 7 * 4)) == 7 * 4
print('PASS: system menu table is mapped and writable')

unique('MsgWindowSystem.cs', 'AitfMsgProgWindow_TUTRIALDraw_DrawBottomRightColor_SIG')
print('PASS: repaired system-message signature is unique')

rva, data = unique('Requests.cs', 'AUIRequest_DifficultyIndicatorButterfly34_SIG')
assert data[0] == 0xe8 and data[21] == 0xe8
print('PASS: the two request difficulty-marker call sites are uniquely anchored')

# Regression: the reported quest crash was at RVA 0x1592c65, adjacent to
# the old number-column detour. Patch argument setup only; never the call.
rva, original = unique('Requests.cs', 'AUIRequest_NumberColumn_SIG')
instructions = list(decoder.disasm(original, rva))
assert instructions[0].mnemonic == 'mov' and instructions[0].op_str == 'dl, 0x72'
assert any(i.address == rva + 8 and i.bytes == bytes.fromhex('0f b6 ca') for i in instructions)
assert any(i.address == rva + 27 and i.bytes == bytes.fromhex('41 b0 99') for i in instructions)
assert instructions[-1].mnemonic == 'call'
constructor_rva = instructions[-1].operands[0].imm
ctor = list(decoder.disasm(pe.get_data(constructor_rva, 40), constructor_rva))
assert any(i.mnemonic == 'movzx' and i.op_str.endswith(', cl') for i in ctor)
patched = bytearray(original)
patched[1] = 0x34
patched[8:11] = bytes([0xb1, 0x12, 0x90])
patched[29] = 0x56
updated = list(decoder.disasm(patched, rva))
assert any(i.address == rva + 8 and i.op_str == 'cl, 0x12' for i in updated)
assert patched[30:] == original[30:]
assert updated[-1].bytes == instructions[-1].bytes
assert any(i.op_str == 'r9d, r14b' for i in updated)
source = (root / 'Requests.cs').read_text(encoding='utf-8-sig')
number_block = source.split('_context._utils.SigScan(AUIRequest_NumberColumn_SIG', 1)[1].split('_context._utils.SigScan', 1)[0]
assert 'CreateAsmHook' not in number_block
print('PASS: request number-column RGB edits preserve the native call and fade alpha')

# Map sites are validated and executed by test_map_color_patches.py.
