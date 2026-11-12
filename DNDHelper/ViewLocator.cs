using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DNDHelper.ViewModels.Infrastructure;
using System;

namespace DNDHelper
{
    /// <summary>
    /// Резолвит View по ViewModel-у соглашением по имени: единственная замена
    /// подстроки "ViewModel" -> "View" в полном имени типа одновременно даёт нужный
    /// результат и для namespace, и для суффикса класса:
    ///   "DNDHelper.ViewModels.MainWindowViewModel" -> "DNDHelper.Views.MainWindowView"
    /// Т.е. класс окна/юзерконтрола должен называться "XxxView" (не просто "Xxx"),
    /// а лежать в namespace DNDHelper.Views. Это стандартное соглашение из шаблона
    /// Avalonia MVVM — держитесь его при создании новых View, чтобы не городить
    /// свой резолвер.
    /// </summary>
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? data)
        {
            if (data is null)
                return null;

            var name = data.GetType().FullName!.Replace("ViewModel", "View");
            var type = Type.GetType(name);

            if (type != null)
            {
                var control = (Control)Activator.CreateInstance(type)!;
                control.DataContext = data;
                return control;
            }

            return new TextBlock { Text = "Не найден View для: " + name };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
