#!/usr/bin/env python3
"""Execute the production native color snippets on Linux x86-64.

Requires .NET 9, GNU as/objcopy, and an x86-64 host. Exercises register, flags,
stack and alpha preservation, and live updates to the native color word.
"""
import argparse
import ctypes
import json
import mmap
import subprocess
import tempfile
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet', default='dotnet')
args = parser.parse_args()
project = Path(__file__).resolve().parents[1] / 'tests/NativeColorCode'
output = subprocess.check_output([args.dotnet, 'run', '--project', str(project), '-c', 'Release'], text=True)
cases = json.loads(output.strip().splitlines()[-1])
color = ctypes.c_uint32()
observed = (ctypes.c_uint64 * 5)()
count = 0
with tempfile.TemporaryDirectory() as tmp:
    tmp = Path(tmp)
    for case in cases:
        register = case['register']
        instructions = '\n'.join(case['instructions'][1:]).replace('dword [', 'DWORD PTR [')
        instructions = instructions.replace('0x1122334455667788', hex(ctypes.addressof(color)))
        # SysV arg EDI supplies an arbitrary packed source color. The emitted
        # instructions use Windows register conventions but make no calls.
        source = f'''.intel_syntax noprefix
.text
.global probe
probe:
    mov eax, edi
    mov r9d, edi
    mov r11, 0x123456789abcdef0
    mov r10, {ctypes.addressof(observed)}
    xor ecx, ecx
    cmp ecx, ecx
    pushfq
    pop QWORD PTR [r10]
    mov QWORD PTR [r10 + 24], rsp
{instructions}
    mov QWORD PTR [r10 + 8], r11
    pushfq
    pop QWORD PTR [r10 + 16]
    mov QWORD PTR [r10 + 32], rsp
    mov eax, {register}
    ret
'''
        (tmp / 'probe.s').write_text(source)
        subprocess.run(['as', '--64', '-o', str(tmp / 'probe.o'), str(tmp / 'probe.s')], check=True)
        subprocess.run(['objcopy', '-O', 'binary', '-j', '.text', str(tmp / 'probe.o'), str(tmp / 'probe.bin')], check=True)
        code = (tmp / 'probe.bin').read_bytes()
        with mmap.mmap(-1, len(code), prot=mmap.PROT_READ | mmap.PROT_WRITE | mmap.PROT_EXEC) as executable:
            executable.write(code)
            probe = ctypes.CFUNCTYPE(ctypes.c_uint32, ctypes.c_uint32)(ctypes.addressof(ctypes.c_char.from_buffer(executable)))
            for rgb in (0, 0xffffff, 0xa60652, 0x123457):
                # Update data while keeping the same generated instructions.
                packed_rgb = rgb if case['bgra'] else rgb << 8
                alpha_mask = 0xff000000 if case['bgra'] else 0xff
                fixed_alpha = 0x9b000000 if case['bgra'] else 0x9b
                color.value = packed_rgb if case['preserveAlpha'] else packed_rgb | fixed_alpha
                for alpha in range(256):
                    for source in ((alpha << 24) | 0xdeadbe, (alpha << 24) | 0xdead00) if case['bgra'] else (0xdeadbe00 | alpha,):
                        expected = packed_rgb | (source & alpha_mask) if case['preserveAlpha'] else color.value
                        if case['onlyIfLowByteNonZero'] and source & 0xff == 0:
                            expected = source
                        assert probe(source) == expected, case
                        assert observed[1] == 0x123456789abcdef0, 'R11 clobbered'
                        assert observed[0] == observed[2], 'RFLAGS clobbered'
                        assert observed[3] == observed[4], 'stack imbalance'
                        count += 1
print(f'PASS: {count} native executions; color, alpha, R11, flags, stack, and live data updates')
