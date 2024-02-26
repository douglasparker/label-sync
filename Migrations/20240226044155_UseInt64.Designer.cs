﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Models.Database;

#nullable disable

namespace LabelSync.Migrations
{
    [DbContext(typeof(ApplicationContext))]
    [Migration("20240226044155_UseInt64")]
    partial class UseInt64
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("Models.Database.Label", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("IndexId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("LabelId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("RepositoryId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Labels");
                });
#pragma warning restore 612, 618
        }
    }
}
