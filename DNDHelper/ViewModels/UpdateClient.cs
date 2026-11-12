using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DNDHelper.ViewModels
{
    internal sealed record ReleaseAsset(string Name, string Url, long Size, string? Sha256);

    internal sealed record ReleaseInfo(string Tag, string PageUrl, string Notes, IReadOnlyList<ReleaseAsset> Assets);

    /// <summary>Работа с GitHub Releases: запрос последнего релиза, сравнение версий, выбор и скачивание архива.</summary>
    internal static class UpdateClient
    {
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DNDHelper-Updater");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        /// <returns>null, если в репозитории (ещё) нет релизов или он не найден.</returns>
        public static async Task<ReleaseInfo?> GetLatestAsync(string repository, CancellationToken ct = default)
        {
            using HttpResponseMessage response =
                await Http.GetAsync($"https://api.github.com/repos/{repository}/releases/latest", ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();

            await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
            using JsonDocument doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return ParseRelease(doc.RootElement);
        }

        internal static ReleaseInfo ParseRelease(JsonElement root)
        {
            string tag = root.GetProperty("tag_name").GetString() ?? "";
            string page = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? "" : "";
            string notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";

            var assets = new List<ReleaseAsset>();
            if (root.TryGetProperty("assets", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement a in arr.EnumerateArray())
                {
                    string name = a.GetProperty("name").GetString() ?? "";
                    string url = a.GetProperty("browser_download_url").GetString() ?? "";
                    long size = a.TryGetProperty("size", out var s) && s.TryGetInt64(out long l) ? l : 0;
                    string? sha = null;
                    // GitHub отдаёт контрольную сумму в поле digest вида "sha256:<hex>"
                    if (a.TryGetProperty("digest", out var d) && d.ValueKind == JsonValueKind.String)
                    {
                        string? digest = d.GetString();
                        if (digest != null && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                            sha = digest.Substring("sha256:".Length);
                    }
                    if (name.Length > 0 && url.Length > 0)
                        assets.Add(new ReleaseAsset(name, url, size, sha));
                }
            }
            return new ReleaseInfo(tag, page, notes, assets);
        }

        /// <summary>"v13.1", "13.1.0", "13.1.0-beta+abc" -> Version. Приписки (-beta, +хэш) отбрасываются.</summary>
        public static bool TryParseVersion(string? text, out Version version)
        {
            version = new Version(0, 0);
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string s = text.Trim().TrimStart('v', 'V');
            int cut = s.IndexOfAny(new[] { '-', '+', ' ' });
            if (cut >= 0)
                s = s.Substring(0, cut);
            if (s.Length == 0)
                return false;
            if (!s.Contains('.'))
                s += ".0";                           // Version.TryParse требует минимум "major.minor"

            if (!Version.TryParse(s, out Version? parsed))
                return false;

            // new Version(13,0) < new Version(13,0,0): неуказанные компоненты у Version равны -1, из-за чего
            // "13.0" и "13.0.0" считались бы разными версиями. Приводим всё к четырём компонентам.
            version = new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0), Math.Max(parsed.Revision, 0));
            return true;
        }

        public static bool IsNewer(string latestTag, string currentVersion) =>
            TryParseVersion(latestTag, out Version latest)
            && TryParseVersion(currentVersion, out Version current)
            && latest > current;

        /// <summary>Суффикс имени архива под текущую платформу: "-win-x64.zip", "-linux-arm64.zip".</summary>
        public static string PlatformSuffix()
        {
            string os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
            string arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.Arm64 => "arm64",
                Architecture.Arm => "arm",
                Architecture.X86 => "x86",
                _ => "x64",
            };
            return $"-{os}-{arch}.zip";
        }

        public static ReleaseAsset? PickAsset(ReleaseInfo release, string suffix)
        {
            foreach (ReleaseAsset a in release.Assets)
                if (a.Name.StartsWith("DNDHelper", StringComparison.OrdinalIgnoreCase)
                    && a.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return a;
            return null;
        }

        /// <summary>Скачивает архив во временный файл; если у релиза есть sha256 - сверяет и при несовпадении удаляет файл.</summary>
        public static async Task DownloadAsync(ReleaseAsset asset, string destination,
            IProgress<double>? progress = null, CancellationToken ct = default)
        {
            using HttpResponseMessage response =
                await Http.GetAsync(asset.Url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            long total = response.Content.Headers.ContentLength ?? asset.Size;
            using var sha = SHA256.Create();
            await using (Stream src = await response.Content.ReadAsStreamAsync(ct))
            await using (FileStream dst = File.Create(destination))
            {
                var buffer = new byte[81920];
                long done = 0;
                int read;
                while ((read = await src.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                {
                    await dst.WriteAsync(buffer.AsMemory(0, read), ct);
                    sha.TransformBlock(buffer, 0, read, null, 0);
                    done += read;
                    if (total > 0)
                        progress?.Report((double)done / total);
                }
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            }

            if (asset.Sha256 != null)
            {
                string actual = Convert.ToHexString(sha.Hash!);
                if (!actual.Equals(asset.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(destination);
                    throw new InvalidDataException("Контрольная сумма скачанного архива не совпала - файл отброшен.");
                }
            }
        }
    }
}
