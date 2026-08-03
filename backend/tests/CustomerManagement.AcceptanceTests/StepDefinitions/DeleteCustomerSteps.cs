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
        // Arrange
        Assert.NotNull(_world.CreatedCustomer);

        // Act
        _state.Response = await _world.Client.DeleteAsync($"/customers/{_world.CreatedCustomer!.Id}");
    }

    [When(@"a customer is deleted with a non-existent ID")]
    public async Task WhenACustomerIsDeletedWithANonExistentId_MissingCustomer_SendsDeleteRequest()
    {
        // Arrange
        const int nonExistentCustomerId = 99999;

        // Act
        _state.Response = await _world.Client.DeleteAsync($"/customers/{nonExistentCustomerId}");
    }
}
