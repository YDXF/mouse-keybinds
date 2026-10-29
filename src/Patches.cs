using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MouseKeybinds
{
    /// <summary>
    /// 核心补丁：让原生改键录入（PollCapture）能识别鼠标侧键（Mouse3 ~ Mouse6）。
    ///
    /// 原理：PollCapture 只遍历 KeyBindHelper.KeyboardKeys() —— 一个
    /// `k != KeyCode.None && k < KeyCode.Mouse0` 的纯键盘白名单（反编译 L46240 附近），
    /// 所以鼠标键永远录不进去。本补丁在原逻辑返回 None 且处于捕获状态时，
    /// 自行检测侧键并把结果改写为 Key。
    ///
    /// 下游链路（改键 UI 暂存/冲突检测/ES3 存档/KeyName 显示/GetKeyDown 触发）
    /// 对鼠标键天然兼容，无需补丁。
    ///
    /// 支持范围（Unity KeyCode 的鼠标键上限就是 Mouse6）：
    /// - Mouse3 / Mouse4：标准鼠标的两个侧键（XButton1 / XButton2）；
    /// - Mouse5 / Mouse6：多侧键鼠标的第 3、4 个额外键（罗技 G502、雷蛇那伽梵蛇等）。
    ///   注意：超过 Mouse6 的按键（如 MMO 鼠标 12 键的其余键）Unity 接收不到，
    ///   需在鼠标驱动里映射为键盘键。
    ///
    /// 为什么不支持 Mouse0/1/2：
    /// - Mouse0（左键）是 UI 点击键，用户点「改键」按钮那一下就会被录成 Mouse0；
    /// - Mouse1（右键）是原生交互键（pin_mode 等），冲突面大；
    /// - Mouse2（中键）在拖拽物品时被 ItemMouseDragHandler 硬编码为「翻转」
    ///   （键监听列表直接包含 KeyCode.Mouse2），且参与 CustomEventHandler 的双击检测，
    ///   绑定其他操作后会双重触发 —— 故明确不支持（v0.2.0 起移除）。
    /// </summary>
    [HarmonyPatch(typeof(KeyBindHelper), nameof(KeyBindHelper.PollCapture))]
    public static class PollCapturePatch
    {
        private static readonly KeyCode[] SideKeys =
        {
            KeyCode.Mouse3, // 侧键 1（XButton1）
            KeyCode.Mouse4, // 侧键 2（XButton2）
            KeyCode.Mouse5, // 侧键 3（多键鼠标的额外键）
            KeyCode.Mouse6, // 侧键 4
        };

        /// <summary>补丁前置检查：目标方法不存在时静默跳过，避免整 Mod 加载失败。</summary>
        public static bool Prepare()
        {
            return AccessTools.Method(typeof(KeyBindHelper), "PollCapture") != null;
        }

        public static void Postfix(ref KeyBindHelper.CaptureResult __result, ref KeyCode key)
        {
            // 原逻辑已产生结论（键盘键 / Escape 取消 / 非捕获状态）时不干预
            if (__result != KeyBindHelper.CaptureResult.None || !KeyBindHelper.IsCapturing)
            {
                return;
            }

            foreach (KeyCode sideKey in SideKeys)
            {
                if (Input.GetKeyDown(sideKey))
                {
                    key = sideKey;
                    __result = KeyBindHelper.CaptureResult.Key;
                    Core.Debug($"录入鼠标侧键: {sideKey}");
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 显示名补丁：把 Mouse3~Mouse6 的默认英文 "Mouse 3~6" 替换为
    /// 玩家当前语言下的键名，走官方本地化机制（Localization/&lt;locale&gt;.csv + ModHelper.GetLocalized，
    /// 见反编译 LoadLocalization L228378 / ModHelper.GetLocalized）。
    /// 当前语言缺条目时自动回退到 en.csv，再缺则回退到 key 本身。
    /// </summary>
    [HarmonyPatch(typeof(KeyBindHelper), nameof(KeyBindHelper.KeyName))]
    public static class KeyNamePatch
    {
        public static bool Prepare()
        {
            return AccessTools.Method(typeof(KeyBindHelper), "KeyName") != null;
        }

        public static void Postfix(KeyCode key, ref string __result)
        {
            switch (key)
            {
                case KeyCode.Mouse3:
                    __result = ModHelper.GetLocalized(Core.ModId, "mouse_side_1");
                    break;
                case KeyCode.Mouse4:
                    __result = ModHelper.GetLocalized(Core.ModId, "mouse_side_2");
                    break;
                case KeyCode.Mouse5:
                    __result = ModHelper.GetLocalized(Core.ModId, "mouse_side_3");
                    break;
                case KeyCode.Mouse6:
                    __result = ModHelper.GetLocalized(Core.ModId, "mouse_side_4");
                    break;
            }
        }
    }

    /// <summary>
    /// 界面清理补丁：隐藏 Mod 详情页的「开发者模式」按钮（devmode）。
    ///
    /// 背景：该按钮是游戏原生功能（ModUIManager.ShowTrust 末尾 SetVisible），
    /// **只对本地安装的 Mod 显示**（source=Local），创意工坊订阅的 Mod 不会出现。
    /// 隐藏它让本地调试时的面板更干净，对实际功能无影响。
    ///
    /// 实现：postfix ShowTrust，通过 Harmony 字段注入拿到 ModUIManager.handle，
    /// 再取出 "devmode" 元素隐藏。找不到元素时静默跳过。
    /// </summary>
    [HarmonyPatch(typeof(ModUIManager), "ShowTrust")]
    public static class HideDevModePatch
    {
        public static bool Prepare()
        {
            return AccessTools.Method(typeof(ModUIManager), "ShowTrust") != null;
        }

        public static void Postfix(CustomUIWindow ___handle)
        {
            try
            {
                ___handle?.Get("devmode")?.SetVisible(false);
            }
            catch
            {
                // UI 尚未构建完成时忽略
            }
        }
    }

    /// <summary>
    /// 滚轮旋转兜底补丁（v0.4.0）。
    ///
    /// 原生「拖拽时滚轮旋转物品」偶尔失效的原因（反编译 ItemMouseDragHandler.OnEventMouseScroll）：
    ///  1. 主因：拖拽的物品是堆叠（maxRemove &gt; 1）时，滚轮被设计为「调整拖拽数量」而非旋转；
    ///  2. 按住 scrollIgnoreKey（滚轮忽略键）时，滚轮改为滚动可滚动容器；
    ///  3. sticky bug：isScrollIgnore 在异常路径（收不到 Release 事件）下会卡在 true，
    ///     此后滚轮持续失效，直到再按一次该键。
    ///
    /// 修复方式：
    ///  - Prefix 检测「正在拖拽 + 按住配置的修饰键（默认 LeftAlt）+ 滚轮有输入」，
    ///    直接执行旋转（或按 swapDragRotateFlip 设置执行翻转）并跳过原逻辑，避免连带调整数量；
    ///  - 顺带自修复 isScrollIgnore 的 sticky 状态（忽略键已松开但仍为 true 时重置）。
    /// </summary>
    [HarmonyPatch(typeof(ItemMouseDragHandler), "OnEventMouseScroll")]
    public static class ScrollRotatePatch
    {
        private static readonly MethodInfo FlipHeldItemMethod =
            AccessTools.Method(typeof(ItemMouseDragHandler), "FlipHeldItem");

        private static readonly MethodInfo RefreshDragHighlightMethod =
            AccessTools.Method(typeof(ItemMouseDragHandler), "RefreshDragHighlight");

        public static bool Prepare()
        {
            return AccessTools.Method(typeof(ItemMouseDragHandler), "OnEventMouseScroll") != null;
        }

        public static bool Prefix(
            ItemMouseDragHandler __instance,
            Vector2 scrollDelta,
            ref GameItemElement ___currentItem,
            ref bool ___isScrollIgnore)
        {
            // ① 自修复 sticky 状态：忽略键已松开但标志卡在 true（原生丢失 Release 事件时会这样）
            if (___isScrollIgnore && !Input.GetKey(__instance.scrollIgnoreKey))
            {
                ___isScrollIgnore = false;
            }

            // ② 仅在「拖拽中 + 按住修饰键 + 滚轮有输入」时接管
            if (!__instance.IsDraggingItem || ___currentItem == null)
            {
                return true;
            }

            KeyCode modifier = ParseModifier(Core.ScrollRotateModifier);
            if (modifier == KeyCode.None || !Input.GetKey(modifier))
            {
                return true;
            }

            int direction = (int)(scrollDelta.x + scrollDelta.y);
            if (direction == 0)
            {
                return true;
            }

            bool swap = EmporiumOption.Instance != null && EmporiumOption.Instance.swapDragRotateFlip;
            if (swap)
            {
                FlipHeldItemMethod?.Invoke(__instance, null);
            }
            else if (direction > 0)
            {
                ___currentItem.RotCCW();
            }
            else
            {
                ___currentItem.RotCW();
            }

            RefreshDragHighlightMethod?.Invoke(__instance, null);
            Core.Debug($"修饰键滚轮旋转 (dir={direction}, swap={swap})");
            return false; // 跳过原生逻辑，避免同时调整数量
        }

        private static KeyCode ParseModifier(ConfigEntry<string> entry)
        {
            string raw = entry?.Value;
            if (string.IsNullOrEmpty(raw) || raw == "None")
            {
                return KeyCode.None;
            }
            return Enum.TryParse(raw, true, out KeyCode key) ? key : KeyCode.None;
        }
    }

    /// <summary>
    /// 拖拽诊断补丁（v0.4.1）：VerboseLog 开启时，每次开始拖拽输出一行状态，
    /// 用于定位「滚轮旋转偶尔失效」的真实原因。
    ///
    /// 输出项：
    ///  - unitCount / maxRemove：maxRemove &gt; 1 时滚轮走「调数量」分支（堆叠物品才会出现）；
    ///  - isScrollIgnore：为 true 时滚轮被原生逻辑忽略（旋转失效）；
    ///  - scrollIgnoreKey / scrollMult10Key / scrollMult100Key：这三个是场景里配置的
    ///    Inspector 字段（不在 KeyBindHelper 体系内），实际值只能运行时读取。
    /// </summary>
    [HarmonyPatch(typeof(ItemMouseDragHandler), "StartDrag")]
    public static class DragDiagnosticsPatch
    {
        public static bool Prepare()
        {
            return AccessTools.Method(typeof(ItemMouseDragHandler), "StartDrag") != null;
        }

        public static void Postfix(
            ItemMouseDragHandler __instance,
            GameItemElement ___currentItem,
            int ___maxRemove,
            bool ___isScrollIgnore)
        {
            if (Core.VerboseLog == null || !Core.VerboseLog.Value)
            {
                return;
            }

            try
            {
                string id = ___currentItem?.identifier ?? "?";
                int units = ___currentItem?.unitCount ?? 0;
                Core.Log?.Log(
                    $"[诊断] 拖拽 {id} unitCount={units} maxRemove={___maxRemove} isScrollIgnore={___isScrollIgnore} | " +
                    $"scrollIgnoreKey={__instance.scrollIgnoreKey} mult10={__instance.scrollMult10Key} mult100={__instance.scrollMult100Key}");
            }
            catch
            {
                // 诊断不影响主流程
            }
        }
    }
}
