using System.Net;
using System.Net.Http.Json;
using CustomerManagement.Api.Models;

namespace CustomerManagement.UnitTests;

public class AddProductTests
{
    [Fact]
    public async Task PostProducts_WithValidRequest_CreatesProductAndReturnsId()
    {
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        var request = new AddProductRequest
        {
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = 99.99m
        };

        var response = await client.PostAsJsonAsync("/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Equal("Keyboard", created.Name);
        Assert.Equal("Mechanical keyboard", created.Description);
        Assert.Equal(99.99m, created.Price);
    }

    [Fact]
    public async Task PostProducts_WithMissingName_ReturnsBadRequest()
    {
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        var request = new AddProductRequest
        {
            Name = "",
            Description = "Mechanical keyboard",
            Price = 99.99m
        };

        var response = await client.PostAsJsonAsync("/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostProducts_WithMissingPrice_ReturnsBadRequest()
    {
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        var request = new AddProductRequest
        {
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = null
        };

        var response = await client.PostAsJsonAsync("/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostProducts_WithNonPositivePrice_ReturnsBadRequest()
    {
        await using var factory = new CustomerApiFactory();
        var client = factory.CreateClient();

        var request = new AddProductRequest
        {
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = 0m
        };

        var response = await client.PostAsJsonAsync("/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
