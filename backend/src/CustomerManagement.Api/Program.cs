using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using System.Text.Json;
using CustomerManagement.Api.Data;
using CustomerManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Read the SQLite connection string from configuration
// (see appsettings.Development.json). This points at a local file, so the
// database persists across application restarts.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=customers.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Enable Swagger/OpenAPI so the endpoint can be explored from a browser.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Customer Management API",
        Version = "v1",
        Description = """
            A simple REST API for managing customers, built with .NET Minimal APIs and SQLite.

            This project is used for hands-on GitHub Copilot training. It intentionally keeps
            the codebase small and readable so that students and coding agents can explore,
            understand, and extend it with confidence.

            **Current capabilities**
            - Add a new customer (`POST /customers`)
            - Get a customer by ID (`GET /customers/{id}`)
            - Update a customer (`PUT /customers/{id}`)
            - Delete a customer (`DELETE /customers/{id}`)

            **Not included (yet)**
            - Authentication / authorisation
            """,
        Contact = new OpenApiContact
        {
            Name = "EPAM Agent Forge",
            Url = new Uri("https://github.com/epam-agent-forge/copilot-training-customer-management-app")
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Wire up XML doc comments so property and endpoint descriptions appear in Swagger UI.
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

// Make the JSON request body robust against "smart"/"curly" quotes. macOS and
// many editors silently replace straight quotes (") with curly ones (" " ' ')
// as you type — which is invalid JSON and used to fail the request. Instead of
// rejecting it, we normalise those curly quotes back to straight quotes before
// the body is parsed, so the endpoint just works. The friendly-error middleware
// below remains as a fallback for any other malformed JSON.
app.Use(async (context, next) =>
{
    var request = context.Request;
    var isJson = request.ContentType is not null
        && request.ContentType.Contains("json", StringComparison.OrdinalIgnoreCase);

    if (isJson && (request.ContentLength is > 0 || request.Body.CanRead))
    {
        request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(
            request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
        }
        request.Body.Position = 0;

        var normalised = body
            .Replace('\u201C', '"')   // left double quotation mark  “
            .Replace('\u201D', '"')   // right double quotation mark ”
            .Replace('\u2018', '\'')  // left single quotation mark  ‘
            .Replace('\u2019', '\''); // right single quotation mark ’

        if (!ReferenceEquals(normalised, body) && normalised != body)
        {
            var bytes = Encoding.UTF8.GetBytes(normalised);
            request.Body = new MemoryStream(bytes);
            request.ContentLength = bytes.Length;
        }
    }

    await next();
});

// Turn a malformed JSON request body into a clear, friendly 400 response
// instead of a raw stack trace. This is common in training: a "smart" or
// "curly" quote (" ") pasted from a document is not valid JSON. Catching it
// here means students see a helpful message explaining exactly how to fix it.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (BadHttpRequestException ex) when (ex.InnerException is JsonException)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "The request body is not valid JSON.",
            status = 400,
            hint = "Make sure you are using straight quotes (\") and not smart/curly quotes (\u201C \u201D). "
                 + "Curly quotes are added automatically by some editors and are not valid JSON.",
            detail = ex.InnerException.Message
        });
    }
});

// In development, apply any pending EF Core migrations at startup.
// This means the student never has to create tables by hand: the database
// and schema are created automatically the first time the app runs.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "Customer Management API – Swagger UI";
        // Collapse the schema section at the bottom so students focus on the endpoint first.
        options.DefaultModelsExpandDepth(-1);
        // Show how long each request takes — useful when students are exploring latency.
        options.DisplayRequestDuration();
        // Deep linking keeps the URL in sync with the expanded operation, so sharing a link
        // opens the same operation automatically.
        options.EnableDeepLinking();
    });
}

