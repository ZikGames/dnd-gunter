using Avalonia.Controls;
using Avalonia.Interactivity;
using DNDHelper.Views;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using static DNDHelper.ViewModels.Character.Skills;

namespace DNDHelper.ViewModels.MagicSpells
{
	internal class MagicSearch
	{
		private static MainWindow main = MainWindow.Instance;
		private static int rollSortState = 0;
		private static string currentSortMember = null;
		public MagicSearch()
		{
			main.CastSearch_textbox.TextChanged += CastSearch_textbox_TextChanged;
			main.CastSearch_Type_cb.SelectionChanged += SelectionChanged;
            main.SchoolSelect_Search_cb.SelectionChanged += SelectionChanged;
			main.Damage_Search_cb.SelectionChanged += SelectionChanged;
			main.Roll_Search_cb.SelectionChanged += SelectionChanged;
			main.LevelCast_Search_cb.SelectionChanged += SelectionChanged;
            main.NatureCast_Search_cb.Click += NatureCast_Search_cb_Checked;
			main.DataGridCurrentSpells.Sorting += DataGridCurrentSpells_Sorting;
			main.DataGridAllSpells.Sorting += DataGridAllSpells_Sorting;
            main.PackCast_Search_cb.Click += PackCast_Search_cb_Click;

        }


        private void DataGridAllSpells_Sorting(object sender, DataGridColumnEventArgs e)
		{
			e.Handled = true;

			if (currentSortMember != e.Column.SortMemberPath)
			{
				currentSortMember = e.Column.SortMemberPath;
				rollSortState = 0;
			}

			rollSortState = (rollSortState == 1) ? 2 : 1;

			// У классического Avalonia.Controls.DataGridColumn (в отличие от WPF и от
			// TreeDataGridColumn) нет свойства SortDirection вообще — визуальную стрелку
			// сортировки в заголовке колонки поставить нечем, но сама сортировка (через
			// rollSortState/currentSortMember ниже, в CastsFilter) не зависит от него и
			// работает как прежде.

			CastsFilter();
		}

		private void DataGridCurrentSpells_Sorting(object sender, DataGridColumnEventArgs e)
		{
			DataGridAllSpells_Sorting(sender, e);
		}

		private static List<Cast> filtersSpellAll = new List<Cast>();

		public static void CastsFilter()
		{
			filtersSpellAll.Clear();
			foreach (var filter in MagicSpells.AllCasts)
			{
				filtersSpellAll.Add(filter);
			}

			FilterName();
			FilterType();
			FilterSchool();
			FilterDamage();
			FilterRoll();
			FilterLevel();
			FilterNature();
			FilterPack();



            MagicSpells.CurrentCasts.Clear();
		    MagicSpells.CurrentAllCasts.Clear();
			foreach (var filter in filtersSpellAll)
				if (MagicSpells.CurrentCastsNames.Contains(filter.SpellName))
					MagicSpells.CurrentCasts.Add(filter);
				else
					MagicSpells.CurrentAllCasts.Add(filter);


		}

