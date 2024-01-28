namespace ADP.Portal.Core.Ado.Entities
{
    public class AdoVariableGroup(string name, List<AdoVariable> variables, string? description)
    {
        public required string Name { get; set; } = name;

        public required List<AdoVariable> Variables { get; set; } = variables;
        public string? Description { get; set; } = description;
    }
}
