using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using DNDHelper.ViewModels;
using DNDHelper.ViewModels.Character;
using DNDHelper.ViewModels.Config;
using DNDHelper.ViewModels.Diary;
using DNDHelper.ViewModels.Inventory;
using DNDHelper.ViewModels.MagicSpells;
using DNDHelper.ViewModels.ReferenceBooks;
using DNDHelper.ViewModels.Settings;
using DNDHelper.ViewModels.Сharacteristics;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using DarkThem = DNDHelper.ViewModels.Settings.Settings;

namespace DNDHelper.Views
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.axaml
	/// </summary>

	public partial class MainWindow : Window
	{


		public static MainWindow Instance;
		public static GridCharacteristics Characteristics;
		public static TreeSkills TreeSkillsScript;
		public static ItemBaffsListScript ItemBaffsListScript;
		public static DamageCalculator DamageCalculator;
		public static string PathMain = "";
		// Каталог для Cache/ (загруженные с репозитория конфиги) - в отличие от PathMain
		// (папка exe, только для чтения — там же лежит Version.txt), это должно быть
		// записываемое место. См. AppPaths.cs про то, почему PathMain для этого не годится
		// под Nix (/nix/store read-only + путь меняется при пересборке).
		public static string PathData = "";



		public MainWindow()
		{
			// Кроссплатформенно: раньше путь собирался через ручной Split('\\'),
			// что ломается на Linux/macOS.
			string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
			PathMain = exeDir + Path.DirectorySeparatorChar;
			PathData = DNDHelper.ViewModels.Settings.AppPaths.GetWritableRoot() + Path.DirectorySeparatorChar;

			DataManager.FindPathSaves();
			InitializeComponent();
			// Фильтры ввода: в Avalonia TextBox сам обрабатывает TextInput раньше обычных (bubble) обработчиков,
			// поэтому, как PreviewTextInput в WPF, вешаем их на туннельную стадию.
			shield_health_textbox.AddHandler(InputElement.TextInputEvent, shield_health_textbox_PreviewTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);
			Cuprum_tb.AddHandler(InputElement.TextInputEvent, Cuprum_tb_PreviewTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);
			Silver_tb.AddHandler(InputElement.TextInputEvent, Silver_tb_PreviewTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);
			Gold_tb.AddHandler(InputElement.TextInputEvent, Gold_tb_PreviewTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);
			backpack_plus_tb.AddHandler(InputElement.TextInputEvent, backpack_plus_tb_PreviewTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);
			DamageMagic_textbox.AddHandler(InputElement.TextInputEvent, DamageMagic_textbox_PreviewTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);
			MagicBulletCurrent_textbox.AddHandler(InputElement.TextInputEvent, MagicBulletCurrent_textbox_PreviewTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);
			// DamageTextBox живёт в шаблоне ячейки DataGrid (имени нет), поэтому ловим его по классу.
			InputElement.TextInputEvent.AddClassHandler<TextBox>((tb, e) => { if (tb.Classes.Contains("damageTextBox")) DamageTextBox_PreviewTextInput(tb, e); }, Avalonia.Interactivity.RoutingStrategies.Tunnel);


			// Версия берётся из самой сборки (release-workflow задаёт её из тега), а не из Version.txt рядом с exe:
			// такого файла больше нет, а локальная сборка показывается как "dev".
			VersionTextBlock.Text = DNDHelper.ViewModels.AppInfo.Version + "  ";

			InitializeClasses();
			DataManager.ReadSaves();
			Resources["StandartBackColor"] = new SolidColorBrush(DarkThem.SelectedTheme[0]);
			Resources["StandartForeColor"] = new SolidColorBrush(DarkThem.SelectedTheme[1]);
			Closed += Main_Closed;

			// Пасте-блок для полей, где в WPF вставка была безусловно запрещена через
			// DataObject.Pasting (в Avalonia такого отменяемого события нет — см. шпаргалку п.9).
			foreach (var tb in new[]
			{
				Cuprum_tb, Silver_tb, Gold_tb, backpack_plus_tb,
				shield_health_textbox, DamageMagic_textbox, MagicBulletCurrent_textbox
			})
			{
				tb.KeyDown += TextboxProcessing.BlockPaste;
			}

			// Дневник переведён на AvaloniaEdit (TextEditor) — см. изменения в MainWindow.axaml.
			diaryTB.TextArea.SelectionChanged += DiaryTB_SelectionChanged;

#if !DEBUG
							            CheckUpdate.Check();
							DataManager.WorkingProgram(false);

#endif
#if DEBUG
			DataManager.WorkingProgram(true);
			#endif
		}

		private void Main_Closed(object? sender, EventArgs e)
		{
			DataManager.Save(true);
		}

		public void InitializeClasses()
		{
			Instance = this;
			SavesMenu savesMenu = new SavesMenu();
			DataContext = DataManager.DataSave;
			LoadingReferenceBook referenceBook = new LoadingReferenceBook();
			referenceBook.InitializeWebView();
			Race @class = new();
			PlayerClass playerClass = new();
			ItemBaffsListScript = new();
			Effects effects = new Effects();
			InventoryLoot inventoryLoot = new();
			Skills skills = new();
			Level level = new();
			MagicSpells magicSpells = new();
			DiaryManager diaryManager = new DiaryManager();
			TreeSkillsScript = new();
			Characteristics = new();
			Characteristics.UpdateAllCharacterisitc();
			MagicSearch magicSearch = new MagicSearch();
			TypeArmorBaffs typeArmorBaffs = new();
			// Раньше тут ещё раз создавался LoadingReferenceBook — ссылка открывалась дважды
			// (дважды подписывался ListBoxUrls.SelectionChanged). Экземпляр уже есть выше (referenceBook).
			DamageCalculator = new();

			WeightScript weightScript = new WeightScript();
			Health health = new Health();

			AttributesCharacter.CallAllMethodInScript();

		}

		public void SetTheme(Color Background, Color Foreground)
		{
			Resources["StandartBackColor"] = new SolidColorBrush(Background);
			Resources["StandartForeColor"] = new SolidColorBrush(Foreground);
		}
		// Меню
		private void CheckUpdate_Click(object sender, RoutedEventArgs e)
		{
			CheckUpdate.Check(true);
		}
		private void UpdateConfingsInHands_Click(object sender, RoutedEventArgs e)
		{
			SetRepository.FileСonnection(true);
		}

		private void UrlTusha(object sender, RoutedEventArgs e)
		{

			Process.Start(new ProcessStartInfo("https://docs.google.com/document/d/1rUMWdTp645Zy80d09ZMoJ6dmcw9lAP25tqxsdNkftaY/edit?usp=sharing") { UseShellExecute = true });
		}
		private void UrlGunter(object sender, RoutedEventArgs e)
		{
			Process.Start(new ProcessStartInfo("https://sites.google.com/view/rulesdndgunter/%D0%BF%D1%80%D0%B0%D0%B2%D0%B8%D0%BB%D0%B0") { UseShellExecute = true });
		}
		private void UrlWeapons(object sender, RoutedEventArgs e)
		{
			Process.Start(new ProcessStartInfo("https://docs.google.com/document/d/1QRy-6NgwGB81BE0QE1DT_mO7DUGCf-PWrM9csBKEuno/edit?usp=sharing") { UseShellExecute = true });
		}
		private void UrlRaces(object sender, RoutedEventArgs e)
		{
			Process.Start(new ProcessStartInfo("https://docs.google.com/document/d/1V5B_7he-2i8BzMviCW9aqQoo831FIbKfi0uRwVEJ4VY/edit?usp=sharing") { UseShellExecute = true });
		}
		private void UrlClasses(object sender, RoutedEventArgs e)
		{
			Process.Start(new ProcessStartInfo("https://docs.google.com/document/d/19zM6JnIa5TNag2Adf22SV7ZI7uZUzzWZLvZbKvLylB0/edit?usp=sharing") { UseShellExecute = true });
		}
		private async void About_Click(object sender, RoutedEventArgs e)
		{
			About about = new();
			await about.ShowDialog(this);
		}
		private void EditMode_click(object sender, RoutedEventArgs e)
		{
			if (EditMode_button.IsChecked == false)
			{
				Resources["IsEdit"] = false;
				Characteristics.UpdatePointText(false);
				TreeSkillsScript.UpdateDisplayPoints();
				character_name_textblock.IsVisible = true;
				character_name_textbox.IsVisible = false;
				character_race_textblock.IsVisible = true;
				character_race_combobox.IsVisible = false;
				character_class_textblock.IsVisible = true;
				character_class_combobox.IsVisible = false;
				Grid.SetColumnSpan(DataGridCurrentSpells, 4);
				Grid.SetColumnSpan(CurrentSpellDescription, 4);
				DataGridAllSpells.IsVisible = false;
				AllSpellDescription.IsVisible = false;
				DataGridCurrentSpells.Columns[0].Width = new DataGridLength(600);
			}
			else
			{
				Resources["IsEdit"] = true;
				Characteristics.UpdatePointText(true);
				TreeSkillsScript.UpdateDisplayPoints();
				character_name_textblock.IsVisible = false;
				character_name_textbox.IsVisible = true;
				character_race_textblock.IsVisible = false;
				character_race_combobox.IsVisible = true;
				character_class_textblock.IsVisible = false;
				character_class_combobox.IsVisible = true;
				DataGridCurrentSpells.Columns[0].Width = new DataGridLength(200);
				Grid.SetColumnSpan(DataGridCurrentSpells, 2);
				Grid.SetColumnSpan(CurrentSpellDescription, 2);
				Grid.SetRowSpan(DataGridAllSpells, 4);
				DataGridAllSpells.IsVisible = true;
				AllSpellDescription.IsVisible = false;
			}
		}

		private async void Settings_Click(object sender, RoutedEventArgs e)
		{
			Settings settings = new();
			await settings.ShowDialog(this);
		}
		// Характеристики
		private void AddCharacteristic_Click(object sender, RoutedEventArgs e)
		{
			Characteristics.AddCharacteristic_Click((sender as Control)?.DataContext);
		}

		private void SubtractCharacteristic_Click(object sender, RoutedEventArgs e)
		{
			Characteristics.SubtractCharacteristic_Click((sender as Control)?.DataContext);
		}
		// Древо Развитие
		private void AddTreeLevel_Click(object sender, RoutedEventArgs e)
		{
			TreeSkillsScript.AddTreeLevel((sender as Control)?.DataContext);
		}

		private void SubtractTreeLevel_Click(object sender, RoutedEventArgs e)
		{
			TreeSkillsScript.SubstractTreeLevel((sender as Control)?.DataContext);
		}
		// Дневник
		// diaryTB — AvaloniaEdit.TextEditor; форматирование (жирный/курсив/размер) и .rtf — см. ViewModels/Diary/RichText.cs.

		private void DiaryTB_SelectionChanged(object? sender, EventArgs e)
		{
			diaryTB.Focus();
			TextChanges textChanges = new();
			textChanges.UpdateFontSizeDisplay();
		}

		private void DiaryTB_MouseMove(object? sender, PointerEventArgs e)
		{
			diaryTB.Focus();
		}

		private void DiaryTB_TextChanged(object? sender, EventArgs e)
		{
			if (diaryTB.SelectionLength == 0)
			{
				TextChanges textChanges = new();
				textChanges.UpdateFontSizeDisplay();
			}
		}

		private void backpack_cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Backpack backpack = new();
			backpack.ChangeComboBoxQuantity();
		}

		// Медяки
		private void Cuprum_tb_PreviewTextInput(object sender, TextInputEventArgs e)
		{
			TextboxProcessing.WholeNumbersOnly(Cuprum_tb, e);
		}
		// Серебреники
		private void Silver_tb_PreviewTextInput(object sender, TextInputEventArgs e)
		{
			TextboxProcessing.WholeNumbersOnly(Silver_tb, e);
		}
		// Золотые
		private void Gold_tb_PreviewTextInput(object sender, TextInputEventArgs e)
		{
			TextboxProcessing.WholeNumbersOnly(Gold_tb, e);
		}
		// Плюс к броне
		private void backpack_plus_tb_PreviewTextInput(object sender, TextInputEventArgs e)
		{
			TextboxProcessing.WholeNumbersOnly(backpack_plus_tb, e);
		}

		// Счёт урона

		private void shield_health_textbox_PreviewTextInput(object sender, TextInputEventArgs e)
		{
			TextboxProcessing.WholeNumbersOnly(shield_health_textbox, e);
		}

		private void DamageTextBox_PreviewTextInput(object sender, TextInputEventArgs e)
		{
			TextboxProcessing.WholeNumbersOnly(sender, e);
		}
		// NOTE: DamageTextBox — шаблонный TextBox внутри ячейки DataGrid (без x:Name),
		// в оригинале Pasting для него вешался через EditingElementStyle/EventSetter,
		// чего в Avalonia DataGridTextColumn нет. Валидацию вставки для DataGrid-ячеек
		// разберём отдельно при портировании InventoryLoot.cs (там же DataGridInventory).

		// Инвентарь
		private void TextBox_PreviewTextInput(object sender, TextInputEventArgs e)
		{
			TextboxProcessing.WholeNumbersOnly(sender, e);
		}
		// NOTE: TextBox_Pasting (проверка, что в буфере только цифры) тоже относится
		// к DataGrid-редактированию инвентаря — переносим в InventoryLoot.cs.

		private void ComboBoxDataGridItemBaffsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ItemBaffsListScript.UpdateValues();
		}
		// Множитель урона магии
		private void DamageMagic_textblock_MouseLeftButtonDown(object sender, PointerPressedEventArgs e)
		{
			if (!e.GetCurrentPoint(DamageMagic_textblock).Properties.IsLeftButtonPressed)
				return;

			DamageMagic_textblock.IsVisible = false;
			DamageMagic_textbox.IsVisible = true;
			DamageMagic_textbox.Text = DamageMagic_textblock.Text;
			DamageMagic_textbox.Focus();
		}

		private void DamageMagic_textbox_PreviewTextInput(object sender, TextInputEventArgs e)
		{
			TextboxProcessing.DoubleNumbersOnly(DamageMagic_textbox, e);
		}

		private void DamageMagic_textbox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				DamageMagic_textblock.IsVisible = true;
				DamageMagic_textbox.IsVisible = false;
				if (string.IsNullOrEmpty(DamageMagic_textbox.Text) ||
					DamageMagic_textbox.Text == "-0" ||
					DamageMagic_textbox.Text == "-" ||
					DamageMagic_textbox.Text == "," ||
					DamageMagic_textbox.Text == "-0,"
					)
				{
					DataManager.DataSave.MultiplyMagicDamage = 0;
					DamageHealth_textblock.Text = "0";
				}
				// Фильтр ввода использует запятую как десятичный разделитель (как в ru-RU Windows). На Linux
				// культура процесса может быть другой (en-US: "1,5" -> 15), поэтому разбираем явно.
				if (double.TryParse(DamageMagic_textbox.Text?.Replace(',', '.'), NumberStyles.Float,
					CultureInfo.InvariantCulture, out double multiplyMagicDamage))
					DataManager.DataSave.MultiplyMagicDamage = multiplyMagicDamage;
			}

			MagicSpells.RepositoryLoad();
		}

		private static readonly SmartDebouncer _debouncer = new();
		private void character_name_textbox_TextChanged(object sender, TextChangedEventArgs e)
		{
			character_name_textblock.Text = character_name_textbox.Text;
			DataManager.Save();
		}
		// Магические пули
		private void MagicBulletCurrent_textblock_MouseLeftButtonDown(object sender, PointerPressedEventArgs e)
		{
			if (!e.GetCurrentPoint(MagicBulletCurrent_textblock).Properties.IsLeftButtonPressed)
				return;

			MagicBulletCurrent_textblock.IsVisible = false;
			MagicBulletCurrent_textbox.IsVisible = true;
			MagicBulletCurrent_textbox.Text = MagicBulletCurrent_textblock.Text;
			MagicBulletCurrent_textbox.Focus();
		}
		private void MagicBulletCurrent_textbox_PreviewTextInput(object sender, TextInputEventArgs e)
		{
			TextboxProcessing.WholeNumbersOnly(MagicBulletCurrent_textbox, e);
		}
		private void MagicBulletCurrent_textbox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				MagicBulletCurrent_textblock.IsVisible = true;
				MagicBulletCurrent_textbox.IsVisible = false;
				if (string.IsNullOrEmpty(MagicBulletCurrent_textbox.Text) ||
					MagicBulletCurrent_textbox.Text == "-0" ||
					MagicBulletCurrent_textbox.Text == "-" ||
					MagicBulletCurrent_textbox.Text == "00" ||
					Convert.ToInt32(MagicBulletCurrent_textbox.Text) < 0
					)
				{
					DataManager.DataSave.SubtractMagicBullet = 0;
					MagicBulletCurrent_textblock.Text = "0";
				}
				else
					DataManager.DataSave.SubtractMagicBullet = MagicSpells.MaxMagicBullet - Convert.ToInt32(MagicBulletCurrent_textbox.Text);
			}
		}

		// Эффекты
		private void AddLevelEffect_Click(object sender, RoutedEventArgs e)
		{
			Effects.AddingLevelEffect(1, (sender as Control)?.DataContext);
		}

		private void SubtractLevelEffect_Click(object sender, RoutedEventArgs e)
		{
			Effects.AddingLevelEffect(-1, (sender as Control)?.DataContext);
		}
		private CurseMagic? _curseMagicWindow;
		private void Cursemagic_menuitem_Click(object sender, RoutedEventArgs e)
		{
			if (_curseMagicWindow != null)
			{
				_curseMagicWindow.Activate();
			}
			else
			{
				_curseMagicWindow = new CurseMagic();
				_curseMagicWindow.Closed += (s, args) => _curseMagicWindow = null;
				_curseMagicWindow.Show();
			}
		}

		private void Gold_tb_TextChanged(object sender, TextChangedEventArgs e)
		{

		}
	}
}
