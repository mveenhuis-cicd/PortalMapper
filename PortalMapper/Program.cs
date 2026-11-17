using PortalMapper.Reader;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.Configure<PlaywrightReaderOptions>(builder.Configuration.GetSection("PlaywrightReader"));
builder.Services.AddSingleton<PortalMapper.Core.Reading.IPageReader, PlaywrightPageReader>();
builder.Services.AddSingleton<PortalMapper.Core.Simplify.LinkSimplifier>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
    .WithStaticAssets();

app.Run();
