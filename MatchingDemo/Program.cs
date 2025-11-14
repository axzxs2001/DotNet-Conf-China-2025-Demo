using MatchingDemo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAIService, AIService>();

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
app.Run();
