using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GeoMapProjesi.Models;

public partial class Analysis
{
    [Key]
    public int Id { get; set; }

    public int? ProjectId { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime? AnalysisDate { get; set; }

    public string OriginalImagePath { get; set; } = null!;

    public string? ResultOverlayPath { get; set; }

    public double? CenterLatitude { get; set; }

    public double? CenterLongitude { get; set; }

    [StringLength(100)]
    public string? DetectedClimate { get; set; }

    [StringLength(150)]
    public string? EstimatedBiome { get; set; }

    public string? Notes { get; set; }

    [InverseProperty("Analysis")]
    public virtual ICollection<Insight> Insights { get; set; } = new List<Insight>();

    [ForeignKey("ProjectId")]
    [InverseProperty("Analyses")]
    public virtual Project? Project { get; set; }

    [InverseProperty("Analysis")]
    public virtual ICollection<SpatialFeature> SpatialFeatures { get; set; } = new List<SpatialFeature>();
}
