using System.Text;
using KotobaApi.Data;
using KotobaApi.Services;
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

// FSRS-7 scheduler (stateless). The DB-backed WordProgressService is the production
// persistence path now; InMemoryLearningEngine is kept only as the harness for
// SrsSchedulingTests/SrsIdempotencyTests and is intentionally not registered here.
builder.Services.AddSingleton<IFsrsScheduler, Fsrs7Scheduler>();

//builder.Services.AddHttpClient<IAiTextGenerationService, OpenAiTextGenerationService>();
builder.Services.AddHttpClient<IAiTextGenerationService, GeminiTextGenerationService>();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
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