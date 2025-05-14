# --- PHT.py (Python Harmony Translator main brain) ---

# Bring in all Harmony (HP) functions so you can call Patch, Unpatch, etc. directly:
import HP as harmony
from HP import *      # now everything in HP is in PHT’s global namespace

# Bring in all UI primitives so you can call ButtonText, Slider, DrawWindow, etc. directly:
import HPUI
from HPUI import *    # now everything in HPUI is in PHT’s global namespace

# Delegate primitives from the Rust side
from rust_delegate import (delegate, shutdown_integration)
from rust_delegate import py_thread as Thread
from rust_delegate import py_thread_end as thread_stop
from rust_delegate import delegate_end as delegate_end
from rust_delegate import clear_delegate_cache_py as clear_cache

# Shortcuts for easier calling
class delegate:
    def Delegate(func):
        def wrapper(*args, **kwargs):
            return delegate(func, *args, **kwargs)
        return wrapper

    def end(num):
        delegate_end(num)

    def start(func, *args, **kwargs):
        delegate(func, *args, **kwargs)

    def cache(cmd):
        if cmd == "clear":
            clear_cache()

    def kill(mode):
        if mode is True or mode == "all" or mode == "Emergency":
            shutdown_integration()

class thread:
    def thread(func):
        def wrapper(*args, **kwargs):
            return Thread(func, *args, **kwargs)
        return wrapper

    def end(num):
        thread_stop(num)

    def start(func, *args, **kwargs):
        Thread(func, *args, **kwargs)

# Example extra utility
def shutdown_everything():
    """Safely shutdown everything related to PHT."""
    delegate.kill(True)
    delegate.cache("clear")
    print("[PHT] Shutdown completed safely.")

# What this module exports when you do from PHT import *
__all__ = [
    # Harmony functions (from HP)
    *[name for name in dir() if name in dir(harmony)],
    # UI functions (from HPUI)
    *[name for name in dir() if name in dir(HPUI)],
    # Controllers
    "delegate", "thread", "shutdown_everything"
]