using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MouseKeybinds
{
    // 补丁清单（v1.3.0：协调器侧键 + 双界面冲突提醒 + 滚轮兜底 + 面板清理）：
    //   CoordinatorCaptureKeysPatch  —— 协调器预设录侧键
    //   CoordinatorKeyNamePatch      —— 协调器预设键名本地化
    //   HideDevModePatch             —— 隐藏本地 Mod 详情页的「开发者模式」
    //   ScrollRotatePatch            —— 拖拽时修饰键 + 滚轮强制旋转
    //   DragDiagnosticsPatch         —— 上述功能的诊断日志
    // 冲突提醒（ConflictPromptPatch / CoordinatorConflictPatch）在 ConflictCheck.cs，
    // 设置改键与协调器改键两处都会弹窗。
    // 原生改键界面（设置 → 按键绑定）自 1.0.0.x 起已原生支持 Mouse1~Mouse6，
    // v1.3.0 移除了该界面的录入与键名补丁，只保留冲突提醒。

    /// <summary>
    /// 储物协调器预设键位录入补丁（v1.1.0）。
    ///
    /// 「储物协调器」的容器预设快捷键录入走的是 MachineStorageCoordinator 自己的一套
    /// （HandleInput → HandleKeyCapture → KeyboardKeys()），与设置界面的
    /// KeyBindHelper.PollCapture 完全独立；其 KeyboardKeys() 带
    /// `k != KeyCode.None && k < KeyCode.Mouse0` 过滤（Rev16 L86270 / Rev13 L83694），
    /// 所以原生改键界面能录侧键、协调器预设却录不进。
    ///
    /// 修复：postfix KeyboardKeys()，把 Mouse3~Mouse6 追加进返回的白名单数组。
    /// 原方法内部缓存的静态数组只含键盘键，每次调用追加生成新数组，幂等且不影响缓存。
    /// 录入后的触发（HandleHotkeys → Input.GetKeyDown((KeyCode)p.keyCode)）
    /// 与持久化（keyCode 存 int）对鼠标键天然兼容，无需处理。
    /// </summary>
    [HarmonyPatch(typeof(MachineStorageCoordinator), "KeyboardKeys")]
    public static class CoordinatorCaptureKeysPatch
    {
        private static readonly KeyCode[] SideKeys =
        {
            KeyCode.Mouse3,
            KeyCode.Mouse4,
            KeyCode.Mouse5,
            KeyCode.Mouse6,
        };

        /// <summary>补丁前置检查：目标方法不存在时静默跳过，避免整 Mod 加载失败。</summary>
        public static bool Prepare()
        {
            return AccessTools.Method(typeof(MachineStorageCoordinator), "KeyboardKeys") != null;
        }

        public static void Postfix(ref KeyCode[] __result)
        {
            if (__result == null || __result.Length == 0)
            {
                return;
            }
            if (Array.IndexOf(__result, KeyCode.Mouse3) >= 0)
            {
                return; // 已包含鼠标键，避免重复追加
            }
            KeyCode[] merged = new KeyCode[__result.Length + SideKeys.Length];
            __result.CopyTo(merged, 0);
            SideKeys.CopyTo(merged, __result.Length);
            __result = merged;
        }
    }

    /// <summary>
    /// 储物协调器键名显示补丁（v1.1.0）。
    ///
    /// 协调器有自己的私有 KeyName(int)（Rev16 L85994 / Rev13 L83418），默认对未识别的键返回
    /// `keyCode.ToString()`，侧键会显示成 "Mouse3" 这类英文名。postfix 接管
    /// Mouse3~Mouse6，显示当前语言的「鼠标侧键N」。
    /// </summary>
    [HarmonyPatch(typeof(MachineStorageCoordinator), "KeyName")]
    public static class CoordinatorKeyNamePatch
    {
        /// <summary>补丁前置检查：目标方法不存在时静默跳过，避免整 Mod 加载失败。</summary>
        public static bool Prepare()
        {
            return AccessTools.Method(typeof(MachineStorageCoordinator), "KeyName") != null;
        }

        public static void Postfix(int keyCode, ref string __result)
        {
            switch ((KeyCode)keyCode)
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
            return AccessTools.Method(typeof(ModUIManager), "ShowTrust") != null
                   && AccessTools.Field(typeof(ModUIManager), "handle") != null;
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
    ///  3. Rev12 及更早还有一个 sticky bug：isScrollIgnore 是字段，异常路径收不到 Release
    ///     时会卡在 true。Rev16 起它改成了计算属性 `Input.GetKey(scrollIgnoreKey)`，
    ///     不再有存储状态，这条 bug 从结构上消失，自修复逻辑也随之删除。
    ///
    /// 修复方式：
    ///  - Prefix 检测「正在拖拽 + 按住配置的修饰键（默认 LeftAlt）+ 滚轮有输入」，
    ///    直接执行旋转（或按 swapDragRotateFlip 设置执行翻转）并跳过原逻辑，避免连带调整数量。
    /// </summary>
    [HarmonyPatch(typeof(ItemMouseDragHandler), "OnEventMouseScroll")]
    public static class ScrollRotatePatch
    {
        private static readonly MethodInfo FlipHeldItemMethod =
            AccessTools.Method(typeof(ItemMouseDragHandler), "FlipHeldItem");

        private static readonly MethodInfo RefreshDragHighlightMethod =
            AccessTools.Method(typeof(ItemMouseDragHandler), "RefreshDragHighlight");

        /// <summary>
        /// Rev12 / Rev13（公开正式版）里 isScrollIgnore 是实例字段，异常路径收不到 Release
        /// 会卡在 true，此后滚轮持续失效；Rev16 起改成计算属性，字段不存在。
        /// 这里按需取字段：取不到（Rev16+）就整段跳过，不依赖版本号判断。
        /// </summary>
        private static readonly FieldInfo ScrollIgnoreField =
            AccessTools.Field(typeof(ItemMouseDragHandler), "isScrollIgnore");

        public static bool Prepare()
        {
            return AccessTools.Method(typeof(ItemMouseDragHandler), "OnEventMouseScroll") != null
                   && AccessTools.Field(typeof(ItemMouseDragHandler), "currentItem") != null;
        }

        public static bool Prefix(
            ItemMouseDragHandler __instance,
            Vector2 scrollDelta,
            ref GameItemElement ___currentItem)
        {
            // Rev12/Rev13 的 sticky 自修复；Rev16+ 无此字段，直接跳过
            RepairStickyScrollIgnore(__instance);

            // 仅在「拖拽中 + 按住修饰键 + 滚轮有输入」时接管
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

        private static void RepairStickyScrollIgnore(ItemMouseDragHandler instance)
        {
            if (ScrollIgnoreField == null)
            {
                return; // Rev16+：计算属性，没有可修复的存储状态
            }
            try
            {
                if (!(ScrollIgnoreField.GetValue(instance) is bool stuck) || !stuck)
                {
                    return;
                }
                if (Input.GetKey(instance.scrollIgnoreKey))
                {
                    return; // 键仍按着，属正常忽略
                }
                ScrollIgnoreField.SetValue(instance, false);
                Core.Debug("修复 sticky isScrollIgnore");
            }
            catch
            {
                // 自修复失败不影响主流程
            }
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
    ///  - scrollIgnore：Rev16 起 isScrollIgnore 是计算属性，这里按 scrollIgnoreKey 实时判断；
    ///  - scrollIgnoreKey / scrollMult10Key / scrollMult100Key：这三个是场景里配置的
    ///    Inspector 字段（不在 KeyBindHelper 体系内），实际值只能运行时读取。
    /// </summary>
    [HarmonyPatch(typeof(ItemMouseDragHandler), "StartDrag")]
    public static class DragDiagnosticsPatch
    {
        public static bool Prepare()
        {
            return AccessTools.Method(typeof(ItemMouseDragHandler), "StartDrag") != null
                   && AccessTools.Field(typeof(ItemMouseDragHandler), "currentItem") != null
                   && AccessTools.Field(typeof(ItemMouseDragHandler), "maxRemove") != null;
        }

        public static void Postfix(
            ItemMouseDragHandler __instance,
            GameItemElement ___currentItem,
            int ___maxRemove)
        {
            if (Core.VerboseLog == null || !Core.VerboseLog.Value)
            {
                return;
            }

            try
            {
                string id = ___currentItem?.identifier ?? "?";
                int units = ___currentItem?.unitCount ?? 0;
                bool scrollIgnore = Input.GetKey(__instance.scrollIgnoreKey);
                Core.Log?.Log(
                    $"[诊断] 拖拽 {id} unitCount={units} maxRemove={___maxRemove} isScrollIgnore={scrollIgnore} | " +
                    $"scrollIgnoreKey={__instance.scrollIgnoreKey} mult10={__instance.scrollMult10Key} mult100={__instance.scrollMult100Key}");
            }
            catch
            {
                // 诊断不影响主流程
            }
        }
    }
}
