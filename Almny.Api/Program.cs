using Almny.Api;
using Almny.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDependency(builder.Configuration)
    .AddAuthConfig(builder.Configuration)
    .AddRateLimitingConfig(builder.Configuration)
    .AddFluentValidation()
    .AddMapsterDependency()
    .AddSwaggerConfig();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

await app.SeedRolesAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
