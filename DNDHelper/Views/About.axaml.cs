using Avalonia.Controls;
using System;
using System.IO;
using System.Reflection;

using DNDHelper.ViewModels.Infrastructure;

namespace DNDHelper.Views
{
	/// <summary>
	/// Логика взаимодействия для About.axaml
	/// </summary>
	public partial class About : Window
	{
		public About()
		{
			InitializeComponent();
			DataContext = new AboutViewModel();
			// Версия берётся из самой сборки (см. AppInfo), а не из Version.txt.
			VersionTextBlock.Text = DNDHelper.ViewModels.AppInfo.Version;
		}
	}
}
