namespace TourismManagementSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddBookingStatusAndCustomerName : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Bookings", "IsApproved", c => c.Boolean());
            AddColumn("dbo.Bookings", "CustomerName", c => c.String(nullable: false, maxLength: 150));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Bookings", "CustomerName");
            DropColumn("dbo.Bookings", "IsApproved");
        }
    }
}
