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
    public async Task WhenTheCustomerIsDeletedByTheirId_ExistingCustomer_SendsDeleteRequest()
    {
        Assert.NotNull(__world.CreatedCustomer);
        __state.Response = await __world.Client.DeleteAsync($"/customers/{__world.CreatedCustomer!.Id}");
    }

    [When(@"a customer is deleted with a non-existent ID")]
    public async Task WhenACustomerIsDeletedWithANonExistentId_MissingCustomer_SendsDeleteRequest()
    {
        const int nonExistentCustomerId = 99999;
        __state.Response = await __world.Client.DeleteAsync($"/customers/{nonExistentCustomerId}");
    }

    [When(@"the deleted customer is requested by their ID")]
    public async Task WhenTheDeletedCustomerIsRequestedByTheirId_DeletedCustomer_SendsGetRequest()
    {
        Assert.NotNull(__world.CreatedCustomer);
        __state.Response = await __world.Client.GetAsync($"/customers/{__world.CreatedCustomer!.Id}");
    }
}
