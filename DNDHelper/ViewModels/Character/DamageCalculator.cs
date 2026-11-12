using Avalonia.Controls;
using Avalonia.Input;
using DNDHelper.ViewModels.Config;
using DNDHelper.ViewModels.Inventory;
using DNDHelper.ViewModels.Settings;
using DNDHelper.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DNDHelper.ViewModels.Character
{
    public class DamageCalculator
    {
        private ObservableCollection<TypeDamage> typeDamages { get; set; } = new();
        MainWindow main = MainWindow.Instance;
        public static Dictionary<string, List<TypeDamageConfig>> data = new();


        public DamageCalculator()
        {
            main.DamageGrid.ItemsSource = typeDamages;

            // WPF DataGrid.ContextMenuOpening -> Avalonia ContextMenu.Opening. Не Control.ContextRequested:
            // ContextMenu сам обрабатывает это событие и помечает его Handled раньше обработчика
            // с обычной подпиской, так что видимость пунктов меню не обновлялась бы.
            main.DamageGrid.ContextMenu!.Opening += (_, _) => DamageGrid_ContextMenuOpening();
            main.AddTypeDamage.Click += AddTypeDamage_Click;
            main.DeleteTypeDamage.Click += DeleteTypeDamage_Click;
            main.CountDamage_button.PointerPressed += CountDamage_button_PointerPressed;

            Update();
        }


        public static void Update()
        {
            string pathFile = MainWindow.PathData + $"Cache/{DataManager.DataSave.SelectedRepository}/TypeDamage.json";
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
                    data = JsonSerializer.Deserialize<Dictionary<string, List<TypeDamageConfig>>>(json, options);
                }
                catch (Exception)
                {
                    data = new();
                }

            }
        }

        private void CountDamage_button_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(main.CountDamage_button).Properties.IsLeftButtonPressed)
                return;

            Calculate();
        }

        private void DamageGrid_ContextMenuOpening()
        {
            int selectedIndex = main.DamageGrid.SelectedIndex;
            if (selectedIndex != -1)
            {
                main.AddTypeDamage.IsVisible = true;
                main.DeleteTypeDamage.IsVisible = true;
            }
            else
            {
                main.AddTypeDamage.IsVisible = true;
                main.DeleteTypeDamage.IsVisible = false;
            }
        }

        private void DeleteTypeDamage_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            int selectedIndex = main.DamageGrid.SelectedIndex;
            if (selectedIndex != -1)
                typeDamages.RemoveAt(selectedIndex);
        }

        private void AddTypeDamage_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            typeDamages.Add(new TypeDamage());
        }



        private void Calculate()
        {
            int TotalDamage = 0;
            int KDArmor = KDScript.ArmorKD;
            int KDBody = KDScript.BodyKD;
            int KDMental = KDScript.MentalKD;
            int UsedKDArmor = 0;
            int USedKDBody = 0;
            int UsedKDMental = 0;

            Dictionary<string, int> effects = new() { { "Кровотечение", 0 }, { "Шок", 0 }, { "Оглушение", 0 }, { "Горение", 0 } };

            foreach (TypeDamage typeDamage in typeDamages)
            {
                if (data.Keys.Contains(typeDamage.NameType.Content.ToString()) != false)
                {
                    var config = data[typeDamage.NameType.Content.ToString()][0];

                    string name = typeDamage.NameType.Content.ToString().ToLower();
                    string type = config.Type;

                    double damage = typeDamage.Damage;
                    double baff = config.BaffDamage;
                    double multiplyArmor = config.MultiplyArmorKD;
                    int damageInEffect = config.DamageInEffect;

                    double multiplyDamageRace = 1;
                    int indexDamageType = 0;
                    if (type == "Физ")
                    {
                        multiplyDamageRace = Race.SelectedClassData.MultiplyPhisDamage;
                        indexDamageType = 1;
                    }
                    else
                    {
                        multiplyDamageRace = Race.SelectedClassData.MultiplyMagDamage;
                        indexDamageType = 0;
                    }


                    if (typeDamage.NameType.Content.ToString() != "ментальный")
                    {

                        // Счет магического щита
                        if (!string.IsNullOrEmpty(MainWindow.Instance.shield_health_textbox.Text) && MainWindow.Instance.shield_health_textbox.Text != "0" && DataManager.DataSave.SelectedTypeMagicShield != -1 && Health.DamageMagicShield != Health.MaxMagicShield)
                            if (DataManager.DataSave.SelectedTypeMagicShield == indexDamageType || DataManager.DataSave.SelectedTypeMagicShield == 2)
                            {
                                int hpSield = Health.MaxMagicShield - Health.DamageMagicShield;

                                if (damage < hpSield)
                                {
                                    Health.DamageMagicShield = Health.DamageMagicShield + (int)damage;
                                    continue;
                                }
                                else
                                {
                                    damage -= hpSield;
                                    Health.DamageMagicShield = Health.MaxMagicShield;
                                }
                            }


                        if (typeDamage.NameType.Content.ToString() != "без дебаффов")
                        {
                            // Броня
                            double multiplyChipping = 1;
                            double multiplyOther = 1;
                            if (TypeArmorBaffs.SelectedArmor > 1 && type == "Физ")
                            {
                                if (TypeArmorBaffs.SelectedArmor == 2)
                                {
                                    multiplyChipping = 0.5;
                                    multiplyOther = 0.75;
                                }
                                else if (TypeArmorBaffs.SelectedArmor == 3)
                                {
                                    multiplyChipping = 0.25;
                                    multiplyOther = 0.5;
                                }

                                if (name == "рубящий")
                                {
                                    damage *= multiplyChipping;
                                    name = "дробящий";
                                    multiplyOther = 1;
                                }
                            }



                            // Счет магического и физичекого урона
                            double _kd = (KDArmor * multiplyArmor);

                            damage *= multiplyDamageRace;
                            if (typeDamage.CheckBox) damage *= baff;

                            if (UsedKDArmor == KDArmor)
                                multiplyOther = 1;
                            calculateDamage(ref damage, ref UsedKDArmor, (int)_kd);
                            if (DataManager.DataSave.SelectedRepository != SetRepository.repositories[2].Name)
                                damage *= multiplyOther;


                            calculateDamage(ref damage, ref USedKDBody, KDBody);

                        }


                    }
                    else // Счет ментального урона
                        if (UsedKDMental < KDMental)
                        calculateDamage(ref damage, ref UsedKDMental, KDMental);

                    calculateEffects(name, damage, damageInEffect, ref effects);

                    TotalDamage += (int)double.Round(damage);
                }
            }

            string receivedEffect = "";
            foreach (var effectName in effects.Keys)
            {
                int effect = effects[effectName];
                if (effect != 0) receivedEffect += $"\r\n{effectName}: {effect}";
            }
            if (receivedEffect != "")
                ToolTip.SetTip(main.DamageHealth_textblock, receivedEffect.Remove(0, 2));

            main.DamageHealth_textblock.Text = TotalDamage.ToString();
            Health.Damage += TotalDamage;
            Health.HealthUpdate();
        }

        // Счет урона
        private void calculateDamage(ref double damage, ref int usedKD, int kd)
        {
            int _kd = kd - usedKD;
            if (damage < _kd)
            {
                usedKD += (int)double.Round(damage);
                damage = 0;
            }
            else
            {
                damage -= _kd;
                usedKD = kd;
            }
        }

        private void calculateEffects(string nameDamage, double damage, int damageInEffect, ref Dictionary<string, int> effects)
        {
            // {"Кровотечение", 0}, {"Шок", 0}, {"Оглушение", 0}, { "Горение", 0 }
            switch (nameDamage)
            {
                case "рубящий":
                    effects["Кровотечение"] += (int)(damage / damageInEffect);
                    break;
                case "электрический":
                    effects["Шок"] += (int)(damage / damageInEffect);
                    break;
                case "дробящий":
                    if (Health.MaxHealth * 0.1 < damage)
                        effects["Оглушение"] = 1;
                    break;
                case "огненный":
                    int stage = 0;
                    if (damage >= 20 * GlobalMultiply.data.GlobalMultiply)
                        stage = 1;
                    if (damage >= 40 * GlobalMultiply.data.GlobalMultiply)
                        stage = 2;
                    if (damage >= 250 * GlobalMultiply.data.GlobalMultiply)
                        stage = 3;
                    if (damage >= 400 * GlobalMultiply.data.GlobalMultiply)
                        stage = 4;

                    if (effects["Горение"] < stage)
                        effects["Горение"] = stage;
                    break;
            }
        }
    }

    public class TypeDamage : INotifyPropertyChanged
    {
        private Dictionary<string, string> typeDamageToolTip = new()
        {
            { "Божественный", " Нечисть или нежить?" },
            { "Колющий", " Блок разбит?" }
        };


        private ComboBoxItem _nameType;
        public ComboBoxItem NameType
        {
            get => _nameType;
            set
            {
                _nameType = value;
                if (_nameType.Content != null)
                    if (typeDamageToolTip.Keys.Contains(_nameType.Content))
                    {
                        VisibilityCheckBox = true;
                        TextCheckBox = typeDamageToolTip[_nameType.Content.ToString()];
                    }
                    else
                    {
                        VisibilityCheckBox = false;
                        CheckBox = false;
                    }

                OnPropertyChanged();
            }
        }

        public int Damage { get; set; } = 0;

        public bool CheckBox { get; set; } = false;

        private string _textCheckBox = "";
        public string TextCheckBox
        {
            get => _textCheckBox;
            set
            {
                _textCheckBox = value;
                OnPropertyChanged();
            }
        }

        // Было typeof(Visibility) (WPF) — в Avalonia у IsVisible (то, на что реально биндится
        // в Main.axaml) уже bool, так что тип поменян на bool напрямую, имя оставлено как было.
        private bool _visibilityCheckBox = false;
        public bool VisibilityCheckBox
        {
            get => _visibilityCheckBox;
            set
            {
                _visibilityCheckBox = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TypeDamageConfig
    {
        [JsonPropertyName("Тип")]
        public string Type { get; set; } = "";

        [JsonPropertyName("МентальноеКД")]
        public int IsMentKD { get; set; } = 0;

        [JsonPropertyName("МножительБрони")]
        public double MultiplyArmorKD { get; set; } = 1;

        [JsonPropertyName("ЕслиЧтотоПроизошлоТоДамагУмножаетсяНа")]
        public double BaffDamage { get; set; } = 1;



        [JsonPropertyName("ЛибоЭффектКаждыйДамаг")]
        public int DamageInEffect { get; set; } = 0;


        [JsonPropertyName("ПроцентУронаОтЗдоровьяЧтобыНаложилсяЭффект")]
        public int ProcentInEffect { get; set; } = 0;

        [JsonPropertyName("ИмяЭффекта")]
        public string NameEffect { get; set; } = "";
    }
}
