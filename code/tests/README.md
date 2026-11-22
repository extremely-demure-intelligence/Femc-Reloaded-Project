# Proton UI patch validation

This branch replaces mid-function managed color callbacks with native substitutions.
The substitutions retain the original opacity, flags, scratch register, and stack;
color data updates independently when configuration changes. Native initializer
patches replace several stack-writing detours. Regular function-entry hooks remain.

The six Deck-disabled modules were reviewed along with equivalent hook patterns
throughout `Components`. This is a code/binary audit, not a claim that every gameplay
path has been exercised. In-game validation below is still required, including
visuals and transitions. The installed Steam executable was the binary reference;
older game builds and Episode Aigis gameplay need separate regression testing.

## What changed

| Area | Changes |
| --- | --- |
| Calendar | Native date-background and previous/current/next month RGB initializers; preserve RBX around six arrow-color calculations. |
| System menu | Update the native seven-color table at startup/config changes instead of detouring every frame; patch unselected RGB while preserving fade alpha and using the system-menu setting. |
| Social stats | Native ring-color initializers; retain the separate music-note background hook. |
| Tutorials | Native list-dot initializers instead of writing a fixed stack location from a detour. |
| Missing persons | Replace four managed color callbacks; retain both detail-background hooks. |
| Requests | Replace the crashing number-column detour with native RGB argument edits, preserving the constructor call and fade alpha. Remove duplicate installation of the status-font hook; retain all five difficulty-marker hooks and disambiguate the paired-marker signature; preserve flags around the conditional status-background color edit. |
| Other UI | Native substitutions for Persona-menu colors, maps/location selection, day/time transitions, cut-ins, difficulty selection, assist messages, and auto/fast-forward triangles. Native immediate for the no-usable-skill label. |
| Thought/system messages | Remove legacy thought-window color callbacks; retain all selection-background hooks; disambiguate the selected-text signature; repair a malformed system-message signature. |
| Button prompts | Avoid modifying the shared configuration color when setting per-frame opacity. |

The Deck compatibility switch remains an optional fallback and defaults to on.
Turn it **off** to exercise all six repaired modules. Changing it requires a restart.

## Automated checks

Build with .NET 9 and the Reloaded mods output variable set to a staging directory:

```sh
RELOADEDIIMODS=/tmp/femc-build dotnet build code/p3rpc.femc/p3rpc.femc/p3rpc.femc.csproj -c Release
python code/tools/test_native_color_code.py --dotnet dotnet
```

The native-code test runs the production instruction generator, assembles its output
with GNU `as`, and executes it on Linux x86-64. It covers both packed color layouts,
all alpha values, conditional recoloring, destination registers, flags, scratch-register
and stack preservation, and updates to color data. It does not exercise Reloaded's
runtime trampoline installation or a live game process.

Install `pefile` and `capstone` in a Python environment for these read-only binary checks:

```sh
python code/tools/test_proton_patch_sites.py /path/to/P3R.exe
python code/tools/audit_proton_hooks.py /path/to/P3R.exe --output /tmp/femc-hooks.json
```

The patch-site test verifies unique matches, immediate-write boundaries, and the native
system-menu color table. The inventory includes alternate/unused signatures: an absent
legacy signature is expected when its current-version alternative matches. It reports
matches rather than treating all missing alternatives as failures.

## In-game checklist

Use the patched `p3rpc.femc.dev`, enable its UI options, set **Deck Compatibility Switch
to off**, and restart. Test on a disposable save slot. For each screen, check opening,
selection changes, scrolling, backing out, and reopening. Watch for broken colors,
missing text, incorrect transparency, flicker, and crashes.

