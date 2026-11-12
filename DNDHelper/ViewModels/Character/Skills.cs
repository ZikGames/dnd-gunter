using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DNDHelper.ViewModels.Config;
using DNDHelper.ViewModels.Settings;
using DNDHelper.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DNDHelper.ViewModels.Character
{
    public class Skills
    {
        MainWindow main = MainWindow.Instance;

        public static ObservableCollection<Skill> ListSkills { get; set; } = new();
        public static ObservableCollection<Skill> CustomSkills => DataManager.DataSave.CustomSkills;

        public Skills()
        {
            main.DataGridChartherCharacterAbilities.ItemsSource = ListSkills;
            main.DataGridChartherCharacterAbilities.BeginningEdit += DataGridChartherCharacterAbilities_BeginningEdit;

            // WPF PreviewMouseLeftButtonDown - туннелируемое (Preview) событие. В Avalonia
            // обычная подписка "+=" на PointerPressed даёт только bubble-фазу; чтобы получить
            // тот же порядок вызова (раньше, чем сам DataGrid обработает клик), подписываемся
            // через AddHandler с RoutingStrategies.Tunnel.
            main.DataGridChartherCharacterAbilities.AddHandler(
                InputElement.PointerPressedEvent,
                DataGridChartherCharacterAbilities_PreviewPointerPressed,
                RoutingStrategies.Tunnel);

            main.DataGridChartherCharacterAbilities.SelectionChanged += DataGridChartherCharacterAbilities_SelectionChanged;
            main.DataGridChartherCharacterAbilities.ContextRequested += DataGridChartherCharacterAbilities_ContextRequested;
            main.Ability_TextBox.TextChanged += Ability_TextBox_TextChanged;
            main.Ability_TextBox.TextInput += Ability_TextBox_PreviewTextInput;
            main.AddMenuSkill.Click += AddMenuSkill_Click;
            main.DeleteMenuSkill.Click += DeleteMenuSkill_Click;

            CustomSkills.CollectionChanged += CustomSkills_CollectionChanged;
        }

        private void CustomSkills_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            DataManager.Save();
        }

        private void Ability_TextBox_PreviewTextInput(object sender, TextInputEventArgs e)
        {
            if (e.Text.Contains('|'))
                e.Handled = true;
        }

        private void Ability_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int selectedIndex = main.DataGridChartherCharacterAbilities.SelectedIndex;
            if (selectedIndex != -1 && ListSkills[selectedIndex].Origin == "Своя")
            {
                ListSkills[selectedIndex].Description = main.Ability_TextBox.Text;
                SaveCustomSkills();
            }
        }

        private void DeleteMenuSkill_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = main.DataGridChartherCharacterAbilities.SelectedIndex;
            if (selectedIndex != -1 && ListSkills[selectedIndex].Origin == "Своя")
            {
                ListSkills.RemoveAt(selectedIndex);
                SaveCustomSkills();
            }
        }

        private void AddMenuSkill_Click(object sender, RoutedEventArgs e)
        {
            ListSkills.Add(new());
            SaveCustomSkills();
        }

        private void DataGridChartherCharacterAbilities_ContextRequested(object sender, ContextRequestedEventArgs e)
        {
            int selectedIndex = main.DataGridChartherCharacterAbilities.SelectedIndex;
        }

        private void DataGridChartherCharacterAbilities_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            main.Ability_TextBox.IsReadOnly = true;
            int selectedIndex = main.DataGridChartherCharacterAbilities.SelectedIndex;
            if (selectedIndex != -1)
            {
                main.Ability_TextBox.Text = ListSkills[selectedIndex].Description;
                if (ListSkills[selectedIndex].Origin == "Своя")
                    main.Ability_TextBox.IsReadOnly = false;
            }
            else
                main.Ability_TextBox.Text = "Выберите способность";
        }

        private void DataGridChartherCharacterAbilities_PreviewPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(main.DataGridChartherCharacterAbilities).Properties.IsLeftButtonPressed)
                return;

            // Подтверждено при сборке: у Avalonia.Controls.DataGridCell нет свойства IsEditing
            // (класс наследуется от ContentControl, WPF-аналога тут не существует) —
            // условие упрощено, проверяем только что клик пришёлся по реальной ячейке.
            var cell = FindParent<DataGridCell>(e.Source as Visual);

            if (cell != null)
            {
                if (e.ClickCount == 1)
                {
                    main.DataGridChartherCharacterAbilities.CommitEdit();
                    main.DataGridChartherCharacterAbilities.SelectedItem = cell.DataContext;
                    e.Handled = true;
                }
            }
            else if (cell == null)
            {
                main.DataGridChartherCharacterAbilities.CommitEdit();
                main.DataGridChartherCharacterAbilities.SelectedIndex = -1;
            }
        }

        private static T FindParent<T>(Visual child) where T : Visual
        {
            while (child != null && !(child is T))
            {
                child = child.GetVisualParent();
            }
            return child as T;
        }

        private void DataGridChartherCharacterAbilities_BeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
        {
            var skill = e.Row.DataContext as Skill;
            if (e.Column.DisplayIndex == 1 || skill.Origin != "Своя")
                e.Cancel = true;
        }

        private static void SaveCustomSkills()
        {
            CustomSkills.Clear();
            foreach (var skill in ListSkills)
                if (skill.Origin == "Своя")
                    CustomSkills.Add(skill);
        }


        public static void ReloadDataGridSkills()
        {
            ListSkills.Clear();
            // В Avalonia SelectedIndex может быть != -1, пока SelectedValue ещё null (выбор
            // "отложен", пока ItemsSource не материализуется после смены репозитория/загрузки).
            // В WPF это было синхронно. Поэтому ориентируемся на SelectedValue, как и UpdateRace().
            if (MainWindow.Instance.character_race_combobox.SelectedValue != null)
            {
                var RaceSkills = Race.SelectedClassData?.Skills ?? new List<string>();
                var NameRace = MainWindow.Instance.character_race_combobox.SelectedValue.ToString();

                foreach (var skill in RaceSkills)
                {
                    string[] sk = skill.Split('|');
                    ListSkills.Add(new Skill { Name = sk[0], Origin = NameRace, Description = sk.Length > 1 ? sk[1] : "" });
                }
            }

            if (MainWindow.Instance.character_class_combobox.SelectedValue != null)
            {
                var NameClass = MainWindow.Instance.character_class_combobox.SelectedValue.ToString();
                var ClassSkills = PlayerClass.SelectedClassData?.Skills ?? new List<string>();
                for (int i = 0; i < ClassSkills.Count; i++)
                    ClassSkills[i] = $"{ClassSkills[i]}|{NameClass}";
                var ClassTreesSkills = TreeSkills.Skills;
                ClassTreesSkills.Reverse();
                // объединение скиллов
                foreach (var skill in ClassSkills)
                    ClassTreesSkills.Add(skill);

                List<string> endListSkills = new List<string>();
                foreach (var skill in ClassTreesSkills)
                {
                    int index = endListSkills.ToList().FindIndex(x =>
                    {
                        var X = x.Split('|');
                        var S = skill.Split('|');
                        return (X[0] == S[0]) && (S[2] == X[2]);
                    });
                    if (index == -1)
                        endListSkills.Add(skill);
                }
                endListSkills.Reverse();
                foreach (var skill in endListSkills)
                {
                    int index = ListSkills.ToList().FindIndex(x => { return x.Name == skill.Split('|')[0]; });
                    string[] sk = skill.Split('|');
                    if (index == -1)
                        ListSkills.Add(new Skill { Name = sk[0], Origin = sk[2], Description = $"  {sk[1]}" });
                    else
                        ListSkills[index].Description += $"\r\n({sk[2].Trim(' ')}):\r\n  {sk[1]}";
                }

            }





            foreach (var skill in CustomSkills)
            {
                ListSkills.Add(new Skill { Name = skill.Name, Origin = skill.Origin, Description = skill.Description });
            }
        }

        public class Skill
        {
            public string Name { get; set; } = "Способность";

            public string Origin { get; set; } = "Своя";

            public string Description { get; set; } = "Описание";
        }
    }
}
