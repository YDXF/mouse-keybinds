using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace MouseKeybinds
{
    /// <summary>
    /// 按键冲突检查 + 确认弹窗（v1.2.0）。
    ///
    /// 设计（agents.md §4.9）：录入一个已被其他功能使用的按键时，弹窗列出该键已承担的
    /// 全部功能（24 个可改键位 / 储物协调器预设 / 拖拽系统的隐藏场景键位），玩家可选择
    /// 继续绑定（一键多功能，原版 1.0.0.6 起游戏在改键界面是直接拒绝的）或取消；
    /// 勾选「以后不再提示」后冲突直接允许，可随时在 Mod 设置里重新打开。
    ///
    /// 冲突登记簿（三处来源）：
    ///  1. KeyBindHelper.Bindings —— 24 个原生可改键位（label 已按语言本地化）；
    ///  2. MachineStorageCoordinator.presets —— 当前打开的协调器的容器预设快捷键；
    ///  3. ItemMouseDragHandler 的三个 Inspector 场景键（滚动忽略 / 数量×10 / ×100），
    ///     不在改键界面显示，只能运行时读取。拖拽固定键（右键交互 / 中键翻转）不参与：
    ///     Mouse1/Mouse2 永远不会被录入（本 Mod 录入范围 = 键盘 + Mouse3~6），不可能冲突。
    ///
    /// 弹窗用 CustomUIManager.CreateWindow + AddLabel/AddToggle/AddButton 搭建
    /// （与原版储物协调器窗口同一套 UI 原语），层用默认 overlay。
    /// </summary>
    internal static class ConflictCheck
    {
        private const string DialogId = "mk_keybind_conflict";
        private const int MaxConflictLines = 12;

        private static bool dialogOpen;
        private static bool dialogResolved;
        private static Action dialogConfirm;
        private static Action dialogCancel;

        internal static bool DialogOpen => dialogOpen;

        /// <summary>冲突弹窗开关（Mod 设置里可改；弹窗内勾选「以后不再提示」会写 false）。</summary>
        internal static bool PromptEnabled => Core.ConflictPrompt == null || Core.ConflictPrompt.Value;

        internal static string Loc(string key, params object[] args)
        {
            return ModHelper.GetLocalized(Core.ModId, key, args);
        }

        // ---------- 冲突收集 ----------

        /// <summary>
        /// 收集 <paramref name="key"/> 已被占用的全部功能名。
        /// exceptOptionId：改键界面录入时排除正在改的键位本身；
        /// exceptPreset：协调器录入时排除正在改的预设本身（按引用比较，允许同名预设）。
        /// </summary>
        internal static List<string> CollectConflicts(KeyCode key, string exceptOptionId, object exceptPreset)
        {
            List<string> list = new List<string>();

            // 1) 原生 24 个可改键位
            try
            {
                foreach (KeyBindHelper.Binding b in KeyBindHelper.Bindings)
                {
                    if (exceptOptionId != null && b.id == exceptOptionId)
                    {
                        continue;
                    }
                    if (KeyBindHelper.Get(b.id) == key)
                    {
                        list.Add(SafeLabel(b));
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Debug("冲突收集（键位表）失败: " + ex.Message);
            }

            // 2) 储物协调器容器预设（当前打开的协调器）
            try
            {
                List<MachineStorageCoordinator.Preset> presets =
                    AccessTools.Field(typeof(MachineStorageCoordinator), "presets")?.GetValue(null)
                    as List<MachineStorageCoordinator.Preset>;
                if (presets != null)
                {
                    foreach (MachineStorageCoordinator.Preset p in presets)
                    {
                        if ((KeyCode)p.keyCode == key && !ReferenceEquals(p, exceptPreset))
                        {
                            list.Add(PresetDisplayName(p));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Debug("冲突收集（协调器预设）失败: " + ex.Message);
            }

            // 3) 拖拽系统的隐藏场景键位
            ItemMouseDragHandler drag = ItemMouseDragHandler.current;
            if (drag != null)
            {
                AddHidden(list, key, drag.scrollIgnoreKey, "hidden_scroll_ignore");
                AddHidden(list, key, drag.scrollMult10Key, "hidden_mult10");
                AddHidden(list, key, drag.scrollMult100Key, "hidden_mult100");
            }

            return list;
        }

        private static void AddHidden(List<string> list, KeyCode key, KeyCode hidden, string nameKey)
        {
            if (key == hidden)
            {
                list.Add(Loc(nameKey));
            }
        }

        private static string SafeLabel(KeyBindHelper.Binding b)
        {
            try
            {
                string s = b.label?.Invoke();
                if (!string.IsNullOrEmpty(s))
                {
                    return s;
                }
            }
            catch
            {
                // label 惰性取本地化失败时回退键名
            }
            return KeyBindHelper.KeyName(KeyBindHelper.Get(b.id));
        }

        /// <summary>预设统一带来源前缀（如「储物协调器预设--水」），玩家才知道冲突项是哪来的。</summary>
        internal static string PresetDisplayName(MachineStorageCoordinator.Preset p)
        {
            string name = string.IsNullOrEmpty(p.name) ? Loc("conflict_unnamed_preset") : p.name;
            return Loc("conflict_preset_prefix") + name;
        }

        /// <summary>
        /// 按 optionId 找键位的功能名（用于「本次修改」行）。
        /// 注意：改键界面捕获流程里流转的是注册表 option id（`key_rotate_ccw` 这种带 key_ 前缀），
        /// 而 Bindings 表里的 id 是裸绑定 id（`rotate_ccw`）——两种都要能查到，查不到才回退原样。
        /// </summary>
        internal static string OptionName(string optionId)
        {
            string bindId = optionId != null && optionId.StartsWith("key_") ? optionId.Substring(4) : optionId;
            try
            {
                foreach (KeyBindHelper.Binding b in KeyBindHelper.Bindings)
                {
                    if (b.id == bindId || b.id == optionId)
                    {
                        return SafeLabel(b);
                    }
                }
            }
            catch
            {
                // 收集失败时回退 optionId
            }
            return optionId;
        }

        // ---------- 确认弹窗 ----------

        /// <summary>
        /// 弹出冲突确认框。清单第一行是 <paramref name="selfName"/>（本次正在修改的功能，
        /// 空/null 则跳过），其后为已占用该键的其他功能。onConfirm：继续绑定（调用方完成写入）；
        /// onCancel：放弃（一般为空操作，改键界面保持录入态，协调器由补丁还原旧键值）。
        /// 弹窗无法打开时退化为直接允许（记日志）。
        /// </summary>
        internal static void ShowConfirm(KeyCode key, string selfName, List<string> conflicts, Action onConfirm, Action onCancel)
        {
            try
            {
                CustomUIManager manager = CustomUIManager.Instance;
                if (manager == null)
                {
                    throw new InvalidOperationException("CustomUIManager 不可用");
                }

                StringBuilder sb = new StringBuilder();
                sb.Append('\n'); // 标题栏与正文之间留空
                sb.Append(Loc("conflict_header", KeyBindHelper.KeyName(key))).Append('\n');
                int n = 1;
                if (!string.IsNullOrEmpty(selfName))
                {
                    sb.Append(n++).Append(". ").Append(selfName).Append(Loc("conflict_this_change")).Append('\n');
                }
                foreach (string c in conflicts)
                {
                    sb.Append(n++).Append(". ").Append(c).Append('\n');
                    if (n > MaxConflictLines && n <= conflicts.Count + 1)
                    {
                        sb.Append("...\n");
                        break;
                    }
                }
                sb.Append('\n').Append(Loc("conflict_confirm"));

                dialogResolved = false;
                dialogConfirm = onConfirm;
                dialogCancel = onCancel;

                bool suppressAfterwards = false;
                CustomUIBuilder b = manager.CreateWindow(DialogId, Loc("conflict_title"), "overlay")
                    .SetSize(540f, 460f)
                    .SetCloseOnEscape()
                    .OnClose(OnDialogClosed);
                b.AddLabel(sb.ToString());
                ApplyBiggerFont(b.Last);
                CustomUIElement listLabel = b.Last;
                b.AddLabel(""); // 「确认继续绑定吗？」↔ 勾选框之间空一行
                b.Last.SetPreferredSize(-1f, 30f);
                b.AddToggle(Loc("conflict_suppress"), false, v => suppressAfterwards = v);
                CustomUIElement suppressToggle = b.Last;
                b.AddLabel(""); // 勾选框 ↔ 按钮之间空一行
                b.Last.SetPreferredSize(-1f, 30f);
                b.BeginRow(4f, TextAnchor.MiddleCenter);
                b.AddFlexibleSpace();
                b.AddButton(Loc("conflict_ok"), () => Resolve(suppressAfterwards, confirmed: true), "ok");
                b.Last.SetPreferredSize(120f, 36f);
                CustomUIElement okBtn = b.Last;
                b.AddButton(Loc("conflict_cancel"), () => Resolve(suppressAfterwards, confirmed: false), "cancel");
                b.Last.SetPreferredSize(120f, 36f);
                CustomUIElement cancelBtn = b.Last;
                b.AddFlexibleSpace();
                b.End();
                ApplyBiggerFont(suppressToggle);
                ApplyBiggerFont(okBtn);
                ApplyBiggerFont(cancelBtn);

                // 高度按正文实际渲染高度确定（实测 preferredHeight + 勾选框/间隔/按钮/标题栏的固定余量），
                // 避免估算公式高估导致按钮下方大片空白
                float labelHeight;
                try
                {
                    labelHeight = listLabel.text.preferredHeight;
                }
                catch
                {
                    labelHeight = 260f; // 取不到时退回估算（约 5 行）
                }
                float height = Mathf.Clamp(labelHeight + 240f, 460f, 1000f);
                b.SetSize(540f, height);
                b.Center();
                b.Show();
                dialogOpen = true;
                Core.Debug($"冲突弹窗: {KeyBindHelper.KeyName(key)}，冲突 {conflicts.Count} 项");
            }
            catch (Exception ex)
            {
                Core.Log?.Warning("冲突弹窗打开失败，退化为直接允许: " + ex.Message);
                dialogOpen = false;
                onConfirm?.Invoke();
            }
        }

        /// <summary>在样式默认值基础上放大字号并加行距（游戏默认字体较小，弹窗清单需要可读性）。</summary>
        private static void ApplyBiggerFont(CustomUIElement e)
        {
            try
            {
                TextMeshProUGUI tmp = e?.text;
                if (tmp == null)
                {
                    return;
                }
                float baseSize = tmp.fontSize;
                if (baseSize <= 0f)
                {
                    return;
                }
                tmp.fontSize = baseSize * 1.4f;
                tmp.lineSpacing = baseSize * 0.45f;
            }
            catch
            {
                // 字体调整失败不影响弹窗功能
            }
        }

        private static void Resolve(bool suppressAfterwards, bool confirmed)
        {
            if (suppressAfterwards && confirmed)
            {
                try
                {
                    Core.ConflictPrompt?.Set(false);
                    Core.Config?.Save();
                }
                catch (Exception ex)
                {
                    Core.Debug("写入「不再提示」配置失败: " + ex.Message);
                }
            }
            dialogResolved = true;
            dialogOpen = false;
            try
            {
                CustomUIManager.Instance?.GetWindow(DialogId)?.Close();
            }
            catch
            {
                // 窗口可能已自行关闭
            }
            Action c = dialogConfirm, x = dialogCancel;
            dialogConfirm = null;
            dialogCancel = null;
            if (confirmed)
            {
                c?.Invoke();
            }
            else
            {
                x?.Invoke();
            }
        }

        /// <summary>窗口被 X / ESC 关闭 = 取消（Resolve 已处理过的关闭到此为空操作）。</summary>
        private static void OnDialogClosed()
        {
            if (dialogResolved)
            {
                return;
            }
            dialogResolved = true;
            dialogOpen = false;
            Action x = dialogCancel;
            dialogConfirm = null;
            dialogCancel = null;
            x?.Invoke();
        }
    }

    /// <summary>
    /// 改键界面冲突接管（v1.2.0）：原生 PollKeyCapture 在发现冲突时直接拒绝写入；
    /// 本补丁在其之前拦截冲突分支——弹确认框（或按配置直接允许），确认后代原生完成
    /// 写入（pendingKeys + EndCapture + RefreshRules）。无冲突 / 保留键 / 取消等分支
    /// 一律放行原生处理。
    /// </summary>
    [HarmonyPatch(typeof(GameOptionUIManager), "PollKeyCapture")]
    public static class ConflictPromptPatch
    {
        private static readonly MethodInfo EndCaptureMethod =
            AccessTools.Method(typeof(GameOptionUIManager), "EndCapture");
        private static readonly MethodInfo SetStatusMethod =
            AccessTools.Method(typeof(GameOptionUIManager), "SetKeyBindStatus");
        private static readonly MethodInfo RefreshRulesMethod =
            AccessTools.Method(typeof(GameOptionUIManager), "RefreshRules");
        private static readonly PropertyInfo IsOpenProperty =
            AccessTools.Property(typeof(GameOptionUIManager), "IsOpen");

        /// <summary>补丁前置检查：任一依赖缺失则静默跳过，保持原生拒绝行为。</summary>
        public static bool Prepare()
        {
            return AccessTools.Method(typeof(GameOptionUIManager), "PollKeyCapture") != null
                   && EndCaptureMethod != null && SetStatusMethod != null
                   && RefreshRulesMethod != null && IsOpenProperty != null;
        }

        public static bool Prefix(GameOptionUIManager __instance, Dictionary<string, KeyCode> ___pendingKeys)
        {
            try
            {
                if (ConflictCheck.DialogOpen)
                {
                    return false; // 弹窗期间冻结录入轮询
                }
                if (!KeyBindHelper.IsCapturing)
                {
                    return true;
                }

                KeyBindHelper.CaptureResult result = KeyBindHelper.PollCapture(out KeyCode key);
                if (result != KeyBindHelper.CaptureResult.Key || KeyBindHelper.IsReserved(key))
                {
                    return true; // 取消 / 保留键 / 无输入：交原生（同帧重复轮询无副作用）
                }

                string captId = KeyBindHelper.CapturingOptionId;
                if (captId == null)
                {
                    return true;
                }

                bool conflict = false;
                foreach (KeyValuePair<string, KeyCode> kv in ___pendingKeys)
                {
                    if (kv.Key != captId && kv.Value == key)
                    {
                        conflict = true;
                        break;
                    }
                }
                if (!conflict)
                {
                    return true;
                }

                // 冲突分支接管：原生在此处会拒绝写入
                List<string> conflicts = ConflictCheck.CollectConflicts(key, captId, null);
                if (conflicts.Count == 0 || !ConflictCheck.PromptEnabled)
                {
                    Commit(__instance, ___pendingKeys, captId, key);
                    return false;
                }

                ConflictCheck.ShowConfirm(key, ConflictCheck.OptionName(captId), conflicts,
                    onConfirm: () => Commit(__instance, ___pendingKeys, captId, key),
                    onCancel: null); // 取消：录入态保持，玩家可改按其他键
                return false;
            }
            catch (Exception ex)
            {
                Core.Debug("改键冲突补丁异常，退回原生行为: " + ex.Message);
                return true;
            }
        }

        private static void Commit(GameOptionUIManager inst, Dictionary<string, KeyCode> pending, string id, KeyCode key)
        {
            try
            {
                // 弹窗期间玩家可能关掉了设置窗口 / 结束了录入，此时放弃提交
                if (!(bool)IsOpenProperty.GetValue(inst, null) || !KeyBindHelper.IsCapturing)
                {
                    return;
                }
                pending[id] = key;
                SetStatusMethod.Invoke(inst, new object[] { "" });
                EndCaptureMethod.Invoke(inst, null);
                RefreshRulesMethod.Invoke(inst, null);
            }
            catch (Exception ex)
            {
                Core.Debug("冲突确认提交失败: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// 协调器预设冲突提醒（v1.2.0）：预设录入本身没有冲突检查，接受后键值直接落盘。
    /// 本补丁在 HandleKeyCapture 前快照正在录入的预设，若其后键值发生变化（= 刚接受新键），
    /// 检查预设×预设、预设×键位、预设×隐藏键冲突并弹窗；取消则还原旧键值并重存、重绘。
    /// </summary>
    [HarmonyPatch(typeof(MachineStorageCoordinator), "HandleKeyCapture")]
    public static class CoordinatorConflictPatch
    {
        private static readonly FieldInfo CapturingField =
            AccessTools.Field(typeof(MachineStorageCoordinator), "capturingPreset");
        private static readonly MethodInfo SavePresetsMethod =
            AccessTools.Method(typeof(MachineStorageCoordinator), "SaveOpenPresets");
        private static readonly MethodInfo RebuildMethod =
            AccessTools.Method(typeof(MachineStorageCoordinator), "RebuildList");

        private static MachineStorageCoordinator.Preset snapshot;
        private static int snapshotKey;

        /// <summary>补丁前置检查：任一依赖缺失则静默跳过，预设行为与原生一致。</summary>
        public static bool Prepare()
        {
            return CapturingField != null && SavePresetsMethod != null && RebuildMethod != null
                   && AccessTools.Method(typeof(MachineStorageCoordinator), "HandleKeyCapture") != null;
        }

        public static void Prefix()
        {
            snapshot = CapturingField?.GetValue(null) as MachineStorageCoordinator.Preset;
            snapshotKey = snapshot?.keyCode ?? 0;
        }

        public static void Postfix()
        {
            try
            {
                if (snapshot == null || ConflictCheck.DialogOpen)
                {
                    return;
                }
                if (CapturingField.GetValue(null) != null)
                {
                    return; // 仍在录入中
                }
                if (snapshot.keyCode == snapshotKey)
                {
                    return; // Escape / 右键取消，或按键被拒（键值未变）
                }

                KeyCode newKey = (KeyCode)snapshot.keyCode;
                List<string> conflicts = ConflictCheck.CollectConflicts(newKey, null, snapshot);
                if (conflicts.Count == 0 || !ConflictCheck.PromptEnabled)
                {
                    return;
                }

                MachineStorageCoordinator.Preset changed = snapshot;
                int oldKey = snapshotKey;
                ConflictCheck.ShowConfirm(newKey, ConflictCheck.PresetDisplayName(changed), conflicts,
                    onConfirm: null, // 确认：原生已保存新键值，无需动作
                    onCancel: () =>
                    {
                        try
                        {
                            changed.keyCode = oldKey;
                            SavePresetsMethod?.Invoke(null, null);
                            RebuildMethod?.Invoke(null, null);
                        }
                        catch (Exception ex)
                        {
                            Core.Debug("还原预设旧键值失败: " + ex.Message);
                        }
                    });
            }
            catch (Exception ex)
            {
                Core.Debug("协调器冲突补丁异常: " + ex.Message);
            }
        }
    }
}
