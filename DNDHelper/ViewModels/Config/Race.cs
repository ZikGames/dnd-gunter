using DNDHelper.ViewModels.Character;
using DNDHelper.ViewModels.Inventory;
using DNDHelper.ViewModels.Settings;
using DNDHelper.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using static DNDHelper.ViewModels.Inventory.InventoryLoot;
using static DNDHelper.ViewModels.Сharacteristics.CharacteristicTable;

namespace DNDHelper.ViewModels.Config
{
    class Race
    {
        private static Dictionary<string, List<RaceData>> data;

        public static RaceData SelectedClassData = new();
        public static List<Stat> Stats = new List<Stat>();

        public Race()
        {
            MainWindow.Instance.character_race_combobox.SelectionChanged += character_race_combobox_SelectionChanged;

            ClearStats();
        }

        public static void Update()
        {
            string pathFile = MainWindow.PathData + $"Cache/{DataManager.DataSave.SelectedRepository}/Race.json";
            if (File.Exists(pathFile))
            {
                string json = File.ReadAllText(pathFile);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                try
                {
                    data = JsonSerializer.Deserialize<Dictionary<string, List<RaceData>>>(json, options);
                    MainWindow.Instance.character_race_combobox.ItemsSource = data.Keys;
                }
                catch
                {
                    data = new();
                }
            }
            else
            {
                MainWindow.Instance.character_race_combobox.ItemsSource = null;
                MainWindow.Instance.character_race_textblock.Text = "[[Раса]]";
            }
        }



        private static void ClearStats()
        {
            Stats.Clear();
            for (int i = 0; i < 30; i++)
            {
                Stats.Add(new Stat { Value = 0, Roll = 0 });
            }
        }

        private void character_race_combobox_SelectionChanged(object sender, Avalonia.Controls.SelectionChangedEventArgs e)
        {
            UpdateRace();
        }

        private static void UpdateRace()
        {
            if (MainWindow.Instance.character_race_combobox.SelectedValue != null)
            {
                ClearStats();

                string selectText = MainWindow.Instance.character_race_combobox.SelectedValue.ToString();
                SelectedClassData = data[selectText][0];

                foreach (var item in SelectedClassData.StandartStats[0])
                {
                    int index = Array.IndexOf(StatNameRus, item.Key.ToLower());

                    if (index != -1)
                    {
                        int[] stat = SelectedClassData.StandartStats[0][item.Key];
                        Stats[index].Value = stat[0];
                        Stats[index].Roll = stat[1];
                    }
                }

                MainWindow.Characteristics.UpdateAllCharacterisitc();
                Skills.ReloadDataGridSkills();

                MainWindow.Instance.character_race_textblock.Text = selectText;

            }
        }

    }

    public class Stat
    {
        public int Value { get; set; } = 0;

        public int Roll { get; set; } = 0;
    }

    internal class RaceData
    {
        [JsonPropertyName("ДопОчки_Навыков")]
        public int AddPoints { get; set; } = 0;
        [JsonPropertyName("ДопКД")]
        public int AddKD { get; set; } = 0;
        [JsonPropertyName("основная_длина_палки")]
        public double StandartStickLength { get; set; } = 1;
        [JsonPropertyName("Множитель_палки")]
        public double MultiplyStickLength { get; set; } = 1;
        [JsonPropertyName("Плюс_веса")]
        public int AddWeight { get; set; } = 0;
        [JsonPropertyName("Множитель_веса")]
        public double MultiplyWeight { get; set; } = 1;
        [JsonPropertyName("Множитель_магУрона")]
        public double MultiplyMagDamage { get; set; } = 1;
        [JsonPropertyName("Множитель_физУрона")]
        public double MultiplyPhisDamage { get; set; } = 1;

        [JsonPropertyName("Статы_Стандарт")]
        public List<Dictionary<string, int[]>> StandartStats { get; set; } = new List<Dictionary<string, int[]>>();

        [JsonPropertyName("Способности")]
        public List<string> Skills { get; set; } = new List<string>();

        [JsonPropertyName("Касты")]
        public List<string> Casts { get; set; } = new List<string>();
    }


}