- [ ] **Rooftop plants:** examine the plants, advance the thought message, open every available choice, cancel, then complete a plant interaction.
- [ ] **Calendar:** open it, change months in both directions, move through past/current/future days, select a day with an event or part-time job, and read its details.
- [ ] **System menu:** move through all entries, open settings, switch tabs, change an option, back out; also test save/load screens and confirmation/cancel prompts.
- [ ] **Tutorials:** open the tutorial list and a tutorial page, scroll, switch categories, and test an empty/unavailable category if the save exposes one.
- [ ] **Social stats:** open Academics/Charm/Courage, perform an activity that awards points, watch music notes and pulse rings; test a rank-up when available.
- [ ] **Missing persons:** open the list and details, switch people, change sort order, scroll; cover unknown details, rescued/completed entries, and an empty list when available.
- [ ] **Requests:** open Elizabeth's list, scroll and sort, inspect different difficulty markers, view unknown/available/completed entries, accept a request, and report a completed one. Scroll long descriptions in both directions.
- [ ] **Persona menu:** move between filled and empty slots, selected/unselected entries, arcana labels, descriptions, and animated menu transitions. Open Persona status and skill details too.
- [ ] **Skills/items/equipment:** open each camp screen, inspect descriptions and highlights, test a character with no usable field skill, use a healing skill/item, and change equipment.
- [ ] **Town map/minimap/location selection:** open maps, move among locations and social-link markers, view details, travel, and reopen the map after arrival.
- [ ] **Day/time changes:** perform an activity that advances time, sleep into the next day, and watch date, moon, countdown, ripple, and band animations.
- [ ] **Dialogue and prompts:** test ordinary dialogue, thought choices, assist/navigation messages, a system information popup, auto-advance, fast-forward, and movie prompts. Toggle auto/fast-forward while text is fading.
- [ ] **Battle:** enter combat, trigger a portrait cut-in/critical or weakness animation, inspect party HP/SP, use a skill, finish battle, and return to exploration.
- [ ] **Title/difficulty:** revisit the title menu and difficulty selection; cancel without replacing an existing save.
- [ ] **Visual configuration:** change one affected color through Reloaded's live configuration update, if available, and verify it changes without restarting; verify transparent/fading elements still fade. Restore the original color afterward.
- [ ] **Longer session:** repeat menu/map transitions after 20–30 minutes of play and a save/load cycle.

If testing Episode Aigis, repeat the applicable dialogue, menu, maps, battle, and prompt
checks there. Also smoke-test on Windows before upstream release.

For a failure, record the exact action, in-game date/location, selected menu entry,
whether it failed before or after a choice, and the time of the crash. Preserve the
Reloaded/Proton log and any new Unreal crash report before launching again. The Deck
switch can be restored to on as a fallback for its six modules.

## Calendar crash follow-up

The 2026-11-10 22:11 local crash dump recorded RIP `0xcc6cd11f`, with
`P3R+0x12bd115` at the top of the stack, adjacent to the previous-month color
detour at `P3R+0x12bd102`. The three month-label detours have been replaced
with RGB initializer edits; native alpha stores and draw calls remain intact.
The installed executable passes unique-match and immediate-boundary checks.
This is a candidate fix pending gameplay validation. The separately reported
fast-travel crash has no corresponding new Unreal dump in the inspected folder
and remains unresolved.

## Arcade fast-forward crash follow-up

The 2026-11-15 crash stopped at `P3R+0x144a6e9`, calling through a misaligned
fast-forward prompt vtable. The installed executable uses the expanded prompt
layout in the base campaign as well as Episode Aigis. The old base-campaign
wrapper read flags at `0x258` as the sprite count (actually `0x260`), read state
at `0x588` instead of `0x590`, and wrote colors eight bytes before their fields.
This allowed the sprite recoloring loop to write beyond the four inline slots.

Prompt hooks now require native layout verification, use the verified offsets,
reject invalid sprite counts, and retain native fade alpha. Scan completion
order does not affect installation. Unsupported layouts leave the corresponding
managed recoloring hooks uninstalled. The movie prompt uses the corrected layout
too. Existing auto/triangle hooks are retained.

```sh
dotnet run --project code/tests/KeyHelpColors/KeyHelpColors.csproj -c Release
python code/tools/test_keyhelp_layout.py /path/to/P3R.exe
```

The memory test covers 20,480 combinations of state, count, and fade alpha,
including poisoned legacy fields, invalid counts, and surrounding guard bytes.
The binary test verifies unique signatures, runtime guards, field offsets, and
inline sprite capacity against the installed executable. Gameplay validation of
arcade fast-forward and movie prompts remains necessary; these checks do not
reproduce the live game crash.