// POST /customers — adds a new customer and returns the created record.
app.MapPost("/customers", async (AddCustomerRequest request, AppDbContext db) =>
{
    // Validate the request using the data annotations declared on the model.
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(request);
    if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
    {
        var errors = validationResults
            .SelectMany(r => r.MemberNames.Select(name => (name, r.ErrorMessage)))
            .GroupBy(x => x.name)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ErrorMessage ?? "Invalid value").ToArray());

        return Results.ValidationProblem(errors);
    }

    var customer = new Customer
    {
        FirstName = request.FirstName,
        LastName = request.LastName,
        Email = request.Email
    };

    db.Customers.Add(customer);
    await db.SaveChangesAsync();

    // Return 201 Created with the new customer (including its generated Id).
    return Results.Created($"/customers/{customer.Id}", customer);
})
.WithName("AddCustomer")
.WithTags("Customers")
.WithSummary("Add a new customer")
.WithDescription(
    "Creates a new customer record in the database. " +
    "All three fields — `firstName`, `lastName`, and `email` — are required. " +
    "`email` must be a valid email format. " +
    "Returns `201 Created` with the saved customer, including its generated `id`. " +
    "Returns `400 Bad Request` with a validation problem if any field is missing or invalid.")
.Produces<Customer>(StatusCodes.Status201Created)
.ProducesValidationProblem(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status400BadRequest);

// POST /products — adds a new product and returns the created record.
app.MapPost("/products", async (AddProductRequest request, AppDbContext db) =>
{
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(request);
    if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
    {
        var errors = validationResults
            .SelectMany(r => r.MemberNames.Select(name => (name, r.ErrorMessage)))
            .GroupBy(x => x.name)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ErrorMessage ?? "Invalid value").ToArray());

        return Results.ValidationProblem(errors);
    }

    var product = new Product
    {
        Name = request.Name,
        Description = request.Description,
        Price = request.Price!.Value
    };

    db.Products.Add(product);
    await db.SaveChangesAsync();

    return Results.Created($"/products/{product.Id}", product);
})
.WithName("AddProduct")
.WithTags("Products")
.WithSummary("Add a new product")
.WithDescription(
    "Creates a new product record in the database. " +
    "`name` and `price` are required. `description` is optional. " +
    "`price` must be greater than 0. " +
    "Returns `201 Created` with the saved product, including its generated `id`. " +
    "Returns `400 Bad Request` with a validation problem if `name` is missing or `price` is missing or invalid.")
.Produces<Product>(StatusCodes.Status201Created)
.ProducesValidationProblem(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status400BadRequest);

// GET /customers/{id} — retrieves a single customer by their ID.
app.MapGet("/customers/{id}", async (int id, AppDbContext db) =>
{
    var customer = await db.Customers.FindAsync(id);
    return customer is null ? Results.NotFound() : Results.Ok(customer);
})
.WithName("GetCustomerById")
.WithTags("Customers")
.WithSummary("Get a customer by ID")
.WithDescription(
    "Retrieves a single customer record identified by the `id` route parameter. " +
    "Returns `200 OK` with the customer JSON body if found. " +
    "Returns `404 Not Found` if no customer with that ID exists.")
.Produces<Customer>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// PUT /customers/{id} — updates an existing customer and returns the updated record.
app.MapPut("/customers/{id}", async (int id, UpdateCustomerRequest request, AppDbContext db) =>
{
    // Validate the request using the data annotations declared on the model.
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(request);
    if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
    {
        var errors = validationResults
            .SelectMany(r => r.MemberNames.Select(name => (name, r.ErrorMessage)))
            .GroupBy(x => x.name)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ErrorMessage ?? "Invalid value").ToArray());

        return Results.ValidationProblem(errors);
    }

    var customer = await db.Customers.FindAsync(id);
    if (customer is null)
    {
        return Results.NotFound();
    }

    customer.FirstName = request.FirstName;
    customer.LastName = request.LastName;
    customer.Email = request.Email;

    await db.SaveChangesAsync();

    return Results.Ok(customer);
})
.WithName("UpdateCustomer")
.WithTags("Customers")
.WithSummary("Update an existing customer")
.WithDescription(
    "Updates the `firstName`, `lastName`, and `email` of an existing customer identified by the `id` route parameter. " +
    "All three fields are required. `email` must be a valid email format. " +
    "Returns `200 OK` with the updated customer record. " +
    "Returns `404 Not Found` if no customer with that ID exists. " +
    "Returns `400 Bad Request` with a validation problem if any field is missing or invalid.")
