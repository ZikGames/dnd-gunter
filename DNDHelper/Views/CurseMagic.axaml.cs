using Avalonia.Controls;
using Avalonia.Input;
using DNDHelper.ViewModels;
using DNDHelper.ViewModels.Infrastructure;
using DNDHelper.ViewModels.MagicSpells;
using System.Linq;
using static DNDHelper.ViewModels.Character.Skills;

namespace DNDHelper.Views
{
	/// <summary>
	/// Логика взаимодействия для CurseMagic.axaml
	/// </summary>
	public partial class CurseMagic : Window
	{
		// Окно теперь создаётся по требованию через MainWindow._curseMagicWindow
		// (см. Cursemagic_menuitem_Click в MainWindow.axaml.cs). Раньше здесь стояло
		// public static CurseMagic curseMagic = new(); — это был WPF-синглтон, который
		// создавал полноценное окно (InitializeComponent, подписки на события,
		// заполнение ItemsSource) сразу при первом обращении к типу CurseMagic,
		// ещё до того как пользователь открыл меню. Поле нигде не использовалось —
		// убрано как мёртвый/лишний код.

		public CurseMagic()
		{
			InitializeComponent();
			// Фильтры ввода: в Avalonia TextBox сам обрабатывает TextInput раньше обычных (bubble) обработчиков,
			// поэтому, как PreviewTextInput в WPF, вешаем их на туннельную стадию.
			ColdBoosting_DammageSpell_textbox.AddHandler(InputElement.TextInputEvent, DammageSpell_textbox_PreviewTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);
			GiftOfHealing_quantity_textbox.AddHandler(InputElement.TextInputEvent, GiftOfHealing_quantity_textbox_PreviewTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);
			GiftOfDark_quantity_textbox.AddHandler(InputElement.TextInputEvent, GiftOfDark_quantity_textbox_PreviewTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);

			DataContext = new CurseMagicViewModel();
			ColdBoosting_SpellName_cb.ItemsSource = MagicSpells.AllCasts.Where(c => c.SpellDamage > 0);

			// Замена запрета вставки (DataObject.Pasting в WPF) — см. шпаргалку п.9.
			ColdBoosting_DammageSpell_textbox.KeyDown += TextboxProcessing.BlockPaste;
			GiftOfHealing_quantity_textbox.KeyDown += TextboxProcessing.BlockPaste;
			GiftOfDark_quantity_textbox.KeyDown += TextboxProcessing.BlockPaste;
		}

		private void DammageSpell_textbox_PreviewTextInput(object sender, TextInputEventArgs e)
		{
			TextboxProcessing.WholeNumbersOnly(ColdBoosting_DammageSpell_textbox, e);
			ColdBoosting_SpellName_cb.SelectedIndex = -1;
		}

		private void DammageSpell_textbox_TextChanged(object sender, TextChangedEventArgs e)
		{
			MagicCurse.ColdBoosting(this);
		}

		private void Consumables_cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			MagicCurse.ColdBoosting(this);
		}

		private void ColdBoosting_SpellName_cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ColdBoosting_SpellName_cb.SelectedItem is Cast selectedSpell)
			{
				ColdBoosting_DammageSpell_textbox.Text = selectedSpell.SpellDamage.ToString();
			}
		}

		private void GiftOfHealing_quantity_textbox_PreviewTextInput(object sender, TextInputEventArgs e)
		{
			TextboxProcessing.WholeNumbersOnly(GiftOfHealing_quantity_textbox, e);
			MagicCurse.GiftOfHealing(this);
		}

		private void GiftOfHealing_quantity_textbox_TextChanged(object sender, TextChangedEventArgs e)
		{
			MagicCurse.GiftOfHealing(this);
		}

		private void GiftOfHealing_Consumables_cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			MagicCurse.GiftOfHealing(this);
		}

		private void GiftOfDark_quantity_textbox_PreviewTextInput(object sender, TextInputEventArgs e)
		{
			TextboxProcessing.WholeNumbersOnly(GiftOfDark_quantity_textbox, e);
		}

		private void GiftOfDark_Consumables_cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			MagicCurse.GiftOfDark(this);
		}

		private void GiftOfDark_quantity_textbox_TextChanged(object sender, TextChangedEventArgs e)
		{
			MagicCurse.GiftOfDark(this);
		}
	}
}
