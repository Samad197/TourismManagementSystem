namespace TourismManagementSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateTourismDb : DbMigration
    {
        public override void Up()
        {
            //DropForeignKey("dbo.TourSessions", "PackageId", "dbo.TourPackages");
            DropForeignKey("dbo.Sessions", "PackageId", "dbo.TourPackages");
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
            
            AddForeignKey("dbo.Bookings", "SessionId", "dbo.Sessions", "SessionId", cascadeDelete: true);
            //AddForeignKey("dbo.TourSessions", "PackageId", "dbo.TourPackages", "PackageId", cascadeDelete: true);
            AddForeignKey("dbo.Sessions", "PackageId", "dbo.TourPackages", "PackageId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Sessions", "PackageId", "dbo.TourPackages");
            //DropForeignKey("dbo.TourSessions", "PackageId", "dbo.TourPackages");
            DropForeignKey("dbo.Bookings", "SessionId", "dbo.Sessions");
            DropForeignKey("dbo.Reviews", "TouristId", "dbo.Tourists");
            DropForeignKey("dbo.Reviews", "PackageId", "dbo.TourPackages");
            DropIndex("dbo.Reviews", new[] { "TouristId" });
            DropIndex("dbo.Reviews", new[] { "PackageId" });
            DropTable("dbo.Tourists");
            DropTable("dbo.Reviews");
            AddForeignKey("dbo.Sessions", "PackageId", "dbo.TourPackages", "PackageId", cascadeDelete: true);
            //AddForeignKey("dbo.TourSessions", "PackageId", "dbo.TourPackages", "PackageId");
        }
    }
}
