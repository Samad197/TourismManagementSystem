namespace TourismManagementSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSessionsTable : DbMigration
    {
        public override void Up()
        {
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
                .ForeignKey("dbo.TourPackages", t => t.PackageId, cascadeDelete: true)
                .Index(t => t.PackageId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Sessions", "PackageId", "dbo.TourPackages");
            DropIndex("dbo.Sessions", new[] { "PackageId" });
            DropTable("dbo.Sessions");
        }
    }
}
