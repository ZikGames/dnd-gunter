using DNDHelper.ViewModels.Settings;
using DNDHelper.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DNDHelper.ViewModels.Diary
{
    // Дневник: заметки хранятся в .rtf (как в WPF-версии), редактор - AvaloniaEdit с собственным
    // слоем форматирования (см. RichText.cs). Заметки .txt от предыдущей версии порта читаются и
    // при первом сохранении превращаются в .rtf.
    internal class DiaryManager
    {
        public static List<Note> Notes = new();
        public static DiaryFormatter? Formatter { get; private set; }
        MainWindow main = MainWindow.Instance;
        int SelectedIndex = -1;


        public DiaryManager()
        {
            main.ListBoxNotes.SelectionChanged += ListBoxNotes_SelectionChanged;
            main.diaryTB.TextChanged += DiaryTB_TextChanged;
            main.CreateNote.Click += CreateNote_Click;
            main.DeleteMenuNote.Click += DeleteMenuNote_Click;

            var formatter = new DiaryFormatter(main.diaryTB);
            Formatter = formatter;
            formatter.Modified += SaveCurrentNote;

            main.diaryUndoBtn.Click += (_, _) => formatter.Undo();
            main.diaryRedoBtn.Click += (_, _) => formatter.Redo();
            main.diaryBoldBtn.Click += (_, _) => formatter.ToggleBold();
            main.diaryItalicBtn.Click += (_, _) => formatter.ToggleItalic();
            main.diaryUnderlineBtn.Click += (_, _) => formatter.ToggleUnderline();
            main.diaryStrikeBtn.Click += (_, _) => formatter.ToggleStrike();
            main.fontSizeComboBox.SelectionChanged += FontSizeComboBox_SelectionChanged;
        }

        private void FontSizeComboBox_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
        {
            // TextChanges.UpdateFontSizeDisplay() сам меняет выбор в комбобоксе - это не действие пользователя
            if (TextChanges.isUpdatingFontSize) return;
            if (main.fontSizeComboBox.SelectedItem is double size)
                Formatter?.ApplySize(size);
        }

        private async void DeleteMenuNote_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (SelectedIndex != -1)
            {
                string path = Notes[SelectedIndex].Path;
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Удаление windows", $"Вы хотите удалить: {Notes[SelectedIndex].Name}",
                    ButtonEnum.YesNo, Icon.Warning);
                var result = await box.ShowAsync();
                if (result == ButtonResult.Yes)
                {
                    File.Delete(path);
                    LoadNotes();
                }
            }
        }

        private async void CreateNote_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var name = main.NameNote.Text;
            if (!string.IsNullOrEmpty(name)) // Avalonia: пустое поле = null
            {
                string pathNote = $"{DataManager.SelectedSave}/Notes/{name}.rtf";
                string pathLegacy = $"{DataManager.SelectedSave}/Notes/{name}.txt";
                if (File.Exists(pathNote) || File.Exists(pathLegacy))
                {
                    var box = MessageBoxManager.GetMessageBoxStandard(
                        "Эбл", "Название занято", ButtonEnum.Ok, Icon.Error);
                    await box.ShowAsync();
                    return;
                }

                File.WriteAllText(pathNote, RtfIO.Write("", Array.Empty<Fmt>()), Encoding.ASCII);
                LoadNotes();
            }
        }

        private static bool IsRtf(string path) => path.EndsWith(".rtf", StringComparison.OrdinalIgnoreCase);

        public static void LoadNotes()
        {
            MainWindow.Instance.ListBoxNotes.Items.Clear();
            Notes.Clear();

            string pathNotes = $"{DataManager.SelectedSave}/Notes";

            if (!Directory.Exists(pathNotes))
                Directory.CreateDirectory(pathNotes);

            var files = Directory.GetFiles(pathNotes)
                .Where(p => IsRtf(p) || p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // если есть и note.rtf, и note.txt - показываем один раз (приоритет у .rtf)
            var rtfNames = new HashSet<string>(
                files.Where(p => IsRtf(p)).Select(p => Path.GetFileNameWithoutExtension(p)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var notePath in files)
            {
                var noteName = Path.GetFileNameWithoutExtension(notePath);
                if (!IsRtf(notePath) && rtfNames.Contains(noteName))
                    continue;

                MainWindow.Instance.ListBoxNotes.Items.Add(noteName);
                Notes.Add(new Note { Name = noteName, Path = notePath });
            }
        }

        private void LoadNote(string filePath)
        {
            try
            {
                if (IsRtf(filePath))
                {
                    var (text, formats) = RtfIO.Parse(File.ReadAllText(filePath, Encoding.Latin1));
                    Formatter?.SetContent(text, formats);
                }
                else
                {
                    Formatter?.SetContent(File.ReadAllText(filePath), Array.Empty<Fmt>());
                }
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Error", $"Error opening file: {ex.Message}", ButtonEnum.Ok, Icon.Error);
                box.ShowAsync();
            }
        }

        private void SaveCurrentNote()
        {
            if (Formatter == null || Formatter.IsLoading) return;
            if (SelectedIndex < 0 || SelectedIndex >= Notes.Count) return;

            try
            {
                Note note = Notes[SelectedIndex];
                string rtfPath = IsRtf(note.Path) ? note.Path : Path.ChangeExtension(note.Path, ".rtf");

                File.WriteAllText(rtfPath, Formatter.ToRtf(), Encoding.ASCII);

                // заметка из старой версии порта (.txt) переехала в .rtf
                if (!string.Equals(rtfPath, note.Path, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(note.Path);
                    note.Path = rtfPath;
                }
            }
            catch
            {

            }
        }

        private void ListBoxNotes_SelectionChanged(object sender, Avalonia.Controls.SelectionChangedEventArgs e)
        {
            SelectedIndex = MainWindow.Instance.ListBoxNotes.SelectedIndex;
            if (SelectedIndex != -1)
                LoadNote(Notes[SelectedIndex].Path);
        }

        private void DiaryTB_TextChanged(object sender, EventArgs e)
        {
            // при загрузке заметки Text меняется программно - сохранять нельзя, форматирование ещё не применено
            if (Formatter != null && Formatter.IsLoading) return;
            SaveCurrentNote();
        }
    }

    public class Note
    {
        public string Name { get; set; }

        public string Path { get; set; }
    }
}
