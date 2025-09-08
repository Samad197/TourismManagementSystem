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
                        UserId = c.Int(nullable: false),
                        AgencyName = c.String(nullable: false, maxLength: 100),
                        Description = c.String(),
                        LogoPath = c.String(),
                        Phone = c.String(),
                        Website = c.String(),
                        Status = c.String(nullable: false, maxLength: 30),
                        VerificationDocPath = c.String(),
                    })
                .PrimaryKey(t => t.UserId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserId = c.Int(nullable: false, identity: true),
                        FullName = c.String(nullable: false, maxLength: 100),
                        Email = c.String(nullable: false, maxLength: 256),
                        PasswordHash = c.String(nullable: false, maxLength: 512),
                        RoleId = c.Int(nullable: false),
                        Phone = c.String(maxLength: 30),
                        IsApproved = c.Boolean(nullable: false),
                        EmailConfirmed = c.Boolean(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.UserId)
                .ForeignKey("dbo.Roles", t => t.RoleId, cascadeDelete: true)
                .Index(t => t.Email, unique: true, name: "IX_User_Email")
                .Index(t => t.RoleId);
            
            CreateTable(
                "dbo.GuideProfiles",
                c => new
                    {
                        UserId = c.Int(nullable: false),
                        FullNameOnLicense = c.String(nullable: false, maxLength: 100),
                        GuideLicenseNo = c.String(maxLength: 50),
                        Bio = c.String(),
                        PhotoPath = c.String(),
                        VerificationDocPath = c.String(),
                        Status = c.String(nullable: false, maxLength: 30),
                        Phone = c.String(),
                    })
                .PrimaryKey(t => t.UserId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Roles",
                c => new
                    {
                        RoleId = c.Int(nullable: false, identity: true),
                        RoleName = c.String(nullable: false, maxLength: 50),
                    })
                .PrimaryKey(t => t.RoleId);
            
            CreateTable(
                "dbo.Bookings",
                c => new
                    {
                        BookingId = c.Int(nullable: false, identity: true),
                        TouristId = c.Int(nullable: false),
                        SessionId = c.Int(nullable: false),
                        IsApproved = c.Boolean(),
                        Participants = c.Int(nullable: false),
                        Status = c.String(nullable: false, maxLength: 20),
                        PaymentStatus = c.String(nullable: false, maxLength: 20),
                        PaidAt = c.DateTime(),
                        CustomerName = c.String(nullable: false, maxLength: 150),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.BookingId)
                .ForeignKey("dbo.Sessions", t => t.SessionId)
                .ForeignKey("dbo.Users", t => t.TouristId)
                .Index(t => t.TouristId)
                .Index(t => t.SessionId);
            
            CreateTable(
                "dbo.Feedbacks",
                c => new
                    {
                        FeedbackId = c.Int(nullable: false, identity: true),
                        BookingId = c.Int(nullable: false),
                        Rating = c.Int(nullable: false),
                        Comment = c.String(maxLength: 1000),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.FeedbackId)
                .ForeignKey("dbo.Bookings", t => t.BookingId, cascadeDelete: true)
                .Index(t => t.BookingId, unique: true, name: "IX_Feedback_Booking");
            
            CreateTable(
                "dbo.Sessions",
                c => new
                    {
                        SessionId = c.Int(nullable: false, identity: true),
                        PackageId = c.Int(nullable: false),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(nullable: false),
                        Capacity = c.Int(nullable: false),
                        IsCanceled = c.Boolean(nullable: false),
                        Notes = c.String(maxLength: 500),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.SessionId)
                .ForeignKey("dbo.TourPackages", t => t.PackageId)
                .Index(t => t.PackageId);
            
            CreateTable(
                "dbo.TourPackages",
                c => new
                    {
                        PackageId = c.Int(nullable: false, identity: true),
                        Title = c.String(nullable: false, maxLength: 100),
                        Description = c.String(),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DurationDays = c.Int(nullable: false),
                        MaxGroupSize = c.Int(nullable: false),
                        StartDate = c.DateTime(),
                        EndDate = c.DateTime(),
                        AgencyId = c.Int(),
                        GuideId = c.Int(),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.PackageId)
                .ForeignKey("dbo.AgencyProfiles", t => t.AgencyId)
                .ForeignKey("dbo.GuideProfiles", t => t.GuideId)
                .Index(t => t.AgencyId)
                .Index(t => t.GuideId);
            
            CreateTable(
                "dbo.TourImages",
                c => new
                    {
                        ImageId = c.Int(nullable: false, identity: true),
                        PackageId = c.Int(nullable: false),
                        ImagePath = c.String(nullable: false, maxLength: 255),
                    })
                .PrimaryKey(t => t.ImageId)
                .ForeignKey("dbo.TourPackages", t => t.PackageId, cascadeDelete: true)
                .Index(t => t.PackageId);
            
            CreateTable(
                "dbo.Reviews",
                c => new
                    {
                        ReviewId = c.Int(nullable: false, identity: true),
                        PackageId = c.Int(nullable: false),
                        TouristId = c.Int(nullable: false),
                        Rating = c.Int(nullable: false),
                        Comment = c.String(),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ReviewId)
                .ForeignKey("dbo.TourPackages", t => t.PackageId, cascadeDelete: true)
                .ForeignKey("dbo.Tourists", t => t.TouristId, cascadeDelete: true)
                .Index(t => t.PackageId)
                .Index(t => t.TouristId);
            
            CreateTable(
                "dbo.Tourists",
                c => new
                    {
                        TouristId = c.Int(nullable: false, identity: true),
                        FullName = c.String(),
                        Email = c.String(),
                    })
                .PrimaryKey(t => t.TouristId);
            
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
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TouristProfiles", "UserId", "dbo.Users");
            DropForeignKey("dbo.Bookings", "TouristId", "dbo.Users");
            DropForeignKey("dbo.Bookings", "SessionId", "dbo.Sessions");
            DropForeignKey("dbo.Sessions", "PackageId", "dbo.TourPackages");
            DropForeignKey("dbo.Reviews", "TouristId", "dbo.Tourists");
            DropForeignKey("dbo.Reviews", "PackageId", "dbo.TourPackages");
            DropForeignKey("dbo.TourImages", "PackageId", "dbo.TourPackages");
            DropForeignKey("dbo.TourPackages", "GuideId", "dbo.GuideProfiles");
            DropForeignKey("dbo.TourPackages", "AgencyId", "dbo.AgencyProfiles");
            DropForeignKey("dbo.Feedbacks", "BookingId", "dbo.Bookings");
            DropForeignKey("dbo.Users", "RoleId", "dbo.Roles");
            DropForeignKey("dbo.GuideProfiles", "UserId", "dbo.Users");
            DropForeignKey("dbo.AgencyProfiles", "UserId", "dbo.Users");
            DropIndex("dbo.TouristProfiles", new[] { "UserId" });
            DropIndex("dbo.Reviews", new[] { "TouristId" });
            DropIndex("dbo.Reviews", new[] { "PackageId" });
            DropIndex("dbo.TourImages", new[] { "PackageId" });
            DropIndex("dbo.TourPackages", new[] { "GuideId" });
            DropIndex("dbo.TourPackages", new[] { "AgencyId" });
            DropIndex("dbo.Sessions", new[] { "PackageId" });
            DropIndex("dbo.Feedbacks", "IX_Feedback_Booking");
            DropIndex("dbo.Bookings", new[] { "SessionId" });
            DropIndex("dbo.Bookings", new[] { "TouristId" });
            DropIndex("dbo.GuideProfiles", new[] { "UserId" });
            DropIndex("dbo.Users", new[] { "RoleId" });
            DropIndex("dbo.Users", "IX_User_Email");
            DropIndex("dbo.AgencyProfiles", new[] { "UserId" });
            DropTable("dbo.TouristProfiles");
            DropTable("dbo.Tourists");
            DropTable("dbo.Reviews");
            DropTable("dbo.TourImages");
            DropTable("dbo.TourPackages");
            DropTable("dbo.Sessions");
            DropTable("dbo.Feedbacks");
            DropTable("dbo.Bookings");
            DropTable("dbo.Roles");
            DropTable("dbo.GuideProfiles");
            DropTable("dbo.Users");
            DropTable("dbo.AgencyProfiles");
        }
    }
}
