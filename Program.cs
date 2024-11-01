using MPESA_V2_APIV2_MSISDN_DECRYPTER;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

DotEnv.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// Authentication and Authorization Services
builder.Services.AddAuthorization();
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY")))
        };
    });

// DATABASE Services
builder.Services
    .AddDbContext<DatabaseContext>(options => options.UseSqlite($"Data Source={Path.Combine(Directory.GetCurrentDirectory(), "src/data/sqlite/database.sqlite")}"))
    .AddDatabaseDeveloperPageExceptionFilter();

// SWAGGER Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "mpesa-msisdn-decrypter-api";
    config.Title = "MPesa MSISDN Decrypter API";
    config.Version = "v1";
});



var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
var url = $"http://127.0.0.1:{port}";

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    using var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    db.Database.EnsureCreated();
}


if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "MPesa MSISDN Decrypter API Documentation";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.MapGet("/error", () => Results.Problem("An error occurred.", statusCode: 500))
   .ExcludeFromDescription();

app.MapGet("/", () =>
{
    return Results.Ok<string>($"Welcome to the MPesa MSISDN Decrypter API. Visit {url}/swagger to view the API Documentation");
})
    .WithName("Home")
    .RequireAuthorization()
    .Produces<string>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized);

app.MapGet("/decrypt/{hash}", async (string hash, DatabaseContext db) =>
{
    if (hash == null || hash.Equals("")) return Results.BadRequest("Hash is required");
    string searchHash = hash.Replace(" ", "");
    string shortenedHash = CryptoUtils.ShortenHash(searchHash.ToUpper());

    if (shortenedHash.Contains("*"))
    {
        //    .FromSqlInterpolated($"SELECT * FROM PhoneNumbers WHERE hash LIKE {shortenedHash.Replace("*", "%")}")
        var potentialMatches = await db.PhoneNumbers
        .Where(p => EF.Functions.Like(p.Hash, $"%{shortenedHash.Replace("*", "%")}%"))
           .ToListAsync(); // Load data into memory

        // Step 2: Apply FuzzySharp scoring in memory
        var matches = potentialMatches
            .Select(row => new
            {
                Row = row,
                Ratio = FuzzySharp.Fuzz.Ratio(row.Hash, searchHash)
            })
            .OrderByDescending(x => x.Ratio) // Order by highest similarity score
            .Select(x => x.Row)
            .ToList();

        if (matches.Count == 0) return Results.NotFound("No phone number found for the given hash");
        return Results.Ok(new { status = true, phone = matches[0].Msisdn, hash });
    }
    else
    {
        Console.WriteLine($"Shortened Hash: {shortenedHash}");

        var matches = await db.PhoneNumbers.Where(phoneNumber => phoneNumber.Hash.Equals(shortenedHash)).ToListAsync();
        if (matches.Count == 0) return Results.NotFound("No phone number found for the given hash");
        return Results.Ok(new { status = true, phone = matches[0].Msisdn, hash });
    }
})
    .RequireAuthorization()
    .WithName("GetPhoneNumberByHash")
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces<PhoneNumber>(StatusCodes.Status200OK);

app.MapPost("/auth/get-token", async (HttpRequest request, DatabaseContext db) =>
{
    var Authorization = request.Headers["Authorization"];
    if (Authorization.Count == 0) return Results.Unauthorized();
    var token = Authorization[0].Split(" ")[1];

    var admins = await db.Users.Where(user => user.Role.Equals("admin")).ToListAsync();
    if (admins.Count == 0) return Results.BadRequest("No admin user found");

    var admin = admins[0];
    if (!admin.Token.Equals(token)) return Results.Unauthorized();
    // Create token
    var jwtToken = JWTUtils.GenerateJwtToken();
    return Results.Ok(new { status = true, token = jwtToken, expires = DateTime.Now.AddHours(1) });
})
    .WithName("GetToken")
    .Produces<string>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);

