using Emotions.Application.Interfaces;
using Emotions.Infrastructure.Options;
using Emotions.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Emotions.Infrastructure.Configurations
{
    public static class AzureSpeechSetup
    {
        public static IServiceCollection AddAzureSpeechTranscription(this IServiceCollection services,
            IConfiguration config)
        {
            services.Configure<AzureSpeechOptions>(config.GetSection("AzureSpeech"));
            services.AddSingleton<ILiveTranscriptionService, AzureStreamingTranscriptionService>();
            return services;
        }
    }
}