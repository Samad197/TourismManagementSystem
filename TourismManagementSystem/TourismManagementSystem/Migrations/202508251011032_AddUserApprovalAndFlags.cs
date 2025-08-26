namespace TourismManagementSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUserApprovalAndFlags : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "IsApproved", c => c.Boolean(nullable: false));
            AddColumn("dbo.Users", "EmailConfirmed", c => c.Boolean(nullable: false));
            AddColumn("dbo.Users", "IsActive", c => c.Boolean(nullable: false));
            AlterColumn("dbo.Users", "Email", c => c.String(nullable: false, maxLength: 256));
            AlterColumn("dbo.Users", "PasswordHash", c => c.String(nullable: false, maxLength: 512));
            CreateIndex("dbo.Users", "Email", unique: true, name: "IX_User_Email");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Users", "IX_User_Email");
            AlterColumn("dbo.Users", "PasswordHash", c => c.String(nullable: false, maxLength: 255));
            AlterColumn("dbo.Users", "Email", c => c.String(nullable: false));
            DropColumn("dbo.Users", "IsActive");
            DropColumn("dbo.Users", "EmailConfirmed");
            DropColumn("dbo.Users", "IsApproved");
        }
    }
}
