using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Libsrd5Blazor;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
await builder.Build().RunAsync();
