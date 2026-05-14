using SplatDev.Api.Common.ApiVersioning;
using Microsoft.AspNetCore.Mvc; // for ApiVersion
using Microsoft.AspNetCore.Mvc.Versioning; // for ApiVersioning extension methods

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Use centralized API Versioning extensions
builder.Services.AddSplatApiVersioning();

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();


app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
