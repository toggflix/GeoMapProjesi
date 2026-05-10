using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GeoMapProjesi.Models;

public partial class Project
{
    [Key]
    public int Id { get; set; }

    [StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(150)]
    public string? ClientName { get; set; }

    [StringLength(100)]
    public string? RegionName { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime? CreatedDate { get; set; }

    public bool? IsActive { get; set; }

    [InverseProperty("Project")]
    public virtual ICollection<Analysis> Analyses { get; set; } = new List<Analysis>();
}
