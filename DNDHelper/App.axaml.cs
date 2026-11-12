using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DNDHelper.ViewModels.Infrastructure;
using DNDHelper.Views;
using System.Linq;

namespace DNDHelper;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Стандартный шаг шаблона Avalonia MVVM: убираем встроенный
        // DataAnnotationsValidationPlugin, чтобы не дублировать валидацию
        // с той, что уже даёт CommunityToolkit.Mvvm (ObservableValidator).
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avalonia 12: BindingPlugins/DataAnnotationsValidationPlugin убраны из фреймворка,
            // валидация data-annotations теперь отключена по умолчанию сама — раньше здесь был
            // вызов DisableAvaloniaDataAnnotationValidation(), которая руками убирала плагин,
            // чтобы не дублировать валидацию с CommunityToolkit.Mvvm. Поведение то же, метод не нужен.

            // DataContext НЕ задаём здесь: MainWindow сам ставит DataContext = DataManager.DataSave
            // в конструкторе (InitializeClasses). Инициализатор объекта выполняется ПОСЛЕ
            // конструктора и затирал его пустой заглушкой MainWindowViewModel -> все [Binding] ошибки.
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
