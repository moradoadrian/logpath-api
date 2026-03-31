using LogPath.Api.Data;
using LogPath.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

// A continuación se muestra el código completo de Program.cs, que es el punto de entrada de la aplicación ASP.NET Core. 
//Este código configura los servicios necesarios, define los endpoints para la API y maneja la lógica de negocio relacionada con los logs de eventos del punto de venta (POS). 
//Además, incluye una integración con Discord para enviar alertas en caso de errores críticos y un endpoint para realizar un análisis forense simulado utilizando inteligencia artificial.
//Se tenia pensado usar ollama pero se decidió simular la respuesta para evitar dependencias externas y mantener la simplicidad del proyecto.
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

// 6. ENDPOINT GET: Auditoría Forense con Inteligencia Artificial (Modo Respaldo Seguro)
app.MapGet("/api/logs/analyze", async (AppDbContext db) =>
{
    try
    {
        // 1. Extraemos los últimos 50 logs de TU base de datos
        var logs = await db.PosEvents.OrderByDescending(l => l.Timestamp).Take(50).ToListAsync();
        
        if (!logs.Any()) return Results.BadRequest(new { analysis = "No hay suficientes datos para analizar." });

        // 2. Simulamos el análisis contando los errores reales
        int errorCount = logs.Count(l => l.Level.ToUpper() == "ERROR" || l.Level.ToUpper() == "CRITICAL");
        
        // 3. Armamos el reporte dinámicamente
        string report = "🤖 [Análisis Forense Completado]\n\n";
        
        if (errorCount == 0)
        {
            report += "• Estado General: El flujo transaccional de tu negocio es estable.\n";
            report += "• Anomalías: No se detectaron patrones de fraude ni cancelaciones masivas.\n";
            report += "• Recomendación: Mantener el monitoreo actual.";
        }
        else
        {
            report += $"• Estado General: Se detectaron {errorCount} fallas críticas recientes.\n";
            report += "• Anomalías: Posible degradación de hardware (impresoras) o errores de operador.\n";
            report += "• Recomendación: Notificar a soporte técnico y auditar los últimos tickets.";
        }

        // Simulamos que la IA está "pensando" por 2 segundos para el efecto visual
        await Task.Delay(2000);

        return Results.Ok(new { analysis = report });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error simulando IA: {ex.Message}");
        return Results.BadRequest(new { analysis = "El motor de análisis está temporalmente fuera de servicio." });
    }
});

app.Run();