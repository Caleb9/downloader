using System.IO.Abstractions;
using System.Text.Json.Serialization;
using Api.Downloading;
using Api.Downloading.Directories;
using Api.Notifications;
using Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace Api;

public class Startup(
    IConfiguration configuration)
{
    private IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        AddOptions();

        services.AddSingleton<IFileSystem, FileSystem>();

        AddDownloadDirectories();

        services.AddHostedService<DownloadDirectoriesCreator>();

        AddDownloadControllerDependencies();

        services
            .AddControllers()
            .AddJsonOptions(options =>
                /* Needed for DownloadJob.DownloadStatus enum */
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        AddWebClients();

        void AddOptions()
        {
            services
                .AddOptions<DownloadDirectoriesOptions>()
                .Bind(Configuration.GetSection(DownloadDirectoriesOptions.Section))
                .ValidateDataAnnotations();
            services.AddSingleton(s =>
                s.GetRequiredService<IOptions<DownloadDirectoriesOptions>>().Value);
            services
                .AddOptions<PushNotificationsOptions>()
                .Bind(Configuration.GetSection(PushNotificationsOptions.Section));
        }

        void AddDownloadDirectories()
        {
            services
                .AddSingleton(
                    new DirectorySeparatorChars(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar))
                .AddSingleton<IncompleteDownloadsDirectory>()
                .AddSingleton<CompletedDownloadsDirectory>();
        }

        void AddDownloadControllerDependencies()
        {
            services
                .AddSingleton<DownloadManager.DownloadIdGenerator>(() => new DownloadJob.JobId(Guid.NewGuid()))
                .AddSingleton<DownloadManager.DateTimeUtcNowTicks>(() => DateTime.UtcNow.Ticks)
                .AddSingleton<DownloadJobsDictionary>()
                .AddSingleton<DownloadTaskFactory>()
                .AddSingleton<DownloadManager>();
            services
                .AddSingleton<ProgressNotificationDictionary>()
                .AddScoped<NotificationsManager>()
                .AddSignalR();
            services
                .AddHttpClient<DownloadStarter>();
        }

        void AddWebClients()
        {
            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(options => options.RootPath = "../client/");

            services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo { Title = "Api" }));
        }
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

        /* Not used currently */
        // app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHub<NotificationsHub>("/hub");
        });

        app.UseSpa(spa =>
        {
            spa.Options.SourcePath = "../client";
            if (env.IsDevelopment())
            {
                spa.UseProxyToSpaDevelopmentServer("http://localhost:3000/");
            }
        });
    }
}