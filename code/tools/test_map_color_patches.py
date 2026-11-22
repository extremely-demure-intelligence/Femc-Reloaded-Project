#!/usr/bin/env python3
"""Validate all map RGB patches against P3R.exe and execute production instruction bytes.
Requires pefile, capstone, .NET 9 and Linux x86-64. Never modifies the game executable.
"""
import argparse, ctypes, json, mmap, re, subprocess, tempfile
from pathlib import Path
import capstone, pefile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('executable', type=Path)
parser.add_argument('--dotnet', default='dotnet')
parser.add_argument('--scanner-directory', type=Path, help='Installed Reloaded.Memory.SigScan.ReloadedII mod directory')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
pe = pefile.PE(str(args.executable), fast_load=True)
pe.parse_data_directories(directories=[pefile.DIRECTORY_ENTRY['IMAGE_DIRECTORY_ENTRY_EXCEPTION']])
sections = [(s.VirtualAddress, s.get_data()) for s in pe.sections if s.Characteristics & 0x20000000]
decoder = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
decoder.detail = True
output = subprocess.check_output([args.dotnet, 'run', '--project', str(root/'tests/MapColorCode'), '-c', 'Release'], text=True)
cases = json.loads(output.strip().splitlines()[-1])
replacement = {(c['channel'], c['length']): bytes.fromhex(c['bytes']) for c in cases}
patches = []; signatures = []; occupied = set(); scanner_cases = []
for file in ('Minimap.cs', 'LocationSelect.cs', 'TownMap.cs', 'DayChange.cs'):
    source = (root/'p3rpc.femc/p3rpc.femc/Components'/file).read_text(encoding='utf-8-sig')
    if file != 'DayChange.cs':
        assert not any(x in source for x in ('CreateAsmHook', 'MakeHooker', 'self->', 'ColorInRegister'))
    definitions = dict(re.findall(r'const string (\w+) = "([^"]+)"', source))
    for match in re.finditer(r'PatchColorInputs\(addr, "(\w+)", (.*?)\)\);', source, re.S):
        name, body = match.groups()
        if file == 'DayChange.cs':
            assert 'CreateAsmHook' not in source and 'ColorInRegister' not in source
            assert '_drawNextDayRipple' not in source
        sig = definitions[name]
        assert sig.split()[0] != "??", (name, "leading wildcard can stall Reloaded scanner")
        pattern = b''.join(b'.' if t == '??' else re.escape(bytes.fromhex(t)) for t in sig.split())
        hits = [(rva+m.start()) for rva, data in sections for m in re.finditer(pattern, data, re.S)]
        assert len(hits) == 1, (name, hits)
        anchor = hits[0]
        scanner_cases.append(dict(Name=name, Signature=sig, FileOffset=pe.get_offset_from_rva(anchor)))
        signatures.append((name, anchor, pattern, len(sig.split())))
        specs = [(int(offset), bytes.fromhex(original), int(channel))
                 for offset, original, channel in re.findall(r'\((-?\d+), "([0-9A-F]+)", (\d)\)', body)]
        assert len(specs) in (1, 3, 4), name
        if name == 'AUIAccessInfoDraw_PlaceNameBackgroundColor_SIG':
            # Prove that this RGB constructor feeds the rectangle actually drawn
            # for location info, rather than the obsolete native-types offset.
            assert len(specs) == 3 and 'MinimapPlaceNameBgColor' in body
            store = next(i for i in decoder.disasm(pe.get_data(anchor, 96), anchor)
                         if i.op_str == 'dword ptr [rbx + 0xf8c], eax')
            draw = list(decoder.disasm(pe.get_data(0x14133af, 21), 0x14133af))
            assert draw[0].op_str == 'rcx, [rbx + 0xf68]'
            assert draw[-1].mnemonic == 'call' and draw[-1].operands[0].imm == 0x1362260
            renderer = list(decoder.disasm(pe.get_data(0x1362260, 60), 0x1362260))
            assert any(i.op_str == 'eax, dword ptr [rcx + 0x24]' for i in renderer)
            assert 0xf68 + 0x24 == 0xf8c

        if 'DrawNextDayRipple' in name:
            assert all(offset + len(original) <= -5 for offset, original, _ in specs)
            # The reported fault was at RVA 0x12b724e, inside the former
            # detour span. Both ripples now leave that whole span untouched.
            head = list(decoder.disasm(pe.get_data(anchor, 24), anchor))
            assert head[0].mnemonic == 'movaps'
            assert head[1].mnemonic == 'lea' and head[1].address == anchor + 7
            if 'Ripple1' in name:
                assert anchor + 7 == 0x12b724e

        fn = next(e.struct.BeginAddress for e in pe.DIRECTORY_ENTRY_EXCEPTION
                  if e.struct.BeginAddress <= min(anchor+x[0] for x in specs) < e.struct.EndAddress)
        instructions = list(decoder.disasm(pe.get_data(fn, anchor-fn+40), fn))
        by_address = {i.address:i for i in instructions}
        last = max(anchor+offset+len(original) for offset, original, _ in specs)
        call = next(i for i in instructions if i.address >= last and i.mnemonic == 'call')
        target = call.operands[0].imm
        # Both entry points must resolve to the byte-packing constructor.
        target_ins = next(decoder.disasm(pe.get_data(target, 15), target))
        if target_ins.mnemonic == 'jmp': target = target_ins.operands[0].imm
        tint = name == 'UUILocationSelect_DrawLocationSelectTintColor_SIG'
        ctor = pe.get_data(target, 145 if tint else 30)
        if tint:
            assert ctor[:14] == bytes.fromhex('f30f1015e82ed2020f57c9410fb6')
            assert ctor[-1] == 0xc3
        else:
            assert ctor == bytes.fromhex('0fb6c1c1e0080fb6ca0bc1410fb6c8c1e0080bc1410fb6c9c1e0080bc1c3'), (name, ctor.hex())
        start = min(anchor+x[0] for x in specs)
        assert not any(i.group(capstone.CS_GRP_JUMP) or i.mnemonic.startswith(('cmov','set'))
                       for i in instructions if start <= i.address < call.address), name
        for offset, original, channel in specs:
            address = anchor+offset
            assert address in by_address and by_address[address].bytes == original, (name, offset)
            assert address not in occupied, (name, address)
            occupied.add(address)
            assert by_address[address].op_str.split(',')[0] in (
                ('cl','ecx'), ('dl','edx'), ('r8b','r8d'), ('r9b','r9d'))[channel]
            code = replacement[(channel, len(original))]
            patches.append((address, code))
        # Exercise the actual generated edits in instruction order, then execute
        # the unmodified native constructor. SysV EDI supplies the animated alpha.
        edits = b''.join(replacement[(channel,len(original))] for offset,original,channel in sorted(specs))
        if len(specs) > 1:
            # Relocate only the tint constructor's RIP-relative float constants
            # into the test buffer. The game implementation remains untouched.
            relocated = bytearray(ctor)
            if tint:
                for ins in decoder.disasm(ctor, target):
                    if any(o.type == capstone.x86.X86_OP_MEM and o.mem.base == capstone.x86.X86_REG_RIP for o in ins.operands):
                        location = len(relocated)
                        relocated.extend(pe.get_data(ins.address+ins.size+ins.disp, 4))
                        displacement = location - (ins.address-target+ins.size)
                        off = ins.address-target+ins.disp_offset
                        relocated[off:off+4] = displacement.to_bytes(4, 'little', signed=True)
            code = bytes.fromhex('4189f9') + edits + relocated
            with mmap.mmap(-1, len(code), prot=mmap.PROT_READ|mmap.PROT_WRITE|mmap.PROT_EXEC) as memory:
                memory.write(code)
                probe = ctypes.CFUNCTYPE(ctypes.c_uint32, ctypes.c_uint32)(ctypes.addressof(ctypes.c_char.from_buffer(memory)))
                for alpha in range(256):
                    if tint:
                        f32 = lambda value: ctypes.c_float(value).value
                        amount = f32(0x78 * f32(1/255))
                        white = f32(f32(1-amount)*255)
                        rgb = [int(f32(f32(channel*amount)+white)) for channel in (0x12,0x34,0x56)]
                        expected = (rgb[0]<<24) | (rgb[1]<<16) | (rgb[2]<<8) | 255
                    else:
                        expected = 0x12345600 | (0x78 if len(specs)==4 else alpha)
                    assert probe(alpha) == expected, (name, alpha, hex(probe(alpha)), hex(expected))
        print('PASS:', name, flush=True)
