var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// The gateway currently doesn't configure a concrete authentication scheme yet (e.g. JWT).
// However, we still want the pipeline middleware to be present without failing startup.
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// CORS: when the client sends credentials: 'include', the browser requires a specific origin (not *).
// Allow localhost/127.0.0.1 with any port so the dashboard (e.g. http://localhost:5174) can POST to the gateway.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrEmpty(origin)) return false;
            try
            {
                var uri = new Uri(origin);
                return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        })
            .AllowCredentials()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Log resolved reverse-proxy cluster addresses at startup (env from Aspire overrides appsettings)
var config = app.Configuration;
var tenantAddress = config["ReverseProxy:Clusters:tenant:Destinations:destination1:Address"];
var identityAddress = config["ReverseProxy:Clusters:identity:Destinations:destination1:Address"];
app.Logger.LogInformation(
    "Reverse proxy cluster addresses: tenant={Tenant}, identity={Identity}",
    tenantAddress ?? "(not set)",
    identityAddress ?? "(not set)");

// Log every request (method + path) so we can confirm POST /api/tenant/with-users reaches the gateway and is proxied
app.Use(async (context, next) =>
{
    app.Logger.LogInformation("Incoming request: {Method} {Path}", context.Request.Method, context.Request.Path);
    await next(context);
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();
app.MapDefaultEndpoints();

await app.RunAsync();
