"""
Usage:
  make_fov_change.py <main_fov> <cutscene_fov> [main.yaml] [cutscene.yaml] [out.yaml]

Defaults:
  main.yaml -> ./main.yaml
  cutscene.yaml -> ./cutscene.yaml
  out.yaml -> ./fov_change.yaml
"""

import sys
from pathlib import Path

def process_lines(lines, fov_value):
    out = []
    for line in lines:
        # keep blank lines as-is
        if line.strip() == "":
            out.append(line.rstrip("\n") + "\n")
        else:
            for fov_key in (".fov", ".fovMin"):
                out.append(line.rstrip("\n") + f"{fov_key}: {fov_value}\n")
    return out

def main():
    if len(sys.argv) < 3:
        print("Usage: make_fov_change.py <main_fov> <cutscene_fov> [main.yaml] [cutscene.yaml] [out.yaml]")
        sys.exit(1)

    main_fov = sys.argv[1]
    cutscene_fov = sys.argv[2]

    main_path = Path(sys.argv[3]) if len(sys.argv) > 3 else Path("./main.yaml")
    cutscene_path = Path(sys.argv[4]) if len(sys.argv) > 4 else Path("./cutscene.yaml")
    out_path = Path(sys.argv[5]) if len(sys.argv) > 5 else Path("./fov_change.yaml")

    if not main_path.exists():
        print(f"Error: main file not found: {main_path}")
        sys.exit(2)
    if not cutscene_path.exists():
        print(f"Error: cutscene file not found: {cutscene_path}")
        sys.exit(2)

    main_lines = main_path.read_text(encoding="utf-8").splitlines(True)
    cutscene_lines = cutscene_path.read_text(encoding="utf-8").splitlines(True)

    out_lines = process_lines(main_lines, main_fov) + process_lines(cutscene_lines, cutscene_fov)

    out_path.write_text("".join(out_lines), encoding="utf-8")
    print(f"Wrote {len(out_lines)} lines to {out_path}")


if __name__ == "__main__":
    main()
