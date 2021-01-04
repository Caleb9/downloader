using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Api
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                using var host = CreateHostBuilder(args).Build();
                host.Run();
            }
            catch (Exception exception) when (
                exception is OptionsValidationException ||
                exception is DownloadDirectoriesCreator.DownloadPathAccessDeniedException)
            {
                Console.WriteLine(exception.Message);
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
        }
    }
}