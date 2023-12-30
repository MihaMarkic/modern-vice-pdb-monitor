using Modern.Vice.PdbMonitor.Core.Common;

namespace Modern.Vice.PdbMonitor.Engine.BindingValidators;
public class PdbVariableBindValidator : BindingValidator
{
    readonly Action<PdbVariable> assignToSource;
    public PdbVariableBindValidator(string sourcePropertyName, Action<PdbVariable> assignToSource) 
        : base(sourcePropertyName)
    {
        this.assignToSource = assignToSource;
    }

    public void Update(PdbVariable? variable)
    {
        if (variable is not null)
        {
            assignToSource(variable);
            Errors = ImmutableArray<string>.Empty;
        }
        else
        {
            Errors = ImmutableArray<string>.Empty.Add("Variable should be set");
        }
    }
}
