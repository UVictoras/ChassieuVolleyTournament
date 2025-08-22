#region ---- Includes ----
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

#endregion

namespace ChassieuVolleyTournament
{
    /// ----------------------------------------------------
    /// Handles a simple web server for referees,
    /// allowing score updates, undo actions, and
    /// team name inversion for matches identified by keys.
    /// ----------------------------------------------------
    public class RefereeWebServer
    {
        #region ---- Properties ----
        private HashSet<string> validKeys;

        private readonly Dictionary<string, Team[]>             keyToTeams;
        private readonly Dictionary<string, bool>               keyInverted;
        private readonly Dictionary<string, int[]>              keyScores;
        private readonly Dictionary<string, Stack<int>>         lastModifiedTeam;

        private readonly string url;

        private readonly Control uiControl;
        #endregion

        #region ---- Constructor ----
        /// ---------------------------------------------------
        /// Initializes a new RefereeWebServer instance
        /// with a set of valid keys and UI control reference.
        /// ---------------------------------------------------
        public RefereeWebServer(IEnumerable<string> keys, Control uiControl, string urlPrefix = "http://+:8080/")
        {
            this.uiControl = uiControl ?? throw new ArgumentNullException(nameof(uiControl));

            validKeys = new HashSet<string>(keys);
            keyToTeams = new Dictionary<string, Team[]>();
            keyInverted = new Dictionary<string, bool>();
            keyScores = new Dictionary<string, int[]>();
            lastModifiedTeam = new Dictionary<string, Stack<int>>();
            url = urlPrefix;

            foreach (var key in validKeys)
            {
                keyToTeams[key] = new[] { new Team("Team 1"), new Team("Team 2") };
                keyInverted[key] = false;
                keyScores[key] = new int[] { 0, 0 };
                lastModifiedTeam[key] = new Stack<int>();
            }
        }
        #endregion

        #region ---- Methods & Tasks ----

        /// --------------------------------------------------------
        /// Starts the HTTP listener and handles incoming requests.
        /// --------------------------------------------------------
        public async Task Start()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();

            Console.WriteLine($"Web server started at {url}");

