using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using DNDHelper.ViewModels.Config;
using DNDHelper.ViewModels.Inventory;
using DNDHelper.ViewModels.Settings;
using DNDHelper.ViewModels.Сharacteristics;
using DNDHelper.Views;

namespace DNDHelper.ViewModels.Character
{
    internal class Health
    {
        public static int Damage
        {
            get => DataManager.DataSave.Damage;
            set
            {
                DataManager.DataSave.Damage = value;
            }
        }
        public static int MaxHealth = 0;
        MainWindow main = MainWindow.Instance;


        public static int DamageMagicShield
        {
            get => DataManager.DataSave.DamageMagicShield;
            set
            {
                DataManager.DataSave.DamageMagicShield = value;
            }
        }

        public static int MaxMagicShield
        {
            get => int.Parse(DataManager.DataSave.MaxMagicShield);
            set
            {
                DataManager.DataSave.MaxMagicShield = value.ToString();
            }
        }

        public Health()
        {
            HealthUpdate();
            main.CurrentHealth_textblock.PointerPressed += CurrentHealth_textblock_PointerPressed;
            main.CurrentHealth_textbox.KeyDown += CurrentHealth_textblock_KeyDown;
            main.CurrentHealth_textbox.TextInput += CurrentHealth_textbox_TextInput;
            main.ResetHealth.Click += ResetHealth_Click;
        }

        private void ResetHealth_Click(object sender, RoutedEventArgs e)
        {
            Damage = 0;
            DamageMagicShield = 0;
            HealthUpdate();
        }

        private void CurrentHealth_textbox_TextInput(object sender, TextInputEventArgs e)
        {
            TextboxProcessing.WholeNumbersOnly(main.CurrentHealth_textbox, e);
        }

        private void CurrentHealth_textblock_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(main.CurrentHealth_textblock).Properties.IsLeftButtonPressed)
                return;

            main.CurrentHealth_textblock.IsVisible = false;
            main.CurrentHealth_textbox.IsVisible = true;
            main.CurrentHealth_textbox.Text = main.CurrentHealth_textblock.Text;
            main.CurrentHealth_textbox.Focus();
        }

        private void CurrentHealth_textblock_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string text = main.CurrentHealth_textbox.Text ?? ""; // Avalonia: пустое поле = null
                main.CurrentHealth_textblock.Text = text;
                if (text.Length == 0 || main.CurrentHealth_textblock.Text == "-")
                {
                    main.CurrentHealth_textblock.Text = "0";
                    main.CurrentHealth_textbox.Text = "0";
                }
                main.CurrentHealth_textblock.IsVisible = true;
                main.CurrentHealth_textbox.IsVisible = false;

                int currectHealth = Convert.ToInt32(main.CurrentHealth_textblock.Text);
                Damage = MaxHealth - currectHealth;
                HealthUpdate();
            }
        }



        public static void CountCriticalRoll(int currentHealth, int maxHealth)
        {
            MainWindow main = MainWindow.Instance;
            if (currentHealth < 0 && maxHealth != 0)
            {
                main.СriticalRoll_Text_textblock.IsVisible = true;
                main.СriticalRoll_GreaterOrEqual_textblock.IsVisible = true;
                main.СriticalRoll_Number_textblock.IsVisible = true;

                main.СriticalRoll_Number_textblock.Text = Convert.ToString((Math.Abs(currentHealth) * 100) / maxHealth / 5 + 10);
            }
            else
            {
                // WPF Visibility.Hidden сохранял место в layout — если это важно визуально,
                // проверьте разметку .axaml (Grid.Row/Column этих текстблоков) при первом запуске.
                main.СriticalRoll_Text_textblock.IsVisible = false;
                main.СriticalRoll_GreaterOrEqual_textblock.IsVisible = false;
                main.СriticalRoll_Number_textblock.IsVisible = false;
            }
        }

        public static void HealthUpdate()
        {
            int body = CharacteristicTable.Buffed(CharacteristicTable.StatName.Body);
            MaxHealth = (int)Math.Floor((body * 10 * GlobalMultiply.data.GlobalMultiply) * PlayerClass.SelectedClassData.MultiplyHealth);
            if (DataManager.DataSave.SelectedRepository != SetRepository.repositories[1].Name)
            {
                int addАfromTheLevel = (int)Math.Floor(MaxHealth * 0.1) * DataManager.DataSave.Level;
                MaxHealth += addАfromTheLevel;
            }
            MaxHealth += ItemBaffsListScript.ItemBaffs[37][0];
            int currentHealth = MaxHealth - Damage;
            CountCriticalRoll(currentHealth, MaxHealth);

            string currentMagicShield = "";
            string maxMagicShield = "";
            if (!string.IsNullOrEmpty(MainWindow.Instance.shield_health_textbox.Text) && MaxMagicShield != 0 && DataManager.DataSave.SelectedTypeMagicShield != -1)
            {
                maxMagicShield = $" {MaxMagicShield}"; ;
                currentMagicShield = $" {MaxMagicShield - DamageMagicShield}";

                MainWindow.Instance.CurrentShieldHealth_textblock.Text = currentMagicShield;
                MainWindow.Instance.MaxShieldHealth_textblock.Text = maxMagicShield;

                IBrush colorFore = Brushes.White;
                switch (DataManager.DataSave.SelectedTypeMagicShield)
                {
                    case 0:
                        colorFore = Brushes.LightBlue;
                        break;
                    case 1:
                        colorFore = Brushes.Orange;
                        break;
                    case 2:
                        colorFore = Brushes.ForestGreen;
                        break;
                }

                MainWindow.Instance.CurrentShieldHealth_textblock.Foreground = colorFore;
                MainWindow.Instance.MaxShieldHealth_textblock.Foreground = colorFore;
            }
            else
            {
                MainWindow.Instance.CurrentShieldHealth_textblock.Text = "";
                MainWindow.Instance.MaxShieldHealth_textblock.Text = "";
            }

            MainWindow.Instance.CurrentHealth_textblock.Text = currentHealth.ToString();
            MainWindow.Instance.MaxHealth_textblock.Text = MaxHealth.ToString();
        }
    }
}
