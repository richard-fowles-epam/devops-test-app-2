using System.Net.Http.Json;
using CustomerManagement.Api.Models;
using CustomerManagement.AcceptanceTests.Support;
using TechTalk.SpecFlow;
using Xunit;

namespace CustomerManagement.AcceptanceTests.StepDefinitions;

[Binding]
public sealed class UpdateCustomerSteps
{
    private readonly ScenarioWorld _world;
    private readonly ScenarioState _state;
    private Customer? _updatedCustomer;
    private UpdateCustomerRequest? _updateRequest;

    public UpdateCustomerSteps(ScenarioWorld world, ScenarioState state)
    {
        _world = world;
        _state = state;
    }

    [When(@"the customer is updated with the following details")]
    public async Task WhenTheCustomerIsUpdatedWithTheFollowingDetails(Table table)
    {
        Assert.NotNull(_world.CreatedCustomer);
        var row = table.Rows[0];
        _updateRequest = new UpdateCustomerRequest
        {
            FirstName = row["FirstName"],
            LastName = row["LastName"],
            Email = row["Email"]
        };
        _state.Response = await _world.Client.PutAsJsonAsync(
            $"/customers/{_world.CreatedCustomer!.Id}", _updateRequest);
    }

    [When(@"a customer update is submitted for a non-existent ID")]
    public async Task WhenACustomerUpdateIsSubmittedForANonExistentId(Table table)
    {
        const int nonExistentCustomerId = 99999;
        var row = table.Rows[0];
        var request = new UpdateCustomerRequest
        {
            FirstName = row["FirstName"],
            LastName = row["LastName"],
            Email = row["Email"]
        };
        _state.Response = await _world.Client.PutAsJsonAsync(
            $"/customers/{nonExistentCustomerId}", request);
    }

    [Then(@"the updated customer should match the new details")]
    public async Task ThenTheUpdatedCustomerShouldMatchTheNewDetails()
    {
        Assert.NotNull(_state.Response);
        Assert.NotNull(_updateRequest);

        if (_updatedCustomer is null)
        {
            var customer = await _state.Response!.Content.ReadFromJsonAsync<Customer>();
            Assert.NotNull(customer);
            _updatedCustomer = customer;
        }

        Assert.Equal(_updateRequest!.FirstName, _updatedCustomer!.FirstName);
        Assert.Equal(_updateRequest.LastName, _updatedCustomer.LastName);
        Assert.Equal(_updateRequest.Email, _updatedCustomer.Email);
    }
}
