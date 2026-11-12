using DNDHelper.ViewModels.Settings;
using DNDHelper.Views;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DNDHelper.ViewModels.Config
{
	internal class ManagerUrls
	{
		public static Dictionary<string, string> _urlData;
		public static void LoadUrls()
		{
			MainWindow main = MainWindow.Instance;
			string pathFile = MainWindow.PathData + $"Cache/{DataManager.DataSave.SelectedRepository}/urls.json";
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
					main.ListBoxUrls.Items.Clear();
					_urlData = JsonSerializer.Deserialize<Dictionary<string, string>>(json, options);
					if (_urlData != null)
					{
						foreach (var item in _urlData)
						{
							main.ListBoxUrls.Items.Add(item.Key);
						}
					}
				}
				catch { }
			}
		}
	}

}
