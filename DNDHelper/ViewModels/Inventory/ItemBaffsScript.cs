using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DNDHelper.ViewModels.Character;
using DNDHelper.ViewModels.Сharacteristics;
using DNDHelper.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static DNDHelper.ViewModels.Inventory.InventoryLoot;

namespace DNDHelper.ViewModels.Inventory
{
    public class ItemBaffsListScript
    {

        MainWindow main = MainWindow.Instance;
        public static ObservableCollection<ItemBaff> ItemBaffsList { get; set; } = new();
        public static List<int[]> ItemBaffs = new(50);
        public ItemBaffsListScript() 
        {
            main.DataGridItemBaffsList.ItemsSource = ItemBaffsList;

            main.DataGridItemBaffsList.BeginningEdit += DataGridItemBaffsList_BeginningEdit; ;
            main.DataGridItemBaffsList.CellEditEnding += DataGridItemBaffsList_CellEditEnding; ;

            // WPF PreviewMouseLeftButtonDown + PreviewMouseRightButtonDown - два отдельных
            // события, оба вели на один и тот же обработчик (без проверки кнопки). В Avalonia
            // это один тоннелируемый PointerPressed, покрывающий все кнопки мыши сразу.
            main.DataGridItemBaffsList.AddHandler(
                InputElement.PointerPressedEvent,
                DataGridItemBaffsList_PreviewMouseButtonDown,
                RoutingStrategies.Tunnel);

            main.DataGridItemBaffsList.ContextMenu!.Opening += (_, _) => DataGridItemBaffsList_ContextMenuOpening();
            main.AddMenuItemBaff.Click += AddMenuItemBaff_Click;
            main.DeleteMenuItemBaff.Click += DeleteMenuItemBaff_Click;

            ClearItemBaffs();
        }

        private void DataGridItemBaffsList_BeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
        {
            ShowOriginalValue(e.Column.DisplayIndex, e.Row.GetIndex());
        }

