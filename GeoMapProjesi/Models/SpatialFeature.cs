using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoMapProjesi.Models;

public partial class SpatialFeature
{
    [Key]
    public int Id { get; set; }

    public int? AnalysisId { get; set; }

    [StringLength(50)]
    public string? FeatureType { get; set; }

    [Column(TypeName = "geometry(Polygon,4326)")]
    public Polygon? Geometry { get; set; }

    public double? AreaM2 { get; set; }

    public double? Confidence { get; set; }

    [ForeignKey("AnalysisId")]
    [InverseProperty("SpatialFeatures")]
    public virtual Analysis? Analysis { get; set; }
}
