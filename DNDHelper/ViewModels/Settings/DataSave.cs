using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DNDHelper.ViewModels.Character;
using DNDHelper.ViewModels.Config;
using DNDHelper.ViewModels.Diary;
using DNDHelper.ViewModels.Inventory;
using DNDHelper.ViewModels.Сharacteristics;
using DNDHelper.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using static DNDHelper.ViewModels.Character.TreeSkills;
using static DNDHelper.ViewModels.Сharacteristics.CharacteristicTable;

namespace DNDHelper.ViewModels.Settings
{
    class SavesMenu
    {
        MainWindow main = MainWindow.Instance;

        private int SelectedIndex = -1;

        public SavesMenu()
        {
            main.AddCharacter.Click += AddCharacter_Click;
            main.ImageIconCharacter.PointerPressed += ImageIconCharacter_PointerPressed;
            main.ImageCharacter.PointerPressed += ImageCharacter_PointerPressed;
        }

        private async void ImageCharacter_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(main.ImageCharacter).Properties.IsLeftButtonPressed)
                return;
            await SaveImage();
        }

        private async Task SaveImage()
        {
            // WPF Microsoft.Win32.OpenFileDialog (синхронный) -> Avalonia IStorageProvider
            // (асинхронный, см. шпаргалку п.4).
            var topLevel = TopLevel.GetTopLevel(main);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите изображение персонажа",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Изображения") { Patterns = new[] { "*.png", "*.jpeg", "*.jpg" } }
                }
            });

            if (files.Count == 0)
                return;

            string sourcePath = files[0].Path.LocalPath;

            try
            {
                string fileExtension = Path.GetExtension(sourcePath).ToLower();
                var formats = new[] { ".png", ".jpg", ".jpeg" };
                foreach (var format in formats)
                {
                    var oldFile = Path.Combine(DataManager.SelectedSave, $"image{format}");
                    if (File.Exists(oldFile))
                        File.Delete(oldFile);
                }
                if (!Directory.Exists(DataManager.SelectedSave))
                    Directory.CreateDirectory(DataManager.SelectedSave);

                string destinationPath = Path.Combine(DataManager.SelectedSave, $"image{fileExtension}");
                File.Copy(sourcePath, destinationPath, true);

                byte[] imageBytes = File.ReadAllBytes(destinationPath);
                using (MemoryStream ms = new(imageBytes))
                {
                    // WPF BitmapImage (BeginInit/CacheOption/StreamSource/EndInit/Freeze) ->
                    // Avalonia Bitmap конструируется прямо из потока, без всей этой обвязки.
                    var bitmap = new Bitmap(ms);
                    main.ImageCharacter.Source = bitmap;
                }

                main.ImageIconCharacter.IsVisible = false;
                main.ImageCharacter.IsVisible = true;

            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Ошибка загрузки изображения: " + ex.Message);
                await box.ShowAsync();
            }
        }
        private async void ImageIconCharacter_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(main.ImageIconCharacter).Properties.IsLeftButtonPressed)
                return;
            await SaveImage();
        }

        private void AddCharacter_Click(object sender, RoutedEventArgs e)
        {
            DataManager.Create();
        }

    }

    public static class DataManager
    {
        private static string pathSaves = "Saves/";

        public static string SelectedSave = "";

        public static DataSaveEmpty DataSave = new DataSaveEmpty();

        public static void FindPathSaves()
        {
            // Раньше: два уровня вверх от exe (см. историю правок) — ломалось под Nix,
            // т.к. /nix/store read-only и путь меняется при каждой пересборке (см. AppPaths.cs).
            pathSaves = Path.Combine(AppPaths.GetWritableRoot(), "Saves") + Path.DirectorySeparatorChar;
        }
        public static void WorkingProgram(bool visibilityBool)
        {
            bool visible = visibilityBool;
			MainWindow.Instance.Magic_menuitem.IsVisible = visible;
			MainWindow.Instance.WorkProgam1.IsVisible = visible;
            MainWindow.Instance.WorkProgam2.IsVisible = visible;
            MainWindow.Instance.WorkProgam3.IsVisible = visible;
            MainWindow.Instance.WorkProgam4.IsVisible = visible;
            MainWindow.Instance.WorkProgam5.IsVisible = visible;
            MainWindow.Instance.WorkProgam6.IsVisible = visible;
            MainWindow.Instance.WorkProgam7.IsVisible = visible;
            MainWindow.Instance.сharacter_weight_textblock.IsVisible = visible;
            MainWindow.Instance.DataGridCharacterisctics.IsVisible = visible;
            // NOTE: в оригинале WorkProgam8 использовал Visibility.Hidden вместо Collapsed
            // при visibilityBool==false (единственный из всей группы) - место в layout
            // сохранялось. В Avalonia такого состояния нет из коробки (см. шпаргалку п.2),
            // так что тут просто IsVisible=true/false как и у остальных; если WorkProgam8
            // визуально "прыгает" в раскладке - см. .axaml для этого элемента.
            if (visibilityBool)
                MainWindow.Instance.WorkProgam8.IsVisible = false;
            else
                MainWindow.Instance.WorkProgam8.IsVisible = true;
        }


        public static void ReadSaves()
        {
			if (!Directory.Exists(pathSaves))
                Directory.CreateDirectory(pathSaves);
            string[] allfolders = Directory.GetDirectories(pathSaves);
            List<string> savefolders = new();
            foreach (string folder in allfolders)
            {
                var folders = folder.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (folders[folders.Length - 1].Contains("Save") && !folders[folders.Length - 1].Contains('.'))
                    savefolders.Add(folder);
            }


            MainWindow.Instance.CharactersMenu.Items.Clear();

            foreach (string folder in savefolders)
            {
                var json = File.ReadAllText($"{folder}/Config.json");
                if (json != null)
                {
                    try
                    {
                        var deleteItem = new MenuItem { Header = "Удалить", Tag = folder };
                        deleteItem.Click += DeleteItem_Click;
                        var deleteContextMenu = new ContextMenu();
                        deleteContextMenu.Items.Add(deleteItem);


                        var dataSave = JsonSerializer.Deserialize<DataSaveEmpty>(json);
                        var newMenuItem = new MenuItem { Header = dataSave.Name, Tag = folder };
                        newMenuItem.ContextMenu = deleteContextMenu;
                        newMenuItem.Click += Load_Click;
                        MainWindow.Instance.CharactersMenu.Items.Add(newMenuItem);
                    }
                    catch (Exception ex)
                    {
                        // Диалог про битое сохранение не блокирует ReadSaves() синхронно (в
                        // Avalonia MessageBox нельзя дождаться синхронно, см. шпаргалку п.3) -
                        // запускается отдельно, ReadSaves() продолжает обрабатывать остальные папки.
                        HandleCorruptedSave(folder);
                    }
				}
            }

        }

        private static async void HandleCorruptedSave(string folder)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Лошара",
                $"У тебя наебнулось сохранения\r\nПуть: {folder}\r\nУДАЛИТЬ?УДАЛИТЬ?УДАЛИТЬ?УДАЛИТЬ?УДАЛИТЬ?УДАЛИТЬ?",
                ButtonEnum.YesNo, Icon.Error);
            var result = await box.ShowAsync();
            if (result == ButtonResult.Yes)
                Directory.Delete(folder, true);
        }


        public static void Create()
        {
            string[] deleteChars = { ".", ":", " " };
            string dateTime = DateTime.Now.ToString();
            foreach (string c in deleteChars)
                dateTime = dateTime.Replace(c, "");
            string newDirectory = $"{pathSaves}Save{dateTime}";
            string newFile = $"{newDirectory}/Config.json";

            Directory.CreateDirectory(newDirectory);
            Directory.CreateDirectory($"{newDirectory}/Notes");
            var file = File.Create(newFile);
            file.Close();



            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var newDataSave = new DataSaveEmpty();
            var json = JsonSerializer.Serialize(newDataSave, options);
            File.WriteAllText(newFile, json);

            ReadSaves();
            Load(newDirectory);
		}

        private static async void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItemDelete = sender as MenuItem;
            var path = menuItemDelete.Tag.ToString();
            await Delete(path);
        }

        public static async Task Delete(string path)
        {
            if (path != SelectedSave)
            {
                var json = File.ReadAllText($"{path}/Config.json");
                var dataSave = JsonSerializer.Deserialize<DataSaveEmpty>(json);
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Удаление персонажа", $"Вы уверены, что хотите удалить персонажа {dataSave.Name}",
                    ButtonEnum.YesNo, Icon.Warning);
                var result = await box.ShowAsync();
                if (result == ButtonResult.Yes)
                {
                    Directory.Delete(path, true);
                    ReadSaves();
                }
            }
            else
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Тыеб", "Это сохранение открыто", ButtonEnum.Ok, Icon.Warning);
                await box.ShowAsync();
            }
        }

        public static bool IsLoad = true;




        private static readonly SmartDebouncer _debouncer = new();
        public static void Save(bool Force = false)
        {
            if (SelectedSave.Length == 0 || IsLoad)
                return;

            if (!Force)
                _debouncer.ExecuteDebounced(async () =>
                {
                    // Этот делегат выполняется в Task.Run (пул потоков). save() только
                    // сериализует данные в файл, а ReadSaves() пересоздаёт пункты меню
                    // CharactersMenu - UI-объекты Avalonia нельзя создавать и менять не из
                    // UI-потока (иначе "The calling thread cannot access this object...").
                    save();
                    await Dispatcher.UIThread.InvokeAsync(ReadSaves);
                });
            else
                ForceSaveDataAsync();
        }

        private static void ForceSaveDataAsync()
        {
            _debouncer.Reset();
            save();
        }

        private static void save()
        {
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            Debug.WriteLine($"{SelectedSave}/Config.json");
            var json = JsonSerializer.Serialize(DataSave, options);

            File.WriteAllText($"{SelectedSave}/Config.json", json);
        }
        private static void Load_Click(object sender, RoutedEventArgs e)
        {
            var clickedItem = sender as MenuItem;
            Load(clickedItem.Tag.ToString());
        }

        public static void Load(string path)
        {
            if (SelectedSave.Length > 0) Save(true);


            IsLoad = true;

            SelectedSave = path;

            var json = File.ReadAllText($"{SelectedSave}/Config.json");

            var dataSave = JsonSerializer.Deserialize<DataSaveEmpty>(json);


            DataSave.SelectedRepository = dataSave.SelectedRepository;
            SetRepository.UpdateRepository();

            if (DataSave.SelectedRepository == SetRepository.repositories[2].Name)
                GridCharacteristics.DataGridChar = GridCharacteristics.DataGridCharNew;
            else
                GridCharacteristics.DataGridChar = GridCharacteristics.DataGridCharOld;
            MainWindow.Instance.DataGridCharacterisctics.ItemsSource = GridCharacteristics.DataGridChar;


            WorkingProgram(true);

            DataSave.Name = dataSave.Name;
            DataSave.SelectedRace = dataSave.SelectedRace;
            DataSave.SelectedClass = dataSave.SelectedClass;
            DataSave.AddWeight = dataSave.AddWeight;
            DataSave.Level = dataSave.Level;
            DataSave.Damage = dataSave.Damage;
            DataSave.DamageMagicShield = dataSave.DamageMagicShield;
            DataSave.SelectedTypeMagicShield = dataSave.SelectedTypeMagicShield;
            DataSave.SelectedQualityArmor = dataSave.SelectedQualityArmor;
            DataSave.MaxMagicShield = dataSave.MaxMagicShield;
            DataSave.Copper = dataSave.Copper;
            DataSave.Silver = dataSave.Silver;
            DataSave.Gold = dataSave.Gold;

            int[] characterisitics = new int[30];
            for ( int i=0; i <= 29; i++)
            {
                if (dataSave.Characterisitics.Length-1 > i)
                    characterisitics[i] = dataSave.Characterisitics[i];
                else
                    characterisitics[i] = 3;
            }

            DataSave.Characterisitics = characterisitics;
            DataSave.SubtractMagicBullet = dataSave.SubtractMagicBullet;
            DataSave.MultiplyMagicDamage = dataSave.MultiplyMagicDamage;
            DataSave.CatalyzerSelect = dataSave.CatalyzerSelect;
            DataSave.CatalyzerQualitySelect = dataSave.CatalyzerQualitySelect;
            DataSave.MagicSchool = dataSave.MagicSchool;



            DataSave.ClassTreeGrid.Clear();
            foreach (var item in dataSave.ClassTreeGrid)
                DataSave.ClassTreeGrid.Add(item);

            DataSave.Inventory.Clear();
            foreach (var item in dataSave.Inventory)
            {
                if (item.Weight != 0)
                    item.Weight /= item.Count;
                DataSave.Inventory.Add(item);
            }
            DataSave.SelectedBackpack = dataSave.SelectedBackpack;
            DataSave.BackpackQuantity = dataSave.BackpackQuantity;

            DataSave.CustomSkills.Clear();
            foreach (var item in dataSave.CustomSkills)
                DataSave.CustomSkills.Add(item);

            DataSave.CurrentCastsNames.Clear();
            foreach (var item in dataSave.CurrentCastsNames)
                DataSave.CurrentCastsNames.Add(item);

            DataSave.EffectTables.Clear();
            foreach (var item in dataSave.EffectTables)
            {
                EffectTable effect = new();
                effect.SelectedEffect = item.SelectedEffect;
                effect.Level = item.Level;
                effect.LevelString = item.LevelString;
                effect.ToolTip = item.ToolTip;
                DataSave.EffectTables.Add(effect);
            }

            Character.Skills.ReloadDataGridSkills();
            Level.SetLevel();
            MainWindow.ItemBaffsListScript.UpdateValues();
            DiaryManager.LoadNotes();
            LoadImage();
            // WPF diaryTB.Document.Blocks.Clear() (FlowDocument) -> diaryTB теперь
            // AvaloniaEdit.TextEditor, у него просто Text.
            MainWindow.Instance.diaryTB.Text = "";
            SetRepository.FileСonnection();

            IsLoad = false;

            PlayerClass.UpdateCharacteristicMagic();
            WeightScript.CountWeightItems();
            Effects.UpdateLevelEffect();
        }

        private static void LoadImage()
        {
            var formats = new[] { ".png", ".jpg", ".jpeg" };
            string imagePath = null;

            foreach (var format in formats)
            {
                var currentPath = Path.Combine(SelectedSave, $"image{format}");
                if (File.Exists(currentPath))
                {
                    imagePath = currentPath;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    byte[] imageBytes = File.ReadAllBytes(imagePath);
                    using (MemoryStream ms = new(imageBytes))
                    {
                        var bitmap = new Bitmap(ms);
                        MainWindow.Instance.ImageCharacter.Source = bitmap;
                    }

                    MainWindow.Instance.ImageIconCharacter.IsVisible = false;
                    MainWindow.Instance.ImageCharacter.IsVisible = true;
                }
                catch (Exception ex)
                {
                    ShowImageLoadError(ex.Message);
                }
            }
            else
            {
                MainWindow.Instance.ImageCharacter.Source = null;
                MainWindow.Instance.ImageIconCharacter.IsVisible = true;
                MainWindow.Instance.ImageCharacter.IsVisible = false;
            }
        }

        private static async void ShowImageLoadError(string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Ошибка загрузки изображения: " + message);
            await box.ShowAsync();
        }

    }

    public class DataSaveEmpty : INotifyPropertyChanged
    {

        private string _selectedRepository = "";
        public string SelectedRepository
        {
            get => _selectedRepository;
            set
            {
                _selectedRepository = value;
                OnPropertyChanged();
            }
        }

        private string _name = "Имя";
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private int _selectedClass = 0;
        public int SelectedClass
        {
            get => _selectedClass;
            set
            {
                _selectedClass = value;
                OnPropertyChanged();
            }
        }

        private int _selectedRace = 0;
        public int SelectedRace
        {
            get => _selectedRace;
            set
            {
                _selectedRace = value;
                OnPropertyChanged();
            }
        }


        private int _selectedBackpack = 0;
        public int SelectedBackpack
        {
            get => _selectedBackpack;
            set
            {
                _selectedBackpack = value;
                OnPropertyChanged();
            }
        }

        private int _backpackQuantity = 0;
        public int BackpackQuantity
        {
            get => _backpackQuantity;
            set
            {
                _backpackQuantity = value;
                OnPropertyChanged();
            }
        }

        private string _addWeight = "0";
        public string AddWeight
        {
            get => _addWeight;
            set
            {
                _addWeight = value;
                OnPropertyChanged();
            }
        }

        private int _selectedQualityArmor = 0;
        public int SelectedQualityArmor
        {
            get => _selectedQualityArmor;
            set
            {
                _selectedQualityArmor = value;
                OnPropertyChanged();
            }
        }

        private string _copper = "0";
        public string Copper
        {
            get => _copper;
            set
            {
                _copper = value;
                OnPropertyChanged();
            }
        }

        private string _silver = "0";
        public string Silver
        {
            get => _silver;
            set
            {
                _silver = value;
                OnPropertyChanged();
            }
        }

        private string _gold = "0";
        public string Gold
        {
            get => _gold;
            set
            {
                _gold = value;
                OnPropertyChanged();
            }
        }

        private int _level { get; set; } = 1;
        public int Level
        {
            get => _level;
            set
            {
                _level = value;
                OnPropertyChanged();
            }
        }
        private int _damage = 0;
        public int Damage
        {
            get => _damage;
            set
            {
                _damage = value;
                OnPropertyChanged();
            }
        }

        private int _damageMagicShield = 0;
        public int DamageMagicShield
        {
            get => _damageMagicShield;
            set
            {
                _damageMagicShield = value;
                OnPropertyChanged();
            }
        }

        private int _selectedTypeMagicShield = 0;
        public int SelectedTypeMagicShield
        {
            get => _selectedTypeMagicShield;
            set
            {
                _selectedTypeMagicShield = value;
                Health.HealthUpdate();
                OnPropertyChanged();
            }
        }

        private string _maxMagicShield = "0";
        public string MaxMagicShield
        {
            get => _maxMagicShield;
            set
            {
                if (value.Length == 0)
                    _maxMagicShield = "0";
                else
                    _maxMagicShield = value;
                Health.HealthUpdate();
                OnPropertyChanged();
            }
        }
        private int _subtractMagicBullet = 0;
        public int SubtractMagicBullet
        {
            get => _subtractMagicBullet;
            set
            {
                _subtractMagicBullet = value;
                MagicSpells.MagicSpells.UpdateMagicBullet();
                OnPropertyChanged();
            }
        }

        private double _multiplyMagicDamage = 1;
        public double MultiplyMagicDamage
        {
            get => _multiplyMagicDamage;
            set
            {
                _multiplyMagicDamage = value;
                MainWindow.Instance.DamageMagic_textblock.Text = value.ToString();
                MainWindow.Instance.DamageMagic_textbox.Text = value.ToString();
                OnPropertyChanged();
            }
        }

        private int _magicSchool = 0;
        public int MagicSchool
        {
            get => _magicSchool;
            set
            {
                _magicSchool = value;
                OnPropertyChanged();
            }
        }

        private int _catalyzerSelect = 1;
        public int CatalyzerSelect
        {
            get => _catalyzerSelect;
            set
            {
                _catalyzerSelect = value;
                OnPropertyChanged();
            }
        }

        private int _catalyzerQualitySelect = 2;
        public int CatalyzerQualitySelect
        {
            get => _catalyzerQualitySelect;
            set
            {
                _catalyzerQualitySelect = value;
                OnPropertyChanged();
            }
        }


        public ObservableCollection<TreeGrid> ClassTreeGrid { get; set; } = new() {
            new TreeGrid { TreeName = "", TreeLevel = 0 },
            new TreeGrid { TreeName = "", TreeLevel = 0 },
            new TreeGrid { TreeName = "", TreeLevel = 0 }
            };

        public int[] Characterisitics { get; set; } = { 5, 5, 5, 3, 3, 3, 5, 5, 3, 3, 3, 3, 3, 3, 5, 3, 3, 3, 3, 3, 5, 3, 3, 3, 3, 3, 3, 3, 3, 3 };

        public ObservableCollection<Skills.Skill> CustomSkills { get; set; } = new();

        public ObservableCollection<InventoryLoot.InventoryItem> Inventory { get; set; } = new();

        public ObservableCollection<string> CurrentCastsNames { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<EffectTable> EffectTables { get; set; } = new();


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            DataManager.Save();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }



    public class SmartDebouncer
    {
        private DateTime _lastRequestTime = DateTime.MinValue;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Func<Task> _pendingAction = null;
        private readonly object _lock = new object();

        // Основной метод для отложенного выполнения
        public void ExecuteDebounced(Func<Task> action)
        {
            lock (_lock)
            {
                // Сохраняем действие
                _pendingAction = action;

                // Отменяем предыдущую отложенную задачу
                _cts.Cancel();
                _cts = new CancellationTokenSource();

                var now = DateTime.UtcNow;
                _lastRequestTime = now;

                var token = _cts.Token;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(2500, token);

                    if (!token.IsCancellationRequested)
                    {
                        await ExecutePendingAction();
                    }
                }, token);
            }
        }

        // Принудительное сохранение (немедленное выполнение)
        public async Task ForceSaveAsync()
        {
            Func<Task> actionToExecute = null;

            lock (_lock)
            {
                // Отменяем отложенную задачу
                _cts.Cancel();
                _cts = new CancellationTokenSource();

                // Берем текущее действие для выполнения
                actionToExecute = _pendingAction;
                _pendingAction = null;
            }

            if (actionToExecute != null)
            {
                await actionToExecute();
            }
        }

        // Сброс всех таймеров и ожиданий
        public void Reset()
        {
            lock (_lock)
            {
                _cts.Cancel();
                _cts = new CancellationTokenSource();
                _pendingAction = null;
                _lastRequestTime = DateTime.MinValue;
            }
        }

        // Проверка есть ли отложенные действия
        public bool HasPendingActions()
        {
            lock (_lock)
            {
                return _pendingAction != null;
            }
        }

        // Внутренний метод выполнения
        private async Task ExecutePendingAction()
        {
            Func<Task> actionToExecute = null;

            lock (_lock)
            {
                actionToExecute = _pendingAction;
                _pendingAction = null;
            }

            if (actionToExecute != null)
            {
                await actionToExecute();
            }
        }
    }

}
