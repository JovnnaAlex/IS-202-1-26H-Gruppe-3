namespace Kriseportal.Web.Models.Entities;

// Statusflyten for et behov: ny → vurderes → tildelt → løst
public enum NeedStatus
{
    New,
    UnderReview,
    Assigned,
    Resolved
}
