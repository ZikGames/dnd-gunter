using DNDHelper.ViewModels.Character;
using DNDHelper.Views;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace DNDHelper.ViewModels.Settings
{
    class GlobalMultiply
    {
        public static globalMultiplyClass data = new globalMultiplyClass();

        public static void Update()
        {
            string pathFile = MainWindow.PathData + $"Cache/{DataManager.DataSave.SelectedRepository}/MultiplyGlobal.json";
            if (File.Exists(pathFile))
            {
                Debug.WriteLine("sds");
                string json = File.ReadAllText(pathFile);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                try
                {
                    data = JsonSerializer.Deserialize<globalMultiplyClass>(json, options);
                    Debug.WriteLine(data.GlobalMultiply);
                }
                catch
                {
                    data = new();
                }
            }
            else
                data = new();
            Health.HealthUpdate();
        }


        public class globalMultiplyClass
        {
            public double GlobalMultiply { get; set; } = 1;

            public double MultiplyMentKD { get; set; } = 1;
        }
    }
}
