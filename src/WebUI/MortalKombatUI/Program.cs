using MortalKombatUI.Services;
using MortalKombatUI.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Registrar servicios personalizados
builder.Services.AddSingleton<CompilerService>();
builder.Services.AddSingleton<InputProcessingService>();

// Configurar CORS para desarrollo
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Configurar logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

// Mapear controllers y hubs
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<InputHub>("/inputHub");

// Inicializar InputProcessingService al arrancar
var inputService = app.Services.GetRequiredService<InputProcessingService>();
inputService.Start();

app.Logger.LogInformation("Mortal Kombat Compiler iniciado");

app.Run();