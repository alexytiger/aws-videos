using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime;
using Customers.Api.Contracts.Responses;
using Customers.Api.Repositories;
using Customers.Api.Services;
using Customers.Api.Validation;
using FastEndpoints;
using FastEndpoints.Swagger;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddFastEndpoints();
builder.Services.AddSwaggerDocument();

builder.Services.AddSingleton<IAmazonDynamoDB>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    var chain = new CredentialProfileStoreChain();
    AWSCredentials awsCredentials;

    if (!chain.TryGetAWSCredentials("badtiger-aws-profile", out awsCredentials))
    {
        logger.LogError("Failed to get AWS credentials for DynamoDB.");

        // Throwing an exception here ensures that the application does not start with invalid AWS credentials
        throw new InvalidOperationException("Failed to get AWS credentials for DynamoDB.");
    }

    // Create the AmazonDynamoDBClient with the obtained credentials
    return new AmazonDynamoDBClient(awsCredentials, RegionEndpoint.USEast1); // Specify your desired region
});

builder.Services.AddSingleton<ICustomerRepository>(provider =>
    new CustomerRepository(provider.GetRequiredService<IAmazonDynamoDB>(),
        config.GetValue<string>("Database:TableName")));
builder.Services.AddSingleton<ICustomerService, CustomerService>();

var app = builder.Build();

app.UseMiddleware<ValidationExceptionMiddleware>();
app.UseFastEndpoints(x =>
{
    x.Errors.ResponseBuilder = (failures, _, _) =>
    {
        return new ValidationFailureResponse
        {
            Errors = failures.Select(y => y.ErrorMessage).ToList()
        };
    };
});

app.UseOpenApi();
app.UseSwaggerUi(s => s.ConfigureDefaults());

app.Run();
