using System.Linq.Expressions;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.SignalR.Client;

namespace SimpleSignalrClient
{
    public class Program
    {
        private static HubConnection _connection;
        private static bool ignoreSsl = true; //Set to true to use a default validation callback
        private static readonly RemoteCertificateValidationCallback _validationCallback = CertificateValidationCallback; //Set to null to use no callback
        private static string certPath = @"<your-ssl-certificate.pfx";
        private static string certPassword = "<ssl-certificate-password>";
        private static string url = "https://localhost:8443/myHub";
        //private static string url = "https://<spp-host>/service/a2a/signalr";
        private static string apiKey = "<A2A-Apikey>";

        public static async Task Main(string[] args)
        {
            var x509 = new X509Certificate2(File.ReadAllBytes(certPath), certPassword);

            _connection = new HubConnectionBuilder()
                .WithUrl(url, options =>
                {
                    options.Headers.Add("Authorization", $"A2A {apiKey}");
                    options.ClientCertificates.Add(x509);

                    options.HttpMessageHandlerFactory = (message) =>
                    {
                        if (message is HttpClientHandler clientHandler)
                        {
                            if (ignoreSsl)
                            {
                                clientHandler.ServerCertificateCustomValidationCallback =
                                    (sender, certificate, chain, sslPolicyErrors) => true;
                            }
                            else
                            {
                                if (_validationCallback == null)
                                {
                                    // Use standard validation
                                }
                                else
                                {
                                    clientHandler.ServerCertificateCustomValidationCallback =
                                        (sender, certificate, chain, sslPolicyErrors) => _validationCallback(sender, certificate, chain, sslPolicyErrors);
                                }
                            }
                        }
                        return message;
                    };
                })
                .Build();

            _connection.Closed += async (error) =>
            {
                Console.WriteLine($"Got Error: {error}");
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await _connection.StartAsync();
            };

            await ConnectAsync();

            bool stop = false;
            while (!stop)
            {
                Console.WriteLine("Press any key to send message to server and receive echo");
                Console.ReadKey();
                Send("testuser", "msg");
                Console.WriteLine("Press q to quit or anything else to resume");
                var key = Console.ReadLine();
                if (key == "q") stop = true;
            }
        }

        private static async Task ConnectAsync()
        {
            _connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                Console.WriteLine("Received message");
                Console.WriteLine($"user: {user}");
                Console.WriteLine($"message: {message}");
            });
            try
            {
                await _connection.StartAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
            }
        }

        private static async void Send(string user, string msg)
        {
            try
            {
                await _connection.InvokeAsync("SendMessage", user, msg);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e}");
            }
        }

        public static bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

    }
}