D&DHelper - портативная версия
==============================

Распакуйте архив в любую папку (с правом записи) и запустите:
  Windows:  DNDHelper\DNDHelper.exe
  Linux:    ./DNDHelper/DNDHelper

Структура:
  DNDHelper/         приложение
  Updater/           программа самообновления (DNDUpdater)
  Data/              ваши сохранения и кэш конфигов - создаётся при первом запуске
  portable.txt       маркер портативного режима; если его удалить, данные будут храниться в профиле пользователя
                     (Windows: %LocalAppData%\DNDHelper, Linux: ~/.local/share/DNDHelper)

Обновление: при запуске приложение проверяет релизы на GitHub; если есть новая версия, предложит её установить.
Заменяются только папки DNDHelper/ и Updater/ - папка Data/ с вашими сохранениями не затрагивается.
Журнал обновления: Data/updater.log. Перенести программу на другой компьютер можно копированием всей папки.

Требования
----------
Windows 10/11 x64:  ничего дополнительно (для встроенного окна справочников нужен Microsoft Edge WebView2 Runtime -
                    в Windows 11 он есть, в Windows 10 при его отсутствии справочники откроются в браузере).
Linux (x64, arm64): окружение рабочего стола (X11 или Wayland), библиотеки fontconfig, libicu и libX11/libxkbcommon.
                    Для встроенного окна справочников нужен WebKitGTK; без него справочники откроются в браузере.
  Debian/Ubuntu:  sudo apt install libicu-dev libfontconfig1 libgtk-3-0 libwebkit2gtk-4.1-0 libsoup-3.0-0
  Fedora:         sudo dnf install libicu fontconfig gtk3 webkit2gtk4.1 libsoup3
  Arch:           sudo pacman -S icu fontconfig gtk3 webkit2gtk-4.1 libsoup3
  NixOS:          используйте flake проекта (nix run), а не портативный архив.

Если после распаковки Linux-версия не запускается, убедитесь, что файлы исполняемые:
  chmod +x DNDHelper/DNDHelper Updater/DNDUpdater
