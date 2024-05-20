namespace ADP.Portal.Api.Models.Group;

public class SetGroupMembersRequest
{
    public required IEnumerable<string> TechUserMembers { get; set; }
    public required IEnumerable<string> NonTechUserMembers { get; set; }
    public required IEnumerable<string> AdminMembers { get; set; }
}
