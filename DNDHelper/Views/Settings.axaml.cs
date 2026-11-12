using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using DNDHelper.ViewModels.Inventory;
using DNDHelper.ViewModels.MagicSpells;
using DNDHelper.ViewModels.Settings;
using DarkThem = DNDHelper.ViewModels.Settings.Settings;

using DNDHelper.ViewModels.Infrastructure;

namespace DNDHelper.Views
{
    /// <summary>
    /// Логика взаимодействия для Settings.axaml
    /// </summary>
    public partial class Settings : Window
    {
        public static Settings Instance { get; set; }

        public Settings()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
            Instance = this;

            SetRepository setRepository = new SetRepository();

            Loaded += Settings_Loaded;
        }

        private void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            SetRepository.UpdateListBox();
        }

        private void ComboBoxTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;

            if (comboBox != null)
            {
                int selectionIndex = comboBox.SelectedIndex;
                object selectionItem = ComboBoxTheme.SelectedItem;

                // WPF MaterialDesignThemes (PaletteHelper/Theme/BaseTheme) заменены на
                // встроенный в Avalonia механизм ThemeVariant, с которым Material.Avalonia
                // (уже подключён в App.axaml) работает "из коробки".
                switch (selectionIndex)
                {
                    case 0:
                        DarkThem.IsDarkTheme = false;
                        DarkThem.SelectedTheme = DarkThem.LightTheme;
                        Application.Current.RequestedThemeVariant = ThemeVariant.Light;
                        break;
                    case 1:
                        DarkThem.IsDarkTheme = true;
                        DarkThem.SelectedTheme = DarkThem.DarkTheme;
                        Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
                        break;
                }
                Views.MainWindow.Instance.SetTheme(DarkThem.SelectedTheme[0], DarkThem.SelectedTheme[1]);
                MagicSpells.RepositoryLoad();
                SetRepository.UpdateListBox();

                // Обновление шрифта в инвенторе
                foreach (var item in InventoryLoot.InventoryItems)
                {
                    item.CountWeight = item.CountWeight;
                    item.CountKD = item.CountKD;
                }

            }
        }


        private void AddProfile_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
