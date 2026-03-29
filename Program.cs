using LogPath.Api.Data;
using LogPath.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar CORS (Para que Angular pueda entrar)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// 2. Conectar a PostgreSQL de CubePath
var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.UseCors("AllowAll");

// 3. ENDPOINT GET: Leer desde la Base de Datos real
app.MapGet("/api/logs", async (AppDbContext db) =>
{
    var logs = await db.PosEvents.OrderByDescending(l => l.Timestamp).ToListAsync();
    return Results.Ok(logs);
});

// 4. ENDPOINT POST: Guardar en la Base de Datos en la nube
app.MapPost("/api/logs", async (PosEvent newLog, AppDbContext db) =>
{
    if (newLog.Timestamp == default)
    {
        newLog.Timestamp = DateTime.UtcNow;
    }
    
    db.PosEvents.Add(newLog);
    await db.SaveChangesAsync(); // ¡Aquí ocurre la magia de guardado!
    
    return Results.Created($"/api/logs/{newLog.Id}", newLog);
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();