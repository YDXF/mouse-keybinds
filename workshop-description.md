[h2]功能 / Features[/h2]

[list][*]储物协调器预设支持鼠标侧键：容器预设的快捷键现在可以录入鼠标侧键 1~4，键名跟随游戏语言显示（8 种）
[*]冲突提醒：设置改键与储物协调器改键时，若按键已被其它功能占用，弹窗列出全部冲突项，可一键多功能或取消，可勾选「以后不再提示」
[*]修复拖拽物品时滚轮旋转偶尔失效的问题[/list]

[list][*]Mouse side buttons for Storage Coordinator presets — record buttons 1–4 as preset hotkeys, key names localized to 8 languages
[*]Conflict warning in both the vanilla rebinding screen and Storage Coordinator presets — lists every conflicting function and lets you allow one key for multiple actions (prompt can be disabled)
[*]Fixes the intermittent scroll-to-rotate failure while dragging[/list]

[b]说明：[/b]「设置 → 按键绑定」界面自游戏 1.0.0.x 起已原生支持录入鼠标键，因此本 Mod 不再接管该界面的录入与键名；只补齐仍然只收键盘键的储物协调器预设。按键冲突提醒在设置改键与协调器改键两处都会出现。之前用旧版本绑在原生界面上的侧键不受影响。

[b]Note:[/b] Settings → Key Bindings has supported mouse buttons natively since game 1.0.0.x, so this mod no longer touches its capture or key names. It only fills the gap in Storage Coordinator presets, which still accept keyboard keys only. Key conflict warnings appear in both the vanilla rebinding screen and the coordinator presets. Side buttons bound in the vanilla UI with older versions keep working.

[h2]版本历程 / Changelog[/h2]

[b]v1.3.0[/b] · 2026-11-20
[list][*]兼容版本 1.0.0.x
[*]移除「设置 → 按键绑定」的侧键录入与键名修改：游戏已原生支持，不再重复接管[/list]
[list][*]Compatible with game version 1.0.0.x
[*]Removed side-button capture and key-name overrides from Settings → Key Bindings — the game now supports them natively[/list]

[b]v1.2.0[/b] · 2026-11-01
[list][*]新增：按键冲突确认弹窗——录入已被占用的按键时列出全部冲突功能（改键键位 / 储物协调器预设 / 隐藏场景键位），允许一键多功能，可勾选「以后不再提示」
[*]弹窗键名跟随游戏语言[/list]
[list][*]Add: key conflict confirm dialog — lists every conflicting function (rebindable keys / coordinator presets / hidden scene keys), allows one key for multiple actions, with an optional “don’t ask again”
[*]Dialog key names follow the game language[/list]

[b]v1.1.0[/b] · 2026-11-01
[list][*]修复：储物协调器也支持录入鼠标侧键了[/list]
[list][*]Fix: Storage Coordinator presets now accept mouse side buttons too[/list]

[b]v1.0.0[/b] · 2026-10-29
[list][*]首发：原生改键界面支持录入鼠标侧键 1~4（覆盖全部键位与工具箱槽位），键名跟随 8 种游戏语言
[*]修复：拖拽时按住修饰键 + 滚轮强制旋转，堆叠物品同样生效[/list]
[list][*]Initial release: bind mouse side buttons 1–4 in the vanilla rebinding UI (all actions incl. toolbox slots), key names in 8 languages
[*]Fix: hold modifier + scroll to rotate while dragging, stacked items included[/list]

[h2]开源 / Open Source[/h2]

[url=https://github.com/YDXF/mouse-keybinds]github.com/YDXF/mouse-keybinds[/url]
