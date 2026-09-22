using KotobaApi.Data;
using KotobaApi.Services;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllers();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDeckService, DeckService>();
builder.Services.AddScoped<IWordService, WordService>();
builder.Services.AddScoped<IWordProgressService, WordProgressService>();
builder.Services.AddScoped<IGenerationRequestService, GenerationRequestService>();
builder.Services.AddScoped<IGeneratedTextService, GeneratedTextService>();
builder.Services.AddScoped<IComprehensionQuestionService, ComprehensionQuestionService>();
builder.Services.AddScoped<IPracticeAttemptService, PracticeAttemptService>();
builder.Services.AddScoped<IUserAnswerService, UserAnswerService>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention());

var app = builder.Build();

app.UseCors("Frontend");
app.MapControllers();
app.Run();