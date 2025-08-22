namespace TourismManagementSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AgencyProfiles",
                c => new
                    {
                        ProfileId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        AgencyName = c.String(nullable: false, maxLength: 100),
                        Description = c.String(),
                        LogoPath = c.String(),
                        Phone = c.String(),
                        Website = c.String(),
                    })
                .PrimaryKey(t => t.ProfileId)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: false)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserId = c.Int(nullable: false, identity: true),
                        FullName = c.String(nullable: false, maxLength: 100),
                        Email = c.String(nullable: false),
                        PasswordHash = c.String(nullable: false, maxLength: 255),
                        Role = c.String(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.UserId);
            
            CreateTable(
                "dbo.Bookings",
                c => new
                    {
                        BookingId = c.Int(nullable: false, identity: true),
                        TouristId = c.Int(nullable: false),
                        PackageId = c.Int(nullable: false),
                        NumberOfPeople = c.Int(nullable: false),
                        BookingDate = c.DateTime(nullable: false),
                        Status = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.BookingId)
                .ForeignKey("dbo.TouristProfiles", t => t.TouristId, cascadeDelete: false)
                .ForeignKey("dbo.TourPackages", t => t.PackageId, cascadeDelete: false)
                .Index(t => t.TouristId)
                .Index(t => t.PackageId);
            
            CreateTable(
                "dbo.TouristProfiles",
                c => new
                    {
                        ProfileId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        Phone = c.String(),
                        Address = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.ProfileId)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: false)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.TourPackages",
                c => new
                    {
                        PackageId = c.Int(nullable: false, identity: true),
                        Title = c.String(nullable: false, maxLength: 100),
                        Description = c.String(),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(nullable: false),
                        MaxGroupSize = c.Int(nullable: false),
                        AgencyId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.PackageId)
                .ForeignKey("dbo.AgencyProfiles", t => t.AgencyId, cascadeDelete: false)
                .Index(t => t.AgencyId);
            
            CreateTable(
                "dbo.Feedbacks",
                c => new
                    {
                        FeedbackId = c.Int(nullable: false, identity: true),
                        TouristId = c.Int(nullable: false),
                        PackageId = c.Int(nullable: false),
                        Comments = c.String(maxLength: 1000),
                        Rating = c.Int(nullable: false),
                        SubmittedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.FeedbackId)
                .ForeignKey("dbo.TouristProfiles", t => t.TouristId, cascadeDelete: false)
                .ForeignKey("dbo.TourPackages", t => t.PackageId, cascadeDelete: false)
                .Index(t => t.TouristId)
                .Index(t => t.PackageId);
            
            CreateTable(
                "dbo.TourImages",
                c => new
                    {
                        ImageId = c.Int(nullable: false, identity: true),
                        PackageId = c.Int(nullable: false),
                        ImagePath = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.ImageId)
                .ForeignKey("dbo.TourPackages", t => t.PackageId, cascadeDelete: false)
                .Index(t => t.PackageId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TourImages", "PackageId", "dbo.TourPackages");
            DropForeignKey("dbo.Feedbacks", "PackageId", "dbo.TourPackages");
            DropForeignKey("dbo.Feedbacks", "TouristId", "dbo.TouristProfiles");
            DropForeignKey("dbo.Bookings", "PackageId", "dbo.TourPackages");
            DropForeignKey("dbo.TourPackages", "AgencyId", "dbo.AgencyProfiles");
            DropForeignKey("dbo.Bookings", "TouristId", "dbo.TouristProfiles");
            DropForeignKey("dbo.TouristProfiles", "UserId", "dbo.Users");
            DropForeignKey("dbo.AgencyProfiles", "UserId", "dbo.Users");
            DropIndex("dbo.TourImages", new[] { "PackageId" });
            DropIndex("dbo.Feedbacks", new[] { "PackageId" });
            DropIndex("dbo.Feedbacks", new[] { "TouristId" });
            DropIndex("dbo.TourPackages", new[] { "AgencyId" });
            DropIndex("dbo.TouristProfiles", new[] { "UserId" });
            DropIndex("dbo.Bookings", new[] { "PackageId" });
            DropIndex("dbo.Bookings", new[] { "TouristId" });
            DropIndex("dbo.AgencyProfiles", new[] { "UserId" });
            DropTable("dbo.TourImages");
            DropTable("dbo.Feedbacks");
            DropTable("dbo.TourPackages");
            DropTable("dbo.TouristProfiles");
            DropTable("dbo.Bookings");
            DropTable("dbo.Users");
            DropTable("dbo.AgencyProfiles");
        }
    }
}
