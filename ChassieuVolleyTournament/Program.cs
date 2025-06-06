using System;
using System.Drawing;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;

namespace ChassieuVolleyTournament
{
    //IP : 192.168.1.144
    class LocalDisplay1 : Form
    {
        public LocalDisplay1() => Text = "Tournoi de Chassieu Volley";
    }

    class LocalDisplay2 : Form
    {
        public LocalDisplay2() => Text = "Infos privées du tournoi";
    }

    static class Program
    {
        static HashSet<string> validKeys = new HashSet<string> { "abc123", "volley2025", "secret" };

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();

            var window1 = new LocalDisplay1()
            {
                Size = new Size(1920, 1080),
                Icon = new Icon("../../Images/ChassieuLogo.ico")
            };

            var window2 = new LocalDisplay2()
            {
                Size = new Size(1920, 1080),
                Icon = new Icon("../../Images/ChassieuLogo.ico")
            };

            window1.Show();
            window2.Show();

            Task.Run(() => StartWebServer());

            Application.Run();
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