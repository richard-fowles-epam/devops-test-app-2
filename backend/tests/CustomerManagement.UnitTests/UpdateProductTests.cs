using System.Net;
using System.Net.Http.Json;
using CustomerManagement.Api.Models;

namespace CustomerManagement.UnitTests;

public class UpdateProductTests
{
    [Fact]
    public async Task PutProduct_WithValidRequest_UpdatesProductAndReturnsOk()
    {
        // Arrange: seed one product via POST, then update it via PUT.
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        var created = await (await client.PostAsJsonAsync("/products", new AddProductRequest
        {
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = 99.99m
        })).Content.ReadFromJsonAsync<Product>();

        var updateRequest = new UpdateProductRequest
        {
            Name = "Mouse",
            Description = "Wireless mouse",
            Price = 49.99m
        };

        // Act
        var response = await client.PutAsJsonAsync($"/products/{created!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal("Mouse", updated.Name);
        Assert.Equal("Wireless mouse", updated.Description);
        Assert.Equal(49.99m, updated.Price);
    }

    [Fact]
    public async Task PutProduct_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        var updateRequest = new UpdateProductRequest
        {
            Name = "Mouse",
            Description = "Wireless mouse",
            Price = 49.99m
        };

        // Act
        var response = await client.PutAsJsonAsync("/products/99999", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutProduct_WithMissingName_ReturnsBadRequest()
    {
        // Arrange: seed one product via POST, then attempt to update with missing name.
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        var created = await (await client.PostAsJsonAsync("/products", new AddProductRequest
        {
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = 99.99m
        })).Content.ReadFromJsonAsync<Product>();

        var updateRequest = new UpdateProductRequest
        {
            Name = "",
            Description = "Wireless mouse",
            Price = 49.99m
        };

        // Act
        var response = await client.PutAsJsonAsync($"/products/{created!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutProduct_WithNonPositivePrice_ReturnsBadRequest()
    {
        // Arrange: seed one product via POST, then attempt to update with invalid price.
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        var created = await (await client.PostAsJsonAsync("/products", new AddProductRequest
        {
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = 99.99m
        })).Content.ReadFromJsonAsync<Product>();

        var updateRequest = new UpdateProductRequest
        {
            Name = "Mouse",
            Description = "Wireless mouse",
            Price = 0m
        };

        // Act
        var response = await client.PutAsJsonAsync($"/products/{created!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
