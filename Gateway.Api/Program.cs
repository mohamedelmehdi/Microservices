using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .ConfigureHttpClient((context, handler) =>
    {
        handler.AllowAutoRedirect = true;
    })
    .AddTransforms(transformBuilderContext =>
    {
        // Optionally add transforms
        transformBuilderContext.AddRequestTransform(async transformContext =>
        {
            var token = await transformContext.HttpContext.GetTokenAsync("access_token");
            if (token != null)
            {
                transformContext.ProxyRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        });
    })
    ;

//builder.Services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme).AddBearerToken();
// JWT Authentication setup
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://keycloak:8080/realms/ibsrealm"; // Keycloak server
        options.Audience = "account"; // Keycloak client
        options.RequireHttpsMetadata = false; // Use false if using HTTP in local dev

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            //ValidIssuer = "http://keycloak:8080/realms/ibsrealm",
            ValidIssuer = "http://localhost:5900/realms/ibsrealm",
            ValidateAudience = true,
            ValidAudience = "account",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
    });
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("custom-policy",
        policy => policy.RequireAuthenticatedUser().RequireRole("productsrole"));//  RequireClaim("custom-claim", true.ToString())); //
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapGet("/hello", () => "Hello from API Gateway");
app.MapGet("/test", async ([FromServices] HttpClient clientFactory) =>
{
    var clientHandler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
    };

    var client = new HttpClient(clientHandler);

    var url = "https://orders.api:5101/api/orders";
    try
    {
        var response = await client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return Results.Ok(content);
        }
        else
        {
            return Results.StatusCode((int)response.StatusCode);
        }
    }
    catch (Exception e)
    {
        return Results.BadRequest(e.Message);
    }
});

app.MapPost("/loginkeycloak", async (string username, string password) =>
{
    var client = new HttpClient();

    var keycloakTokenEndpoint = "http://keycloak:8080/realms/ibsrealm/protocol/openid-connect/token";

    var requestContent = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("client_id", "yarp-client"),
        new KeyValuePair<string, string>("username", username),
        new KeyValuePair<string, string>("password", password),
        new KeyValuePair<string, string>("grant_type", "password")
    });

    var request = new HttpRequestMessage(HttpMethod.Post, keycloakTokenEndpoint)
    {
        Content = requestContent
    };

    var response = await client.SendAsync(request);

    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        return Results.Ok(JsonDocument.Parse(content));
    }
    else
    {
        return Results.Unauthorized();
    }
});
app.MapGet("/", async context =>
{
    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        context.Response.Redirect("/auth/login");
        return;
    }

    await context.Response.WriteAsync("Welcome, authenticated user!");
});

// Login Endpoint
app.MapGet("/auth/login", async context =>
{
    var redirectUrl = "http://localhost:7200/realms/yourrealm/protocol/openid-connect/auth" +
                      "?client_id=yarp-client" +
                      "&response_type=code"; // +
                      //"&redirect_uri=http://your-gateway/api/callback";
    context.Response.Redirect(redirectUrl);
});

//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();
app.MapHealthChecks("health");

app.Run();