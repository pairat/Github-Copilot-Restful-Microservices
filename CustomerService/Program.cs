using CustomerService.Settings;
using CustomerService.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// Configure Guid serialization globally before any other MongoDB-related code
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection(nameof(MongoDbSettings)));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    // MongoClient will use the globally registered GuidSerializer
    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    var database = client.GetDatabase(settings.DatabaseName);
    return database.GetCollection<Customer>(settings.CollectionName);
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Define Customer API endpoints
app.MapGet("/api/customers", async (IMongoCollection<Customer> collection) =>
{
    var customers = await collection.Find(_ => true).ToListAsync();
    return Results.Ok(customers);
});

// Name the GET by ID route to be used by CreatedAtRoute
app.MapGet("/api/customers/{id}", async (IMongoCollection<Customer> collection, Guid id) =>
{
    var customer = await collection.Find(c => c.Id == id).FirstOrDefaultAsync();
    return customer is not null ? Results.Ok(customer) : Results.NotFound();
}).WithName("GetCustomerById");

app.MapPost("/api/customers", async (IMongoCollection<Customer> collection, CustomerCreateDto customerDto) =>
{
    if (string.IsNullOrWhiteSpace(customerDto.FirstName) || 
        string.IsNullOrWhiteSpace(customerDto.LastName) || 
        string.IsNullOrWhiteSpace(customerDto.EmailAddress))
    {
        // This basic validation can be removed if relying solely on model validation attributes for DTOs
        return Results.BadRequest("First name, last name, and email address are required.");
    }

    var customer = new Customer
    {
        Id = Guid.NewGuid(), // Server generates ID
        FirstName = customerDto.FirstName,
        LastName = customerDto.LastName,
        EmailAddress = customerDto.EmailAddress
    };

    await collection.InsertOneAsync(customer);
    return Results.CreatedAtRoute("GetCustomerById", new { id = customer.Id }, customer); // Return the full Customer object
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
