using System.ComponentModel.DataAnnotations;

namespace Modern.Vice.PdbMonitor.Engine.Common;

public enum CompilerType
{
    [Display(Description = "ACME")]
    Acme,
    [Display(Description = "Oscar64")]
    Oscar64,
    [Display(Description = "KickAssembler")]
    KickAssembler,
}
