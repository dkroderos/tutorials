using Ctf.Api.Features.RoomMembers;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.UnitTests.Features.RoomMembers;

public sealed class RemoveMemberTests
{
    private static (
        Mock<IRoomMemberRepository>,
        Mock<IValidator<RemoveMember.Command>>,
        RemoveMember.Handler,
        RemoveMember.Command
    ) Init()
    {
        var r = new Mock<IRoomMemberRepository>();
        var v = new Mock<IValidator<RemoveMember.Command>>();

        var h = new RemoveMember.Handler(r.Object, v.Object);
        var c = new RemoveMember.Command(It.IsAny<Guid>(), Guid.NewGuid(), Guid.NewGuid());

        return (r, v, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenCommandIsInvalidAsync()
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
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.RemoverId), Times.Never);
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.TargetUserId), Times.Never);
        r.Verify(r => r.DeleteAsync(c.RoomId, c.TargetUserId), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenRemoverIsNotInRoomAsync()
    {
        // Arrange
        var (r, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        r.Setup(r => r.GetRoleAsync(c.RoomId, c.RemoverId)).ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Room was not found.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.RemoverId), Times.Once);
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.TargetUserId), Times.Never);
        r.Verify(r => r.DeleteAsync(c.RoomId, c.TargetUserId), Times.Never);
    }

    [Theory]
    [InlineData(RoomRole.Editor)]
    [InlineData(RoomRole.Player)]
    public async Task Handle_Should_ReturnForbidden_WhenRemoverIsNotAnAdminAsync(
        RoomRole removerRole
    )
    {
        // Arrange
        var (r, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        r.Setup(r => r.GetRoleAsync(c.RoomId, c.RemoverId)).ReturnsAsync(removerRole);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status409Conflict);
        a.Error.Detail.Should().Be("You must be an admin in this room to remove members.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.RemoverId), Times.Once);
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.TargetUserId), Times.Never);
        r.Verify(r => r.DeleteAsync(c.RoomId, c.TargetUserId), Times.Never);
    }

    [Theory]
    [InlineData(RoomRole.Admin)]
    [InlineData(RoomRole.Owner)]
    public async Task Handle_Should_ReturnNotFound_WhenTargetUserIsNotAMemberAsync(
        RoomRole removerRole
    )
    {
        // Arrange
        var (r, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        r.Setup(r => r.GetRoleAsync(c.RoomId, c.RemoverId)).ReturnsAsync(removerRole);
        r.Setup(r => r.GetRoleAsync(c.RoomId, c.TargetUserId)).ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("User was not found.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.RemoverId), Times.Once);
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.TargetUserId), Times.Once);
        r.Verify(r => r.DeleteAsync(c.RoomId, c.TargetUserId), Times.Never);
    }

    [Theory]
    [InlineData(RoomRole.Admin, RoomRole.Admin)]
    [InlineData(RoomRole.Owner, RoomRole.Admin)]
    [InlineData(RoomRole.Owner, RoomRole.Owner)] // Should not happen but test it anyway.
    public async Task Handle_Should_ReturnForbidden_WhenTargetUserRoleIsNotLowerThenRemoverRoleAsync(
        RoomRole targetUserRole,
        RoomRole removerRole
    )
    {
        // Arrange
        var (r, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        r.Setup(r => r.GetRoleAsync(c.RoomId, c.RemoverId)).ReturnsAsync(removerRole);
        r.Setup(r => r.GetRoleAsync(c.RoomId, c.TargetUserId)).ReturnsAsync(targetUserRole);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should().Be("You can only remove users that are in lower roles than yours.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.RemoverId), Times.Once);
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.TargetUserId), Times.Once);
        r.Verify(r => r.DeleteAsync(c.RoomId, c.TargetUserId), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserFailedToRemoveInRoomAsync()
    {
        // Arrange
        var (r, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        r.Setup(r => r.GetRoleAsync(c.RoomId, c.RemoverId)).ReturnsAsync((RoomRole)int.MaxValue);
        r.Setup(r => r.GetRoleAsync(c.RoomId, c.TargetUserId)).ReturnsAsync(It.IsAny<RoomRole>());
        r.Setup(r => r.DeleteAsync(c.RoomId, c.TargetUserId)).ReturnsAsync(false);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("User was not found.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.RemoverId), Times.Once);
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.TargetUserId), Times.Once);
        r.Verify(r => r.DeleteAsync(c.RoomId, c.TargetUserId), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_RemoveMemberAsync()
    {
        // Arrange
        var (r, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        r.Setup(r => r.GetRoleAsync(c.RoomId, c.RemoverId)).ReturnsAsync((RoomRole)int.MaxValue);
        r.Setup(r => r.GetRoleAsync(c.RoomId, c.TargetUserId)).ReturnsAsync(It.IsAny<RoomRole>());
        r.Setup(r => r.DeleteAsync(c.RoomId, c.TargetUserId)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.RemoverId), Times.Once);
        r.Verify(r => r.GetRoleAsync(c.RoomId, c.TargetUserId), Times.Once);
        r.Verify(r => r.DeleteAsync(c.RoomId, c.TargetUserId), Times.Once);
    }
}
