using System.Diagnostics;
using System.IO.Compression;

namespace DNDUpdater
{
    internal static class Program
    {
        // Папки, которые приходят из архива и подменяются целиком. Data/ и portable.txt в списке нет - не трогаем.
        private static readonly string[] ManagedFolders = { "DNDHelper", "Updater" };

        private static string _logPath = Path.Combine(Path.GetTempPath(), "dndhelper-updater.log");

        private static int Main(string[] args)
        {
            string? pidArg = Arg(args, "--pid"), zip = Arg(args, "--zip"), root = Arg(args, "--root"), exeRel = Arg(args, "--exe");
            if (zip == null || root == null || exeRel == null)
            {
                Console.Error.WriteLine("Использование: DNDUpdater --pid <pid> --zip <архив> --root <корень> --exe <относительный путь к exe>");
                return 2;
            }

            string dataDir = Path.Combine(root, "Data");
            try
            {
                Directory.CreateDirectory(dataDir);
                _logPath = Path.Combine(dataDir, "updater.log");
            }
            catch { /* лог останется во временной папке */ }

            string appExe = Path.Combine(root, exeRel);
            string staging = Path.Combine(Path.GetTempPath(), "dndhelper-staging-" + Guid.NewGuid().ToString("N"));
            var backups = new List<(string Original, string Backup)>();
            bool success = false;

            try
            {
                Log($"Старт: pid={pidArg}, zip={zip}, root={root}");
                WaitForExit(pidArg);

                ZipFile.ExtractToDirectory(zip, staging);
                string source = FindPayloadRoot(staging)
                    ?? throw new InvalidDataException("В архиве нет папки DNDHelper/ - это не архив обновления.");

                foreach (string name in ManagedFolders)
                {
                    string from = Path.Combine(source, name);
                    if (!Directory.Exists(from))
                        continue;

                    string to = Path.Combine(root, name);
                    string backup = to + ".old";
                    if (Directory.Exists(backup))
                        Retry(() => Directory.Delete(backup, true));

                    if (Directory.Exists(to))
                    {
                        Retry(() => Directory.Move(to, backup));
                        backups.Add((to, backup));
                    }
                    CopyDirectory(from, to);
                    Log($"Заменено: {name}");
                }

                MakeExecutable(Path.Combine(root, exeRel));
                MakeExecutable(Path.Combine(root, "Updater", OperatingSystem.IsWindows() ? "DNDUpdater.exe" : "DNDUpdater"));
                success = true;
                Log("Обновление применено.");
            }
            catch (Exception e)
            {
                Log("ОШИБКА: " + e);
                Rollback(backups);
            }
            finally
            {
                if (success)
                    foreach (var (_, backup) in backups)
                        TryDelete(backup);
                TryDelete(staging);
                try { File.Delete(zip); } catch { }
            }

            // Запускаем приложение в любом случае: при удаче - новое, при откате - прежнее.
            try
            {
                var psi = new ProcessStartInfo(appExe)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(appExe) ?? root,
                };
                Process.Start(psi);
            }
            catch (Exception e)
            {
                Log("Не удалось запустить приложение: " + e.Message);
                return 3;
            }
            return success ? 0 : 1;
        }

        private static string? Arg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name)
                    return args[i + 1];
            return null;
        }

        private static void WaitForExit(string? pidArg)
        {
            if (!int.TryParse(pidArg, out int pid))
                return;
            try
            {
                using Process p = Process.GetProcessById(pid);
                if (!p.WaitForExit(TimeSpan.FromSeconds(120)))
                    throw new TimeoutException("Приложение не завершилось за 120 секунд.");
            }
            catch (ArgumentException) { /* процесса уже нет */ }
        }

        /// <summary>Архив может содержать файлы в корне или внутри одной папки - ищем каталог, где лежит DNDHelper/.</summary>
        internal static string? FindPayloadRoot(string dir, int depth = 0)
        {
            if (Directory.Exists(Path.Combine(dir, "DNDHelper")))
                return dir;
            if (depth >= 2)
                return null;
            foreach (string sub in Directory.GetDirectories(dir))
            {
                string? found = FindPayloadRoot(sub, depth + 1);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static void Rollback(List<(string Original, string Backup)> backups)
        {
            // Идём в обратном порядке: сначала убираем частично скопированное, потом возвращаем прежнее.
            for (int i = backups.Count - 1; i >= 0; i--)
            {
                var (original, backup) = backups[i];
                try
                {
                    if (Directory.Exists(original))
                        Directory.Delete(original, true);
                    if (Directory.Exists(backup))
                        Directory.Move(backup, original);
                    Log($"Откат: {original}");
                }
                catch (Exception e)
                {
                    Log($"Откат {original} не удался: {e.Message}");
                }
            }
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
            foreach (string dir in Directory.GetDirectories(source))
                CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }

        /// <summary>ZipFile.ExtractToDirectory не всегда сохраняет бит исполняемости - выставляем явно (Linux/macOS).</summary>
        private static void MakeExecutable(string path)
        {
            if (OperatingSystem.IsWindows() || !File.Exists(path))
                return;
            File.SetUnixFileMode(path, File.GetUnixFileMode(path)
                | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
        }

        /// <summary>На Windows антивирус или индексатор могут секунду-две держать файлы - пробуем несколько раз.</summary>
        private static void Retry(Action action)
        {
            for (int attempt = 1; ; attempt++)
            {
                try { action(); return; }
                catch (IOException) when (attempt < 10) { Thread.Sleep(500); }
                catch (UnauthorizedAccessException) when (attempt < 10) { Thread.Sleep(500); }
            }
        }

        private static void TryDelete(string dir)
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
        }

        private static void Log(string message)
        {
            try { File.AppendAllText(_logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}"); } catch { }
        }
    }
}
