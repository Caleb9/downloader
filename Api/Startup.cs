using System;
using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text.Json.Serialization;
using Api.Downloading;
using Api.Downloading.Directories;
using Api.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Api
{
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
            services
                .AddOptions<DownloadOptions>()
                .Bind(Configuration.GetSection(DownloadOptions.Section))
                .ValidateDataAnnotations();

            services
                .AddSingleton<IFileSystem, FileSystem>();

            services
                .AddSingleton<IncompleteDownloadsDirectory>()
                .AddSingleton<CompletedDownloadsDirectory>();

            services
                .AddHostedService<DownloadDirectoriesCreator>();

            services
                .AddSingleton<Func<Guid>>(Guid.NewGuid)
                .AddSingleton<ConcurrentDictionary<Guid, Download>>()
                .AddHttpClient<Downloads>();

            services
                .AddControllers()
                .AddJsonOptions(options =>
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(options =>
                options.RootPath = "../downloader-client/");

            services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo {Title = "Api"}); });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Api"));
            }

            app.UseSpaStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "../downloader-client";
                if (env.IsDevelopment())
                {
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:3000/");
                }
            });
        }
    }
}