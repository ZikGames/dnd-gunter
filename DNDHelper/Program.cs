using Avalonia;
using System;
using System.Diagnostics;

namespace DNDHelper
{
    class Program
    {
        // Инициализация в отдельном методе (не прямо в Main) — важно для дизайнера
        // Avalonia (Previewer) и для платформ вроде Android/iOS, где вызывается
        // BuildAvaloniaApp() напрямую, минуя обычный Main.
        [STAThread]
        public static void Main(string[] args)
        {
            // Avalonia пишет предупреждения (ошибки биндингов, стилей, рендера) через Trace,
            // а в Release у Trace нет слушателя — из-за этого "пустое окно" без единой строки
            // в терминале. Выводим Trace в консоль.
            Trace.Listeners.Add(new ConsoleTraceListener());
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder.Configure<App>()
                .WithInterFont()
                .LogToTrace();

            // Нативный Wayland-бэкенд (Avalonia.Wayland, официально с 12.1) — говорит с
            // Wayland-композитором напрямую, без прохода через XWayland. Помечен как
            // experimental самой Avalonia (не включается через UsePlatformDetect() сам,
            // не хватает части KDE-специфичных вещей вроде глобального меню/blur-behind),
            // поэтому по умолчанию НЕ используется — включается явно:
            // DND_USE_WAYLAND=1 ./result/bin/DNDHelper
            // Если её нет или что-то пошло не так - используем прежний путь (UsePlatformDetect,
            // на Linux это X11/XWayland), который до апгрейда хотя бы частично рисовал окно.
            if (Environment.GetEnvironmentVariable("DND_USE_WAYLAND") == "1")
            {
                // UsePlatformDetect() сам подключает и windowing-бэкенд, и рендер (Skia).
                // UseWayland() — это только windowing-бэкенд, рендер отдельно не тянет,
                // без явного UseSkia() падает с "No rendering system configured".
                builder = builder.UseWayland().UseSkia().UseHarfBuzz();
            }
            else
            {
                builder = builder.UsePlatformDetect();
            }

            // Диагностика полностью прозрачного окна на Linux (частая история с GL-рендером
            // под XWayland/без нужных GL-библиотек): DND_SOFTWARE_RENDER=1 ./result/bin/DNDHelper
            // Действует только для X11-пути (у нативного Wayland-бэкенда свой рендер через EGL,
            // тут не настраивается).
            if (Environment.GetEnvironmentVariable("DND_SOFTWARE_RENDER") == "1")
            {
                builder = builder.With(new X11PlatformOptions
                {
                    RenderingMode = new[] { X11RenderingMode.Software }
                });
            }

            return builder;
        }
    }
}