# Installation order cannot invalidate another site's signature, and no call is patched.
for name, anchor, pattern, length in signatures:
    data = bytearray(pe.get_data(anchor, length))
    for address, code in patches:
        for n, byte in enumerate(code):
            if anchor <= address+n < anchor+length: data[address+n-anchor] = byte
    assert re.fullmatch(pattern, data, re.S), ('signature invalidated by another color patch',name)
print(f'PASS: {len(signatures)} map sites, {len(patches)} nonoverlapping edits; native calls and animated alpha preserved')

# Auto prompt crash follow-up: the native short branch must land at the start
# of the replacement, never in its middle. Only the converged CALL is hooked.
source = (root/'p3rpc.femc/p3rpc.femc/Components/KeyHelp.cs').read_text(encoding='utf-8-sig')
name = 'FKeyHelpButtonAuto_DrawTextFillColor_SIG'
sig = re.search(name + r' = "([^"]+)"', source).group(1)
pattern = b''.join(b'.' if t == '??' else re.escape(bytes.fromhex(t)) for t in sig.split())
hits = [rva+m.start() for rva, data in sections for m in re.finditer(pattern, data, re.S)]
assert len(hits) == 1
anchor = hits[0]
ins = list(decoder.disasm(pe.get_data(anchor, 12), anchor))
assert [i.mnemonic for i in ins] == ['cmp', 'ja', 'mov', 'call']
assert ins[1].operands[0].imm == anchor + 7 == ins[3].address
assert ins[3].size == 5 and ins[3].operands[0].imm == 0x143e370
assert 'var callAddress = addr + 7;' in source
assert 'hookLength = 5' in source and 'MaxOpcodeSize = 5' in source
assert 'PreferRelativeJump = true' in source
assert 'Utils.GetGlobalAddress' not in source and 'DoNotExecuteOriginal' not in source
assert not any(anchor <= address < anchor + 12 for address, _ in patches)
scanner_cases.append(dict(Name=name, Signature=sig, FileOffset=pe.get_offset_from_rva(anchor)))
print('PASS: Auto prompt branches converge on a complete five-byte call; no manually recreated call')

if args.scanner_directory:
    with tempfile.TemporaryDirectory() as directory:
        cases_file = Path(directory)/'signatures.json'
        cases_file.write_text(json.dumps(scanner_cases))
        # Bound the run: a leading-wildcard regression used to hang inside AVX2.
        subprocess.run([args.dotnet, 'run', '--project', str(root/'tests/MapScanner'),
                        '-c', 'Release', '-p:ScannerDirectory='+str(args.scanner_directory),
                        '--', str(args.executable), str(cases_file)], check=True, timeout=30)
