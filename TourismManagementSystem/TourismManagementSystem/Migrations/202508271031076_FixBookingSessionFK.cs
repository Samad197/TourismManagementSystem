namespace TourismManagementSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixBookingSessionFK : DbMigration
    {
        public override void Up()
        {
            //DropForeignKey("dbo.Bookings", "SessionId", "dbo.TourSessions");
            DropForeignKey("dbo.Bookings", "SessionId", "dbo.Sessions");
            //AddForeignKey("dbo.Bookings", "SessionId", "dbo.TourSessions", "SessionId", cascadeDelete: true);
            AddForeignKey("dbo.Bookings", "SessionId", "dbo.Sessions", "SessionId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Bookings", "SessionId", "dbo.Sessions");
            //DropForeignKey("dbo.Bookings", "SessionId", "dbo.TourSessions");
            AddForeignKey("dbo.Bookings", "SessionId", "dbo.Sessions", "SessionId", cascadeDelete: true);
            //AddForeignKey("dbo.Bookings", "SessionId", "dbo.TourSessions", "SessionId");
        }
    }
}