        private void DataGridItemBaffsList_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditingElement is TextBox textBox)
            {
                string newValue = textBox.Text;

                ShowBaffValue(e.Column.DisplayIndex, e.Row.GetIndex(), newValue);
                
            }
        }



        private void DataGridItemBaffsList_PreviewMouseButtonDown(object sender, PointerPressedEventArgs e)
        {
            var cell = FindParent<DataGridCell>(e.Source as Visual);

            if (cell == null)
                main.DataGridItemBaffsList.SelectedIndex = -1;
        }

        private void DataGridItemBaffsList_ContextMenuOpening()
        {
            int selectedIndex = main.DataGridItemBaffsList.SelectedIndex;
            if (selectedIndex != -1)
            {
                main.AddMenuItemBaff.IsVisible = true;
                main.DeleteMenuItemBaff.IsVisible = true;
            }
            else
            {
                main.AddMenuItemBaff.IsVisible = true;
                main.DeleteMenuItemBaff.IsVisible = false;
            }
            if(main.DataGridInventory.SelectedIndex == -1)
            {
                main.AddMenuItemBaff.IsVisible = false;
                main.DeleteMenuItemBaff.IsVisible = false;
            }
        }
        private void AddMenuItemBaff_Click(object sender, RoutedEventArgs e)
        {
            ItemBaffsList.Add(new ItemBaff());
        }

        private async void DeleteMenuItemBaff_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = main.DataGridItemBaffsList.SelectedIndex;
            if (selectedIndex != -1)
            {
                int selectedCount = main.DataGridItemBaffsList.SelectedItems.Count;
                var box = selectedCount == 1
                    ? MessageBoxManager.GetMessageBoxStandard("Удалить Бафф", "Вы точно хотите удалить Бафф", ButtonEnum.YesNo)
                    : MessageBoxManager.GetMessageBoxStandard("Удалить Баффы", "Вы точно хотите удалить Баффы", ButtonEnum.YesNo);
                var result = await box.ShowAsync();
                if (result == ButtonResult.Yes)
                    for (int i = 0; i < selectedCount; i++)
                    {
                        ItemBaffsList.RemoveAt(main.DataGridItemBaffsList.SelectedIndex);
                    }
                UpdateValues();
            }
        }

        public void UpdateValues()
        {
            int selectedItemIndex = main.DataGridInventory.SelectedIndex;
            if (selectedItemIndex != -1)
            {
                InventoryItems[selectedItemIndex].Baffs.Clear();
                foreach (var baff in ItemBaffsList)
                    if (baff.AddValue != 0 || baff.AddRoll != 0)
                        InventoryItems[selectedItemIndex].Baffs.Add(baff);
            }

            ClearItemBaffs();
            foreach (var item in InventoryItems)
            {
                if (item.Equipped)
                    foreach (var baff in item.Baffs)
                    {
                        if (baff.SelectedIndex != -1)
                        {
                            ItemBaffs[baff.SelectedIndex][0] += baff.WholeAddValue;
                            ItemBaffs[baff.SelectedIndex][1] += baff.WholeAddRoll;

                            if (baff.SelectedIndex == 38)
                                MagicSpells.MagicSpells.RepositoryLoad();
                        }
                    }
            }
            MainWindow.Characteristics.UpdateAllCharacterisitc();
            Health.HealthUpdate();
            AttributesCharacter.CallAllMethodInScript();
            KDScript.UpdateAllKD();
        }

        private void ClearItemBaffs()
        {
            ItemBaffs.Clear();
            for (int i = 0;i < 50;i++)
                ItemBaffs.Add(new int[] {0, 0});
        }

        private void ShowOriginalValue(int indexColumn, int indexRow)
        {
            int selectedIndex = main.DataGridInventory.SelectedIndex;
            double value = 0;
            switch (indexColumn)
            {
                case 1:
                    value = ItemBaffsList[indexRow].AddValue;
                    ItemBaffsList[indexRow].AddValue = value / (float)QualityToDouble(InventoryItems[selectedIndex].Quality, value < 0);
                    break;
                case 2:
                    value = ItemBaffsList[indexRow].AddRoll;
                    ItemBaffsList[indexRow].AddRoll = value / (float)QualityToDouble(InventoryItems[selectedIndex].Quality, value < 0);
                    break;
            }
        }

        private void ShowBaffValue(int indexColumn, int indexRow, string newValue)
        {
            int selectedIndex = main.DataGridInventory.SelectedIndex;
            newValue = newValue.Replace(" ", "");
            double value = 0;
            if (newValue != "" && newValue != "-")
                switch (indexColumn)
                {
                    case 1:
                        value = ItemBaffsList[indexRow].AddValue;
                        value = int.Parse(newValue) * (float)QualityToDouble(InventoryItems[selectedIndex].Quality, int.Parse(newValue) < 0);
                        ItemBaffsList[indexRow].AddValue = value;
                        break;
                    case 2:
                        value = ItemBaffsList[indexRow].AddRoll;
                        value = int.Parse(newValue) * (float)QualityToDouble(InventoryItems[selectedIndex].Quality, int.Parse(newValue) < 0);
                        ItemBaffsList[indexRow].AddRoll = value;
                        break;

                }
            UpdateValues();
        }

        public static void SetQualityBaffs(int Stage, string Quality)
        {
            if(Stage == 0)
                foreach(var baff in ItemBaffsList)
                {
                    baff.AddValue = baff.AddValue / (float)QualityToDouble(Quality, baff.AddValue < 0);
                    baff.AddRoll = baff.AddRoll / (float)QualityToDouble(Quality, baff.AddRoll < 0);
                }
            else
                foreach (var baff in ItemBaffsList)
                {
                    baff.AddValue = baff.AddValue * (float)QualityToDouble(Quality, baff.AddValue < 0);
                    baff.AddRoll = baff.AddRoll / (float)QualityToDouble(Quality, baff.AddRoll < 0);
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

        public class ItemBaff : INotifyPropertyChanged
        {
            public int SelectedIndex { get; set; } = -1;

            private int _wholeAddValue;
            private double _addValue;
            public int WholeAddValue
            {
                get => _wholeAddValue;
                set
                {
                    _wholeAddValue = value;
                    OnPropertyChanged();
                }
            }
            public double AddValue
            {
                get => _addValue;
                set
                {
                    _addValue = value;

                    _wholeAddValue = (int)_addValue;
                    OnPropertyChanged();
                }
            }
            private int _wholeAddRoll;
            private double _addRoll;
            public int WholeAddRoll
            {
                get => _wholeAddRoll;
                set
                {
                    _wholeAddRoll = value;
                    OnPropertyChanged();
                }
            }
            public double AddRoll
            {
                get => _addRoll;
                set
                {
                    _addRoll = value;

                    _wholeAddRoll = (int)_addRoll;
                    OnPropertyChanged();
                }
            }


            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

//Ролл атаки - 30
//Ролл блока - 31
//Ролл уворота -  32
//Ролл контрдействия - 33
//Ролл атаки кулаками - 34
//Ролл дальнобойным - 35
//Вес - 36
//Здоровье- 37
//Ментальное КД - 38
//Палка Хода - 39
//Действие - 40
//Контрдействие - 41
//Маг. пули - 42
//Маг. ролл - 43
