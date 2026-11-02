"""Queue one command in the already-open Unity editor and wait for its result."""
import argparse
import time
from pathlib import Path


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=["inspect", "setup", "tests", "integration", "refresh", "wire", "scenery", "polish", "webprofile", "restorequality", "build", "play", "stop"])
    parser.add_argument("--timeout", type=int, default=900)
    args = parser.parse_args()
    tools = Path(__file__).resolve().parent
    result = tools / "editor-result.txt"
    command = tools / "editor-command.txt"
    if command.exists():
        raise SystemExit("An editor command is already queued.")
    previous = result.stat().st_mtime_ns if result.exists() else 0
    temporary = tools / "editor-command.tmp"
    temporary.write_text(args.command, encoding="utf-8")
    temporary.replace(command)
    started = time.monotonic()
    while time.monotonic() - started < args.timeout:
        if result.exists() and result.stat().st_mtime_ns > previous:
            message = result.read_text(encoding="utf-8-sig")
            if message.startswith(args.command + " "):
                print(message, flush=True)
                raise SystemExit(1 if "FAILED" in message else 0)
        time.sleep(0.5)
    raise SystemExit("Editor command timed out. Focus Unity and use Assets > Refresh if needed.")
