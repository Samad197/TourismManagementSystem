namespace TourismManagementSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PendingChanges : DbMigration
    {
        public override void Up()
        {
            //DropForeignKey("dbo.Bookings", "SessionId", "dbo.TourSessions");
            //DropForeignKey("dbo.TourSessions", "PackageId", "dbo.TourPackages");
            //DropIndex("dbo.TourSessions", new[] { "PackageId" });
            //DropTable("dbo.TourSessions");
        }
        
        public override void Down()
        {
            //CreateTable(
            //    "dbo.TourSessions",
            //    c => new
            //        {
            //            SessionId = c.Int(nullable: false, identity: true),
            //            PackageId = c.Int(nullable: false),
            //            StartDate = c.DateTime(nullable: false),
            //            AvailableSlots = c.Int(nullable: false),
            //        })
            //    .PrimaryKey(t => t.SessionId);
            
            //CreateIndex("dbo.TourSessions", "PackageId");
            //AddForeignKey("dbo.TourSessions", "PackageId", "dbo.TourPackages", "PackageId", cascadeDelete: true);
            //AddForeignKey("dbo.Bookings", "SessionId", "dbo.TourSessions", "SessionId", cascadeDelete: true);
        }
    }
}
