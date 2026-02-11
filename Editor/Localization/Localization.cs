using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace jp.lilxyzw.lilemo
{
    internal partial class L10n : ScriptableSingleton<L10n>
    {
        public LocalizationAsset localizationAsset;
        private static string[] languages;
        private static string[] languageNames;
        private static string localizationFolder => AssetDatabase.GUIDToAssetPath("5a55955ff2ae9c44c9ebd813911f9ac7");
        public delegate void CallbackFunction();
        public static CallbackFunction langchanged;

        internal static void Load()
        {
            var path = localizationFolder + "/" + Settings.instance.language + ".po";
            if(File.Exists(path)) instance.localizationAsset = AssetDatabase.LoadAssetAtPath<LocalizationAsset>(path);
            if(!instance.localizationAsset) instance.localizationAsset = new LocalizationAsset();
        }

        internal static string[] GetLanguages()
        {
            return languages ??= Directory.GetFiles(localizationFolder).Where(f => f.EndsWith(".po")).Select(f => Path.GetFileNameWithoutExtension(f)).Where(f => !f.StartsWith(".")).ToArray();
        }

        private static string[] GetLanguageNames()
        {
            return languageNames ??= languages.Select(l => {
                if(l == "zh-Hans") return "简体中文";
                if(l == "zh-Hant") return "繁體中文";
                return new CultureInfo(l).NativeName;
            }).ToArray();
        }

        internal static string L(string key)
        {
            if (!instance.localizationAsset) Load();
            return instance.localizationAsset.GetLocalizedString(key);
        }

        internal static string L(string key, string code)
        {
            try
            {
                var path = localizationFolder + "/" + new CultureInfo(code).Name + ".po";
                if (File.Exists(path)) return AssetDatabase.LoadAssetAtPath<LocalizationAsset>(path).GetLocalizedString(key);
            }
            catch { }
            return key;
        }

        internal static VisualElement SelectionGUI()
        {
            return new IMGUIContainer(() =>
            {
                EditorGUI.BeginChangeCheck();
                var value = EditorGUILayout.Popup("Language", Array.IndexOf(GetLanguages(), GetLanguages().Contains(Settings.instance.language) ? Settings.instance.language : "en-US"), GetLanguageNames());
                if (EditorGUI.EndChangeCheck())
                {
                    Settings.instance.language = GetLanguages()[value];
                    Settings.Save();
                    Load();
                    langchanged?.Invoke();
                }
            });
        }
    }
}
