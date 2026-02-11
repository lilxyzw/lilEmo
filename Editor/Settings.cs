using System.Globalization;
using UnityEditor;

namespace jp.lilxyzw.lilemo
{
    [FilePath("jp.lilxyzw/lilemo.asset", FilePathAttribute.Location.PreferencesFolder)]
    internal class Settings : ScriptableSingleton<Settings>
    {
        public string language = CultureInfo.CurrentCulture.Name;
        internal static void Save() => instance.Save(true);
    }
}
