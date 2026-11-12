using Avalonia.Controls;
using DNDHelper.ViewModels.Config;
using DNDHelper.ViewModels.Settings;
using DNDHelper.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace DNDHelper.ViewModels.ReferenceBooks
{
	/// <summary>
	/// Вкладка "Справочники": страница из urls.json открывается во встроенном окне (NativeWebView из
	/// Avalonia.Controls.WebView - в Avalonia 12 он бесплатный и опирается на системный движок: WebView2 на Windows,
	/// WPE/WebKitGTK на Linux, WKWebView на macOS, без встроенного Chromium).
	/// Если встроенный движок недоступен (на Linux не установлены библиотеки WebKit, на Windows сломан WebView2) -
	/// страница открывается в браузере по умолчанию, как раньше.
	/// </summary>
	internal class LoadingReferenceBook
	{
		MainWindow main = MainWindow.Instance;

		private NativeWebView? _webView;
		private bool _webViewUnavailable;

		public LoadingReferenceBook()
		{
			main.ListBoxUrls.SelectionChanged += ListBoxUrls_SelectionChanged;
		}

		// Вызов оставлен для совместимости с MainWindow.axaml.cs. Браузер создаётся лениво при первом выборе
		// страницы, чтобы не тратить время на запуск движка и не рисковать стартом приложения.
		public void InitializeWebView()
		{
		}

		private void ListBoxUrls_SelectionChanged(object? sender, SelectionChangedEventArgs e)
		{
			if (main.ListBoxUrls.SelectedItem == null || ManagerUrls._urlData == null)
				return;

			string selectedKey = main.ListBoxUrls.SelectedItem.ToString() ?? "";
			if (!ManagerUrls._urlData.TryGetValue(selectedKey, out string? url)
				|| !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
				return;

			if (EnsureWebView())
			{
				try
				{
					_webView!.Source = uri;
					return;
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Встроенный браузер не открыл страницу: {ex}");
					_webViewUnavailable = true;
				}
			}

			OpenInSystemBrowser(uri);
		}

		private bool EnsureWebView()
		{
			if (_webView != null)
				return true;
			if (_webViewUnavailable)
				return false;

			if (!IsEngineAvailable(out string reason))
			{
				Debug.WriteLine($"Встроенный браузер недоступен: {reason}");
				_webViewUnavailable = true;
				main.LoadinTextUrl.Text = "Встроенный браузер недоступен в этой системе - страница откроется во внешнем браузере";
				return false;
			}

			try
			{
				var view = new NativeWebView();

				// Данные браузера (кэш, куки) держим в каталоге данных приложения, а не рядом с exe: в
				// портативном режиме папка DNDHelper/ при обновлении заменяется целиком.
				view.EnvironmentRequested += (s, args) =>
				{
					if (args is WindowsWebView2EnvironmentRequestedEventArgs webView2)
						webView2.UserDataFolder = Path.Combine(AppPaths.GetWritableRoot(), "webview2");
					else if (args is GtkWebViewEnvironmentRequestedEventArgs gtk)
						gtk.ExperimentalOffscreen = true;   // рисуем внутри окна Avalonia, а не отдельным X11-окном поверх
				};
				view.NavigationStarted += (s, args) => main.LoadingIcon.IsVisible = true;
				view.NavigationCompleted += (s, args) => main.LoadingIcon.IsVisible = false;

				main.ReferenceHost.Child = view;
				main.ReferenceHost.IsVisible = true;
				main.LoadinTextUrl.IsVisible = false;
				_webView = view;
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Не удалось создать встроенный браузер: {ex}");
				_webViewUnavailable = true;
				main.LoadinTextUrl.Text = "Встроенный браузер недоступен в этой системе - страница откроется во внешнем браузере";
				return false;
			}
		}

		/// <summary>
		/// На Linux NativeWebView сам выбирает WPE WebKit или WebKitGTK (4.1 + GTK 3), но если ни одной из них нет,
		/// нативная ошибка случается уже при создании окна браузера. Поэтому заранее смотрим, находятся ли библиотеки.
		/// </summary>
		private static bool IsEngineAvailable(out string reason)
		{
			reason = "";
			if (!OperatingSystem.IsLinux())
				return true;   // Windows: WebView2, macOS: WKWebView - есть в системе

			bool gtkStack = CanLoad("libgtk-3.so.0")
				&& (CanLoad("libwebkit2gtk-4.1.so.0") || CanLoad("libwebkit2gtk-4.0.so.37"));
			bool wpe = CanLoad("libWPEWebKit-2.0.so.1") || CanLoad("libWPEWebKit-1.1.so.0");
			if (gtkStack || wpe)
				return true;

			reason = "не найдены libwebkit2gtk-4.1 + libgtk-3 (WebKitGTK) и libWPEWebKit (WPE)";
			return false;
		}

		private static bool CanLoad(string library)
		{
			if (NativeLibrary.TryLoad(library, out IntPtr handle))
			{
				NativeLibrary.Free(handle);
				return true;
			}
			return false;
		}

		private static void OpenInSystemBrowser(Uri uri)
		{
			try
			{
				// UseShellExecute: true - кроссплатформенный способ открыть ссылку в системном браузере по
				// умолчанию (на Linux это xdg-open).
				Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Не удалось открыть ссылку в браузере: {ex}");
			}
		}
	}
}
