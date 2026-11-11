using Kriseportal.Web.Models.Entities;

namespace Kriseportal.Tests;

// B13: tester reglene for statusflyten ny → vurderes → tildelt → løst.
public class NeedStatusFlowTests
{
    [Theory]
    [InlineData(NeedStatus.New, NeedStatus.UnderReview)]
    [InlineData(NeedStatus.UnderReview, NeedStatus.Assigned)]
    [InlineData(NeedStatus.Assigned, NeedStatus.Resolved)]
    public void NextStatus_GirNesteStegIFlyten(NeedStatus current, NeedStatus expected)
    {
        Assert.Equal(expected, NeedStatusFlow.NextStatus(current));
    }

    [Fact]
    public void NextStatus_LostBehovHarIngenNesteStatus()
    {
        Assert.Null(NeedStatusFlow.NextStatus(NeedStatus.Resolved));
    }

    [Theory]
    [InlineData(NeedStatus.New, NeedStatus.UnderReview)]
    [InlineData(NeedStatus.UnderReview, NeedStatus.Assigned)]
    [InlineData(NeedStatus.Assigned, NeedStatus.Resolved)]
    public void CanChange_EttStegFramErLov(NeedStatus from, NeedStatus to)
    {
        Assert.True(NeedStatusFlow.CanChange(from, to));
    }

    [Theory]
    [InlineData(NeedStatus.New, NeedStatus.Assigned)]       // hopper over et steg
    [InlineData(NeedStatus.New, NeedStatus.Resolved)]       // hopper over to steg
    [InlineData(NeedStatus.UnderReview, NeedStatus.New)]    // bakover
    [InlineData(NeedStatus.Assigned, NeedStatus.UnderReview)]
    [InlineData(NeedStatus.Resolved, NeedStatus.New)]       // løst kan ikke åpnes igjen
    [InlineData(NeedStatus.New, NeedStatus.New)]            // samme status
    public void CanChange_UlovligeOvergangerAvvises(NeedStatus from, NeedStatus to)
    {
        Assert.False(NeedStatusFlow.CanChange(from, to));
    }

    [Theory]
    [InlineData(NeedStatus.New, "Ny")]
    [InlineData(NeedStatus.UnderReview, "Vurderes")]
    [InlineData(NeedStatus.Assigned, "Tildelt")]
    [InlineData(NeedStatus.Resolved, "Løst")]
    public void DisplayName_GirNorskTekst(NeedStatus status, string expected)
    {
        Assert.Equal(expected, NeedStatusFlow.DisplayName(status));
    }
}
