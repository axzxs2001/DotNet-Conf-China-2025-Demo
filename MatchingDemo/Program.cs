using MatchingDemo;
using Microsoft.AspNetCore.Mvc;























var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddSingleton<JobManagerPlugin>();
var app = builder.Build();
app.UseStaticFiles();

app.MapGet("/motivation", (IAIService aiService) =>
{
    return aiService.ApplyingMotivationAsync();
});

app.MapGet("/translate", (IAIService aiService) =>
{
    return aiService.PerfectTranslationAsync();
});
app.MapGet("/culturetra", (IAIService aiService) =>
{
    var cv = File.ReadAllText("CV.md");
    return aiService.CultureTranslationAsync(cv);
});

app.MapPost("/optimizephoto", (IAIService aiService, [FromBody] PhotoData photoData) =>
{
    return aiService.OptimizePhotosAsync(photoData.Name, photoData.Prompt);
});

app.MapGet("/search", async (IAIService aiService, string prompt) =>
{
    return new JsonResult(await aiService.MatchJobsAsync(prompt));
});

app.Run();


class PhotoData
{
    public string Name { get; set; }
    public string Prompt { get; set; }
}