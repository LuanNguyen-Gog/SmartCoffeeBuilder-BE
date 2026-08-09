using Moq;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectWorking;
using SmartCoffeeBuilder.Tests.Infrastructure;

namespace SmartCoffeeBuilder.Tests;

/// <summary>
/// Huỷ ngang engagement cần ĐỒNG THUẬN HAI BÊN — không bên nào tự huỷ thẳng được.
/// Bao phủ: đề nghị → noti bên kia; đồng ý → terminated; từ chối → giữ accepted;
/// rút đề nghị; chặn tự duyệt; và phạm vi ảnh hưởng sau khi huỷ.
/// </summary>
public class EngagementTerminationTests
{
    // ───────── Bước 1: đề nghị huỷ ngang KHÔNG huỷ ngay ─────────

    [Fact]
    public async Task RequestTermination_ByOwner_KeepsAccepted_AndNotifiesProvider()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed();

        using var db = h.NewDb();
        var result = await h.NewService(db).RequestTerminationAsync(
            s.OwnerAccountId, s.EngagementId, new RequestEngagementTerminationRequest { Reason = "Đổi phương án" });

        // Chưa huỷ — vẫn đang chạy, chỉ treo đề nghị.
        Assert.Equal("accepted", result.Status);
        Assert.True(result.IsAwaitingTerminationApproval);
        Assert.Equal("owner", result.TerminationRequestedBy);
        Assert.Equal("Đổi phương án", result.TerminationRequestNote);
        Assert.Null(result.TerminatedAt);

        var saved = h.Reload(s.EngagementId);
        Assert.Equal(ProviderStatus.accepted, saved.Status);
        Assert.NotNull(saved.TerminationRequestedAt);
        Assert.Equal(EngagementParty.owner, saved.TerminationRequestedBy);

