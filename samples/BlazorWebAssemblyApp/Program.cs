using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorWebAssemblyApp;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Uses Composition as an alternative IServiceProviderFactory
builder.ConfigureContainer(new Composition());

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();