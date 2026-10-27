using System.Net;
using System.Net.Http.Json;
using Logiwall.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Logiwall.IntegrationTests.People;

// Regression tests for plan 002: a contact resolves only for its owner.
[Collection("api")]
public class CustomerContactOwnershipTests(SqlServerFixture fixture)
{
    private async Task<(HttpClient Owner, HttpClient Other, int ContactId)> ArrangeAsync()
    {
        var api = fixture.CreateApiClient();

        var owner = api.Authed(await api.LoginWithOtpAsync(ApiClient.NewPhone()));
        await ApiClient.RegisterAsync(owner, "Strong-Pass-1");

        var other = api.Authed(await api.LoginWithOtpAsync(ApiClient.NewPhone()));
        await ApiClient.RegisterAsync(other, "Strong-Pass-1");

        var create = await owner.PostAsJsonAsync("api/Person/CreateCustomerContact", new
        {
            title = "Home",
            lan = "51.3890",
            lat = "35.6892",
            countryId = 1,
            cityId = 1,
            postalCode = "1234567890",
            address = "Owner address",
            unit = "1",
            plaque = "10",
            phoneNumber = "02100000000"
        });
        create.EnsureSuccessStatusCode();

        var list = await ApiClient.ReadJsonAsync(await owner.GetAsync("api/Person/GetCustomerContactList"));
        var contactId = list.GetProperty("data")[0].GetProperty("id").GetInt32();

        return (owner, other, contactId);
    }

    [Fact]
    public async Task Other_user_cannot_read_a_contact()
    {
        var (_, other, contactId) = await ArrangeAsync();

        var response = await other.GetAsync($"api/Person/GetCustomerContact?id={contactId}");

        // This action returns the ModelState problem shape, not ServiceResponse.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("There is not contact info with this id", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Other_user_cannot_update_a_contact()
    {
        var (_, other, contactId) = await ArrangeAsync();

        var response = await other.PostAsJsonAsync("api/Person/UpdateCustomerContact", new
        {
            id = contactId,
            title = "Hijacked",
            lan = "0",
            lat = "0",
            countryId = 1,
            cityId = 1,
            postalCode = "0000000000",
            address = "Attacker address",
            unit = "0",
            plaque = "0",
            phoneNumber = "02199999999",
            customerId = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var address = await fixture.WithDbAsync(db =>
            db.CustomerContacts.Where(c => c.Id == contactId).Select(c => c.Address).SingleAsync());
        Assert.Equal("Owner address", address);
    }
}