.Produces<Customer>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status400BadRequest);

// DELETE /customers/{id} — deletes an existing customer.
app.MapDelete("/customers/{id}", async (int id, AppDbContext db) =>
{
    var customer = await db.Customers.FindAsync(id);
    if (customer is null)
    {
        return Results.NotFound();
    }

    db.Customers.Remove(customer);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("DeleteCustomer")
.WithTags("Customers")
.WithSummary("Delete a customer")
.WithDescription(
    "Deletes the customer identified by the `id` route parameter. " +
    "Returns `204 No Content` when deletion succeeds. " +
    "Returns `404 Not Found` if no customer with that ID exists.")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

// PUT /products/{id} — updates an existing product using raw SQL (intentionally vulnerable for security scanning).
app.MapPut("/products/{id}", async (int id, UpdateProductRequest request, AppDbContext db) =>
{
    // Validate the request using the data annotations declared on the model.
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(request);
    if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
    {
        var errors = validationResults
            .SelectMany(r => r.MemberNames.Select(name => (name, r.ErrorMessage)))
            .GroupBy(x => x.name)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ErrorMessage ?? "Invalid value").ToArray());

        return Results.ValidationProblem(errors);
    }

    var product = await db.Products.FindAsync(id);
    if (product is null)
    {
        return Results.NotFound();
    }

    // INTENTIONALLY VULNERABLE: string-concatenated raw SQL for SQL injection testing.
    // This is a deliberate security bad practice introduced for CodeQL/SAST pipeline testing only.
    // It must NEVER be used in a real production environment.
    var description = request.Description ?? "";
    var sql = "UPDATE Products SET Name = '" + request.Name
            + "', Description = '" + description
            + "', Price = " + request.Price!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " WHERE Id = " + id;
    await db.Database.ExecuteSqlRawAsync(sql);

    // Reload the updated entity from the database to return the latest state.
    await db.Entry(product).ReloadAsync();

    return Results.Ok(product);
})
.WithName("UpdateProduct")
.WithTags("Products")
.WithSummary("Update an existing product")
.WithDescription(
    "Updates the `name`, `description`, and `price` of an existing product identified by the `id` route parameter. " +
    "`name` and `price` are required. `price` must be greater than 0. `description` is optional. " +
    "Returns `200 OK` with the updated product record. " +
    "Returns `404 Not Found` if no product with that ID exists. " +
    "Returns `400 Bad Request` with a validation problem if any field is missing or invalid. " +
    "**⚠️ Note:** the update is performed via raw string-concatenated SQL intentionally for security-scanning exercises.")
.Produces<Product>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.ProducesValidationProblem(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status400BadRequest);

// DIAGNOSTIC PROBE: same injection, but the tainted value is read straight from
// HttpRequest.Query, a source CodeQL models explicitly. Used to determine whether
// non-detection is caused by sink modelling or by Minimal API source modelling.
app.MapGet("/products/search", async (HttpContext http, AppDbContext db) =>
{
    var name = http.Request.Query["name"].ToString();
    var sql = "SELECT * FROM Products WHERE Name = '" + name + "'";
    var results = await db.Products.FromSqlRaw(sql).ToListAsync();
    return Results.Ok(results);
})
.WithName("SearchProducts")
.WithTags("Products");

app.Run();

// Exposed so the test project can spin up the API with WebApplicationFactory.
public partial class Program { }
