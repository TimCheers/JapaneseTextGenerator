using System.Text;
using KotobaApi.Data;
using KotobaApi.Services;
using KotobaApi.Srs.Learning;
using KotobaApi.Srs.Reading;
using KotobaApi.Srs.Scheduling;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<ITokenService, JwtTokenService>();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer =  true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// FSRS-7 scheduler (stateless) + in-memory learning engine (experimental store).
// Engine must stay singleton while in-memory so progress survives across requests.
// Data is reset on restart by design; replace with persistent store later without touching the scheduler.
builder.Services.AddSingleton<IFsrsScheduler, Fsrs7Scheduler>();
builder.Services.AddSingleton<ReadingEngagementPolicy>();
builder.Services.AddSingleton<InMemoryLearningEngine>();


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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();