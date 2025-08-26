namespace TourismManagementSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixFeedbackRelation : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.AgencyProfiles", "UserId", "dbo.Users");
            DropForeignKey("dbo.GuideProfiles", "UserId", "dbo.Users");
            DropForeignKey("dbo.TourPackages", "AgencyId", "dbo.AgencyProfiles");
            DropForeignKey("dbo.TourPackages", "GuideId", "dbo.GuideProfiles");
            DropIndex("dbo.Feedbacks", new[] { "BookingId" });
            DropIndex("dbo.TourPackages", new[] { "AgencyId" });
            DropIndex("dbo.TourPackages", new[] { "GuideId" });
            DropPrimaryKey("dbo.AgencyProfiles");
            DropPrimaryKey("dbo.Feedbacks");
            DropPrimaryKey("dbo.GuideProfiles");
            AddColumn("dbo.Feedbacks", "FeedbackId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.Feedbacks", "Comment", c => c.String(maxLength: 1000));
            AlterColumn("dbo.Bookings", "Status", c => c.String(nullable: false, maxLength: 20));
            AlterColumn("dbo.Bookings", "PaymentStatus", c => c.String(nullable: false, maxLength: 20));
            AddPrimaryKey("dbo.AgencyProfiles", "UserId");
            AddPrimaryKey("dbo.Feedbacks", "FeedbackId");
            AddPrimaryKey("dbo.GuideProfiles", "UserId");
            CreateIndex("dbo.Feedbacks", "BookingId", unique: true, name: "IX_Feedback_Booking");
            CreateIndex("dbo.TourPackages", "AgencyId");
            CreateIndex("dbo.TourPackages", "GuideId");
            AddForeignKey("dbo.AgencyProfiles", "UserId", "dbo.Users", "UserId");
            AddForeignKey("dbo.GuideProfiles", "UserId", "dbo.Users", "UserId");
            AddForeignKey("dbo.TourPackages", "AgencyId", "dbo.AgencyProfiles", "UserId");
            AddForeignKey("dbo.TourPackages", "GuideId", "dbo.GuideProfiles", "UserId");
            DropColumn("dbo.AgencyProfiles", "ProfileId");
            DropColumn("dbo.Feedbacks", "Comments");
            DropColumn("dbo.GuideProfiles", "ProfileId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.GuideProfiles", "ProfileId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.Feedbacks", "Comments", c => c.String(maxLength: 1000));
            AddColumn("dbo.AgencyProfiles", "ProfileId", c => c.Int(nullable: false, identity: true));
            DropForeignKey("dbo.TourPackages", "GuideId", "dbo.GuideProfiles");
            DropForeignKey("dbo.TourPackages", "AgencyId", "dbo.AgencyProfiles");
            DropForeignKey("dbo.GuideProfiles", "UserId", "dbo.Users");
            DropForeignKey("dbo.AgencyProfiles", "UserId", "dbo.Users");
            DropIndex("dbo.TourPackages", new[] { "GuideId" });
            DropIndex("dbo.TourPackages", new[] { "AgencyId" });
            DropIndex("dbo.Feedbacks", "IX_Feedback_Booking");
            DropPrimaryKey("dbo.GuideProfiles");
            DropPrimaryKey("dbo.Feedbacks");
            DropPrimaryKey("dbo.AgencyProfiles");
            AlterColumn("dbo.Bookings", "PaymentStatus", c => c.String());
            AlterColumn("dbo.Bookings", "Status", c => c.String());
            DropColumn("dbo.Feedbacks", "Comment");
            DropColumn("dbo.Feedbacks", "FeedbackId");
            AddPrimaryKey("dbo.GuideProfiles", "ProfileId");
            AddPrimaryKey("dbo.Feedbacks", "BookingId");
            AddPrimaryKey("dbo.AgencyProfiles", "ProfileId");
            CreateIndex("dbo.TourPackages", "GuideId");
            CreateIndex("dbo.TourPackages", "AgencyId");
            CreateIndex("dbo.Feedbacks", "BookingId");
            AddForeignKey("dbo.TourPackages", "GuideId", "dbo.GuideProfiles", "ProfileId");
            AddForeignKey("dbo.TourPackages", "AgencyId", "dbo.AgencyProfiles", "ProfileId");
            AddForeignKey("dbo.GuideProfiles", "UserId", "dbo.Users", "UserId", cascadeDelete: true);
            AddForeignKey("dbo.AgencyProfiles", "UserId", "dbo.Users", "UserId", cascadeDelete: true);
        }
    }
}
