using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ChassieuVolleyTournament
{
    public enum TunnelState
    {
        Stopped,
        Downloading,
        Starting,
        Online,
        Failed
    }

    /// ------------------------------------------------------------------
    /// Makes the referee web server reachable from ANY network (4G, other
    /// Wi-Fi...) by running a Cloudflare "Quick Tunnel":
    ///
    ///     cloudflared tunnel --url http://127.0.0.1:PORT
    ///
    /// It needs no account, no router configuration and no admin rights:
    /// cloudflared opens an OUTGOING connection to Cloudflare and gives a
    /// public https://xxxx.trycloudflare.com address that forwards to this
    /// PC. cloudflared.exe is downloaded once from the official GitHub
    /// releases (into %LOCALAPPDATA%\ChassieuVolleyTournament) if it is
    /// not already next to the application or in the PATH.
    ///
    /// Events are raised from background threads.
    /// ------------------------------------------------------------------
    public class TunnelManager : IDisposable
    {
        #region ---- Properties ----

        private const string DownloadBase = "https://github.com/cloudflare/cloudflared/releases/latest/download/";

        private static readonly Regex UrlRegex = new Regex(
            @"https://[a-z0-9][a-z0-9\-]*\.trycloudflare\.com",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly object processLock = new object();
        private Process process;
        private bool disposed;

        /// <summary>State changed (state, message to show to the user).</summary>
        public event Action<TunnelState, string> StateChanged;

        /// <summary>Public address changed (null when the tunnel went down).</summary>
        public event Action<string> UrlChanged;

        public string PublicUrl { get; private set; }
        public TunnelState State { get; private set; }

        #endregion

        #region ---- Start ----

        public void Start(int port)
        {
            Task.Run(() => RunAsync(port));
        }

        private async Task RunAsync(int port)
        {
            try
            {
                string exe = FindExecutable();

                if (exe == null)
                {
                    SetState(TunnelState.Downloading, "Téléchargement de cloudflared (une seule fois)...");
                    exe = await DownloadAsync();
                    if (exe == null) return;    // state already set to Failed
                }

                int failures = 0;

                while (!cts.IsCancellationRequested)
                {
                    // Some networks block QUIC (UDP): after a failure, try plain HTTP/2.
                    string protocol = (failures % 2 == 1) ? "http2" : null;

                    bool gotUrl = await RunOnce(exe, port, protocol);
                    if (cts.IsCancellationRequested) break;

                    failures = gotUrl ? 0 : failures + 1;

                    if (failures >= 6)
                    {
                        SetState(TunnelState.Failed,
                            "Connexion Internet impossible. Vérifiez le réseau (le lien réseau local reste utilisable).");
                        return;
                    }

                    SetState(TunnelState.Starting, "Reconnexion en cours...");
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(20, 2 + 3 * failures)), cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                AppPaths.Log("Tunnel : erreur : " + ex);
                SetState(TunnelState.Failed, "Erreur du tunnel : " + ex.Message);
            }
        }

        /// ---------------------------------------------------------------
        /// Runs cloudflared once until it exits. Returns true if it managed
        /// to publish an address (i.e. the connection worked for a while).
        /// ---------------------------------------------------------------
        private async Task<bool> RunOnce(string exe, int port, string protocol)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "tunnel --no-autoupdate"
                          + (protocol != null ? " --protocol " + protocol : "")
                          + " --url http://127.0.0.1:" + port
                          + " --http-host-header localhost:" + port,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            bool gotUrl = false;
            string lastError = null;
            var exited = new TaskCompletionSource<bool>();

            var p = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            DataReceivedEventHandler onLine = (s, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;

                System.Text.RegularExpressions.Match m = UrlRegex.Match(e.Data);
                if (m.Success && !gotUrl)
                {
                    gotUrl = true;
                    string url = m.Value.ToLowerInvariant();
                    PublicUrl = url;

                    AppPaths.Log("Tunnel : adresse publique " + url);
                    UrlChanged?.Invoke(url);
                    SetState(TunnelState.Online, "Lien créé, vérification en cours...");
                    Task.Run(() => VerifyReachableAsync(url));
                }
                else if (e.Data.IndexOf("ERR", StringComparison.Ordinal) >= 0
                      || e.Data.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    lastError = e.Data;
                    AppPaths.Log("Tunnel : " + e.Data);
                }
            };

            p.OutputDataReceived += onLine;
            p.ErrorDataReceived += onLine;
            p.Exited += (s, e) => exited.TrySetResult(true);

            lock (processLock)
            {
                if (disposed) return false;
                p.Start();
                process = p;
            }

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            SetState(TunnelState.Starting, "Connexion au tunnel Internet...");

            var clock = Stopwatch.StartNew();

            while (!exited.Task.IsCompleted && !cts.IsCancellationRequested)
            {
                await Task.WhenAny(exited.Task, Task.Delay(1000));

                // No address after 45 s: this attempt is stuck, retry (with another protocol).
                if (!gotUrl && clock.Elapsed > TimeSpan.FromSeconds(45))
                {
                    AppPaths.Log("Tunnel : pas d'adresse après 45 s, nouvel essai");
                    break;
                }
            }

            Kill(p);

            if (PublicUrl != null)
            {
                PublicUrl = null;
                UrlChanged?.Invoke(null);
            }

            if (!gotUrl && lastError != null)
                AppPaths.Log("Tunnel : dernière erreur : " + lastError);

            return gotUrl;
        }

        /// ------------------------------------------------------------
        /// A new trycloudflare.com name can take a few seconds to be
        /// reachable: poll it, so the staff knows when it really works.
        /// ------------------------------------------------------------
        private async Task VerifyReachableAsync(string url)
        {
            try
            {
                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) })
                {
                    for (int i = 0; i < 30 && !cts.IsCancellationRequested && PublicUrl == url; i++)
                    {
                        try
                        {
                            HttpResponseMessage response = await http.GetAsync(url + "/api/ping");
                            if (response.IsSuccessStatusCode)
                            {
                                if (PublicUrl == url)
                                    SetState(TunnelState.Online, "En ligne : utilisable depuis n'importe quel réseau");
                                return;
                            }
                        }
                        catch
                        {
                            // not reachable yet
                        }

                        await Task.Delay(3000);
                    }
                }

                if (PublicUrl == url)
                    SetState(TunnelState.Online, "Lien créé, mais pas encore joignable : patientez ou testez-le en 4G");
            }
            catch (Exception ex)
            {
                AppPaths.Log("Tunnel : vérification impossible : " + ex.Message);
            }
        }

        #endregion

        #region ---- cloudflared executable ----

        private static string FindExecutable()
        {
            var candidates = new System.Collections.Generic.List<string>
            {
                Path.Combine(AppPaths.BaseDir, "cloudflared.exe"),
                Path.Combine(AppPaths.DataDir, "cloudflared.exe")
            };

            string path = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(path))
            {
                foreach (string dir in path.Split(Path.PathSeparator))
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(dir))
                            candidates.Add(Path.Combine(dir.Trim(), "cloudflared.exe"));
                    }
                    catch (ArgumentException)
                    {
                        // invalid path entry
                    }
                }
            }

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate)) return candidate;
            }

            return null;
        }

        /// ---------------------------------------------------------
        /// Downloads cloudflared.exe from the official GitHub release.
        /// Returns the path, or null (state set to Failed) on error.
        /// ---------------------------------------------------------
        private async Task<string> DownloadAsync()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                string fileName = Environment.Is64BitOperatingSystem
                    ? "cloudflared-windows-amd64.exe"
                    : "cloudflared-windows-386.exe";

                string target = Path.Combine(AppPaths.DataDir, "cloudflared.exe");
                string temp = target + ".part";

                using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
                using (HttpResponseMessage response = await http.GetAsync(DownloadBase + fileName, HttpCompletionOption.ResponseHeadersRead, cts.Token))
                {
                    response.EnsureSuccessStatusCode();

                    long total = response.Content.Headers.ContentLength ?? -1;

                    using (Stream input = await response.Content.ReadAsStreamAsync())
                    using (var output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[81920];
                        long received = 0;
                        int lastPercent = -1;
                        int count;

                        while ((count = await input.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                        {
                            output.Write(buffer, 0, count);
                            received += count;

                            if (total > 0)
                            {
                                int percent = (int)(received * 100 / total);
                                if (percent != lastPercent)
                                {
                                    lastPercent = percent;
                                    SetState(TunnelState.Downloading, "Téléchargement de cloudflared... " + percent + " %");
                                }
                            }
                        }
                    }
                }

                // Sanity check: a Windows executable of several MB.
                var info = new FileInfo(temp);
                if (info.Length < 5000000)
                    throw new InvalidDataException("fichier téléchargé incomplet (" + info.Length + " octets)");

                if (File.Exists(target)) File.Delete(target);
                File.Move(temp, target);

                AppPaths.Log("cloudflared téléchargé : " + target);
                return target;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                AppPaths.Log("Téléchargement cloudflared impossible : " + ex);
                SetState(TunnelState.Failed,
                    "Téléchargement impossible (" + ex.Message + "). Placez cloudflared.exe à côté de l'application.");
                return null;
            }
        }

        #endregion

        #region ---- Helpers ----

        private void SetState(TunnelState state, string message)
        {
            State = state;
            StateChanged?.Invoke(state, message);
        }

        private static void Kill(Process p)
        {
            try
            {
                if (!p.HasExited) p.Kill();
            }
            catch
            {
                // already gone
            }
        }

        public void Dispose()
        {
            cts.Cancel();

            lock (processLock)
            {
                disposed = true;
                if (process != null) Kill(process);
            }
        }

        #endregion
    }
}
