using MatchingDemo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAIService, AIService>();

var app = builder.Build();



app.MapGet("/translate", (IAIService aiService) =>
{
    return aiService.PerfectTranslationAsync();
});

app.Run();
