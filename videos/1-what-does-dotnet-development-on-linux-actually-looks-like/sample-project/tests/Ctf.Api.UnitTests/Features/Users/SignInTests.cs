using Ctf.Api.Features.Users;
using Ctf.Api.Helpers.Security;
using Ctf.Api.Repositories.Users;

namespace Ctf.Api.UnitTests.Features.Users;

public sealed class SignInTests
{
    private static (
        Mock<IUserRepository>,
        Mock<IAuthHelper>,
        Mock<IPasswordHasher>,
        Mock<IValidator<SignIn.Command>>,
        SignIn.Handler,
        SignIn.Command
    ) Init()
    {
        var r = new Mock<IUserRepository>();
        var ah = new Mock<IAuthHelper>();
        var p = new Mock<IPasswordHasher>();
        var v = new Mock<IValidator<SignIn.Command>>();

        var h = new SignIn.Handler(r.Object, ah.Object, p.Object, v.Object);
        var c = new SignIn.Command(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>());

        return (r, ah, p, v, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenCommandIsNotValidAsync()
    {
        // Arrange
        var (r, ah, p, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default))
            .ReturnsAsync(
                new ValidationResult([
                    new ValidationFailure(It.IsAny<string>(), It.IsAny<string>()),
                ])
            );

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        p.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        ah.Verify(
            t => t.CreateAuthResponseWithRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenUserNotFoundAsync()
    {
        // Arrange
        var (r, ah, p, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        r.Setup(r => r.GetByEmailAsync(c.Email)).ReturnsAsync((UserDto?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status400BadRequest);
        a.Error.Detail.Should().Be("Incorrect email or password.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.GetByEmailAsync(c.Email), Times.Once);
        p.Verify(p => p.Verify(c.Password, It.IsAny<string>()), Times.Never);
        ah.Verify(
            t => t.CreateAuthResponseWithRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenUserPasswordIsNullAsync()
    {
        // Arrange
        var (r, ah, p, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        r.Setup(r => r.GetByEmailAsync(c.Email))
            .ReturnsAsync(
                new UserDto
                {
                    Id = It.IsAny<Guid>(),
                    Username = It.IsAny<string>(),
                    PasswordHash = null,
                    IsVerified = It.IsAny<bool>(),
                }
            );

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status400BadRequest);
        a.Error.Detail.Should().Be("Incorrect email or password.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.GetByEmailAsync(c.Email), Times.Once);
        p.Verify(p => p.Verify(c.Password, It.IsAny<string>()), Times.Never);
        ah.Verify(
            t => t.CreateAuthResponseWithRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenPasswordIsIncorrectAsync()
    {
        // Arrange
        var (r, ah, p, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        r.Setup(r => r.GetByEmailAsync(c.Email))
            .ReturnsAsync(
                new UserDto
                {
                    Id = It.IsAny<Guid>(),
                    Username = It.IsAny<string>(),
                    PasswordHash = Guid.NewGuid().ToString(),
                    IsVerified = It.IsAny<bool>(),
                }
            );
        p.Setup(p => p.Verify(c.Password, It.IsAny<string>())).Returns(false);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status400BadRequest);
        a.Error.Detail.Should().Be("Incorrect email or password.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.GetByEmailAsync(c.Email), Times.Once);
        p.Verify(p => p.Verify(c.Password, It.IsAny<string>()), Times.Once);
        ah.Verify(
            t => t.CreateAuthResponseWithRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenEmailIsNotVerifiedAsync()
    {
        // Arrange
        var (r, ah, p, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        r.Setup(r => r.GetByEmailAsync(c.Email))
            .ReturnsAsync(
                new UserDto
                {
                    Id = It.IsAny<Guid>(),
                    Username = It.IsAny<string>(),
                    PasswordHash = Guid.NewGuid().ToString(),
                    IsVerified = false,
                }
            );
        p.Setup(p => p.Verify(c.Password, It.IsAny<string>())).Returns(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should().Be("Email is not verified. Please verify your email.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.GetByEmailAsync(c.Email), Times.Once);
        p.Verify(p => p.Verify(c.Password, It.IsAny<string>()), Times.Once);
        ah.Verify(
            t => t.CreateAuthResponseWithRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Should_SignIn_WhenCommandIsValidAsync()
    {
        // Arrange
        var (r, ah, p, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        r.Setup(r => r.GetByEmailAsync(c.Email))
            .ReturnsAsync(
                new UserDto
                {
                    Id = It.IsAny<Guid>(),
                    Username = It.IsAny<string>(),
                    PasswordHash = Guid.NewGuid().ToString(),
                    IsVerified = true,
                }
            );
        p.Setup(p => p.Verify(c.Password, It.IsAny<string>())).Returns(true);
        ah.Setup(t =>
                t.CreateAuthResponseWithRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>())
            )
            .ReturnsAsync(It.IsAny<AuthResponseWithRefreshToken>());

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.GetByEmailAsync(c.Email), Times.Once);
        p.Verify(p => p.Verify(c.Password, It.IsAny<string>()), Times.Once);
        ah.Verify(
            t => t.CreateAuthResponseWithRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Once
        );
    }
}
