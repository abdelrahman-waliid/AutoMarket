using CarMarketplace.Application.DTOs;
using CarMarketplace.Application.Validators;
using Xunit;

namespace CarMarketplace.Tests.Validators;

public class RegisterRequestDTOValidatorTests
{
    private readonly RegisterRequestDTOValidator _validator = new();

    [Fact]
    public void Validate_WhenPasswordMeetsPolicy_ShouldBeValid()
    {
        var dto = CreateValidDto();
        dto.Password = "ValidPassword1!";

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("short1!A", "Password must be at least 12 characters.")]
    [InlineData("lowercasepassword1!", "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character.")]
    [InlineData("UPPERCASEPASSWORD1!", "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character.")]
    [InlineData("NoDigitsHere!", "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character.")]
    [InlineData("NoSpecial1234", "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character.")]
    public void Validate_WhenPasswordViolatesPolicy_ShouldReturnPasswordError(string password, string expectedError)
    {
        var dto = CreateValidDto();
        dto.Password = password;

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequestDTO.Password)
                                            && e.ErrorMessage == expectedError);
    }

    private static RegisterRequestDTO CreateValidDto()
    {
        return new RegisterRequestDTO
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = "ValidPassword1!"
        };
    }
}
