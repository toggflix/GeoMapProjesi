using System;
using System.Collections.Generic;
using GeoMapProjesi.Models;
using Microsoft.EntityFrameworkCore;

namespace GeoMapProjesi.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Analysis> Analyses { get; set; }

    public virtual DbSet<Insight> Insights { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<SpatialFeature> SpatialFeatures { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=127.0.0.1;Database=BiyomDB;Username=postgres", x => x.UseNetTopologySuite());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<Analysis>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Analyses_pkey");

            entity.Property(e => e.AnalysisDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Project).WithMany(p => p.Analyses)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("Analyses_ProjectId_fkey");
        });

        modelBuilder.Entity<Insight>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Insights_pkey");

            entity.HasOne(d => d.Analysis).WithMany(p => p.Insights)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("Insights_AnalysisId_fkey");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Projects_pkey");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<SpatialFeature>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("SpatialFeatures_pkey");

            entity.HasOne(d => d.Analysis).WithMany(p => p.SpatialFeatures)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("SpatialFeatures_AnalysisId_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
