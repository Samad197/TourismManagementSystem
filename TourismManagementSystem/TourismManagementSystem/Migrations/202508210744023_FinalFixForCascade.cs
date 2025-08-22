namespace TourismManagementSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FinalFixForCascade : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Bookings", "TouristId", "dbo.TouristProfiles");
            DropForeignKey("dbo.Bookings", "PackageId", "dbo.TourPackages");
            DropForeignKey("dbo.TourPackages", "AgencyId", "dbo.AgencyProfiles");
            DropForeignKey("dbo.Feedbacks", "TouristId", "dbo.TouristProfiles");
            DropForeignKey("dbo.Feedbacks", "PackageId", "dbo.TourPackages");
            AddForeignKey("dbo.Bookings", "TouristId", "dbo.TouristProfiles", "ProfileId");
            AddForeignKey("dbo.Bookings", "PackageId", "dbo.TourPackages", "PackageId");
            AddForeignKey("dbo.TourPackages", "AgencyId", "dbo.AgencyProfiles", "ProfileId");
            AddForeignKey("dbo.Feedbacks", "TouristId", "dbo.TouristProfiles", "ProfileId");
            AddForeignKey("dbo.Feedbacks", "PackageId", "dbo.TourPackages", "PackageId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Feedbacks", "PackageId", "dbo.TourPackages");
            DropForeignKey("dbo.Feedbacks", "TouristId", "dbo.TouristProfiles");
            DropForeignKey("dbo.TourPackages", "AgencyId", "dbo.AgencyProfiles");
            DropForeignKey("dbo.Bookings", "PackageId", "dbo.TourPackages");
            DropForeignKey("dbo.Bookings", "TouristId", "dbo.TouristProfiles");
            AddForeignKey("dbo.Feedbacks", "PackageId", "dbo.TourPackages", "PackageId", cascadeDelete: true);
            AddForeignKey("dbo.Feedbacks", "TouristId", "dbo.TouristProfiles", "ProfileId", cascadeDelete: true);
            AddForeignKey("dbo.TourPackages", "AgencyId", "dbo.AgencyProfiles", "ProfileId", cascadeDelete: true);
            AddForeignKey("dbo.Bookings", "PackageId", "dbo.TourPackages", "PackageId", cascadeDelete: true);
            AddForeignKey("dbo.Bookings", "TouristId", "dbo.TouristProfiles", "ProfileId", cascadeDelete: true);
        }
    }
}
