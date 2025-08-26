namespace TourismManagementSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MakeFeedbackSharedPkWithBooking : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Bookings", "TouristId", "dbo.TouristProfiles");
            DropForeignKey("dbo.Bookings", "PackageId", "dbo.TourPackages");
            DropForeignKey("dbo.Feedbacks", "TouristId", "dbo.TouristProfiles");
            DropForeignKey("dbo.Feedbacks", "PackageId", "dbo.TourPackages");
            DropIndex("dbo.Bookings", new[] { "TouristId" });
            DropIndex("dbo.Bookings", new[] { "PackageId" });
            DropIndex("dbo.TourPackages", new[] { "AgencyId" });
            DropIndex("dbo.Feedbacks", new[] { "TouristId" });
            DropIndex("dbo.Feedbacks", new[] { "PackageId" });
            DropPrimaryKey("dbo.Feedbacks");
            CreateTable(
                "dbo.TourSessions",
                c => new
                    {
                        SessionId = c.Int(nullable: false, identity: true),
                        PackageId = c.Int(nullable: false),
                        StartDate = c.DateTime(nullable: false),
                        AvailableSlots = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.SessionId)
                .ForeignKey("dbo.TourPackages", t => t.PackageId)
                .Index(t => t.PackageId);
            
            CreateTable(
                "dbo.GuideProfiles",
                c => new
                    {
                        ProfileId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        FullNameOnLicense = c.String(nullable: false, maxLength: 100),
                        GuideLicenseNo = c.String(maxLength: 50),
                        Bio = c.String(),
                        PhotoPath = c.String(),
                        VerificationDocPath = c.String(),
                        Status = c.String(nullable: false, maxLength: 30),
                        Phone = c.String(),
                    })
                .PrimaryKey(t => t.ProfileId)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            AddColumn("dbo.AgencyProfiles", "Status", c => c.String(nullable: false, maxLength: 30));
            AddColumn("dbo.AgencyProfiles", "VerificationDocPath", c => c.String());
            AddColumn("dbo.Bookings", "SessionId", c => c.Int(nullable: false));
            AddColumn("dbo.Bookings", "TouristUserId", c => c.Int(nullable: false));
            AddColumn("dbo.Bookings", "Participants", c => c.Int(nullable: false));
            AddColumn("dbo.Bookings", "PaymentStatus", c => c.String());
            AddColumn("dbo.Bookings", "CreatedAt", c => c.DateTime(nullable: false));
            AddColumn("dbo.TourPackages", "DurationDays", c => c.Int(nullable: false));
            AddColumn("dbo.TourPackages", "GuideId", c => c.Int());
            AddColumn("dbo.TourPackages", "CreatedAt", c => c.DateTime(nullable: false));
            AddColumn("dbo.Feedbacks", "BookingId", c => c.Int(nullable: false));
            AddColumn("dbo.Feedbacks", "CreatedAt", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Bookings", "Status", c => c.String());
            AlterColumn("dbo.TourPackages", "StartDate", c => c.DateTime());
            AlterColumn("dbo.TourPackages", "EndDate", c => c.DateTime());
            AlterColumn("dbo.TourPackages", "AgencyId", c => c.Int());
            AlterColumn("dbo.TourImages", "ImagePath", c => c.String(nullable: false, maxLength: 255));
            AddPrimaryKey("dbo.Feedbacks", "BookingId");
            CreateIndex("dbo.Bookings", "SessionId");
            CreateIndex("dbo.Bookings", "TouristUserId");
            CreateIndex("dbo.Feedbacks", "BookingId");
            CreateIndex("dbo.TourPackages", "AgencyId");
            CreateIndex("dbo.TourPackages", "GuideId");
            AddForeignKey("dbo.Feedbacks", "BookingId", "dbo.Bookings", "BookingId", cascadeDelete: true);
            AddForeignKey("dbo.TourPackages", "GuideId", "dbo.GuideProfiles", "ProfileId");
            AddForeignKey("dbo.Bookings", "SessionId", "dbo.TourSessions", "SessionId");
            AddForeignKey("dbo.Bookings", "TouristUserId", "dbo.Users", "UserId");
            DropColumn("dbo.Bookings", "TouristId");
            DropColumn("dbo.Bookings", "PackageId");
            DropColumn("dbo.Bookings", "NumberOfPeople");
            DropColumn("dbo.Bookings", "BookingDate");
            DropColumn("dbo.Feedbacks", "FeedbackId");
            DropColumn("dbo.Feedbacks", "TouristId");
            DropColumn("dbo.Feedbacks", "PackageId");
            DropColumn("dbo.Feedbacks", "SubmittedAt");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Feedbacks", "SubmittedAt", c => c.DateTime(nullable: false));
            AddColumn("dbo.Feedbacks", "PackageId", c => c.Int(nullable: false));
            AddColumn("dbo.Feedbacks", "TouristId", c => c.Int(nullable: false));
            AddColumn("dbo.Feedbacks", "FeedbackId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.Bookings", "BookingDate", c => c.DateTime(nullable: false));
            AddColumn("dbo.Bookings", "NumberOfPeople", c => c.Int(nullable: false));
            AddColumn("dbo.Bookings", "PackageId", c => c.Int(nullable: false));
            AddColumn("dbo.Bookings", "TouristId", c => c.Int(nullable: false));
            DropForeignKey("dbo.Bookings", "TouristUserId", "dbo.Users");
            DropForeignKey("dbo.Bookings", "SessionId", "dbo.TourSessions");
            DropForeignKey("dbo.TourSessions", "PackageId", "dbo.TourPackages");
            DropForeignKey("dbo.TourPackages", "GuideId", "dbo.GuideProfiles");
            DropForeignKey("dbo.GuideProfiles", "UserId", "dbo.Users");
            DropForeignKey("dbo.Feedbacks", "BookingId", "dbo.Bookings");
            DropIndex("dbo.GuideProfiles", new[] { "UserId" });
            DropIndex("dbo.TourPackages", new[] { "GuideId" });
            DropIndex("dbo.TourPackages", new[] { "AgencyId" });
            DropIndex("dbo.TourSessions", new[] { "PackageId" });
            DropIndex("dbo.Feedbacks", new[] { "BookingId" });
            DropIndex("dbo.Bookings", new[] { "TouristUserId" });
            DropIndex("dbo.Bookings", new[] { "SessionId" });
            DropPrimaryKey("dbo.Feedbacks");
            AlterColumn("dbo.TourImages", "ImagePath", c => c.String(nullable: false));
            AlterColumn("dbo.TourPackages", "AgencyId", c => c.Int(nullable: false));
            AlterColumn("dbo.TourPackages", "EndDate", c => c.DateTime(nullable: false));
            AlterColumn("dbo.TourPackages", "StartDate", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Bookings", "Status", c => c.String(nullable: false));
            DropColumn("dbo.Feedbacks", "CreatedAt");
            DropColumn("dbo.Feedbacks", "BookingId");
            DropColumn("dbo.TourPackages", "CreatedAt");
            DropColumn("dbo.TourPackages", "GuideId");
            DropColumn("dbo.TourPackages", "DurationDays");
            DropColumn("dbo.Bookings", "CreatedAt");
            DropColumn("dbo.Bookings", "PaymentStatus");
            DropColumn("dbo.Bookings", "Participants");
            DropColumn("dbo.Bookings", "TouristUserId");
            DropColumn("dbo.Bookings", "SessionId");
            DropColumn("dbo.AgencyProfiles", "VerificationDocPath");
            DropColumn("dbo.AgencyProfiles", "Status");
            DropTable("dbo.GuideProfiles");
            DropTable("dbo.TourSessions");
            AddPrimaryKey("dbo.Feedbacks", "FeedbackId");
            CreateIndex("dbo.Feedbacks", "PackageId");
            CreateIndex("dbo.Feedbacks", "TouristId");
            CreateIndex("dbo.TourPackages", "AgencyId");
            CreateIndex("dbo.Bookings", "PackageId");
            CreateIndex("dbo.Bookings", "TouristId");
            AddForeignKey("dbo.Feedbacks", "PackageId", "dbo.TourPackages", "PackageId");
            AddForeignKey("dbo.Feedbacks", "TouristId", "dbo.TouristProfiles", "ProfileId");
            AddForeignKey("dbo.Bookings", "PackageId", "dbo.TourPackages", "PackageId");
            AddForeignKey("dbo.Bookings", "TouristId", "dbo.TouristProfiles", "ProfileId");
        }
    }
}
