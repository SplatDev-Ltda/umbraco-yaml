using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Microsoft.Extensions.DependencyInjection;
using UmbracoYaml.Services;
using UmbracoYaml.Handlers;

namespace UmbracoYaml.Composers
{
    public class YamlStartupComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // Register YamlParser
            builder.Services.AddScoped<YamlParser>();

            // Register all Creators
            builder.Services.AddScoped<DataTypeCreator>();
            builder.Services.AddScoped<DocumentTypeCreator>();
            builder.Services.AddScoped<TemplateCreator>();
            builder.Services.AddScoped<ContentCreator>();

            // Register the initialization handler for startup notification
            builder.AddNotificationHandler<UmbracoApplicationStartedNotification, YamlInitializationHandler>();
        }
    }
}
