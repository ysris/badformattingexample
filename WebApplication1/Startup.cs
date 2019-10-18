using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace WebApplication1
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()

                // Global filters (we don't need these at all)
                .Filter.ByExcluding(a => new List<string>
                {
                    "{HostingRequestStartingLog:l}",
                    "{HostingRequestFinishedLog:l}",
                    "Sending file. Request path: '{VirtualPath}'. Physical path: '{PhysicalPath}'",
                    "Executed DbCommand ({elapsed}ms) [Parameters=[{parameters}], CommandType='{commandType}', CommandTimeout='{commandTimeout}']{newLine}{commandText}",
                    "Entity Framework Core {version} initialized '{contextType}' using provider '{provider}' with options: {options}"
                }.Contains(a.MessageTemplate.Text))

                // Console Logger
                .WriteTo.Console()

                // MSSQL Logger
                .WriteTo.Logger(lc =>
                    lc
                        // Filter per mssql only
                        .Filter.ByExcluding(a => new List<string>
                        {
                            "Authorization was successful for user: {UserName}.",
                            "Executing ObjectResult, writing value {Value}.",
                            "Executing FileResult, sending file as {FileDownloadName}",
                            "Accessing expired session, Key:{sessionKey}",
                            "The Include operation for navigation '{include}' is unnecessary and was ignored because the navigation is not reachable in the final query results. See https://go.microsoft.com/fwlink/?linkid=850303 for more information.",
                            "The LINQ expression '{expression}' could not be translated and will be evaluated locally.",
                            "Committing the session was canceled.",
                            //"AuthenticationScheme: {AuthenticationScheme} was challenged.",
                            "Executing ChallengeResult with authentication schemes ({Schemes}).",
                            "User profile is available. Using '{FullName}' as key repository and Windows DPAPI to encrypt keys at rest.",
                            "The file {Path} was not modified",
                            "Listening queues: 'default'",
                            "Using job storage: 'Hangfire.MemoryStorage.MemoryStorage'",
                            "Shutdown timeout: 00:00:15",
                            "Using the following options for Hangfire Server:",
                            "Starting Hangfire Server",
                            "Worker count: 20",
                            "Schedule polling interval: 00:00:15",
                            "Executing HttpStatusCodeResult, setting HTTP status code {StatusCode}",
                            "Query: '{queryModel}' uses a row limiting operation (Skip/Take) without OrderBy which may lead to unpredictable results.",
                            "Session started; Key:{sessionKey}, Id:{sessionId}",
                            "{State:l}"
                        }.Contains(a.MessageTemplate.Text.Trim()))
                        .WriteTo.Console()
                )
                .CreateLogger();

            services.AddResponseCompression();

            //The default bearer need to be this one for making JWT token working (fair enough)
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidIssuer = Configuration["Tokens:Issuer"],
                    ValidAudience = Configuration["Tokens:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Tokens:Key"]))
                };
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.Cookie.Expiration = TimeSpan.FromHours(8);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Events.OnRedirectToLogin = (context) =>
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}