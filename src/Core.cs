using System;

namespace MouseKeybinds
{
    /// <summary>
    /// Mod 入口，由 ModLoader 反射找到并实例化（程序集内只能有一个 IMod 实现）。
    ///
    /// 本 Mod 是纯 Harmony 补丁形态：所有功能都在 Patches.cs 的补丁里，
    /// 由游戏改键界面 / 拖拽系统被动触发，没有每帧逻辑，因此不挂 ModDriver。
    /// 补丁本身在 Init 之前由加载器的 PatchAll 自动完成（见总纲 §3.2）。
    /// </summary>
    public sealed class Core : IMod
    {
        internal const string ModId = "mouse_keybinds";

        internal static ModManifest Manifest { get; private set; }
        internal static ModLog Log { get; private set; }
        internal static ModConfig Config { get; private set; }

        internal static ConfigEntry<bool> VerboseLog { get; private set; }
        internal static ConfigEntry<string> ScrollRotateModifier { get; private set; }

        public void Init(ModManifest manifest)
        {
            Manifest = manifest;
            Log = new ModLog(manifest);

            try
            {
                // 配置会写到 persistentDataPath/ModConfigs/<ModId>.json，并自动出现在游戏内 Mod 设置界面
                Config = ModHelper.GetConfig(manifest.ID);

                VerboseLog = Config.Define(
                    "VerboseLog", false, "通用",
                    () => "详细日志", () => "打开后记录每次鼠标键录入与滚轮旋转接管");

                ScrollRotateModifier = Config.Define(
                    "ScrollRotateModifier", "LeftAlt", "通用",
                    () => "滚轮旋转修饰键",
                    new[] { "None", "LeftAlt", "RightAlt", "LeftShift", "LeftControl", "RightControl" },
                    () => "拖拽物品时按住该键滚动滚轮，强制旋转（堆叠物品同样生效）。选 None 恢复原版行为");
            }
            catch (Exception ex)
            {
                Log.Warning("配置初始化失败，改用默认值: " + ex.Message);
            }

            Log.Log($"v{manifest.ModVersion} 已加载（补丁: 侧键录入 / 键名本地化 / 滚轮旋转兜底 / 面板清理，无驱动对象）");
        }

        public void OnEnable()
        {
            Log?.Log("已启用");
        }

        public void OnDisable()
        {
            // 补丁由加载器在加载失败路径 UnpatchAll，正常禁用流程下保持挂载（与官方 PatchAll 语义一致）
            Log?.Log("已禁用（需重启游戏生效）");
        }

        internal static void Debug(string message)
        {
            if (VerboseLog != null && VerboseLog.Value)
            {
                Log?.Log(message);
            }
        }
    }
}
