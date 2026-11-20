#!/usr/bin/env python3
"""由 workshop-description.md 生成 SteamCMD 上传用的 vdf。

描述正文只维护一份（workshop-description.md），避免以前那种
「改了 md、vdf 里还是旧描述」的不一致。

用法：
    uv run --no-project workshop/make_vdf.py "v1.3.0 changenote ..."

生成 workshop/upload_mouse_keybinds.vdf，随后用 SteamCMD 上传即可
（流程见 docs/09-release.md）。
"""
import pathlib
import sys

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parent
DESC = ROOT / "workshop-description.md"
OUT = HERE / "upload_mouse_keybinds.vdf"

APPID = "4348910"
PUBLISHED_FILE_ID = "3829349177"
CONTENT = (HERE / "content").as_posix()
PREVIEW = (ROOT / "art" / "cover.jpg").as_posix()


def vdf_quote(text: str) -> str:
    """vdf 字符串转义：反斜杠与双引号。"""
    return text.replace("\\", "\\\\").replace('"', '\\"')


def main() -> int:
    if len(sys.argv) < 2 or not sys.argv[1].strip():
        print("用法: make_vdf.py \"<changenote>\"", file=sys.stderr)
        return 2
    changenote = sys.argv[1].strip()
    description = DESC.read_text(encoding="utf-8").strip()

    body = "\n".join(
        [
            '"workshopitem"',
            "{",
            f'    "appid"           "{APPID}"',
            f'    "publishedfileid" "{PUBLISHED_FILE_ID}"',
            f'    "contentfolder"   "{CONTENT}"',
            f'    "previewfile"     "{PREVIEW}"',
            f'    "changenote"      "{vdf_quote(changenote)}"',
            '    "visibility"      "0"',
            f'    "description"     "{vdf_quote(description)}"',
            "}",
            "",
        ]
    )
    OUT.write_text(body, encoding="utf-8", newline="\n")
    print(f"已生成 {OUT}（描述 {len(description)} 字符，changenote {len(changenote)} 字符）")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
