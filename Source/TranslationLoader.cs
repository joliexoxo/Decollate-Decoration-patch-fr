using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx.Logging;

namespace DecollateFR
{
    internal static class TranslationLoader
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly HashSet<string> LoggedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static ManualLogSource _logger;

        internal static string RootDirectory { get; private set; }

        internal static void Initialize(string rootDirectory, ManualLogSource logger)
        {
            RootDirectory = Path.GetFullPath(rootDirectory);
            _logger = logger;
            Directory.CreateDirectory(RootDirectory);
        }

        internal static bool TryReadResource(string resourcePath, out string text)
        {
            text = null;
            string normalized = normalize_path(resourcePath);
            const string englishPrefix = "localize/en/";

            if (!normalized.StartsWith(englishPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            string relativePath = normalized.Substring(englishPrefix.Length);
            return try_read_file(relativePath, out text);
        }

        internal static bool TryReadLocalizedFile(string folderName, string fileName, out string text)
        {
            text = null;

            string folder = normalize_path(folderName).Trim('/');
            string file = normalize_path(fileName).Trim('/');
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(file))
                return false;

            return try_read_file(folder + "/" + file, out text);
        }

        internal static void ApplyTableOverrides(
            string resourcePath,
            Dictionary<string, Dictionary<Language, string>> data)
        {
            string normalized = normalize_path(resourcePath);
            const string prefix = "localize/";

            if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return;

            string relativePath = normalized.Substring(prefix.Length);
            if (!try_read_file("tables/" + relativePath, out string text, ".tsv"))
                return;

            int applied = 0;
            int unknown = 0;

            foreach (string line in split_lines(text))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                int separator = line.IndexOf('\t');
                if (separator <= 0)
                    continue;

                string key = line.Substring(0, separator);
                string value = line.Substring(separator + 1);

                if (data.TryGetValue(key, out Dictionary<Language, string> values))
                {
                    values[Language.EN] = value;
                    applied++;
                }
                else
                {
                    unknown++;
                }
            }

            _logger.LogInfo($"Table FR appliquée : {relativePath} ({applied} entrée(s)).");
            if (unknown > 0)
                _logger.LogWarning($"{unknown} clé(s) FR inconnue(s) dans la table {relativePath}.");
        }

        internal static string RemoveComments(string text)
        {
            string[] lines = split_lines(text);

            for (int i = 0; i < lines.Length; i++)
            {
                int comment = lines[i].IndexOf("//", StringComparison.Ordinal);
                if (comment >= 0)
                    lines[i] = lines[i].Substring(0, comment);
            }

            return string.Join("\r\n", lines);
        }

        private static bool try_read_file(string relativePath, out string text, string extraExtension = null)
        {
            text = null;
            if (string.IsNullOrEmpty(RootDirectory))
                return false;

            string normalized = normalize_path(relativePath).Trim('/');
            if (string.IsNullOrEmpty(normalized))
                return false;

            var candidates = new List<string> { normalized };
            if (!Path.HasExtension(normalized))
            {
                candidates.Add(normalized + ".txt");
                if (!string.IsNullOrEmpty(extraExtension))
                    candidates.Add(normalized + extraExtension);
            }

            foreach (string candidate in candidates)
            {
                if (!try_get_safe_path(candidate, out string fullPath) || !File.Exists(fullPath))
                    continue;

                try
                {
                    text = normalize_line_endings(File.ReadAllText(fullPath, Utf8));
                    if (LoggedFiles.Add(fullPath))
                        _logger.LogInfo($"Traduction chargée : {candidate}");
                    return true;
                }
                catch (Exception exception) when (
                    exception is IOException ||
                    exception is UnauthorizedAccessException ||
                    exception is DecoderFallbackException)
                {
                    _logger.LogError($"Impossible de lire la traduction UTF-8 '{fullPath}' : {exception.Message}");
                    return false;
                }
            }

            return false;
        }

        private static bool try_get_safe_path(string relativePath, out string fullPath)
        {
            fullPath = null;

            try
            {
                fullPath = Path.GetFullPath(Path.Combine(
                    RootDirectory,
                    relativePath.Replace('/', Path.DirectorySeparatorChar)));

                string root = RootDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is NotSupportedException ||
                exception is PathTooLongException)
            {
                _logger.LogWarning($"Chemin de traduction ignoré : {relativePath}");
                return false;
            }
        }

        private static string normalize_path(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').Trim();
        }

        private static string normalize_line_endings(string text)
        {
            return string.Join("\r\n", split_lines(text));
        }

        private static string[] split_lines(string text)
        {
            return (text ?? string.Empty).Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.None);
        }
    }
}
