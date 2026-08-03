using System.Net;
using System.Net.Http.Json;
using CustomerManagement.Api.Models;

namespace CustomerManagement.UnitTests;

public class UpdateCustomerTests
{
    [Fact]
    public async Task PutCustomer_WithValidRequest_UpdatesCustomerAndReturnsOk()
    {
        // Arrange: seed one customer via POST, then update it via PUT.
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        var created = await (await client.PostAsJsonAsync("/customers", new AddCustomerRequest
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com"
        })).Content.ReadFromJsonAsync<Customer>();

        var updateRequest = new UpdateCustomerRequest
        {
            FirstName = "Alan",
            LastName = "Turing",
            Email = "alan@example.com"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/customers/{created!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<Customer>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal("Alan", updated.FirstName);
        Assert.Equal("Turing", updated.LastName);
        Assert.Equal("alan@example.com", updated.Email);
    }

    [Fact]
    public async Task PutCustomer_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        var updateRequest = new UpdateCustomerRequest
        {
            FirstName = "Alan",
            LastName = "Turing",
            Email = "alan@example.com"
        };

        // Act
        var response = await client.PutAsJsonAsync("/customers/99999", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutCustomer_WithMissingRequiredField_ReturnsBadRequest()
    {
        // Arrange: seed one customer via POST, then attempt to update with an invalid request.
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        var created = await (await client.PostAsJsonAsync("/customers", new AddCustomerRequest
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com"
        })).Content.ReadFromJsonAsync<Customer>();

        var updateRequest = new UpdateCustomerRequest
        {
            FirstName = "",
            LastName = "Turing",
            Email = "alan@example.com"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/customers/{created!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutCustomer_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange: seed one customer via POST, then attempt to update with an invalid email.
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        var created = await (await client.PostAsJsonAsync("/customers", new AddCustomerRequest
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com"
        })).Content.ReadFromJsonAsync<Customer>();

        var updateRequest = new UpdateCustomerRequest
        {
            FirstName = "Alan",
            LastName = "Turing",
            Email = "not-a-valid-email"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/customers/{created!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
