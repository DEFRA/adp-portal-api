namespace ADP.Portal.Core.Ado.Entities
{
    public class AdoVariableGroup
    {
        public required string Name { get; set; }

        public required List<AdoVariable> Variables { get; set; }
        public string? Description { get; set; }
    }
}
