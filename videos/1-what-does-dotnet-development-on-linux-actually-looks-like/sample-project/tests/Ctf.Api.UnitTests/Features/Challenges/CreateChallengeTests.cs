using Ctf.Api.Features.Challenges;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.UnitTests.Features.Challenges;

public sealed class CreateChallengeTests
{
    private static (
        Mock<IChallengeRepository>,
        Mock<IRoomMemberRepository>,
        Mock<IValidator<CreateChallenge.Command>>,
        CreateChallenge.Handler,
        CreateChallenge.Command
    ) Init()
    {
        var cr = new Mock<IChallengeRepository>();
        var rmr = new Mock<IRoomMemberRepository>();
        var v = new Mock<IValidator<CreateChallenge.Command>>();

        var h = new CreateChallenge.Handler(rmr.Object, cr.Object, v.Object);
        var c = new CreateChallenge.Command(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            [],
            []
        );

        return (cr, rmr, v, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenCommandIsInvalidAsync()
    {
        // Arrange
        var (cr, rmr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default))
            .ReturnsAsync(
                new ValidationResult(
                    [new ValidationFailure(It.IsAny<string>(), It.IsAny<string>())]
                )
            );

        // Act
        var r = await h.Handle(c);

        // Assert
        r.IsSuccess.Should().BeFalse();
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Never);
        cr.Verify(r => r.NameInRoomExistsAsync(c.Name, c.RoomId), Times.Never);
        cr.Verify(r => r.CreateAsync(It.IsAny<CreateChallengeDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserIsNotInRoomAsync()
    {
        var (cr, rmr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Room was not found.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        cr.Verify(r => r.NameInRoomExistsAsync(c.Name, c.RoomId), Times.Never);
        cr.Verify(r => r.CreateAsync(It.IsAny<CreateChallengeDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenUserIsPlayerAsync()
    {
        var (cr, rmr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync(RoomRole.Player);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should().Be("You must be an editor in this room to modify challenges.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        cr.Verify(r => r.NameInRoomExistsAsync(c.Name, c.RoomId), Times.Never);
        cr.Verify(r => r.CreateAsync(It.IsAny<CreateChallengeDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenNameAlreadyExistsInRoomAsync()
    {
        // Arrange
        var (cr, rmr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync((RoomRole)int.MaxValue);
        cr.Setup(r => r.NameInRoomExistsAsync(c.Name, c.RoomId)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status409Conflict);
        a.Error.Detail.Should()
            .Be(
                "There's already a challenge in the room with this name. Please choose a different name."
            );
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        cr.Verify(r => r.NameInRoomExistsAsync(c.Name, c.RoomId), Times.Once);
        cr.Verify(r => r.CreateAsync(It.IsAny<CreateChallengeDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Create_WhenCommandIsValidAsync()
    {
        // Arrange
        var (cr, rmr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync((RoomRole)int.MaxValue);
        cr.Setup(r => r.NameInRoomExistsAsync(c.Name, c.RoomId)).ReturnsAsync(false);
        cr.Setup(r => r.CreateAsync(It.IsAny<CreateChallengeDto>()));

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        cr.Verify(r => r.NameInRoomExistsAsync(c.Name, c.RoomId), Times.Once);
        cr.Verify(r => r.CreateAsync(It.IsAny<CreateChallengeDto>()), Times.Once);
    }
}
