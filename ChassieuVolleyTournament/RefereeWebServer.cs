using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChassieuVolleyTournament
{
    /// ----------------------------------------------------------------
    /// Small web server used by referees (from a phone) to enter scores.
    ///
    ///   GET  /            -> the referee web page (Web/referee.html)
    ///   POST /api/state   -> current score of a match        (key)
    ///   POST /api/score   -> +1 point for a team             (key, team, rid)
    ///   POST /api/undo    -> cancels the last point          (key, rid)
    ///
    /// All reads/writes of tournament data are executed on the UI thread
    /// (ISynchronizeInvoke), so there is no concurrent access to the
    /// tournament objects. Scores are stored in the Match objects: the
    /// server keeps no copy that could get out of sync.
    /// ----------------------------------------------------------------
    public class RefereeWebServer : IDisposable
    {
        #region ---- Types ----

        /// Per-key data that is not part of the match itself.
        private class KeyState
        {
            public readonly Stack<int> History = new Stack<int>();
            public readonly Queue<string> RecentIds = new Queue<string>();
            public readonly HashSet<string> RecentIdSet = new HashSet<string>();
        }

        private class Attempts
        {
            public int Count;
            public DateTime WindowStart;
            public DateTime BlockedUntil;
        }

        private class ApiResult
        {
            public int Status = 200;
            public string Json;
            public bool InvalidKey;
        }

        #endregion

        #region ---- Properties ----

        private const int MaxFailures = 15;
        private const int MaxHistory = 500;
        private const int MaxRecentIds = 64;
        private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(5);

        private readonly ISynchronizeInvoke ui;
        private HttpListener listener;
        private volatile bool stopping;

        // Only touched from the UI thread
        private readonly Dictionary<string, KeyState> states = new Dictionary<string, KeyState>();

        // Touched from worker threads
        private readonly Dictionary<string, Attempts> failures = new Dictionary<string, Attempts>();
        private readonly object failuresLock = new object();

        private static string cachedPage;

        /// <summary>TCP port the server listens on (valid after Start succeeded).</summary>
        public int Port { get; private set; }

        /// <summary>
        /// True if other devices of the local network can connect directly.
        /// False when the server could only bind to localhost (no admin rights):
        /// it is then reachable through the Internet tunnel only.
        /// </summary>
        public bool AcceptsNetworkClients { get; private set; }

        public bool IsRunning => listener != null && listener.IsListening;

        #endregion

        #region ---- Constructor ----

        /// <param name="ui">Object whose Invoke runs code on the UI thread (a Form).</param>
        public RefereeWebServer(ISynchronizeInvoke ui)
        {
            this.ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        #endregion

        #region ---- Start / Stop ----

        /// ---------------------------------------------------------------
        /// Starts listening. Tries, for each port from preferredPort to
        /// preferredPort + 9:
        ///   1. http://+:PORT/        (all network cards; needs admin or a
        ///                             netsh urlacl, otherwise it throws)
        ///   2. http://localhost:PORT/ (always allowed; used by the tunnel)
        /// Returns false if nothing could be bound.
        /// ---------------------------------------------------------------
        public bool Start(int preferredPort = 8080)
        {
            for (int port = preferredPort; port < preferredPort + 10; port++)
            {
                foreach (bool allNetworks in new[] { true, false })
                {
                    string prefix = allNetworks
                        ? "http://+:" + port + "/"
                        : "http://localhost:" + port + "/";

                    var candidate = new HttpListener();
                    candidate.Prefixes.Add(prefix);

                    try
                    {
                        candidate.Start();
                    }
                    catch (Exception ex)
                    {
                        AppPaths.Log("Serveur web : impossible d'écouter sur " + prefix + " (" + ex.Message + ")");
                        try { candidate.Close(); } catch { }
                        continue;
                    }

                    listener = candidate;
                    Port = port;
                    AcceptsNetworkClients = allNetworks;
                    AppPaths.Log("Serveur web démarré sur " + prefix);

                    Task.Run(() => AcceptLoop(candidate));
                    return true;
                }
            }

            return false;
        }

        public void Stop()
        {
            stopping = true;
            try { listener?.Close(); } catch { }
        }

        public void Dispose()
        {
            Stop();
        }

        private async Task AcceptLoop(HttpListener l)
        {
            while (!stopping && l.IsListening)
            {
                HttpListenerContext context;

                try
                {
                    context = await l.GetContextAsync();
                }
                catch (Exception)
                {
                    if (stopping || !l.IsListening) break;
                    continue;
                }

                // One failing request must never stop the server.
                Task.Run(() => HandleRequest(context));
            }
        }

        #endregion

        #region ---- HTTP handling ----

        private async Task HandleRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                response.Headers["Cache-Control"] = "no-store";
                response.Headers["X-Content-Type-Options"] = "nosniff";

                string path = request.Url.AbsolutePath;
                if (path.Length > 1) path = path.TrimEnd('/');

                if (request.HttpMethod == "GET")
                {
                    if (path == "/" || path == "/index.html")
                        await Write(response, 200, "text/html; charset=utf-8", GetPage());
                    else if (path == "/api/ping")
                        await Write(response, 200, "text/plain; charset=utf-8", "ok");
                    else if (path == "/favicon.ico")
                        response.StatusCode = 204;
                    else
                        await Write(response, 404, "text/plain; charset=utf-8", "Introuvable");
                }
                else if (request.HttpMethod == "POST" && path.StartsWith("/api/"))
                {
                    Dictionary<string, string> form = await ReadForm(request);
                    await HandleApi(request, response, path, form);
                }
                else
                {
                    await Write(response, 405, "text/plain; charset=utf-8", "Méthode non autorisée");
                }
            }
            catch (Exception ex)
            {
                AppPaths.Log("Serveur web : erreur requête : " + ex);
                try { await Write(response, 500, "application/json; charset=utf-8", Error("server", "Erreur du serveur")); }
                catch { }
            }
            finally
            {
                try { response.Close(); } catch { }
            }
        }

        private async Task HandleApi(HttpListenerRequest request, HttpListenerResponse response, string path, Dictionary<string, string> form)
        {
            string client = GetClientId(request);

            if (IsBlocked(client))
            {
                await Write(response, 429, "application/json; charset=utf-8",
                    Error("blocked", "Trop d'essais avec une mauvaise clef. Réessayez dans quelques minutes."));
                return;
            }

            string key = PrivateKeyGenerator.Normalize(Get(form, "key"));

            ApiResult result;
            try
            {
                result = (ApiResult)ui.Invoke(new Func<ApiResult>(() => Execute(path, key, form)), null);
            }
            catch (Exception ex)
            {
                // Window closed / app shutting down
                AppPaths.Log("Serveur web : appel UI impossible : " + ex.Message);
                await Write(response, 503, "application/json; charset=utf-8", Error("unavailable", "Application indisponible"));
                return;
            }

            if (result.InvalidKey) RecordFailure(client);

            await Write(response, result.Status, "application/json; charset=utf-8", result.Json);
        }

        /// -------------------------------------------------------
        /// Runs on the UI thread: looks up the match and applies the
        /// requested operation.
        /// -------------------------------------------------------
        private ApiResult Execute(string path, string key, Dictionary<string, string> form)
        {
            Tournament tournament = Tournament.Instance;
            Match match = tournament.GetMatchByKey(key);

            if (match == null)
            {
                if (tournament.IsValidKey(key))
                    return new ApiResult { Json = Error("expired", "Cette clef correspond à un match d'une phase précédente.") };

                return new ApiResult { Json = Error("invalid_key", "Clef invalide"), InvalidKey = true };
            }

            KeyState state = GetState(key);

            switch (path)
            {
                case "/api/state":
                    return new ApiResult { Json = StateJson(match, state) };

                case "/api/score":
                {
                    int team;
                    if (!int.TryParse(Get(form, "team"), out team) || (team != 0 && team != 1))
                        return new ApiResult { Status = 400, Json = Error("bad_request", "Requête invalide") };

                    // A retried request (same id) must not add a second point.
                    if (IsNewRequest(state, Get(form, "rid")))
                    {
                        if (!tournament.IsMatchActive(match))
                            return new ApiResult { Json = Error("not_active", NotActiveMessage(match)) };

                        if (team == 0) match.ScoreTeam1++; else match.ScoreTeam2++;

                        state.History.Push(team);
                        if (state.History.Count > MaxHistory) TrimHistory(state);

                        tournament.OnMatchScoreChanged(match);
                    }

                    return new ApiResult { Json = StateJson(match, state) };
                }

                case "/api/undo":
                {
                    if (IsNewRequest(state, Get(form, "rid")) && state.History.Count > 0)
                    {
                        if (!tournament.IsMatchActive(match))
                            return new ApiResult { Json = Error("not_active", NotActiveMessage(match)) };

                        int last = state.History.Pop();

                        if (last == 0) { if (match.ScoreTeam1 > 0) match.ScoreTeam1--; }
                        else { if (match.ScoreTeam2 > 0) match.ScoreTeam2--; }

                        tournament.OnMatchScoreChanged(match);
                    }

                    return new ApiResult { Json = StateJson(match, state) };
                }

                default:
                    return new ApiResult { Status = 404, Json = Error("not_found", "Introuvable") };
            }
        }

        private static string NotActiveMessage(Match match)
        {
            return match.Locked
                ? "Match terminé et verrouillé. Demandez au staff de le rouvrir pour une correction."
                : "Ce match n'est pas encore sur un terrain.";
        }

        private KeyState GetState(string key)
        {
            KeyState state;
            if (!states.TryGetValue(key, out state))
            {
                state = new KeyState();
                states[key] = state;
            }
            return state;
        }

        private static void TrimHistory(KeyState state)
        {
            int[] all = state.History.ToArray();           // newest first
            state.History.Clear();
            for (int i = Math.Min(all.Length, MaxHistory) - 1; i >= 0; i--)
                state.History.Push(all[i]);
        }

        /// Returns false if this request id was already processed.
        private static bool IsNewRequest(KeyState state, string requestId)
        {
            if (string.IsNullOrEmpty(requestId)) return true;
            if (requestId.Length > 40) requestId = requestId.Substring(0, 40);

            if (!state.RecentIdSet.Add(requestId)) return false;

            state.RecentIds.Enqueue(requestId);
            while (state.RecentIds.Count > MaxRecentIds)
                state.RecentIdSet.Remove(state.RecentIds.Dequeue());

            return true;
        }

        private static string StateJson(Match match, KeyState state)
        {
            Tournament tournament = Tournament.Instance;

            var sb = new StringBuilder();
            sb.Append("{\"ok\":true");
            sb.Append(",\"team1\":").Append(Js(match.Team1.Name));
            sb.Append(",\"team2\":").Append(Js(match.Team2.Name));
            sb.Append(",\"score1\":").Append(match.ScoreTeam1);
            sb.Append(",\"score2\":").Append(match.ScoreTeam2);
            sb.Append(",\"canUndo\":").Append(state.History.Count > 0 ? "true" : "false");
            sb.Append(",\"active\":").Append(tournament.IsMatchActive(match) ? "true" : "false");
            sb.Append(",\"locked\":").Append(match.Locked ? "true" : "false");
            sb.Append(",\"status\":").Append(Js(tournament.DescribeMatch(match)));
            sb.Append(",\"referee\":").Append(Js(match.GetRefereeName()));
            sb.Append("}");
            return sb.ToString();
        }

        private static string Error(string code, string message)
        {
            return "{\"ok\":false,\"code\":" + Js(code) + ",\"error\":" + Js(message) + "}";
        }

        /// JSON string literal; also escapes < > & so the text is safe anywhere.
        private static string Js(string s)
        {
            if (s == null) return "\"\"";

            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '<': sb.Append("\\u003c"); break;
                    case '>': sb.Append("\\u003e"); break;
                    case '&': sb.Append("\\u0026"); break;
                    case '\u2028': sb.Append("\\u2028"); break;
                    case '\u2029': sb.Append("\\u2029"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        #endregion

        #region ---- Helpers ----

        private static string Get(Dictionary<string, string> form, string name)
        {
            string value;
            return form.TryGetValue(name, out value) ? value : null;
        }

        /// Reads an application/x-www-form-urlencoded body (max 4 KB).
        private static async Task<Dictionary<string, string>> ReadForm(HttpListenerRequest request)
        {
            var result = new Dictionary<string, string>();

            if (request.ContentLength64 > 4096)
                return result;

            string body;
            using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
            {
                var buffer = new char[4097];
                int count = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
                if (count > 4096) return result;
                body = new string(buffer, 0, count);
            }

            foreach (string pair in body.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = pair.IndexOf('=');
                string name = eq < 0 ? pair : pair.Substring(0, eq);
                string value = eq < 0 ? string.Empty : pair.Substring(eq + 1);

                try
                {
                    name = Uri.UnescapeDataString(name.Replace('+', ' '));
                    value = Uri.UnescapeDataString(value.Replace('+', ' '));
                }
                catch (UriFormatException)
                {
                    continue;
                }

                result[name] = value;
            }

            return result;
        }

        private static async Task Write(HttpListenerResponse response, int status, string contentType, string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            response.StatusCode = status;
            response.ContentType = contentType;
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }

        /// ---------------------------------------------------------------
        /// Identifies the caller for the brute-force protection. Behind the
        /// Cloudflare tunnel every connection comes from 127.0.0.1, so the
        /// real address is read from CF-Connecting-IP - but only when the
        /// connection really is local (nobody else can forge it).
        /// ---------------------------------------------------------------
        private static string GetClientId(HttpListenerRequest request)
        {
            IPAddress remote = request.RemoteEndPoint != null ? request.RemoteEndPoint.Address : null;
            string id = remote != null ? remote.ToString() : "?";

            if (remote != null && IPAddress.IsLoopback(remote))
            {
                string forwarded = request.Headers["CF-Connecting-IP"];
                if (string.IsNullOrEmpty(forwarded))
                {
                    forwarded = request.Headers["X-Forwarded-For"];
                    if (!string.IsNullOrEmpty(forwarded))
                        forwarded = forwarded.Split(',')[0];
                }

                if (!string.IsNullOrEmpty(forwarded) && forwarded.Length <= 64)
                    id = forwarded.Trim();
            }

            return id;
        }

        private bool IsBlocked(string client)
        {
            lock (failuresLock)
            {
                Attempts attempts;
                return failures.TryGetValue(client, out attempts) && attempts.BlockedUntil > DateTime.UtcNow;
            }
        }

        private void RecordFailure(string client)
        {
            lock (failuresLock)
            {
                DateTime now = DateTime.UtcNow;

                if (failures.Count > 1000)
                {
                    var stale = new List<string>();
                    foreach (var pair in failures)
                    {
                        if (pair.Value.BlockedUntil < now && now - pair.Value.WindowStart > FailureWindow)
                            stale.Add(pair.Key);
                    }
                    foreach (string k in stale) failures.Remove(k);
                }

                Attempts attempts;
                if (!failures.TryGetValue(client, out attempts))
                {
                    attempts = new Attempts { WindowStart = now };
                    failures[client] = attempts;
                }

                if (now - attempts.WindowStart > FailureWindow)
                {
                    attempts.WindowStart = now;
                    attempts.Count = 0;
                }

                attempts.Count++;
                if (attempts.Count >= MaxFailures)
                {
                    attempts.BlockedUntil = now + BlockDuration;
                    attempts.Count = 0;
                }
            }
        }

        private static string GetPage()
        {
            if (cachedPage != null) return cachedPage;

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("ChassieuVolleyTournament.Web.referee.html"))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                            return cachedPage = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                AppPaths.Log("Page arbitre illisible : " + ex.Message);
            }

            return "<!DOCTYPE html><html><head><meta charset='utf-8'></head><body style='font-family:Arial;text-align:center;margin-top:50px'>"
                 + "<h2>Page arbitre introuvable</h2><p>Le fichier Web/referee.html n'est pas inclus dans l'application.</p></body></html>";
        }

        /// <summary>IPv4 addresses of this PC on the local network(s).</summary>
        public static List<string> GetLocalAddresses()
        {
            var list = new List<string>();

            try
            {
                foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                    if (nic.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;

                    foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            list.Add(unicast.Address.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                AppPaths.Log("Lecture des adresses réseau impossible : " + ex.Message);
            }

            return list;
        }

        #endregion
    }
}
