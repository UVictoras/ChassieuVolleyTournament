using System;
using System.Drawing;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var window1 = new LocalDisplay1();
            window1.Size = new Size(1920, 1080);
            window1.Icon = new Icon("../../Images/ChassieuLogo.ico");

            var window2 = new LocalDisplay2();
            window2.Size = new Size(1920, 1080);
            window2.Icon = new Icon("../../Images/ChassieuLogo.ico");

            window1.Show();
            window2.Show();

            // Run web server in background
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
                HttpListenerResponse response = context.Response;

                string html = @"<html><body><h1>Hello from the server!</h1><p>Data goes here.</p></body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(html);

                response.ContentLength64 = buffer.Length;
                response.ContentType = "text/html";
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();
            }
        }
    }
}