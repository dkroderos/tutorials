using Ctf.Api.Features.RoomMembers;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.UnitTests.Features.RoomMembers;

public sealed class GetRoomMembersTests
{
    private static (
        Mock<IRoomMemberRepository>,
        GetRoomMembers.Handler,
        GetRoomMembers.Query
    ) Init()
    {
        var r = new Mock<IRoomMemberRepository>();

        var h = new GetRoomMembers.Handler(r.Object);
        var q = new GetRoomMembers.Query(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<int>()
        );

        return (r, h, q);
    }

    [Theory]
    [InlineData(RoomRole.Editor)]
    [InlineData(RoomRole.Player)]
    public async Task Handle_Should_ReturnNotFound_WhenUserRoleIsLowerThanAdminAsync(
        RoomRole userRole
    )
    {
        // Arrange
        var (r, h, q) = Init();
        r.Setup(r => r.GetRoleAsync(q.RoomId, q.UserId)).ReturnsAsync(userRole);

        // Act
        var a = await h.Handle(q);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Room was not found.");
        r.Verify(r => r.GetRoleAsync(q.RoomId, q.UserId), Times.Once);
        r.Verify(
            r => r.QueryAsync(q.RoomId, q.SearchTerm, q.SortBy, q.IsAscending, q.Page, q.PageSize),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Should_GetReceivedInvitesAsync()
    {
        // Arrange
        var (r, h, q) = Init();
        r.Setup(r => r.GetRoleAsync(q.RoomId, q.UserId)).ReturnsAsync((RoomRole)int.MaxValue);
        r.Setup(r =>
                r.QueryAsync(q.RoomId, q.SearchTerm, q.SortBy, q.IsAscending, q.Page, q.PageSize)
            )
            .ReturnsAsync(PagedList<RoomMemberDto>.Empty());

        // Act
        var a = await h.Handle(q);

        // Assert
        a.IsSuccess.Should().BeTrue();
        r.Verify(r => r.GetRoleAsync(q.RoomId, q.UserId), Times.Once);
        r.Verify(
            r => r.QueryAsync(q.RoomId, q.SearchTerm, q.SortBy, q.IsAscending, q.Page, q.PageSize),
            Times.Once
        );
    }
}
