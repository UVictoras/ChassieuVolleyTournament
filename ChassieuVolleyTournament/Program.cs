using System;
using System.Drawing;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace ChassieuVolleyTournament
{
    //IP : 192.168.1.144

    static class Program
    {
        static HashSet<string> validKeys = new HashSet<string> { "abc123", "volley2025", "secret" };

        public static DisplayWindow window1;
        public static StaffWindow window2;

        private static Timer timer;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();

            window1 = new DisplayWindow();

            window2 = new StaffWindow();

            timer = new Timer();

            window1.Show();
            window2.Show();

            Task.Run(() => MainLoop());
            Task.Run(() => StartWebServer());

            Application.Run();
        }

        public static async Task MainLoop()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            long lastTime = stopwatch.ElapsedMilliseconds;

            timer.StartTimer(true);

            while (true)
            {
                long currentTime = stopwatch.ElapsedMilliseconds;
                float deltaTime = (currentTime - lastTime) / 1000f; // deltaTime in seconds
                lastTime = currentTime;

                if (!timer.GetTimerIsEnabled())
                    return;

                timer.DecrementTimer(deltaTime);
                window1.Invoke((MethodInvoker)delegate {
                    window1.SetTimerText(timer.GetCurrentTime()); // replace with your real time
                });

            }
        }

        static async Task StartWebServer()
        {
            string url = "http://+:8080/";
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine($"Server started at {url}");

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                if (request.HttpMethod == "POST")
                {
                    var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    string body = await reader.ReadToEndAsync();

                    string key = Uri.UnescapeDataString(body).Replace("key=", "").Trim();
                    bool isValid = validKeys.Contains(key);

                    // Simple HTML result per user
                    string htmlResponse = $@"<!DOCTYPE html>
                    <html>
                    <head><meta charset='utf-8'></head>
                    <body style='font-family:Arial;text-align:center;margin-top:50px;'>
                        <h2>{(isValid ? "✅ Clé valide" : "❌ Clé invalide")}</h2>
                        <a href='/'>Retour</a>
                    </body>
                    </html>";

                    byte[] buffer = Encoding.UTF8.GetBytes(htmlResponse);
                    response.ContentType = "text/html";
                    response.ContentLength64 = buffer.Length;

                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                else
                {
                    string html = @"<!DOCTYPE html>
                    <html>
                    <head><meta charset='utf-8'></head>
                    <body>
                        <form method='post'>
                            <input name='key' placeholder='Entrer la clé' />
                            <button type='submit'>Valider</button>
                        </form>
                    </body>
                    </html>";
                    byte[] buffer = Encoding.UTF8.GetBytes(html);
                    response.ContentType = "text/html";
                    response.ContentLength64 = buffer.Length;

                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }

            }
        }
    }
}