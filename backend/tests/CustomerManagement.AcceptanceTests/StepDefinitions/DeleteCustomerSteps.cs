using CustomerManagement.AcceptanceTests.Support;
using TechTalk.SpecFlow;
using Xunit;

namespace CustomerManagement.AcceptanceTests.StepDefinitions;

[Binding]
public sealed class DeleteCustomerSteps
{
    private readonly ScenarioWorld __world;
    private readonly ScenarioState __state;

    public DeleteCustomerSteps(ScenarioWorld world, ScenarioState state)
    {
        __world = world;
        __state = state;
    }

    [When(@"the customer is deleted by their ID")]
    public async Task WhenCustomerIsDeletedByTheirId_ExistingCustomer_SendsDeleteRequest()
    {
        // Arrange
        Assert.NotNull(__world.CreatedCustomer);

        // Act
        __state.Response = await __world.Client.DeleteAsync($"/customers/{__world.CreatedCustomer!.Id}");
    }

    [When(@"a customer is deleted with a non-existent ID")]
    public async Task WhenCustomerIsDeletedWithNonExistentId_MissingCustomer_SendsDeleteRequest()
    {
        // Arrange
        const int nonExistentCustomerId = 99999;

        // Act
        __state.Response = await __world.Client.DeleteAsync($"/customers/{nonExistentCustomerId}");
    }

    [When(@"the deleted customer is requested by their ID")]
    public async Task WhenDeletedCustomerIsRequestedByTheirId_DeletedCustomer_SendsGetRequest()
    {
        // Arrange
        Assert.NotNull(__world.CreatedCustomer);

        // Act
        __state.Response = await __world.Client.GetAsync($"/customers/{__world.CreatedCustomer!.Id}");
    }
}
