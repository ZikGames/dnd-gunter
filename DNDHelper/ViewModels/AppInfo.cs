using System;
using System.Reflection;

namespace DNDHelper.ViewModels
{
    /// <summary>Версия приложения и адрес репозитория, откуда берутся обновления.</summary>
    public static class AppInfo
    {
        /// <summary>
        /// GitHub-репозиторий форка в формате "владелец/имя" - отсюда берутся релизы для проверки и установки
        /// обновлений. ЗАМЕНИТЕ на свой (пока стоит заглушка - проверка обновлений просто отключена).
        /// </summary>
        public const string Repository = "ZikGames/dnd-gunter";

        public static bool IsRepositoryConfigured =>
            !Repository.StartsWith("CHANGE-ME", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Версия сборки. Release-workflow собирает проект с -p:Version=&lt;тег без "v"&gt;, локальные
        /// сборки получают 0.0.0-dev из .csproj и показываются как "dev".
        /// </summary>
        public static string Version { get; } = ReadVersion();

        private static string ReadVersion()
        {
            Assembly asm = typeof(AppInfo).Assembly;
            string? info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (string.IsNullOrWhiteSpace(info))
                info = asm.GetName().Version?.ToString();
            if (string.IsNullOrWhiteSpace(info))
                return "dev";

            int plus = info.IndexOf('+');            // отрезаем "+<хэш коммита>", который добавляет SDK
            if (plus >= 0)
                info = info.Substring(0, plus);

            return info.StartsWith("0.0.0", StringComparison.Ordinal) ? "dev" : info;
        }
    }
}
