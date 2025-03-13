using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using System;
using ToYouEMS.ToYouEMS.Core.Models.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ToYouEMS.ToYouEMS.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Case> Cases { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Resume> Resumes { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<Stat> Stats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 用户表配置
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.UserID);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.UserType).IsRequired();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // 用户资料表配置
            modelBuilder.Entity<Profile>(entity =>
            {
                entity.ToTable("Profiles");
                entity.HasKey(e => e.ProfileID);
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.Gender).HasMaxLength(10);
                entity.Property(e => e.BirthPlace).HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(255);
                entity.Property(e => e.AvatarUrl).HasMaxLength(255);

                // 关系配置
                entity.HasOne(d => d.User)
                      .WithOne(p => p.Profile)
                      .HasForeignKey<Profile>(d => d.UserID);
            });

            // 案件信息表配置
            modelBuilder.Entity<Case>(entity =>
            {
                entity.ToTable("Cases");
                entity.HasKey(e => e.CaseID);
                entity.Property(e => e.CaseName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CompanyName).HasMaxLength(100);
                entity.Property(e => e.Position).HasMaxLength(100);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.Property(e => e.ContactPerson).HasMaxLength(100);
                entity.Property(e => e.ContactInfo).HasMaxLength(100);
                entity.Property(e => e.Status).HasDefaultValue(CaseStatus.Active);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 关系配置
                entity.HasOne(d => d.Creator)
                      .WithMany(p => p.Cases)
                      .HasForeignKey(d => d.CreatedBy);
            });

            // 面试问题表配置
            modelBuilder.Entity<Question>(entity =>
            {
                entity.ToTable("Questions");
                entity.HasKey(e => e.QuestionID);
                entity.Property(e => e.QuestionText).IsRequired().HasColumnName("Question");
                entity.Property(e => e.Source).HasDefaultValue(QuestionSource.Personal);
                entity.Property(e => e.Status).HasDefaultValue(QuestionStatus.Approved);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 关系配置
                entity.HasOne(d => d.Case)
                      .WithMany(p => p.Questions)
                      .HasForeignKey(d => d.CaseID);

                entity.HasOne(d => d.User)
                      .WithMany(p => p.Questions)
                      .HasForeignKey(d => d.UserID);
            });

            // 简历表配置
            modelBuilder.Entity<Resume>(entity =>
            {
                entity.ToTable("Resumes");
                entity.HasKey(e => e.ResumeID);
                entity.Property(e => e.FileName).HasMaxLength(100);
                entity.Property(e => e.FileUrl).HasMaxLength(255);
                entity.Property(e => e.Status).HasDefaultValue(ResumeStatus.Pending);
                entity.Property(e => e.UploadDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 关系配置
                entity.HasOne(d => d.User)
                      .WithMany(p => p.Resumes)
                      .HasForeignKey(d => d.UserID);

                entity.HasOne(d => d.Reviewer)
                      .WithMany()
                      .HasForeignKey(d => d.ReviewerID)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);
            });

            // 勤务表配置
            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.ToTable("Attendance");
                entity.HasKey(e => e.AttendanceID);
                entity.Property(e => e.Month).HasMaxLength(7);
                entity.Property(e => e.FileUrl).HasMaxLength(255);
                entity.Property(e => e.Status).HasDefaultValue(AttendanceStatus.Pending);
                entity.Property(e => e.UploadDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 关系配置
                entity.HasOne(d => d.User)
                      .WithMany(p => p.Attendances)
                      .HasForeignKey(d => d.UserID);
            });

            // 系统日志配置
            modelBuilder.Entity<Log>(entity =>
            {
                entity.ToTable("Logs");
                entity.HasKey(e => e.LogID);
                entity.Property(e => e.Action).HasMaxLength(50);
                entity.Property(e => e.LogTime).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // 关系配置
                entity.HasOne(d => d.User)
                      .WithMany(p => p.Logs)
                      .HasForeignKey(d => d.UserID)
                      .IsRequired(false);
            });

            // 仪表盘统计配置
            modelBuilder.Entity<Stat>(entity =>
            {
                entity.ToTable("Stats");
                entity.HasKey(e => e.StatID);
                entity.Property(e => e.StatCategory).IsRequired().HasMaxLength(50);
                entity.Property(e => e.StatName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LastUpdated).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}
