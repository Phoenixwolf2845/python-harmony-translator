# File: SUIM.py

import os
import xml.etree.ElementTree as ET
import json

import HPUI
from rust_delegate import py_thread

# ——————————————————————————————————————————————————————————————————————————
# Module-level state
window_open   = False
mod_entries   = []
pht_config    = json.load(open("PythonHarmonyTranslator/config.json"))
pht_version   = pht_config.get("version", "unknown")

# ——————————————————————————————————————————————————————————————————————————
# Delegate: scan all mods for PRAP/PHT dependency, compare to imports.json
def scan_installed_mods():
    mods_dir      = "Mods"
    imports_path  = os.path.join("PythonHarmonyTranslator","imports.json")
    output_path   = os.path.join("PythonHarmonyTranslator","installed_mods.json")

    installed = {}
    # 1) parse each About.xml
    for mod in os.listdir(mods_dir):
        about = os.path.join(mods_dir,mod,"About","About.xml")
        if not os.path.isfile(about): 
            continue
        try:
            root = ET.parse(about).getroot()
            deps = [li.text for li in root.findall(".//li")]
            if "PRAP" in deps or "PHT" in deps:
                verNode = root.find(".//version")
                installed[mod] = {
                    "version": verNode.text if verNode is not None else "unknown"
                }
        except Exception as e:
            print(f"[scan_installed_mods] failed parsing {about}: {e}")

    # 2) cross-check imports.json
    try:
        imports = json.load(open(imports_path))["KnownMods"]
    except Exception as e:
        print(f"[scan_installed_mods] could not load imports.json: {e}")
        imports = {}

    for mod in installed:
        installed[mod]["in_imports"] = mod in imports

    # 3) write out results
    try:
        with open(output_path,"w") as f:
            json.dump(installed, f, indent=2)
        print(f"[scan_installed_mods] wrote {len(installed)} entries")
    except Exception as e:
        print(f"[scan_installed_mods] failed writing installed_mods.json: {e}")

    return installed

# ——————————————————————————————————————————————————————————————————————————
# Called once by PythonInitializer.cs (after venv is active)
def initialize():
    # schedule the scan in a background Python delegate (3rd terminal)
    py_thread(scan_installed_mods, [])

# ——————————————————————————————————————————————————————————————————————————
# Load the scan results into mod_entries for UI
def load_mod_entries():
    global mod_entries
    mod_entries = []
    data = {}
    try:
        data = json.load(open("PythonHarmonyTranslator/installed_mods.json"))
    except:
        pass

    for pid, info in data.items():
        entry = {
            "pid": pid,
            "version": info.get("version","unknown"),
            "requirements_ok": info.get("in_imports", False),
            "settings": []    # fill later if you have per-mod settings
        }
        mod_entries.append(entry)

# ——————————————————————————————————————————————————————————————————————————
# Hook called from C# SUIMHook.DoButtons_Postfix
def do_buttons(x, y, w, h):
    global window_open
    # draw PHT button next to gear
    if HPUI.ButtonText(x - w - 4, y, w, h, "PHT"):
        load_mod_entries()
        window_open = True
    if window_open:
        draw_window()

# ——————————————————————————————————————————————————————————————————————————
def draw_window():
    width, height = 500, 600
    x = (HPUI.ScreenWidth() - width)/2
    y = (HPUI.ScreenHeight() - height)/2
    HPUI.DrawWindow(x, y, width, height, lambda: _draw_contents(x, y, width, height))

def _draw_contents(x, y, width, height):
    HPUI.Label(x+10, y+10, f"Python Harmony Translator v{pht_version}")
    curY = y + 50
    for m in mod_entries:
        HPUI.Label(x+10, curY, f"{m['pid']}  ver:{m['version']}")
        curY += 24
        status = "OK" if m["requirements_ok"] else "Missing imports"
        HPUI.Label(x+20, curY, f"Deps: {status}")
        curY += 30
    if HPUI.ButtonText(x+width-80, y+height-40, 70, 30, "Close"):
        global window_open
        window_open = False
