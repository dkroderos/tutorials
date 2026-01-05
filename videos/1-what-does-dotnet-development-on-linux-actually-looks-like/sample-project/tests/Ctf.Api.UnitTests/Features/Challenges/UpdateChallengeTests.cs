using Ctf.Api.Features.Challenges;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.UnitTests.Features.Challenges;

public sealed class UpdateChallengeTests
{
    private static (
        Mock<IChallengeRepository>,
        Mock<IRoomMemberRepository>,
        Mock<IValidator<UpdateChallenge.Command>>,
        UpdateChallenge.Handler,
        UpdateChallenge.Command
    ) Init()
    {
        var cr = new Mock<IChallengeRepository>();
        var rmr = new Mock<IRoomMemberRepository>();
        var v = new Mock<IValidator<UpdateChallenge.Command>>();

        var h = new UpdateChallenge.Handler(rmr.Object, cr.Object, v.Object);
        var c = new UpdateChallenge.Command(
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
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.Id), Times.Never);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        cr.Verify(r => r.UpdateAsync(It.IsAny<UpdateChallengeDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenRoomIdDoesNotExistsAsync()
    {
        // Arrange
        var (cr, rmr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        cr.Setup(r => r.GetRoomIdAsync(c.Id)).ReturnsAsync((Guid?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.Id), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Never);
        cr.Verify(r => r.UpdateAsync(It.IsAny<UpdateChallengeDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserIsNotInRoomAsync()
    {
        // Arrange
        var (cr, rmr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        cr.Setup(r => r.GetRoomIdAsync(c.Id)).ReturnsAsync(It.IsAny<Guid>());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.Id), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        cr.Verify(r => r.UpdateAsync(It.IsAny<UpdateChallengeDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenUserIsPlayerAsync()
    {
        // Arrange
        var (cr, rmr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        cr.Setup(r => r.GetRoomIdAsync(c.Id)).ReturnsAsync(It.IsAny<Guid>());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId)).ReturnsAsync(RoomRole.Player);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should().Be("You must be an editor in this room to modify challenges.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.Id), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        cr.Verify(r => r.UpdateAsync(It.IsAny<UpdateChallengeDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenChallengeDoesNotExistsAsync()
    {
        // Arrange
        var (cr, rmr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        cr.Setup(r => r.GetRoomIdAsync(c.Id)).ReturnsAsync(It.IsAny<Guid>());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        cr.Setup(r => r.UpdateAsync(It.IsAny<UpdateChallengeDto>())).ReturnsAsync(false);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Challenge was not found.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.Id), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        cr.Verify(r => r.UpdateAsync(It.IsAny<UpdateChallengeDto>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Update_WhenCommandIsValidAsync()
    {
        // Arrange
        var (cr, rmr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        cr.Setup(r => r.GetRoomIdAsync(c.Id)).ReturnsAsync(It.IsAny<Guid>());
        rmr.Setup(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId))
            .ReturnsAsync((RoomRole)int.MaxValue);
        cr.Setup(r => r.UpdateAsync(It.IsAny<UpdateChallengeDto>())).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        cr.Verify(r => r.GetRoomIdAsync(c.Id), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(It.IsAny<Guid>(), c.UserId), Times.Once);
        cr.Verify(r => r.UpdateAsync(It.IsAny<UpdateChallengeDto>()), Times.Once);
    }
}
