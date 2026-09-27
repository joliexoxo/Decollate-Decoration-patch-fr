using System.IO;
using BepInEx;
using HarmonyLib;

namespace DecollateFR
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "fr.jolie.decollatefr";
        public const string PluginName = "DecollateFR";
        public const string PluginVersion = "1.0.0";

        private Harmony _harmony;

        private void Awake()
        {
            string pluginDirectory = Path.GetDirectoryName(Info.Location);
            TranslationLoader.Initialize(Path.Combine(pluginDirectory, "translations"), Logger);

            LanguageSetting.Lang = Language.EN;

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            Logger.LogInfo($"{PluginName} {PluginVersion} chargé. Langue interne : EN.");
            Logger.LogInfo($"Traductions : {TranslationLoader.RootDirectory}");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}

