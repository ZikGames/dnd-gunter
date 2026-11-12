using System;
using System.IO;

namespace DNDHelper.ViewModels.Settings
{
    /// <summary>
    /// Оригинал (WPF) хранил Saves/ и Cache/ прямо рядом с exe — "портативный" режим.
    /// Под Nix (buildDotnetModule) это ломается в двух смыслах сразу:
    ///   1. exe лежит в /nix/store/&lt;hash&gt;-DNDHelper-.../bin/ — сам /nix/store
    ///      смонтирован read-only, писать туда вообще нельзя (UnauthorizedAccessException).
    ///   2. даже если бы получилось - при каждой пересборке хэш в пути меняется,
    ///      то есть "рядом с exe" каждый раз означает новую пустую директорию,
    ///      а старые сохранения так и останутся лежать в уже недостижимом
    ///      предыдущем /nix/store/&lt;старый-hash&gt;-.../.
    ///
    /// Поэтому по умолчанию используем XDG-каталоги (на Linux .NET сам мапит
    /// Environment.SpecialFolder.LocalApplicationData на $XDG_DATA_HOME или
    /// ~/.local/share, на Windows — на %LocalAppData%, так что код кросс-платформенный
    /// без ручного парсинга переменных окружения).
    ///
    /// "Портативный" режим (как в оригинале) остаётся доступен как явный опт-ин:
    /// если рядом с исполняемым файлом лежит файл-маркер portable.txt И туда реально
    /// можно писать — используем директорию exe. Для обычного nix-пакета такого файла
    /// не будет (и даже если бы был - там read-only), так что по умолчанию всегда
    /// сработает XDG-путь.
    /// </summary>
    public static class AppPaths
    {
        private const string AppFolderName = "DNDHelper";

        private static string? _cachedRoot;
        private static readonly Lazy<string?> _portableRoot = new(DetectPortableRoot);

        /// <summary>
        /// Корень портативной установки (каталог с файлом portable.txt) или null, если это обычная установка
        /// (Nix, пакет дистрибутива, установщик). Маркер ищется рядом с exe (старая схема) и на уровень выше -
        /// в новой схеме релизов приложение лежит в подпапке:
        ///   &lt;корень&gt;/portable.txt, &lt;корень&gt;/DNDHelper/ (exe), &lt;корень&gt;/Updater/, &lt;корень&gt;/Data/ (сохранения).
        /// Корень должен быть записываемым, иначе портативный режим не включается.
        /// </summary>
        public static string? PortableRoot => _portableRoot.Value;

        private static string? DetectPortableRoot()
        {
            string exeDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (string? dir in new[] { exeDir, Path.GetDirectoryName(exeDir) })
            {
                if (!string.IsNullOrEmpty(dir)
                    && File.Exists(Path.Combine(dir, "portable.txt"))
                    && IsWritable(dir))
                    return dir;
            }
            return null;
        }

        public static string GetWritableRoot()
        {
            if (_cachedRoot != null)
                return _cachedRoot;

            string? portable = PortableRoot;
            if (portable != null)
            {
                string exeDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                bool markerBesideExe = string.Equals(Path.GetFullPath(portable), Path.GetFullPath(exeDir),
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

                // Старая схема (маркер рядом с exe) - данные рядом с exe, как в WPF-оригинале.
                // Новая (маркер уровнем выше) - данные в <корень>/Data: так обновление может целиком заменить
                // папки DNDHelper/ и Updater/ и не тронуть сохранения.
                _cachedRoot = markerBesideExe ? portable : Path.Combine(portable, "Data");
            }
            else
            {
                // На Linux .NET сам резолвит это в $XDG_DATA_HOME или ~/.local/share,
                // на Windows - в %LocalAppData%. См. Environment.SpecialFolder на MSDN
                // и таблицу маппинга для Unix (Red Hat Developer blog, "special folder api linux").
                // SpecialFolderOption.Create: без него на чистом Linux-профиле, где ещё нет ~/.local/share,
                // GetFolderPath возвращает пустую строку.
                string dataHome = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData,
                    Environment.SpecialFolderOption.Create);
                if (string.IsNullOrEmpty(dataHome))   // нет $HOME (например, служебная учётка) - не пишем в текущий каталог
                    dataHome = Path.GetTempPath();
                _cachedRoot = Path.Combine(dataHome, AppFolderName);
            }

            Directory.CreateDirectory(_cachedRoot);
            return _cachedRoot;
        }

        private static bool IsWritable(string dir)
        {
            try
            {
                string probe = Path.Combine(dir, $".write-test-{Guid.NewGuid():N}");
                File.WriteAllText(probe, "");
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
