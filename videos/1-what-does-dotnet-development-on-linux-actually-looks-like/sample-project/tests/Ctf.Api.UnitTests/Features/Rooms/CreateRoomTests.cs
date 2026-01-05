using Ctf.Api.Features.Rooms;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.UnitTests.Features.Rooms;

public sealed class CreateRoomTests
{
    private static (
        Mock<IRoomRepository>,
        Mock<IValidator<CreateRoom.Command>>,
        CreateRoom.Handler,
        CreateRoom.Command
    ) Init()
    {
        var r = new Mock<IRoomRepository>();
        var v = new Mock<IValidator<CreateRoom.Command>>();

        var h = new CreateRoom.Handler(r.Object, v.Object);
        var c = new CreateRoom.Command(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>()
        );

        return (r, v, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenCommandIsNotValidAsync()
    {
        // Arrange
        var (r, v, h, c) = Init();
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
        r.Verify(r => r.ExistsByNameAsync(c.UserId, c.Name), Times.Never);
        r.Verify(r => r.CreateAsync(It.IsAny<CreateRoomDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenExistsByNameAsync()
    {
        // Arrange
        var (r, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        r.Setup(r => r.ExistsByNameAsync(c.UserId, c.Name)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status409Conflict);
        a.Error.Detail.Should()
            .Be("You already have a room with this name. Please choose a different name.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.ExistsByNameAsync(c.UserId, c.Name), Times.Once);
        r.Verify(r => r.CreateAsync(It.IsAny<CreateRoomDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Create_WhenWhenCommandIsValidAsync()
    {
        // Arrange
        var (r, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        r.Setup(r => r.ExistsByNameAsync(c.UserId, c.Name)).ReturnsAsync(false);
        r.Setup(r => r.CreateAsync(It.IsAny<CreateRoomDto>()));

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.ExistsByNameAsync(c.UserId, c.Name), Times.Once);
        r.Verify(r => r.CreateAsync(It.IsAny<CreateRoomDto>()), Times.Once);
    }
}
