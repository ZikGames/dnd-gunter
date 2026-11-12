# D&D Helper
The original program was created by two people, for a local group of people

`i rewrite code to use avaloniaui and webkitgtk instead of WPF and webview2 just for myself`

> [Attention]

> this fork (as the original) maybe, will not suitable with original D&D

## Установка

| Способ | Платформа | Обновления |
|---|---|---|
| **Портативный архив** со страницы Releases (`DNDHelper-<версия>-<платформа>.zip`) | Windows x64, Linux x64/arm64 | приложение обновляется само |
| `nix run github:<владелец>/dnd-gunter` | NixOS / Nix | обновляйте через `nix flake update`; приложение только сообщает о новой версии |

**Портативный режим.** Распакуйте архив куда угодно и запустите `DNDHelper/DNDHelper(.exe)`. Рядом лежат
`Updater/` (самообновление), `Data/` (ваши сохранения, создаётся при запуске) и `portable.txt` (маркер режима).
Обновление заменяет только `DNDHelper/` и `Updater/`, `Data/` не трогает. Подробности и требования для Linux - в
`packaging/README-PORTABLE.txt` (он же кладётся в каждый архив).

## Выпуск релиза

1. Впишите свой репозиторий в `DNDHelper/ViewModels/AppInfo.cs` (константа `Repository`, формат `владелец/имя`).
2. Создайте и отправьте тег: `git tag v13.1 && git push origin v13.1`.
3. Workflow `.github/workflows/release.yml` соберёт приложение и Updater для `win-x64`, `linux-x64`, `linux-arm64`,
   упакует портативные архивы (`tools/make-portable-zip.py`) и опубликует релиз. Номер версии = тег без `v`.

Приложение сравнивает версию из тега со своей и качает архив своей платформы; если у релиза есть контрольная сумма
(`digest` на GitHub), она проверяется.

## Для разработчиков (Nix)

После изменения пакетов в `DNDHelper.csproj` пересоздайте список зависимостей:

```
nix build .#default.passthru.fetch-deps --no-link --print-out-paths   # выведет путь к скрипту
$(nix build .#default.passthru.fetch-deps --no-link --print-out-paths) deps.json
```

Встроенный браузер вкладки "Справочники" (`Avalonia.Controls.WebView`) на Linux использует WebKitGTK или WPE;
в `flake.nix` для этого уже есть `gtk3` и `webkitgtk_4_1`.
