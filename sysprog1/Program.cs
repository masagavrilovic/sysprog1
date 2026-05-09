using System.Net;
using System.Text;
using DotNetEnv;
namespace sysprog1
{
    class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string? api_key;
        private static Cache _cache = new Cache(_httpClient);
        static void Main(string[] args)
        {
            Env.Load();
            api_key = Environment.GetEnvironmentVariable("API_KEY");
            if (api_key == null)
            {
                Logger.Log("Greska pri ucitavanju iz .env fajla");
                return;
            }
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5000/");
            listener.Start();
            Logger.Log("Server pokrenut na http://localhost:5000/");
        
            while (true)
            {
                var context = listener.GetContext();
                ThreadPool.QueueUserWorkItem(a => HandleRequest(context));
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            var timestamp = DateTime.Now;

            if (request.HttpMethod != "GET")
            {
                Logger.Log("Podrzan je samo GET metod");
                SendResponse(response, 405, "Podrzan je samo GET metod.");
                return;
            }
            if (request.Url!.AbsolutePath == "/favicon.ico")
            {
                response.StatusCode = 404;
                response.Close();
                return;
            }
            Logger.Log(timestamp + " | Primljen zahtev: " + request.Url);

            try
            {
                string? query = request.QueryString["q"];
                if (string.IsNullOrEmpty(query))
                {
                    Logger.Log(timestamp + " | Nedostaje parametar pretrage");
                    SendResponse(response, 400, "Nedostaje parametar.");
                    return;
                }

                List<Article> articles = _cache.Get(query, api_key);
                if (articles.Count == 0)
                {
                    SendResponse(response, 404, $"Nisu pronadjeni clanci za: {query}");
                    return;
                }

                string html = BuildHtml(query, articles);
                SendResponse(response, 200, html);
                Logger.Log(timestamp + " | Odgovor uspesno vracen");
            }
            catch (Exception e)
            {
                Logger.Log(e.Message);
                SendResponse(response, 500, $"Greska: {e.Message}");
            }
        }

        private static string BuildHtml(string query, List<Article> articles)
        {
            var sb = new StringBuilder();
            sb.Append($"<html><body><h3>Rezultati za: {query}</h3><ul>");
            foreach (var a in articles)
            {
                sb.Append($"<li><a href='{a.WebUrl}'>{a.Headline}</a>");
                sb.Append($"<p>{a.Abstract}</p>");
                sb.Append($"<small>{a.Date}</small></li>");
            }
            sb.Append("</ul></body></html>");
            return sb.ToString();
        }
        private static void SendResponse(HttpListenerResponse response, int statusCode, string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            response.ContentType = "text/html; charset=utf-8";
            response.StatusCode = statusCode;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            response.Close();
        }
    }
}