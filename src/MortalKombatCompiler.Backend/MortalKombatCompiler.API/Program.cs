using MortalKombatCompiler.API.Compiler;
using MortalKombatCompiler.API.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configuración CORS para producción
var frontendUrls = new[]
{
    "http://localhost:3000",
    "http://localhost:5173",
    "https://tu-frontend.netlify.app" // Reemplazar con tu URL real
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowProduction",
        policy =>
        {
            policy.WithOrigins(frontendUrls)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseCors("AllowProduction");
app.MapControllers();

app.Run();