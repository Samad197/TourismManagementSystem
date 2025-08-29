namespace TourismManagementSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPaidAtToBooking : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Bookings", "PaidAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Bookings", "PaidAt");
        }
    }
}
