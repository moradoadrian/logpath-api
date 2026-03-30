using LogPath.Api.Data;
using LogPath.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

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
    await db.SaveChangesAsync(); 

    //  Notificación a Discord si es un error
    if (newLog.Level.ToUpper() == "ERROR" || newLog.Level.ToUpper() == "CRITICAL")
    {
        try
        {
            using var httpClient = new HttpClient();
            
            // El mensaje que llegará a tu celular/Discord
            var discordMessage = new 
            { 
                content = $"🚨 **¡ALERTA EN LOGPATH!** 🚨\n**Nivel:** {newLog.Level}\n**Acción:** {newLog.Action}\n**Operador:** {newLog.UserName}\n**Detalles:** {newLog.Details}" 
            };

            string webhookUrl = "https://discordapp.com/api/webhooks/1488008412968255579/GnoAKWADQp-iZ0Q0MobtSMt_XOCn-paLvKMi86WNrfCcsEqLurXGY7hN2ZEj81U_PNmH"; 
            
            // Disparamos la alerta sin bloquear la respuesta de la API
            await httpClient.PostAsJsonAsync(webhookUrl, discordMessage);
        }
        catch (Exception ex)
        {
            // Si Discord falla, lo ignoramos para no tirar nuestra API
            Console.WriteLine($"Error al enviar alerta a Discord: {ex.Message}");
        }
    }
    
    return Results.Created($"/api/logs/{newLog.Id}", newLog);
});

// 5. Auto-migraciones al arrancar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();