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

// 6. ENDPOINT GET: Auditoría Forense con Inteligencia Artificial
app.MapGet("/api/logs/analyze", async (AppDbContext db) =>
{
    // 1. Extraemos los últimos 50 logs de la base de datos
    var logs = await db.PosEvents.OrderByDescending(l => l.Timestamp).Take(50).ToListAsync();
    
    if (!logs.Any()) return Results.BadRequest("No hay suficientes datos para analizar.");

    // 2. Preparamos el resumen para la IA
    var logSummary = string.Join("\n", logs.Select(l => 
        $"[{l.Timestamp:HH:mm}] Usuario: {l.UserName} | Acción: {l.Action} | Nivel: {l.Level}"
    ));

    // El Prompt que le da el rol a la IA
    var prompt = $"Eres un auditor experto en ciberseguridad y prevención de fraude en sistemas de Punto de Venta (POS). Analiza estos últimos 50 eventos. Detecta patrones inusuales, posibles fraudes (ej. muchas cancelaciones del mismo cajero) o fallas recurrentes. Dame un reporte técnico, profesional y muy breve en 3 puntos clave (usa viñetas):\n\n{logSummary}";

    // 3. Llamada REAL a la API de Inteligencia Artificial (Gemini)
    string geminiApiKey = builder.Configuration["GeminiApiKey"] ?? "";
string geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={geminiApiKey}";
    using var httpClient = new HttpClient();
    var requestBody = new
    {
        contents = new[] { new { parts = new[] { new { text = prompt } } } }
    };

try
    {
        var response = await httpClient.PostAsJsonAsync(geminiUrl, requestBody);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Gemini API Error: {errorContent}");
            // Devolvemos un 400 con un mensaje para que Angular lo muestre
            return Results.BadRequest(new { analysis = "Fallo la validación con Google AI. Revisa tu API Key." });
        }

        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        
        // Extraemos la respuesta de la IA
        var aiText = result.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

        return Results.Ok(new { analysis = aiText });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Excepción de código en IA: {ex.Message}");
        return Results.BadRequest(new { analysis = "El auditor de IA no está disponible debido a un error interno." });
    }
});

app.Run();