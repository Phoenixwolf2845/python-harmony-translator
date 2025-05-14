# File: SettingsWizard.py
import json
import os

CONFIG = "config.json"

def run_wizard():
    # Load existing or start fresh
    data = {}
    if os.path.exists(CONFIG):
        data = json.load(open(CONFIG))

    def ask_bool(prompt, default):
        resp = input(f"{prompt} [{'Y/n' if default else 'y/N'}]: ").strip().lower()
        if not resp:
            return default
        return resp[0] == 'y'

    def ask_int(prompt, default):
        resp = input(f"{prompt} (default {default}): ").strip()
        return int(resp) if resp.isdigit() else default

    print("=== PhoenixRAP Setup Wizard ===")
    data['useGpu']               = ask_bool("Use GPU delegation?", data.get('useGpu', True))
    data['gpuPowerPreference']   = input("GPU power preference (HighPerformance/LowPower): ") or data.get('gpuPowerPreference', "HighPerformance")
    data['gpuFallback']          = ask_bool("Force fallback adapter if GPU unavailable?", data.get('gpuFallback', True))
    data['maxThreads']           = ask_int("Max CPU threads:", data.get('maxThreads', os.cpu_count()))
    data['enablePythonDelegate'] = ask_bool("Enable Python delegation?", data.get('enablePythonDelegate', True))
    data['errorDestination']     = input("Error destination (file/terminal/rimworld): ") or data.get('errorDestination', "file")

    # Mark first-run complete
    data['hasPlayedMod'] = True

    with open(CONFIG, "w") as f:
        json.dump(data, f, indent=2)

    print("Setup complete! You can now close this window.")
