using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Plugins.Yaml2Schema.Services;
using Umbraco.Plugins.Yaml2Schema.Handlers;

namespace Umbraco.Plugins.Yaml2Schema.Composers
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
            builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, YamlInitializationHandler>();
        }
    }
}
