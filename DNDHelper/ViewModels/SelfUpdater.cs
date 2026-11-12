using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using DNDHelper.ViewModels.Settings;
using DNDHelper.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DNDHelper.ViewModels
{
    /// <summary>
    /// Самообновление портативной установки. Работающий exe нельзя заменить (на Windows он залочен), поэтому
    /// приложение скачивает архив, копирует папку Updater/ во временный каталог и запускает оттуда
    /// DNDUpdater - тот ждёт завершения приложения, подменяет папки DNDHelper/ и Updater/ (Data/ и
    /// portable.txt не трогает) и запускает новую версию.
    /// </summary>
    internal static class SelfUpdater
    {
        public static string UpdaterFileName => OperatingSystem.IsWindows() ? "DNDUpdater.exe" : "DNDUpdater";

        public static bool CanSelfUpdate(out string reason)
        {
            string? root = AppPaths.PortableRoot;
            if (root == null)
            {
                reason = "Приложение установлено не в портативном режиме (Nix, пакет дистрибутива, установщик) - "
                       + "обновляйте его тем же способом, которым устанавливали.";
                return false;
            }
            if (!File.Exists(Path.Combine(root, "Updater", UpdaterFileName)))
            {
                reason = $"Не найден Updater/{UpdaterFileName} рядом с приложением.";
                return false;
            }
            reason = "";
            return true;
        }

        public static async Task DownloadAndApplyAsync(ReleaseAsset asset)
        {
            string root = AppPaths.PortableRoot ?? throw new InvalidOperationException("Нет портативного корня.");
            string appExe = Environment.ProcessPath ?? throw new InvalidOperationException("Не определён путь к exe.");

            string zip = Path.Combine(Path.GetTempPath(), $"dndhelper-update-{Guid.NewGuid():N}.zip");
            MainWindow? window = MainWindow.Instance;
            string? oldTitle = window?.Title;
            try
            {
                var progress = new Progress<double>(p =>
                {
                    if (window != null)
                        window.Title = $"D&DHelper - скачивание обновления {p:P0}";
                });
                await UpdateClient.DownloadAsync(asset, zip, progress);
            }
            finally
            {
                if (window != null)
                    window.Title = oldTitle;
            }

            // Updater запускаем из временной копии, чтобы он мог заменить и самого себя.
            string tempUpdaterDir = Path.Combine(Path.GetTempPath(), "dndhelper-updater-" + Guid.NewGuid().ToString("N"));
            CopyDirectory(Path.Combine(root, "Updater"), tempUpdaterDir);
            string updaterExe = Path.Combine(tempUpdaterDir, UpdaterFileName);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(updaterExe, File.GetUnixFileMode(updaterExe)
                    | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);

            var psi = new ProcessStartInfo(updaterExe) { UseShellExecute = false, WorkingDirectory = tempUpdaterDir };
            psi.ArgumentList.Add("--pid");
            psi.ArgumentList.Add(Environment.ProcessId.ToString());
            psi.ArgumentList.Add("--zip");
            psi.ArgumentList.Add(zip);
            psi.ArgumentList.Add("--root");
            psi.ArgumentList.Add(root);
            psi.ArgumentList.Add("--exe");
            psi.ArgumentList.Add(Path.GetRelativePath(root, appExe));
            Process.Start(psi);

            // Штатное завершение (сработает Closed -> сохранение данных), а не Environment.Exit.
            Dispatcher.UIThread.Post(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    desktop.Shutdown();
                else
                    Environment.Exit(0);
            });
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
            foreach (string dir in Directory.GetDirectories(source))
                CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }
    }
}
