using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Web;
using MudBlazor.Services;
using Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient<DataService>(client =>
{
    client.BaseAddress = new Uri("http://192.168.0.194:5001/");
});

builder.Services.AddScoped<BaseService>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
