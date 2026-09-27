using HarmonyLib;

namespace DecollateFR
{
    [HarmonyPatch(typeof(Game), nameof(Game.readText), typeof(string))]
    internal static class GameReadTextPatch
    {
        private static bool Prefix(string fileName, ref string __result)
        {
            if (!TranslationLoader.TryReadResource(fileName, out string translation))
                return true;

            __result = translation;
            return false;
        }
    }

    [HarmonyPatch(typeof(Game), "LoadText", typeof(string), typeof(string), typeof(bool))]
    internal static class GameLoadTextPatch
    {
        private static bool Prefix(string folderName, string fileName, bool without_comment, ref string __result)
        {
            if (!TranslationLoader.TryReadLocalizedFile(folderName, fileName, out string translation))
                return true;

            __result = without_comment
                ? TranslationLoader.RemoveComments(translation)
                : translation;
            return false;
        }
    }

    [HarmonyPatch(typeof(LocalizeFile), nameof(LocalizeFile.readFrom), typeof(string), typeof(bool))]
    internal static class LocalizeFileReadFromPatch
    {
        private static void Postfix(
            LocalizeFile __instance,
            string path)
        {
            TranslationLoader.ApplyTableOverrides(path, __instance.data);
        }
    }

    [HarmonyPatch(typeof(GameInitialize), "get_Lang")]
    internal static class GameInitializeLanguagePatch
    {
        private static bool Prefix(ref Language __result)
        {
            __result = Language.EN;
            return false;
        }
    }

    [HarmonyPatch(typeof(LanguageSetting), "get_LocalizeFolder")]
    internal static class LanguageFolderPatch
    {
        private static bool Prefix(ref string __result)
        {
            __result = "localize/en";
            return false;
        }
    }
}

