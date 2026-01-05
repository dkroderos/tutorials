using Ctf.Api.Features.Users;
using Ctf.Api.Helpers.Security;
using Ctf.Api.Repositories.Users;

namespace Ctf.Api.UnitTests.Features.Users;

public sealed class SignUpTests
{
    private static (
        Mock<IUserRepository>,
        Mock<IPasswordHasher>,
        Mock<IValidator<SignUp.Command>>,
        SignUp.Handler,
        SignUp.Command
    ) Init()
    {
        var r = new Mock<IUserRepository>();
        var p = new Mock<IPasswordHasher>();
        var v = new Mock<IValidator<SignUp.Command>>();

        var h = new SignUp.Handler(r.Object, p.Object, v.Object);
        var c = new SignUp.Command(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        );

        return (r, p, v, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenCommandIsNotValidAsync()
    {
        // Arrange
        var (r, p, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default))
            .ReturnsAsync(
                new ValidationResult(
                    [new ValidationFailure(It.IsAny<string>(), It.IsAny<string>())]
                )
            );

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.UsernameExistsAsync(c.Username), Times.Never);
        r.Verify(r => r.EmailExistsAsync(c.Email), Times.Never);
        p.Verify(p => p.Hash(c.Password), Times.Never);
        r.Verify(
            r => r.CreateAsync(It.IsAny<CreateUserDto>(), It.IsAny<CreateUserProviderDto>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenUsernameTakenAsync()
    {
        // Arrange
        var (r, p, v, h, c) = Init();

        v.Setup(v => v.ValidateAsync(c, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        r.Setup(r => r.UsernameExistsAsync(c.Username)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status409Conflict);
        a.Error.Detail.Should().Be("Username is already taken.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.UsernameExistsAsync(c.Username), Times.Once);
        r.Verify(r => r.EmailExistsAsync(c.Email), Times.Never);
        p.Verify(p => p.Hash(c.Password), Times.Never);
        r.Verify(
            r => r.CreateAsync(It.IsAny<CreateUserDto>(), It.IsAny<CreateUserProviderDto>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenEmailTakenAsync()
    {
        // Arrange
        var (r, p, v, h, c) = Init();

        v.Setup(v => v.ValidateAsync(c, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        r.Setup(r => r.UsernameExistsAsync(c.Username)).ReturnsAsync(false);
        r.Setup(r => r.EmailExistsAsync(c.Email)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status409Conflict);
        a.Error.Detail.Should().Be("Email is already taken.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.UsernameExistsAsync(c.Username), Times.Once);
        r.Verify(r => r.EmailExistsAsync(c.Email), Times.Once);
        p.Verify(p => p.Hash(c.Password), Times.Never);
        r.Verify(
            r => r.CreateAsync(It.IsAny<CreateUserDto>(), It.IsAny<CreateUserProviderDto>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Should_Register_WhenCommandIsValidAsync()
    {
        // Arrange
        var (r, p, v, h, c) = Init();

        v.Setup(v => v.ValidateAsync(c, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        r.Setup(r => r.UsernameExistsAsync(c.Username)).ReturnsAsync(false);
        r.Setup(r => r.EmailExistsAsync(c.Email)).ReturnsAsync(false);
        p.Setup(p => p.Hash(c.Password)).Returns(It.IsAny<string>);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.UsernameExistsAsync(c.Username), Times.Once);
        r.Verify(r => r.EmailExistsAsync(c.Email), Times.Once);
        p.Verify(p => p.Hash(c.Password), Times.Once);
        r.Verify(
            r => r.CreateAsync(It.IsAny<CreateUserDto>(), It.IsAny<CreateUserProviderDto>()),
            Times.Once
        );
    }
}