app.MapPost("/users/add", async (HttpRequest request, DatabaseContext db) =>
{
    try
    {
        var passkeys = request.Headers["x-passkey"];
        if (passkeys.Count == 0 || passkeys[0] == null) return Results.Unauthorized();
        var pass_key = passkeys[0];
        if (!(pass_key is not null) || pass_key.Length == 0) return Results.BadRequest("Pass Key Header is required");
        var adminToken = CryptoUtils.HashToString(pass_key);

        var admins = await db.Users.Where(user => user.Role.Equals("admin")).ToListAsync();
        if (admins.Count == 0) return Results.BadRequest("No admin user found");

        var admin = admins.Where(admin => admin.Email.Equals("gidraph@gidraphdanford.dev")).FirstOrDefault();
        if (admin == null || !admin.Token.Equals(adminToken)) return Results.Unauthorized();

        var user = await request.ReadFromJsonAsync<User>();
        if (user == null) return Results.BadRequest("Invalid User Data");

        if (user?.Name == null || user.Name.Equals("") || user.Name.Length < 5) return Results.BadRequest("Invalid User Name");
        if (user?.Email == null || user.Email.Equals("") || user.Email.Length < 10) return Results.BadRequest("Invalid User Email");
        if (user?.Role == null || user.Role.Equals("") || user.Role.Length < 5) return Results.BadRequest("Invalid User Role");

        UserRecord userRecord = new(user.Name, user.Email, user.Role, CryptoUtils.HashToString(user.Email + user.Name + DateTime.Now.ToString()));

        await db.Users.AddAsync(userRecord);
        await db.SaveChangesAsync();
        return Results.Created($"/users/{userRecord.Token}", userRecord);

    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error:: {ex.Message}");
        return Results.BadRequest("Invalid User Data");
    }
})
    .WithName("AddUser")
    .Produces<string>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);

app.MapPost("/db/split", async (HttpRequest request, DatabaseContext db) =>
{
    try
    {
        var passkeys = request.Headers["x-passkey"];
        if (passkeys.Count == 0 || passkeys[0] == null) return Results.Unauthorized();
        var pass_key = passkeys[0];
        if (!(pass_key is not null) || pass_key.Length == 0) return Results.BadRequest("Pass Key Header is required");
        var adminToken = CryptoUtils.HashToString(pass_key);

        var admins = await db.Users.Where(user => user.Role.Equals("admin")).ToListAsync();
        if (admins.Count == 0) return Results.BadRequest("No admin user found");

        var admin = admins.Where(admin => admin.Email.Equals("gidraph@gidraphdanford.dev")).FirstOrDefault();
        if (admin == null || !admin.Token.Equals(adminToken)) return Results.Unauthorized();

        DatabaseGenerator.SplitDatabase(db);

        return Results.Ok("Database Split Successfully");

    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error:: {ex.Message}");
        return Results.Unauthorized();
    }
})
    .WithName("SplitDatabase")
    .Produces<string>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized);

app.MapPost("/db/rehydrate", (HttpRequest request, DatabaseContext db) =>
{
    try
    {
        DatabaseGenerator.RehydrateDatabase(db);
        return Results.Ok("Database Rehydrated Successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error:: {ex.Message}");
        return Results.Unauthorized();
    }
})
    .RequireAuthorization()
    .WithName("RehydrateDatabase")
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces<PhoneNumber>(StatusCodes.Status200OK);

app.MapPost("/db/generate", (HttpRequest request, DatabaseContext db) =>
{
    try
    {
        DatabaseGenerator.PopulateDtabase(db);
        return Results.Ok("Database Generated Successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error:: {ex.Message}");
        return Results.Unauthorized();
    }
})
    .RequireAuthorization()
    .WithName("GenerateDatabase")
    .Produces(StatusCodes.Status401Unauthorized)
    .Produces<PhoneNumber>(StatusCodes.Status200OK);


app.UseAuthentication();
app.UseAuthorization();
app.Run();
