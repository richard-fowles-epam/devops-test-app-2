using System.Net;
using System.Net.Http.Json;
using CustomerManagement.Api.Models;

namespace CustomerManagement.UnitTests;

public class GetCustomerByIdTests
{
    [Fact]
    public async Task GetCustomer_WithExistingId_ReturnsOkWithCustomer()
    {
        // Arrange: seed one customer via POST, then GET it back by its generated ID.
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        var created = await (await client.PostAsJsonAsync("/customers", new AddCustomerRequest
        {
            FirstName = "Alan",
            LastName = "Turing",
            Email = "alan@example.com"
        })).Content.ReadFromJsonAsync<Customer>();

        // Act
        var response = await client.GetAsync($"/customers/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var customer = await response.Content.ReadFromJsonAsync<Customer>();
        Assert.NotNull(customer);
        Assert.Equal(created.Id, customer!.Id);
        Assert.Equal("Alan", customer.FirstName);
        Assert.Equal("Turing", customer.LastName);
        Assert.Equal("alan@example.com", customer.Email);
    }

    [Fact]
    public async Task GetCustomer_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/customers/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
