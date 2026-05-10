using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GeoMapProjesi.Models;

public partial class Insight
{
    [Key]
    public int Id { get; set; }

    public int? AnalysisId { get; set; }

    [StringLength(150)]
    public string? Title { get; set; }

    [StringLength(20)]
    public string? Severity { get; set; }

    public string? Description { get; set; }

    [ForeignKey("AnalysisId")]
    [InverseProperty("Insights")]
    public virtual Analysis? Analysis { get; set; }
}
