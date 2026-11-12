using Avalonia.Controls;
using DNDHelper.Views;
using System;

namespace DNDHelper.ViewModels.Diary
{
    // Размер шрифта дневника: как в WPF-версии - применяется к выделению или к тексту, который
    // будет набран у каретки. Само форматирование хранит DiaryFormatter (см. RichText.cs).
    // Подписка на комбобокс размера - в DiaryManager (один раз), здесь только показ текущего размера.
    public class TextChanges
    {
        MainWindow main = MainWindow.Instance;
        public static bool isUpdatingFontSize = false;

        public void UpdateFontSizeDisplay()
        {
            var formatter = DiaryManager.Formatter;
            if (isUpdatingFontSize || main.diaryTB == null || formatter == null) return;

            isUpdatingFontSize = true;

            try
            {
                double size = formatter.CurrentSize;
                object? match = null;
                foreach (var item in main.fontSizeComboBox.Items)
                {
                    if (item is double d && Math.Abs(d - size) < 0.01)
                    {
                        match = item;
                        break;
                    }
                }
                main.fontSizeComboBox.SelectedItem = match;
            }
            finally
            {
                isUpdatingFontSize = false;
            }
        }

        public void ApplyFontSize(double fontSize)
        {
            DiaryManager.Formatter?.ApplySize(fontSize);
        }

    }
}