		private static void FilterName()
		{
			filtersSpellAll = filtersSpellAll.Where((x) => x.SpellName.ToLower().Contains((main.CastSearch_textbox.Text ?? "").ToLower())).ToList();

			if (currentSortMember == nameof(Cast.SpellName))
			{
				if (rollSortState == 1)
					filtersSpellAll = filtersSpellAll.OrderBy(x => x.SpellName).ToList();
				else if (rollSortState == 2)
					filtersSpellAll = filtersSpellAll.OrderByDescending(x => x.SpellName).ToList();
			}
		}
		private static void FilterType()
		{
			if (main.CastSearch_Type_cb.SelectedIndex != 0)
				filtersSpellAll = filtersSpellAll.Where((x) => x.NumberType == (main.CastSearch_Type_cb.SelectedIndex - 1).ToString()).ToList();
		}
		private static void FilterSchool()
		{

			if (main.SchoolSelect_Search_cb.SelectedIndex != 0)
				filtersSpellAll = filtersSpellAll.Where((x) => x.SpellSchool == MagicSpells.NumberToSchool((main.SchoolSelect_Search_cb.SelectedIndex - 1).ToString())).ToList();

			if (currentSortMember == nameof(Cast.SpellSchool))
			{
				if (rollSortState == 1)
					filtersSpellAll = filtersSpellAll.OrderBy(x => x.SpellSchool).ToList();
				else if (rollSortState == 2)
					filtersSpellAll = filtersSpellAll.OrderByDescending(x => x.SpellSchool).ToList();
			}
		}
		private static void FilterDamage()
		{
            if (currentSortMember == nameof(Cast.SpellDamage))
            {
                if (rollSortState == 1)
                    filtersSpellAll = filtersSpellAll.OrderBy(x => x.SpellDamage).ToList();
                else if (rollSortState == 2)
                    filtersSpellAll = filtersSpellAll.OrderByDescending(x => x.SpellDamage).ToList();
            }

            if (main.Damage_Search_cb.SelectedIndex == 0)
				return;

			if (main.Damage_Search_cb.SelectedIndex == 1)
				filtersSpellAll = filtersSpellAll.Where(x => x.SpellDamage > 0).ToList();
			else if (main.Damage_Search_cb.SelectedIndex == 2)
				filtersSpellAll = filtersSpellAll.Where(x => x.SpellDamage == 0).ToList();

		}
		private static void FilterRoll()
		{
			if (main.Roll_Search_cb.SelectedIndex != 0)
			{
				if (main.Roll_Search_cb.SelectedIndex == 1)
					filtersSpellAll = filtersSpellAll.Where(x => x.SpellRoll > 0).ToList();
				else if (main.Roll_Search_cb.SelectedIndex == 2)
					filtersSpellAll = filtersSpellAll.Where(x => x.SpellRoll < 0).ToList();
				else if (main.Roll_Search_cb.SelectedIndex == 3)
					filtersSpellAll = filtersSpellAll.Where(x => x.SpellRoll == 0).ToList();
			}

			if (currentSortMember == nameof(Cast.SpellRoll))
			{
				if (rollSortState == 1)
					filtersSpellAll = filtersSpellAll.OrderBy(x => x.SpellRoll).ToList();
				else if (rollSortState == 2)
					filtersSpellAll = filtersSpellAll.OrderByDescending(x => x.SpellRoll).ToList();
            }


		}
		private static void FilterLevel()
		{

			if (main.LevelCast_Search_cb.SelectedIndex != 0)
				filtersSpellAll = filtersSpellAll.Where((x) => x.SpellLevel == (main.LevelCast_Search_cb.SelectedIndex - 1).ToString()).ToList();

			if (currentSortMember == nameof(Cast.SpellLevel))
			{
				if (rollSortState == 1)
					filtersSpellAll = filtersSpellAll.OrderBy(x => x.SpellLevel).ToList();
				else if (rollSortState == 2)
					filtersSpellAll = filtersSpellAll.OrderByDescending(x => x.SpellLevel).ToList();
			}
		}

		private static void FilterNature()
		{
			if (main.NatureCast_Search_cb.IsChecked.Value)
				filtersSpellAll = filtersSpellAll.Where((x) => x.IsNature).ToList();
		}

        private static void FilterPack()
        {
            if (main.PackCast_Search_cb.IsChecked.Value)
                filtersSpellAll = filtersSpellAll.Where((x) => x.IsPack).ToList();
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			CastsFilter();
		}

		private void CastSearch_textbox_TextChanged(object sender, TextChangedEventArgs e)
		{
			CastsFilter();
		}

        private void PackCast_Search_cb_Click(object sender, RoutedEventArgs e)
        {
            CastsFilter();
        }

        private void NatureCast_Search_cb_Checked(object sender, RoutedEventArgs e)
		{
			CastsFilter();
		}
	}
}
