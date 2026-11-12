using Avalonia.Controls;
using Avalonia.Input;
using System;

namespace DNDHelper.ViewModels
{
	public class TextboxProcessing
	{
		public static void WholeNumbersOnly(object sender, TextInputEventArgs e)
		{
			var textBox = sender as TextBox;
			int caretIndex = textBox.CaretIndex;
			// В Avalonia у пустого TextBox Text == null (в WPF - ""), поэтому читаем через ?? "".
			string text = textBox.Text ?? "";
			string newText = text.Replace(" ", "");
			if (text != newText)
			{
				textBox.Text = newText;
				text = newText;
				textBox.CaretIndex = Math.Min(caretIndex, newText.Length);
			}
			if (string.IsNullOrEmpty(e.Text))
			{
				e.Handled = false;
				return;
			}

			if (char.IsDigit(e.Text, 0))
			{
				e.Handled = false;
				return;
			}
			if (e.Text == "-")
			{
				if (textBox.SelectionStart == 0 && !text.Contains("-"))
				{
					e.Handled = false;
				}
				else
				{
					e.Handled = true;
				}
				return;
			}
			e.Handled = true;
		}
		public static void DoubleNumbersOnly(object sender, TextInputEventArgs e)
		{
			var textBox = sender as TextBox;
			int caretIndex = textBox.CaretIndex;
			// В Avalonia у пустого TextBox Text == null (в WPF - ""), поэтому читаем через ?? "".
			string text = textBox.Text ?? "";
			string newText = text.Replace(" ", "");
			if (text != newText)
			{
				textBox.Text = newText;
				text = newText;
				textBox.CaretIndex = Math.Min(caretIndex, newText.Length);
			}

			if (string.IsNullOrEmpty(e.Text))
			{
				e.Handled = false;
				return;
			}

			if (e.Text == "-")
			{
				bool isAtStart = textBox.SelectionStart == 0;
				bool hasMinus = text.Contains("-");
				e.Handled = !(isAtStart && !hasMinus);
				return;
			}

			if (e.Text == ",")
			{
				bool hasComma = text.Contains(",");
				bool insertingBeforeMinus =
					textBox.SelectionStart > 0 &&
					text[textBox.SelectionStart - 1] == '-';
				e.Handled = hasComma || insertingBeforeMinus;
				return;
			}

			if (char.IsDigit(e.Text, 0))
			{
				string simulatedText =
					text.Substring(0, Math.Min(textBox.SelectionStart, text.Length)) +
					e.Text +
					text.Substring(Math.Min(textBox.SelectionStart, text.Length));
				string numPart = simulatedText.StartsWith("-") ? simulatedText.Substring(1) : simulatedText;
				if (numPart.StartsWith("0") && numPart.Length > 1 && numPart[1] != ',')
				{
					e.Handled = true;
					return;
				}

				e.Handled = false;
				return;
			}
			e.Handled = true;
		}

		/// <summary>
		/// Замена WPF DataObject.Pasting: в Avalonia нет отменяемого события вставки,
		/// поэтому вместо валидации содержимого буфера блокируем саму комбинацию Ctrl+V.
		/// Используется там, где в оригинале Pasting безусловно вызывал e.CancelCommand().
		/// </summary>
		public static void BlockPaste(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.V && e.KeyModifiers == KeyModifiers.Control)
			{
				e.Handled = true;
			}
		}

		public static void IsTextTrimmed()
		{

		}
	}
}
