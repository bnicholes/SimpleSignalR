using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SimpleSignalrServer
{
    public class Program
    {
        private static string certPath = @"<Your-Client-Certficate.pfs";
        private static string certPassword = "<client-certificate-password>";

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, 8443, listenOptions => // https
                        {
                            listenOptions.UseHttps(certPath, certPassword);
                        });
                        options.ConfigureHttpsDefaults(configOptions =>
                        {
                            configOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                            configOptions.CheckCertificateRevocation = false;
                            configOptions.ClientCertificateValidation = (certificate2, validationChain, policyErrors) =>
                            {
                                return true;
                            };
                        });

                    });
                    webBuilder.UseStartup<Startup>();
                });
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR(o => o.HandshakeTimeout = TimeSpan.FromSeconds(30));
            services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddCertificate(options =>
            {
                options.AllowedCertificateTypes = CertificateTypes.All;
                options.Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
                        var cert = context.ClientCertificate;
                        var claims = new[]
                        {
                            new Claim(
                                ClaimTypes.NameIdentifier,
                                context.ClientCertificate.Subject,
                                ClaimValueTypes.String, context.Options.ClaimsIssuer),
                            new Claim(
                                ClaimTypes.Name,
                                context.ClientCertificate.Subject,
                                ClaimValueTypes.String, context.Options.ClaimsIssuer)
                        };

                        context.Principal = new ClaimsPrincipal(
                            new ClaimsIdentity(claims, context.Scheme.Name));
                        context.Success();

                        return Task.CompletedTask;
                    }
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.Use((context, next) => 
            {
                Console.WriteLine("Called the authentication handlers.");
                var cert = Task.Run(async () => await context.Connection.GetClientCertificateAsync()).Result;


                if (cert != null)
                {
                    Console.WriteLine("Got a certificate {cert}");
                }
                return next(context);
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<MyHub>("/myHub");
            });
        }
    }
}
