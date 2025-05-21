using FluentAssertions;

public class AspNetRoleIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AspNetRoleIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsOk()
    {
        // Arrange
        var roleId = "R0001"; // đã seed ở trên

        // Act
        var response = await _client.GetAsync($"/api/admin/AspNetRole/GetById?id={roleId}");

        // Assert
        response.EnsureSuccessStatusCode(); // 200 OK
        var json = await response.Content.ReadAsStringAsync();

        // Optional: parse json để kiểm tra nội dung
        json.Should().Contain("Admin");
    }
}