            while (true)
            {
                var context = await listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context));
            }
        }

        /// ----------------------------------
        /// Processes incoming HTTP requests.
        /// ----------------------------------
        private async Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "POST")
            {
                var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                string body = await reader.ReadToEndAsync();

                if (body.StartsWith("key="))
                {
                    string key = Uri.UnescapeDataString(body).Replace("key=", "").Trim();
                    if (validKeys.Contains(key))
                    {
                        await ServeScoreboardPage(response, key);
                    }
                    else
                    {
                        await RespondAsync(response, GenerateErrorPage("❌ Clef invalide"));
                    }
                }
                else if (body.StartsWith("invertKey="))
                {
                    string key = Uri.UnescapeDataString(body).Replace("invertKey=", "").Trim();
                    if (validKeys.Contains(key))
                    {
                        keyInverted[key] = !keyInverted[key];
                        await ServeScoreboardPage(response, key);
                    }
                }
                else if (body.StartsWith("scoreKey="))
                {
                    var data = body.Split('&');
                    string key = data[0].Replace("scoreKey=", "").Trim();
                    int teamIndex = int.Parse(data[1].Replace("team=", "").Trim());

                    if (validKeys.Contains(key))
                    {
                        keyScores[key][teamIndex]++;
                        lastModifiedTeam[key].Push(teamIndex);

                        UpdateTournamentMatchScore(key);
                        await ServeScoreboardPage(response, key);
                    }
                }
                else if (body.StartsWith("undoKey="))
                {
                    string key = Uri.UnescapeDataString(body).Replace("undoKey=", "").Trim();
                    if (validKeys.Contains(key) && lastModifiedTeam[key].Count > 0)
                    {
                        int lastTeam = lastModifiedTeam[key].Pop();
                        if (keyScores[key][lastTeam] > 0)
                            keyScores[key][lastTeam]--;

                        UpdateTournamentMatchScore(key);
                        await ServeScoreboardPage(response, key);
                    }
                }
                else
                {
                    await RespondAsync(response, GenerateErrorPage("❌ Requête invalide"));
                }
            }
            else
            {
                await RespondAsync(response, GenerateLoginForm());
            }
        }

        /// ----------------------------------------------
        /// Updates the tournament match score for a key.
        /// ----------------------------------------------
        private void UpdateTournamentMatchScore(string key)
        {
            if (!validKeys.Contains(key)) return;

            var teams = keyToTeams[key];
            var scores = keyScores[key];
            var match = Tournament.Instance.GetMatchByKey(key);

            if (match != null)
            {
                uiControl.Invoke((Action)(() =>
                {
                    match.SetScores(teams[0].Name, teams[1].Name, scores[0], scores[1]);
                    Tournament.Instance.UpdateLiveScores();
                }));
            }
        }

        /// -------------------------------------------
        /// Serves the HTML scoreboard page for a key.
        /// -------------------------------------------
        private async Task ServeScoreboardPage(HttpListenerResponse response, string key)
        {
            var match = Tournament.Instance.GetMatchByKey(key);
            if (match == null)
            {
                await RespondAsync(response, GenerateErrorPage("❌ Match introuvable"));
                return;
            }

            var teams = match.GetTeamsNames(); 

            bool inverted = keyInverted[key];
            var scores = keyScores[key];

            int leftIdx = inverted ? 1 : 0;
            int rightIdx = inverted ? 0 : 1;

            string html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body {{
                        margin: 0;
                        font-family: Arial, sans-serif;
                        position: relative;
                    }}
                    .scoreboard {{
                        display: flex;
                        height: 100vh;
                        width: 100%;
                        color: white;
                        text-align: center;
                        font-size: 48px;
                        font-weight: bold;
                    }}
                    .left {{
                        background-color: #0A2B4B;
                        flex: 1;
                        display: flex;
                        flex-direction: column;
                        justify-content: center;
                        align-items: center;
                    }}
                    .right {{
                        background-color: #F3A712;
                        flex: 1;
                        display: flex;
                        flex-direction: column;
                        justify-content: center;
                        align-items: center;
                        color: black;
                    }}
                    .middle-button {{
                        position: absolute;
                        top: 20px;
                        left: 50%;
                        transform: translateX(-50%);
                    }}
                    .bottom-button {{
                        position: absolute;
                        bottom: 30px;
                        left: 50%;
                        transform: translateX(-50%);
                    }}
                    .team-name {{
                        font-size: 32px;
                        margin-bottom: 10px;
                    }}
                    .team-score {{
                        font-size: 96px;
                        margin-top: 0;
                    }}
                    button {{
                        padding: 10px 20px;
                        font-size: 18px;
                        border: none;
                        border-radius: 8px;
                        cursor: pointer;
                    }}
                    .panel-form {{
                        width: 100%;
                        height: 100%;
                        border: none;
                        background: none;
                    }}
                </style>
            </head>
            <body>
                <div class='middle-button'>
                    <form method='post'>
                        <input type='hidden' name='invertKey' value='{key}' />
                        <button type='submit'>Inverser Équipes</button>
                    </form>
                </div>

                <div class='scoreboard'>
                    <form class='left' method='post'>
                        <input type='hidden' name='scoreKey' value='{key}' />
                        <input type='hidden' name='team' value='{leftIdx}' />
                        <button type='submit' class='panel-form'>
                            <div class='team-name'>{teams[leftIdx]}</div>
                            <div class='team-score'>{scores[leftIdx]}</div>
                        </button>
                    </form>

                    <form class='right' method='post'>
                        <input type='hidden' name='scoreKey' value='{key}' />
                        <input type='hidden' name='team' value='{rightIdx}' />
                        <button type='submit' class='panel-form'>
                            <div class='team-name'>{teams[rightIdx]}</div>
                            <div class='team-score'>{scores[rightIdx]}</div>
                        </button>
                    </form>
                </div>

                <div class='bottom-button'>
                    <form method='post'>
                        <input type='hidden' name='undoKey' value='{key}' />
                        <button type='submit'>Annuler Dernier Point</button>
                    </form>
                </div>
            </body>
            </html>";

            await RespondAsync(response, html);
        }

        /// ---------------------------------------------
        /// Generates an error HTML page with a message.
        /// ---------------------------------------------
        private string GenerateErrorPage(string message)
        {
            return $@"<!DOCTYPE html>
            <html>
            <head><meta charset='utf-8'></head>
            <body style='font-family:Arial;text-align:center;margin-top:50px;'>
                <h2>{message}</h2>
                <a href='/'>Retour</a>
            </body>
            </html>";
        }

        /// ------------------------------------
        /// Generates the login form HTML page.
        /// ------------------------------------
        private string GenerateLoginForm()
        {
            return @"<!DOCTYPE html>
            <html>
            <head><meta charset='utf-8'></head>
            <body style='font-family:Arial;text-align:center;margin-top:50px;'>
                <h2>Connexion Clef</h2>
                <form method='post'>
                    <input name='key' placeholder='Entrer la clef' />
                    <button type='submit'>Valider</button>
                </form>
            </body>
            </html>";
        }

        /// -----------------------------------
        /// Sends HTML response to the client.
        /// -----------------------------------
        private async Task RespondAsync(HttpListenerResponse response, string html)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            response.ContentType = "text/html";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        /// -------------------------------------
        /// Clears all keys and associated data.
        /// -------------------------------------
        public void ResetKeys()
        {
            validKeys.Clear();

            keyToTeams.Clear();
            keyInverted.Clear();
            keyScores.Clear();
            lastModifiedTeam.Clear();
        }

        /// -------------------------------------------------------
        /// Adds new valid keys along with their associated teams.
        /// -------------------------------------------------------
        public void AddValidKeys(Dictionary<string, Team[]> keysWithTeams)
        {
            foreach (var kvp in keysWithTeams)
            {
                var key = kvp.Key;
                var teams = kvp.Value;

                if (teams.Length != 2 || validKeys.Contains(key))
                    continue;

                validKeys.Add(key);

                keyToTeams[key] = teams;
                keyScores[key] = new int[] { 0, 0 };
                keyInverted[key] = false;
                lastModifiedTeam[key] = new Stack<int>();
            }
        }

        #endregion

        #region ---- Getters & Setters ----

        /// ---------------------------------
        /// Sets team names for a given key.
        /// ---------------------------------
        public void SetInfoForKey(string key, string info)
        {
            if (validKeys.Contains(key))
            {
                var split = info.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 2)
                {
                    keyToTeams[key][0].Name = split[0].Trim();
                    keyToTeams[key][1].Name = split[1].Trim();
                }
            }
        }

        #endregion
    }
}
