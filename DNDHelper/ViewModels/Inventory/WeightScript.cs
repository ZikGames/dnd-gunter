using Avalonia.Controls;
using Avalonia.Media;
using DNDHelper.ViewModels.Config;
using DNDHelper.ViewModels.Settings;
using DNDHelper.ViewModels.Сharacteristics;
using DNDHelper.Views;
using static DNDHelper.ViewModels.Inventory.InventoryLoot;

namespace DNDHelper.ViewModels.Inventory
{
    internal class WeightScript
    {
        MainWindow main = MainWindow.Instance;
        public static int UsedItemWeight = 0;

        public static int Attack = 0;
        public static int Dodge = 0;
        public WeightScript()
        {
            main.backpack_plus_tb.TextChanged += Backpack_plus_tb_TextChanged;
        }

        private void Backpack_plus_tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            CountWeightCharacter();
        }

        public static void CountWeightItems()
        {
            int weightItem = 0;
            int weightArmor = 0;
            foreach (var item in InventoryItems)
            {
                if (item.CountWeight == true)
                    if (item.KD > 0)
                        weightArmor += item.Weight;
                    else
                        weightItem += item.Weight;
            }
            if (!Backpack.IsDivideArmorWeight)
                if (weightItem > 0)
                    UsedItemWeight = (int)(weightItem / Backpack.DivideItemWeight) + weightArmor;
                else
                    UsedItemWeight = weightItem + weightArmor;
            else
                UsedItemWeight = (int)(weightItem + weightArmor) / Backpack.DivideItemWeight;
            CountWeightCharacter();
        }

        public static void CountWeightCharacter()
        {
            // В Avalonia TextBox.Text пустого поля = null (в WPF было ""), поэтому TryParse.
            int.TryParse(MainWindow.Instance.backpack_plus_tb.Text, out int addWeight);
            int MaxWeight = (int)MathF.Round((CharacteristicTable.Buffed(0) * 30 * (float)Race.SelectedClassData.MultiplyWeight) + Race.SelectedClassData.AddWeight + Backpack.AddWeight + addWeight + ItemBaffsListScript.ItemBaffs[36][0]);
            int remainedWeight = MaxWeight - UsedItemWeight;
            MainWindow.Instance.сharacter_weight_textblock.Text = $"Вес: {remainedWeight}/{MaxWeight}";

            CheckOverWeight(remainedWeight);
        }

        public static void CheckOverWeight(int remainedWeight)
        {
            Attack = 0;
            Dodge = 0;

            if (remainedWeight < 0)
            {
                Attack = -5;
                Dodge = -5;
                MainWindow.Instance.сharacter_weight_textblock.Foreground = Brushes.Firebrick;
            }
            else
                MainWindow.Instance.сharacter_weight_textblock.Foreground = new SolidColorBrush(Settings.Settings.SelectedTheme[1]);

            GridCharacteristics.UpdateOtherDeBaff();
        }
    }
}
