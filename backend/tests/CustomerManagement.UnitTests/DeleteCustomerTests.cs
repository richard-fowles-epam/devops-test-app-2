using System.Net;
using System.Net.Http.Json;
using CustomerManagement.Api.Models;

namespace CustomerManagement.UnitTests;

public class DeleteCustomerTests
{
    [Fact]
    public async Task DeleteCustomer_WithExistingId_DeletesCustomerAndReturnsNoContent()
    {
        // Arrange: seed one customer via POST, then delete it.
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        var created = await (await client.PostAsJsonAsync("/customers", new AddCustomerRequest
        {
            FirstName = "Grace",
            LastName = "Hopper",
            Email = "grace@example.com"
        })).Content.ReadFromJsonAsync<Customer>();

        // Act
        var deleteResponse = await client.DeleteAsync($"/customers/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteCustomer_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/customers/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
