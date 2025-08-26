namespace TourismManagementSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MakeSessionBasedBooking : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.Bookings", name: "TouristUserId", newName: "TouristId");
            RenameIndex(table: "dbo.Bookings", name: "IX_TouristUserId", newName: "IX_TouristId");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.Bookings", name: "IX_TouristId", newName: "IX_TouristUserId");
            RenameColumn(table: "dbo.Bookings", name: "TouristId", newName: "TouristUserId");
        }
    }
}
