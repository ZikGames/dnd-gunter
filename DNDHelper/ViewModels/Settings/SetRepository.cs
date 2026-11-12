using Avalonia.Controls;
using Avalonia.Media;
using DNDHelper.ViewModels.Character;
using DNDHelper.ViewModels.Config;
using DNDHelper.ViewModels.Inventory;
using DNDHelper.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace DNDHelper.ViewModels.Settings
{
    internal class SetRepository
    {
        // Имя поля "settings" намеренно с маленькой буквы и через полный Views.Settings —
        // в этом же namespace (DNDHelper.ViewModels.Settings) уже есть класс Settings
        // (цвета темы), без квалификатора компилятор резолвил бы не то.
        Views.Settings settings = Views.Settings.Instance;
        public static string SelectedRepository
        {
            get => DataManager.DataSave.SelectedRepository;
            set
            {
                DataManager.DataSave.SelectedRepository = value;
            }
        }

        public static List<Repository> repositories = new()
        {
            new Repository { Name = "Санекхуек", Link = "https://raw.githubusercontent.com/Chucapabra/dnd/refs/heads/Settings"  },
            new Repository { Name = "Гунер", Link = "https://raw.githubusercontent.com/GunterSuperPenguin/dnd-configs-gunter/refs/heads/main"  },
            new Repository { Name = "Жирный", Link = "https://raw.githubusercontent.com/Chucapabra/dnd/refs/heads/wh40k", Height = 60, FontSize = 25, FontWeight = FontWeight.Bold  }
        };

        public SetRepository()
        {
            settings.ListProfiles.SelectionChanged += SelectedRepository_Click;

            foreach (Repository repository in repositories)
                settings.ListProfiles.Items.Add(repository.Name);
        }

        public static void UpdateListBox()
        {
            for (int i = 0; i < Views.Settings.Instance.ListProfiles.Items.Count; i++)
            {
                // WPF ItemContainerGenerator.ContainerFromIndex(i) -> в Avalonia у ItemsControl
                // (и, соответственно, у ListBox) есть свой метод ContainerFromIndex(int) напрямую.
                var container = Views.Settings.Instance.ListProfiles.ContainerFromIndex(i) as ListBoxItem;
                if (container != null)
                {
                    container.Height = repositories[i].Height;
                    container.FontSize = repositories[i].FontSize;
                    container.FontWeight = repositories[i].FontWeight;
                    if (container.Content.ToString() == SelectedRepository)
                    {
                        container.Foreground = Brushes.Green;
                    }
                    else if (container != null)
                        container.Foreground = new SolidColorBrush(Settings.SelectedTheme[1]);
                }
            }
        }

        private void SelectedRepository_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (settings.ListProfiles.SelectedIndex != -1)
            {
                SelectedRepository = repositories[settings.ListProfiles.SelectedIndex].Name;
                for (int i = 0; i < settings.ListProfiles.Items.Count; i++)
                {
                    var container = settings.ListProfiles.ContainerFromIndex(i) as ListBoxItem;
                    if (container != null)
                        if (container.Content.ToString() == SelectedRepository)
                        {
                            container.Foreground = Brushes.Green;
                            DataManager.Load(DataManager.SelectedSave);
                        }
                        else if (container != null)
                            container.Foreground = new SolidColorBrush(Settings.SelectedTheme[1]);
                }

            }
        }

        public static void UpdateRepository()
        {
            MagicSpells.MagicSpells.RepositoryLoad();
            Race.Update();
            PlayerClass.Update();
            ManagerUrls.LoadUrls();
            Effects.Update();
            GlobalMultiply.Update();
            TypeArmorBaffs.SetArmor(MainWindow.Instance.armor_type_cb.SelectedIndex);
            DamageCalculator.Update();
        }



        private static string FileSpells = "";
        private static string FileRace = "";
        private static string FileClass = "";
        private static string FileTypeDamage = "";
        private static string FileMultiplyGlobal = "";
        private static string FileGlobalUrls = "";
        private static string FileEffects = "";
        public static async Task FileСonnection(bool check = false)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;


            int index = repositories.FindIndex(x => { return x.Name == DataManager.DataSave.SelectedRepository; });
            string Repos = repositories[index].Link;


            string castFileUrl = $"{Repos}/Casts";
            string raceFileUrl = $"{Repos}/Race.json";
            string classFileUrl = $"{Repos}/Class.json";
            string typeDamageFileUrl = $"{Repos}/TypesDamage.json";
            string globalMultiplyUrl = $"{Repos}/GlobalMultiply.json";
            string globalUrls = $"{Repos}/urls.json";
            string effectsUrl = $"{Repos}/Effects.json";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    async Task<string> SafeGetAsync(string url)
                    {
                        try
                        {
                            return await client.GetStringAsync(url);
                        }
                        catch (HttpRequestException)
                        {
                            return "";
                        }
                    }

                    var tasks = new List<Task<string>>
                    {
                        SafeGetAsync(castFileUrl),
                        SafeGetAsync(raceFileUrl),
                        SafeGetAsync(classFileUrl),
                        SafeGetAsync(globalMultiplyUrl),
                        SafeGetAsync(typeDamageFileUrl),
                        SafeGetAsync(globalUrls),
                        SafeGetAsync(effectsUrl)
                    };

                    await Task.WhenAll(tasks);

                    FileSpells = tasks[0].Result;
                    FileRace = tasks[1].Result;
                    FileClass = tasks[2].Result;
                    FileMultiplyGlobal = tasks[3].Result;
                    FileTypeDamage = tasks[4].Result;
                    FileGlobalUrls = tasks[5].Result;
                    FileEffects = tasks[6].Result;
                    CheckCacheFiles(check);
                }
                catch (HttpRequestException e)
                {
                    var box = MessageBoxManager.GetMessageBoxStandard(
                        "Ошибка", $"Ошибка соединения: {e}", ButtonEnum.Ok, Icon.Error);
                    await box.ShowAsync();
                }
            }
        }



        public static void CheckCacheFiles(bool check = false)
        {
            // Кроссплатформенно вместо ручного "Cache\\..." (шпаргалка п.16).
            string path = Path.Combine(MainWindow.PathData, "Cache", SelectedRepository) + Path.DirectorySeparatorChar;

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var spell_ = CheckCacheFile("Spells.txt", FileSpells, path);
            var race_ = CheckCacheFile("Race.json", FileRace, path);
            var class_ = CheckCacheFile("Class.json", FileClass, path);
            var typeDamage_ = CheckCacheFile("TypeDamage.json", FileTypeDamage, path);
            var multiplyGlobal_ = CheckCacheFile("MultiplyGlobal.json", FileMultiplyGlobal, path);
            var globalUrls_ = CheckCacheFile("urls.json", FileGlobalUrls, path);
            var effects_ = CheckCacheFile("Effects.json", FileEffects, path);
            if (spell_ || race_ || class_ || typeDamage_ || multiplyGlobal_ || globalUrls_ || effects_)
            {
                DataManager.Load(DataManager.SelectedSave);
                MessageBoxManager.GetMessageBoxStandard("", "Конфиг обновлён.").ShowAsync();
            }
            else if (check)
            {
                MessageBoxManager.GetMessageBoxStandard("", "У вас актуальная версия конфига.").ShowAsync();
            }


        }

        public static bool CheckCacheFile(string name, string content, string path)
        {
            string pathFile = Path.Combine(path, name);
            if (content.Length > 3)
            {
                if (!File.Exists(pathFile))
                {
                    var file = File.Create(pathFile);
                    file.Close();
                }

                if (File.ReadAllText(pathFile) != content)
                {
                    File.WriteAllText(pathFile, content);
                    return true;
                }
            }
            else if (File.Exists(pathFile))
                File.Delete(pathFile);

            return false;
        }
    }

    public class Repository
    {
        public string Name { get; set; }

        public string Link { get; set; }

        public IBrush Foreground { get; set; }

        public int Height { get; set; } = 45;

        public int FontSize { get; set; } = 20;

        public FontWeight FontWeight { get; set; } = FontWeight.Normal;

    }
}
