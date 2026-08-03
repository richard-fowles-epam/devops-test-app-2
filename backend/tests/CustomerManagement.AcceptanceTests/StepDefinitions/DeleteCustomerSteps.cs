using CustomerManagement.AcceptanceTests.Support;
using TechTalk.SpecFlow;
using Xunit;

namespace CustomerManagement.AcceptanceTests.StepDefinitions;

[Binding]
public sealed class DeleteCustomerSteps
{
    private readonly ScenarioWorld _world;
    private readonly ScenarioState _state;

    public DeleteCustomerSteps(ScenarioWorld world, ScenarioState state)
    {
        _world = world;
        _state = state;
    }

    [When(@"the customer is deleted by their ID")]
    public async Task WhenTheCustomerIsDeletedByTheirId_ExistingCustomer_SendsDeleteRequest()
    {
        Assert.NotNull(_world.CreatedCustomer);
        _state.Response = await _world.Client.DeleteAsync($"/customers/{_world.CreatedCustomer!.Id}");
    }

    [When(@"a customer is deleted with a non-existent ID")]
    public async Task WhenACustomerIsDeletedWithANonExistentId_MissingCustomer_SendsDeleteRequest()
    {
        const int nonExistentCustomerId = 99999;
        _state.Response = await _world.Client.DeleteAsync($"/customers/{nonExistentCustomerId}");
    }

    [When(@"the deleted customer is requested by their ID")]
    public async Task WhenTheDeletedCustomerIsRequestedByTheirId_DeletedCustomer_SendsGetRequest()
    {
        Assert.NotNull(_world.CreatedCustomer);
        _state.Response = await _world.Client.GetAsync($"/customers/{_world.CreatedCustomer!.Id}");
    }
}
