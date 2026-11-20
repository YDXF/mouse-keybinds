#!/usr/bin/env python3
"""把 workshop-description.md 渲染成可本地查看的 HTML 预览（模拟工坊排版）。

Steam 工坊描述是 BBCode，直接看 md 不直观；发布前用它检查排版。
用法：uv run --no-project workshop/make_preview.py
输出：workshop-description.preview.html（已在 .gitignore）
"""
import html
import pathlib
import re

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parent
SRC = ROOT / "workshop-description.md"
OUT = ROOT / "workshop-description.preview.html"

STYLE = """
body { background:#1a1a1e; color:#c6d4df; font-family:'Motiva Sans','Segoe UI',sans-serif;
       max-width:820px; margin:40px auto; padding:0 24px 60px; line-height:1.55; }
h2 { color:#5aaede; font-weight:400; margin:28px 0 10px; font-size:22px; }
p { margin:12px 0; }
b { color:#e5e5e5; }
a { color:#4cb2ff; text-decoration:none; } a:hover { color:#8ed2ff; }
ul { margin:8px 0 14px 6px; padding-left:20px; } li { margin:5px 0; }
.note { color:#6b7684; font-size:13px; margin-bottom:24px; }
"""


def to_html(md: str) -> str:
    s = html.escape(md, quote=False)

    s = re.sub(r"\[h2\](.*?)\[/h2\]", r"<h2>\1</h2>", s, flags=re.S)
    s = re.sub(r"\[b\](.*?)\[/b\]", r"<b>\1</b>", s, flags=re.S)
    s = re.sub(r"\[url=(.*?)\](.*?)\[/url\]", r'<a href="\1">\2</a>', s, flags=re.S)

    def conv_list(m: re.Match) -> str:
        items = [x.strip() for x in m.group(1).split("[*]") if x.strip()]
        return "<ul>" + "".join(f"<li>{i}</li>" for i in items) + "</ul>"

    s = re.sub(r"\[list\](.*?)\[/list\]", conv_list, s, flags=re.S)

    # 剩余的行：连续非空行合并成 <p>
    parts = []
    for block in re.split(r"\n\s*\n", s.strip()):
        if block.lstrip().startswith("<h2") or block.lstrip().startswith("<ul"):
            parts.append(block)
        else:
            parts.append(f"<p>{block.replace(chr(10), '<br>')}</p>")
    return "\n".join(parts)


def main() -> None:
    body = to_html(SRC.read_text(encoding="utf-8"))
    page = (
        "<!doctype html><meta charset='utf-8'><title>工坊描述预览</title>"
        f"<style>{STYLE}</style>"
        "<div class='note'>预览自 workshop-description.md —— 仅模拟 Steam 工坊排版，"
        "实际以工坊页面渲染为准</div>"
        f"{body}\n"
    )
    OUT.write_text(page, encoding="utf-8", newline="\n")
    print(f"已生成 {OUT}")


if __name__ == "__main__":
    main()
