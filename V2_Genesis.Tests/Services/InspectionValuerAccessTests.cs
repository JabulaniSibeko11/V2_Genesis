using V2_Genesis.Services.Attributes;
using Xunit;

namespace V2_Genesis.Tests.Services;

/// <summary>
/// Protecting the valuer: who may see the authorised valuer on the secure
/// inspection page (name, cell, e-mail, vehicle, photo).
/// </summary>
public class InspectionValuerAccessTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0);
    private const string Owner = "user-owner";

    private static string Decide(
        DateTime? linkExpiresAt = null,
        bool released = true,
        string? pin = "1234",
        string? signedIn = Owner,
        string? submittedBy = Owner,
        int failed = 0,
        DateTime? validFrom = null,
        DateTime? validUntil = null,
        bool verified = true) =>
        InspectionValuerAccess.Decide(Now, linkExpiresAt, released, pin, signedIn, submittedBy,
            failed, validFrom, validUntil, verified);

    [Fact]
    public void Owner_with_verified_pin_inside_the_window_sees_the_valuer()
        => Assert.Equal(InspectionValuerAccess.Visible,
            Decide(validFrom: Now.AddHours(-1), validUntil: Now.AddHours(5)));

    [Fact]
    public void Expired_link_shows_nothing()
        => Assert.Equal(InspectionValuerAccess.Hidden, Decide(linkExpiresAt: Now.AddMinutes(-1)));

    [Theory]
    [InlineData(false, "1234")]
    [InlineData(true, null)]
    [InlineData(true, "  ")]
    public void Not_released_until_the_valuer_sends_details_and_pin(bool released, string? pin)
        => Assert.Equal(InspectionValuerAccess.NotReleased, Decide(released: released, pin: pin));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Anonymous_visitor_must_sign_in(string? signedIn)
        => Assert.Equal(InspectionValuerAccess.SignIn, Decide(signedIn: signedIn));

    [Fact]
    public void Someone_with_a_forwarded_link_and_another_account_is_refused()
        => Assert.Equal(InspectionValuerAccess.NotOwner, Decide(signedIn: "someone-else"));

    [Fact]
    public void Submission_without_an_owner_is_refused()
        => Assert.Equal(InspectionValuerAccess.NotOwner, Decide(submittedBy: null));

    [Theory]
    [InlineData(5)]
    [InlineData(9)]
    public void Locked_after_five_wrong_pins_even_if_verified_before(int failed)
        => Assert.Equal(InspectionValuerAccess.Locked, Decide(failed: failed));

    [Fact]
    public void Four_wrong_pins_is_not_locked_yet()
        => Assert.Equal(InspectionValuerAccess.Visible, Decide(failed: 4));

    [Fact]
    public void Before_the_pin_window_the_valuer_is_not_shown()
        => Assert.Equal(InspectionValuerAccess.NotYetValid, Decide(validFrom: Now.AddMinutes(1)));

    [Fact]
    public void After_the_pin_window_the_valuer_is_not_shown()
        => Assert.Equal(InspectionValuerAccess.Ended, Decide(validUntil: Now.AddMinutes(-1)));

    [Fact]
    public void Owner_still_has_to_enter_the_pin()
        => Assert.Equal(InspectionValuerAccess.Pin, Decide(verified: false));

    [Theory]
    [InlineData("1234", "1234", true)]
    [InlineData(" 1234 ", "1234", true)]
    [InlineData("1235", "1234", false)]
    [InlineData("123", "1234", false)]
    [InlineData("", "1234", false)]
    [InlineData("1234", "", false)]
    [InlineData(null, null, false)]
    public void Pin_must_match_exactly(string? supplied, string? expected, bool matches)
        => Assert.Equal(matches, InspectionValuerAccess.PinMatches(supplied, expected));

    [Theory]
    [InlineData(0, 5)]
    [InlineData(3, 2)]
    [InlineData(5, 0)]
    [InlineData(8, 0)]
    public void Attempts_left(int failed, int left)
        => Assert.Equal(left, InspectionValuerAccess.AttemptsLeft(failed));

    [Theory]
    [InlineData("ABC", "abc", true)]
    [InlineData(" abc ", "abc", true)]
    [InlineData("abc", "abd", false)]
    [InlineData(null, "abc", false)]
    public void Owner_is_matched_on_the_user_id(string? submittedBy, string userId, bool owner)
        => Assert.Equal(owner, InspectionValuerAccess.IsOwner(submittedBy, userId));
}
