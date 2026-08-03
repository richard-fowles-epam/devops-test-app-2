using System.Net;
using System.Net.Http.Json;
using CustomerManagement.Api.Models;
using CustomerManagement.AcceptanceTests.Support;
using TechTalk.SpecFlow;
using Xunit;

namespace CustomerManagement.AcceptanceTests.StepDefinitions;

[Binding]
public sealed class GetCustomerSteps
{
    private readonly ScenarioWorld _world;
    private readonly ScenarioState _state;
    private Customer? _retrievedCustomer;

    public GetCustomerSteps(ScenarioWorld world, ScenarioState state)
    {
        _world = world;
        _state = state;
    }

    [Given(@"the customer has been created via POST /customers")]
    public async Task GivenTheCustomerHasBeenCreatedViaPostCustomers_ValidCustomer_CustomerExists()
    {
        // Arrange
        Assert.NotNull(_world.Request);

        // Act
        var response = await _world.Client.PostAsJsonAsync("/customers", _world.Request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Customer>();
        Assert.NotNull(created);
        _world.CreatedCustomer = created;
    }

    [When(@"the customer is requested by their ID")]
    public async Task WhenCustomerIsRequestedByTheirId_ExistingCustomer_SendsGetRequest()
    {
        // Arrange
        Assert.NotNull(_world.CreatedCustomer);

        // Act
        _state.Response = await _world.Client.GetAsync($"/customers/{_world.CreatedCustomer!.Id}");
    }

    [When(@"a customer is requested with a non-existent ID")]
    public async Task WhenCustomerIsRequestedWithNonExistentId_MissingCustomer_SendsGetRequest()
    {
        // Arrange
        const int nonExistentCustomerId = 99999;

        // Act
        _state.Response = await _world.Client.GetAsync($"/customers/{nonExistentCustomerId}");
    }

    [Then(@"the get customer response status is 200")]
    public void ThenGetCustomerResponseStatus_OkResponse_Returns200()
    {
        // Assert
        Assert.NotNull(_state.Response);
        Assert.Equal(HttpStatusCode.OK, _state.Response!.StatusCode);
    }

    [Then(@"the get customer response status is 404")]
    public void ThenGetCustomerResponseStatus_NotFoundResponse_Returns404()
    {
        // Assert
        Assert.NotNull(_state.Response);
        Assert.Equal(HttpStatusCode.NotFound, _state.Response!.StatusCode);
    }

    [Then(@"the retrieved customer should match the created customer details")]
    public async Task ThenRetrievedCustomer_ExistingCustomer_MatchesCreatedCustomerDetails()
    {
        // Arrange
        Assert.NotNull(_state.Response);
        Assert.NotNull(_world.CreatedCustomer);

        // Act
        if (_retrievedCustomer is null)
        {
            var customer = await _state.Response!.Content.ReadFromJsonAsync<Customer>();
            Assert.NotNull(customer);
            _retrievedCustomer = customer;
        }

        // Assert
        Assert.Equal(_world.CreatedCustomer!.Id, _retrievedCustomer!.Id);
        Assert.Equal(_world.CreatedCustomer.FirstName, _retrievedCustomer.FirstName);
        Assert.Equal(_world.CreatedCustomer.LastName, _retrievedCustomer.LastName);
        Assert.Equal(_world.CreatedCustomer.Email, _retrievedCustomer.Email);
    }
}
