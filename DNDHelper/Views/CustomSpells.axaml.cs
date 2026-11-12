using Avalonia.Controls;
using Avalonia.Input;
using DNDHelper.ViewModels;
using DNDHelper.ViewModels.Infrastructure;

namespace DNDHelper.Views
{
	/// <summary>
	/// Логика взаимодействия для CustomSpells.axaml
	/// </summary>
	public partial class CustomSpells : Window
	{
		public CustomSpells()
		{
			InitializeComponent();
			// Фильтры ввода: в Avalonia TextBox сам обрабатывает TextInput раньше обычных (bubble) обработчиков,
			// поэтому, как PreviewTextInput в WPF, вешаем их на туннельную стадию.
			Damage_Custom_textbox.AddHandler(InputElement.TextInputEvent, Damage_Custom_textbox_PreviewTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);

			DataContext = new CustomSpellsViewModel();
			Damage_Custom_textbox.KeyDown += TextboxProcessing.BlockPaste;
		}

		private void Damage_Custom_textbox_PreviewTextInput(object sender, TextInputEventArgs e)
		{
			TextboxProcessing.WholeNumbersOnly(Damage_Custom_textbox, e);
		}
	}
}