        // Bên còn lại (provider) nhận noti + email.
        h.Notifications.Verify(
            n => n.NotifyEngagementTerminationRequestedAsync(s.EngagementId, true), Times.Once);
    }

    [Fact]
    public async Task RequestTermination_ByProvider_KeepsAccepted_AndNotifiesOwner()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed();

        using var db = h.NewDb();
        var result = await h.NewService(db).RequestTerminationAsync(
            s.ProviderAccountId, s.EngagementId, new RequestEngagementTerminationRequest { Reason = "Quá tải" });

        Assert.Equal("accepted", result.Status);
        Assert.Equal("provider", result.TerminationRequestedBy);
        h.Notifications.Verify(
            n => n.NotifyEngagementTerminationRequestedAsync(s.EngagementId, false), Times.Once);
    }

    [Fact]
    public async Task RequestTermination_Twice_BySameParty_Throws()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed(terminationRequestedAt: DateTime.UtcNow, terminationRequestedBy: EngagementParty.owner);

        using var db = h.NewDb();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.NewService(db).RequestTerminationAsync(
                s.OwnerAccountId, s.EngagementId, new RequestEngagementTerminationRequest()));
    }

    [Fact]
    public async Task RequestTermination_OnNonAcceptedEngagement_Throws()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed(engagementStatus: ProviderStatus.requested);

        using var db = h.NewDb();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.NewService(db).RequestTerminationAsync(
                s.OwnerAccountId, s.EngagementId, new RequestEngagementTerminationRequest()));
    }

    // ───────── Bước 2: bên còn lại phản hồi ─────────

    [Fact]
    public async Task RespondTermination_Approve_TerminatesAndNotifiesRequester()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed(terminationRequestedAt: DateTime.UtcNow, terminationRequestedBy: EngagementParty.owner);

        using var db = h.NewDb();
        var result = await h.NewService(db).RespondTerminationAsync(
            s.ProviderAccountId, s.EngagementId, new RespondEngagementTerminationRequest { Approve = true });

        Assert.Equal("terminated", result.Status);
        Assert.NotNull(result.TerminatedAt);
        Assert.False(result.IsAwaitingTerminationApproval);
        // Vết "ai đề nghị" được giữ lại sau khi chốt.
        Assert.Equal("owner", result.TerminationRequestedBy);

        Assert.Equal(ProviderStatus.terminated, h.Reload(s.EngagementId).Status);

        h.Notifications.Verify(
            n => n.NotifyEngagementTerminationDecisionAsync(s.EngagementId, true, true), Times.Once);
    }

    [Fact]
    public async Task RespondTermination_Reject_KeepsAcceptedAndClearsRequest()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed(terminationRequestedAt: DateTime.UtcNow, terminationRequestedBy: EngagementParty.provider);

        using var db = h.NewDb();
        var result = await h.NewService(db).RespondTerminationAsync(
            s.OwnerAccountId, s.EngagementId, new RespondEngagementTerminationRequest { Approve = false });

        Assert.Equal("accepted", result.Status);
        Assert.False(result.IsAwaitingTerminationApproval);
        Assert.Null(result.TerminationRequestedAt);
        Assert.Null(result.TerminationRequestedBy);

        var saved = h.Reload(s.EngagementId);
        Assert.Equal(ProviderStatus.accepted, saved.Status);
        Assert.Null(saved.TerminationRequestedAt);

        h.Notifications.Verify(
            n => n.NotifyEngagementTerminationDecisionAsync(s.EngagementId, false, false), Times.Once);
    }

    [Fact]
    public async Task RespondTermination_ByRequesterItself_Throws()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed(terminationRequestedAt: DateTime.UtcNow, terminationRequestedBy: EngagementParty.owner);

        using var db = h.NewDb();
        // Owner là bên đề nghị — không được tự duyệt đề nghị của chính mình.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.NewService(db).RespondTerminationAsync(
                s.OwnerAccountId, s.EngagementId, new RespondEngagementTerminationRequest { Approve = true }));

        Assert.Equal(ProviderStatus.accepted, h.Reload(s.EngagementId).Status);
    }

    [Fact]
    public async Task RespondTermination_WithoutPendingRequest_Throws()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed();

        using var db = h.NewDb();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.NewService(db).RespondTerminationAsync(
                s.ProviderAccountId, s.EngagementId, new RespondEngagementTerminationRequest { Approve = true }));
    }

    [Fact]
    public async Task RespondTermination_ByOutsider_Throws401()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed(terminationRequestedAt: DateTime.UtcNow, terminationRequestedBy: EngagementParty.owner);

        using var db = h.NewDb();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            h.NewService(db).RespondTerminationAsync(
                s.OwnerAccountId + 9999, s.EngagementId, new RespondEngagementTerminationRequest { Approve = true }));
    }

    // ───────── Rút lại đề nghị ─────────

    [Fact]
    public async Task CancelTerminationRequest_ByRequester_ClearsAndNotifiesOtherSide()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed(terminationRequestedAt: DateTime.UtcNow, terminationRequestedBy: EngagementParty.provider);

        using var db = h.NewDb();
        var result = await h.NewService(db).CancelTerminationRequestAsync(s.ProviderAccountId, s.EngagementId);

        Assert.Equal("accepted", result.Status);
        Assert.Null(result.TerminationRequestedAt);
        h.Notifications.Verify(
            n => n.NotifyEngagementTerminationCancelledAsync(s.EngagementId, false), Times.Once);
    }

    [Fact]
    public async Task CancelTerminationRequest_ByOtherSide_Throws()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed(terminationRequestedAt: DateTime.UtcNow, terminationRequestedBy: EngagementParty.provider);

        using var db = h.NewDb();
        // Owner không phải bên đề nghị — phải "từ chối", không được "rút".
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.NewService(db).CancelTerminationRequestAsync(s.OwnerAccountId, s.EngagementId));
    }

    // ───────── Cửa vào gộp POST /terminate ─────────

    [Fact]
    public async Task Terminate_FirstCall_OnlyCreatesRequest()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed();

        using var db = h.NewDb();
        var result = await h.NewService(db).TerminateAsync(s.OwnerAccountId, s.EngagementId);

        // Điểm mấu chốt: một chạm KHÔNG còn huỷ thẳng được nữa.
        Assert.Equal("accepted", result.Status);
        Assert.True(result.IsAwaitingTerminationApproval);
        Assert.Equal(ProviderStatus.accepted, h.Reload(s.EngagementId).Status);
    }

    [Fact]
    public async Task Terminate_ByOtherSide_AfterRequest_Terminates()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed();

        using (var db1 = h.NewDb())
            await h.NewService(db1).TerminateAsync(s.OwnerAccountId, s.EngagementId);

        using var db2 = h.NewDb();
        var result = await h.NewService(db2).TerminateAsync(s.ProviderAccountId, s.EngagementId);

        Assert.Equal("terminated", result.Status);
        Assert.Equal(ProviderStatus.terminated, h.Reload(s.EngagementId).Status);
    }

    [Fact]
    public async Task Terminate_TwiceBySameSide_Throws()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed();

        using (var db1 = h.NewDb())
            await h.NewService(db1).TerminateAsync(s.OwnerAccountId, s.EngagementId);

        using var db2 = h.NewDb();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.NewService(db2).TerminateAsync(s.OwnerAccountId, s.EngagementId));
    }

    [Fact]
    public async Task UpdateStatus_ToTerminated_RoutesThroughConsentFlow()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed();

        using var db = h.NewDb();
        var result = await h.NewService(db).UpdateStatusAsync(
            s.OwnerAccountId, s.EngagementId, new UpdateProjectWorkingStatusRequest { Status = "terminated" });

        // Endpoint tổng cũng không còn là cửa hậu để huỷ thẳng.
        Assert.Equal("accepted", result.Status);
        Assert.True(result.IsAwaitingTerminationApproval);
    }

    [Fact]
    public async Task Terminate_ByAdmin_TerminatesImmediately_AndNotifiesBothSides()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed(adminAccount: 1);

        using var db = h.NewDb();
        var result = await h.NewService(db).TerminateAsync(s.AdminAccountId, s.EngagementId);

        Assert.Equal("terminated", result.Status);
        // Admin đứng ngoài hai bên → cả owner lẫn provider đều được báo.
        h.Notifications.Verify(n => n.NotifyEngagementTerminatedAsync(s.EngagementId, true), Times.Once);
        h.Notifications.Verify(n => n.NotifyEngagementTerminatedAsync(s.EngagementId, false), Times.Once);
    }

    // ───────── Sau khi huỷ: quay lại được HAI cửa vào ─────────

    [Fact]
    public async Task AfterTermination_OwnerCanReInviteSameProvider_DirectHire()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed();

        using (var db1 = h.NewDb())
            await h.NewService(db1).TerminateAsync(s.OwnerAccountId, s.EngagementId);
        using (var db2 = h.NewDb())
            await h.NewService(db2).TerminateAsync(s.ProviderAccountId, s.EngagementId);

        // Cửa vào 1 — mời trực tiếp lại chính provider vừa huỷ.
        using var db3 = h.NewDb();
        var reInvited = await h.NewService(db3).CreateDirectRequestAsync(new CreateProjectWorkingRequest
        {
            ProjectShopOwnerId = s.ProjectId,
            ServiceProviderProfileId = s.ProviderProfileId,
            ContractType = "design",
            RequestMessage = "Mời lại"
        });

        Assert.Equal("requested", reInvited.Status);
        Assert.NotEqual(s.EngagementId, reInvited.Id);
    }

    [Fact]
    public async Task AfterTermination_SourcePostStaysClosed()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed(viaMarketplace: true, postDeadline: DateTime.UtcNow.AddDays(30));

        using (var db1 = h.NewDb())
            await h.NewService(db1).TerminateAsync(s.OwnerAccountId, s.EngagementId);
        using (var db2 = h.NewDb())
            await h.NewService(db2).TerminateAsync(s.ProviderAccountId, s.EngagementId);

        // Huỷ ngang chỉ đóng đúng engagement — KHÔNG đụng tới bài đăng nguồn.
        Assert.Equal(PostStatus.closed, h.ReloadPost(s.PostId).Status);
    }

    [Fact]
    public async Task ActiveEngagement_StillBlocksDuplicateInvite()
    {
        var h = new EngagementTerminationHarness();
        var s = h.Seed();

        // Chưa huỷ xong (mới chỉ đề nghị) → vẫn là hợp tác đang chạy, không mời trùng được.
        using (var db1 = h.NewDb())
            await h.NewService(db1).TerminateAsync(s.OwnerAccountId, s.EngagementId);

        using var db2 = h.NewDb();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.NewService(db2).CreateDirectRequestAsync(new CreateProjectWorkingRequest
            {
                ProjectShopOwnerId = s.ProjectId,
                ServiceProviderProfileId = s.ProviderProfileId,
                ContractType = "design"
            }));
    }
}
