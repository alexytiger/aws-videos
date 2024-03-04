using Amazon;
using Amazon.DynamoDBv2;
using Customers.Api.Contracts.Responses;
using Customers.Api.Repositories;
using Customers.Api.Services;
using Customers.Api.Validation;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var config = builder.Configuration;

// AWS Lambda Hosting
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

// FastEndpoints and Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddFastEndpoints();
builder.Services.AddSwaggerDocument(config =>
{
    config.Title = "Customers API"; // Customize your Swagger documentation title
    config.Version = "v1";
    // Additional Swagger configurations can be added here
});

// AWS DynamoDB Client
builder.Services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient(RegionEndpoint.USEast1));

// Services and Repositories
builder.Services.AddSingleton<ICustomerRepository>(provider =>
    new CustomerRepository(provider.GetRequiredService<IAmazonDynamoDB>(),
        config.GetValue<string>("Database:TableName")));
builder.Services.AddSingleton<ICustomerService, CustomerService>();

var app = builder.Build();

// Custom Middleware for Validation Exceptions
app.UseMiddleware<ValidationExceptionMiddleware>();

// FastEndpoints Configuration
app.UseFastEndpoints();

// Swagger UI
app.UseOpenApi(); // Serves the OpenAPI/Swagger JSON document
app.UseSwaggerUi(s => s.ConfigureDefaults()); // Serves the Swagger UI with defaults

app.Run();
