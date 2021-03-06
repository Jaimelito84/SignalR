﻿using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AspNetCoreAngularSignalR.SignalRHubs;
using AspNetCoreAngularSignalR.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using AspNetCoreAngularSignalR;

namespace Angular2WebpackVisualStudio
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ValidateMimeMultipartContentFilter>();

            var sqlConnectionString = Configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<NewsContext>(options =>
                options.UseSqlite(
                    sqlConnectionString
                ), ServiceLifetime.Singleton
            );

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
            });

            services.AddSingleton<NewsStore>();

            services.AddSignalR()
              .AddMessagePackProtocol();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            var angularRoutes = new[] {
                 "/home",
                 "/news",
                 "/images"
             };

            app.UseHsts();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.HasValue && null != angularRoutes.FirstOrDefault(
                    (ar) => context.Request.Path.Value.StartsWith(ar, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Request.Path = new PathString("/");
                }

                await next();
            });

            app.UseCors("AllowAllOrigins");

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseSignalR(routes =>
            {
                routes.MapHub<LoopyHub>("/loopy");
                routes.MapHub<NewsHub>("/looney");
                routes.MapHub<LoopyMessageHub>("/loopymessage");
                routes.MapHub<ImagesMessageHub>("/zub");
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=FileClient}/{action=Index}/{id?}");
            });
        }
    }
}
