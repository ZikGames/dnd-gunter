using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DNDHelper.ViewModels
{
    /// <summary>
    /// Проверка обновлений по GitHub Releases форка (см. AppInfo.Repository).
    ///  - портативная установка (portable.txt + Updater/) - скачивает архив и обновляется сама;
    ///  - любая другая (Nix, пакет дистрибутива, установщик) - только сообщает о новой версии и открывает страницу релиза.
    /// Check() без аргумента - тихая проверка при запуске, Check(true) - ручная из меню (всегда отвечает).
    /// </summary>
    internal class CheckUpdate
    {
        public static async Task Check(bool check = false)
        {
            if (!AppInfo.IsRepositoryConfigured)
            {
                if (check)
                    await Say("Обновления", "Репозиторий обновлений не настроен: укажите его в AppInfo.Repository.");
                return;
            }

            ReleaseInfo? release;
            try
            {
                release = await UpdateClient.GetLatestAsync(AppInfo.Repository);
            }
            catch (Exception e)
            {
                if (check)
                    await Say("Ошибка", $"Ошибка подключения: {e.Message}", Icon.Error);
                return;
            }

            if (release == null)
            {
                if (check)
                    await Say("Обновления", "В репозитории ещё нет релизов.");
                return;
            }

            // Локальные сборки ("dev") с релизами не сравниваются, чтобы не предлагать "обновиться" с отладочной.
            if (!UpdateClient.TryParseVersion(AppInfo.Version, out _))
            {
                if (check)
                    await Say("Обновления", $"Сборка разработки ({AppInfo.Version}). Последний релиз: {release.Tag}.");
                return;
            }

            if (!UpdateClient.IsNewer(release.Tag, AppInfo.Version))
            {
                if (check)
                    await Say("Обновления", $"Уже установлена последняя версия ({AppInfo.Version}).");
                return;
            }

            ReleaseAsset? asset = null;
            string hint = "";
            if (SelfUpdater.CanSelfUpdate(out string reason))
            {
                asset = UpdateClient.PickAsset(release, UpdateClient.PlatformSuffix());
                if (asset == null)
                    hint = $"\nВ релизе нет архива для вашей платформы ({UpdateClient.PlatformSuffix()}).";
            }
            else
            {
                hint = "\n" + reason;
            }

            if (asset == null)
            {
                var open = await Ask("Обновление",
                    $"Доступна версия {release.Tag} (у вас {AppInfo.Version}).{hint}\nОткрыть страницу релиза?");
                if (open == ButtonResult.Yes)
                    OpenUrl(release.PageUrl);
                return;
            }

            var answer = await Ask("Обновление",
                $"Доступна версия {release.Tag} (у вас {AppInfo.Version}). Скачать и установить сейчас?\n"
                + "Сохранения лежат отдельно и обновление их не затронет.");
            if (answer != ButtonResult.Yes)
                return;

            try
            {
                await SelfUpdater.DownloadAndApplyAsync(asset);
            }
            catch (Exception e)
            {
                await Say("Ошибка обновления", e.Message, Icon.Error);
            }
        }

        private static async Task Say(string title, string text, Icon icon = Icon.None)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, text, ButtonEnum.Ok, icon);
            await box.ShowAsync();
        }

        private static async Task<ButtonResult> Ask(string title, string text)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, text, ButtonEnum.YesNo);
            return await box.ShowAsync();
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Не удалось открыть ссылку: {ex}");
            }
        }
    }
}
