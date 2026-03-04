using Connectivity.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Named HttpClient for GitHub
builder.Services.AddHttpClient("github", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ConnectivityApp/1.0");
});

// DI
builder.Services.AddScoped<IGitHubService, GitHubService>();

// CORS: allow only your frontend origins
const string FrontendCorsPolicy = "FrontendOnly";
var allowedOrigins = new[]
{
    "http://localhost:3000",
    // "https://localhost:3000",
};

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders(
                "Content-Type",
                "Authorization",
                "X-Pat-Token",
                "X-GitHub-Token",
                "X-GitHub-Repo"
            )
            .WithExposedHeaders("Location", "X-RateLimit-Remaining");
    });
});

// (Optional) Forwarded headers
builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();