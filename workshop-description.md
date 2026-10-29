[h1]Mouse Keybinds / 鼠标键位支持[/h1]

[b]让游戏原生的改键界面支持鼠标侧键，并修复拖拽物品时滚轮旋转偶尔失效的问题。[/b]

[hr][/hr]

[h2]功能[/h2]

[list]
[*][b]侧键录入[/b]：侧键 1~4（共 4 个，对应 Unity 的 Mouse3~Mouse6）可绑定到游戏全部 22 个可改键位——旋转、翻转、丢弃、快捷移动、固定模式、工具箱槽位等，随存档保存。
[*][b]键名跟随语言[/b]：键名自动匹配游戏语言（简中 / 英 / 俄 / 西 / 法 / 德 / 波 / 日），缺条目回退英文。
[*][b]滚轮旋转兜底[/b]：拖拽时按住 [b]LeftAlt[/b] 滚动滚轮强制旋转，堆叠物品（钢板、硬币、燃料电池等原本滚轮调数量的物品）同样生效。
[*][b]卡死自修复[/b]：修复会导致「怎么滚都不转」的偶发卡死状态。
[*][b]面板清理[/b]：隐藏本地 Mod 详情页的「开发者模式」开关。
[/list]

[h2]使用方法[/h2]

[olist]
[*]打开 [b]设置 → 按键绑定[/b]
[*]点击任意一项，按下鼠标侧键——按钮会显示当前语言下的键名
[*]完成。绑定写入存档，重启后保持
[/olist]

[h2]配置[/h2]

[list]
[*][b]滚轮旋转修饰键[/b]（默认 LeftAlt）：可选 None / LeftAlt / RightAlt / LeftShift / LeftControl / RightControl；选 None 恢复原版行为
[*][b]详细日志[/b]（默认关）：排查问题时打开
[/list]

[h2]兼容性[/h2]

[list]
[*]适配游戏版本：[b]0.46D（1.0）[/b]
[*]基于官方 ModLoader + Harmony，不使用反射、不申请额外权限、不修改存档格式
[*]可与其他改键类 Mod 共存（例如「退格清空改键」补的是同一方法的另一个阶段）
[*]不支持鼠标左键 / 右键 / 中键（中键在拖拽时是翻转键，绑定其他操作会双重触发）
[*]超出侧键 4 的额外按键需在鼠标驱动里映射为键盘键
[/list]

[hr][/hr]

[h1]English[/h1]

[b]Capture mouse side buttons in the vanilla key rebinding UI — and fix the intermittent scroll-to-rotate failure while dragging.[/b]

[h2]Features[/h2]

[list]
[*][b]Side button capture[/b]: side buttons 1–4 (Unity Mouse3–Mouse6) can be bound to all 22 rebindable actions and persist with your save.
[*][b]Localized key names[/b]: follows the game language (zh / en / ru / es / fr / de / pl / ja), falling back to English.
[*][b]Scroll-to-rotate fallback[/b]: hold [b]LeftAlt[/b] while dragging and scroll to rotate — works even for stacked items (steel sheets, coins, fuel cells) that normally change quantity on scroll.
[*][b]Stuck-state self-heal[/b]: fixes the rare state that makes scrolling stop working entirely.
[*][b]Cleaner mod panel[/b]: hides the native "Developer mode" toggle on the local mod detail page.
[/list]

[h2]How to use[/h2]

[olist]
[*]Open [b]Settings → Key Bindings[/b]
[*]Click any binding and press a mouse side button — it shows the localized name
[*]Done. Bindings are stored in the save file and survive restarts
[/olist]

[h2]Compatibility[/h2]

[list]
[*]Game version: [b]0.46D (1.0)[/b]
[*]Built on the official ModLoader + Harmony; no reflection, no extra capabilities, no save-format changes
[*]Coexists with other keybind mods (e.g. BackspaceClear patches a different phase of the same method)
[*]Left / right / middle mouse buttons are not supported (middle click is the vanilla flip key while dragging)
[*]Buttons beyond side button 4 must be mapped to keyboard keys in your mouse driver
[/list]

[hr][/hr]

[quote]Author: YDXF · License: MIT · Source: https://github.com/YDXF/mouse-keybinds[/quote]
