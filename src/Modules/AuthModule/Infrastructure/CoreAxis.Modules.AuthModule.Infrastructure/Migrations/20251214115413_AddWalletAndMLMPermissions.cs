using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.AuthModule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletAndMLMPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Insert 'MarkPaid' Action
                IF NOT EXISTS (SELECT 1 FROM Actions WHERE Code = 'MarkPaid')
                BEGIN
                    INSERT INTO Actions (Id, Code, Name, Description, SortOrder, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (NEWID(), 'MarkPaid', 'Mark Paid', 'Mark commission as paid', 0, GETDATE(), 'System', 'System', GETDATE(), 1)
                END

                -- Ensure standard Actions exist
                IF NOT EXISTS (SELECT 1 FROM Actions WHERE Code = 'Read')
                BEGIN
                    INSERT INTO Actions (Id, Code, Name, Description, SortOrder, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (NEWID(), 'Read', 'Read', 'Read permission', 1, GETDATE(), 'System', 'System', GETDATE(), 1)
                END

                IF NOT EXISTS (SELECT 1 FROM Actions WHERE Code = 'Manage')
                BEGIN
                    INSERT INTO Actions (Id, Code, Name, Description, SortOrder, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (NEWID(), 'Manage', 'Manage', 'Manage permission', 2, GETDATE(), 'System', 'System', GETDATE(), 1)
                END

                IF NOT EXISTS (SELECT 1 FROM Actions WHERE Code = 'Create')
                BEGIN
                    INSERT INTO Actions (Id, Code, Name, Description, SortOrder, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (NEWID(), 'Create', 'Create', 'Create permission', 3, GETDATE(), 'System', 'System', GETDATE(), 1)
                END

                IF NOT EXISTS (SELECT 1 FROM Actions WHERE Code = 'Update')
                BEGIN
                    INSERT INTO Actions (Id, Code, Name, Description, SortOrder, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (NEWID(), 'Update', 'Update', 'Update permission', 4, GETDATE(), 'System', 'System', GETDATE(), 1)
                END

                IF NOT EXISTS (SELECT 1 FROM Actions WHERE Code = 'Delete')
                BEGIN
                    INSERT INTO Actions (Id, Code, Name, Description, SortOrder, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (NEWID(), 'Delete', 'Delete', 'Delete permission', 5, GETDATE(), 'System', 'System', GETDATE(), 1)
                END

                -- Insert Pages
                DECLARE @WalletPageId UNIQUEIDENTIFIER
                DECLARE @CommissionsPageId UNIQUEIDENTIFIER
                DECLARE @CommissionRulesPageId UNIQUEIDENTIFIER
                DECLARE @UserReferralsPageId UNIQUEIDENTIFIER

                IF NOT EXISTS (SELECT 1 FROM Pages WHERE Code = 'WALLET')
                BEGIN
                    SET @WalletPageId = NEWID()
                    INSERT INTO Pages (Id, Code, Name, ModuleName, Description, SortOrder, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (@WalletPageId, 'WALLET', 'Wallet', 'WalletModule', 'Wallet management', 0, GETDATE(), 'System', 'System', GETDATE(), 1)
                END
                ELSE
                BEGIN
                    SELECT @WalletPageId = Id FROM Pages WHERE Code = 'WALLET'
                END

                IF NOT EXISTS (SELECT 1 FROM Pages WHERE Code = 'Commissions')
                BEGIN
                    SET @CommissionsPageId = NEWID()
                    INSERT INTO Pages (Id, Code, Name, ModuleName, Description, SortOrder, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (@CommissionsPageId, 'Commissions', 'Commissions', 'MLMModule', 'Commission management', 0, GETDATE(), 'System', 'System', GETDATE(), 1)
                END
                ELSE
                BEGIN
                    SELECT @CommissionsPageId = Id FROM Pages WHERE Code = 'Commissions'
                END

                IF NOT EXISTS (SELECT 1 FROM Pages WHERE Code = 'CommissionRules')
                BEGIN
                    SET @CommissionRulesPageId = NEWID()
                    INSERT INTO Pages (Id, Code, Name, ModuleName, Description, SortOrder, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (@CommissionRulesPageId, 'CommissionRules', 'Commission Rules', 'MLMModule', 'Commission rules management', 0, GETDATE(), 'System', 'System', GETDATE(), 1)
                END
                ELSE
                BEGIN
                    SELECT @CommissionRulesPageId = Id FROM Pages WHERE Code = 'CommissionRules'
                END

                IF NOT EXISTS (SELECT 1 FROM Pages WHERE Code = 'UserReferrals')
                BEGIN
                    SET @UserReferralsPageId = NEWID()
                    INSERT INTO Pages (Id, Code, Name, ModuleName, Description, SortOrder, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (@UserReferralsPageId, 'UserReferrals', 'User Referrals', 'MLMModule', 'User referrals management', 0, GETDATE(), 'System', 'System', GETDATE(), 1)
                END
                ELSE
                BEGIN
                    SELECT @UserReferralsPageId = Id FROM Pages WHERE Code = 'UserReferrals'
                END

                -- Get Action Ids
                DECLARE @ReadActionId UNIQUEIDENTIFIER
                DECLARE @ManageActionId UNIQUEIDENTIFIER
                DECLARE @CreateActionId UNIQUEIDENTIFIER
                DECLARE @UpdateActionId UNIQUEIDENTIFIER
                DECLARE @DeleteActionId UNIQUEIDENTIFIER
                DECLARE @MarkPaidActionId UNIQUEIDENTIFIER

                SELECT @ReadActionId = Id FROM Actions WHERE Code = 'Read'
                SELECT @ManageActionId = Id FROM Actions WHERE Code = 'Manage'
                SELECT @CreateActionId = Id FROM Actions WHERE Code = 'Create'
                SELECT @UpdateActionId = Id FROM Actions WHERE Code = 'Update'
                SELECT @DeleteActionId = Id FROM Actions WHERE Code = 'Delete'
                SELECT @MarkPaidActionId = Id FROM Actions WHERE Code = 'MarkPaid'

                -- Insert Permissions
                -- WALLET:Read
                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PageId = @WalletPageId AND ActionId = @ReadActionId)
                BEGIN
                    INSERT INTO Permissions (Id, PageId, ActionId, Name, Description, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (NEWID(), @WalletPageId, @ReadActionId, 'WALLET:Read', 'WALLET Read permission', GETDATE(), 'System', 'System', GETDATE(), 1)
                END

                -- WALLET:Manage
                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PageId = @WalletPageId AND ActionId = @ManageActionId)
                BEGIN
                    INSERT INTO Permissions (Id, PageId, ActionId, Name, Description, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (NEWID(), @WalletPageId, @ManageActionId, 'WALLET:Manage', 'WALLET Manage permission', GETDATE(), 'System', 'System', GETDATE(), 1)
                END

                -- Commissions:Read
                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PageId = @CommissionsPageId AND ActionId = @ReadActionId)
                BEGIN
                    INSERT INTO Permissions (Id, PageId, ActionId, Name, Description, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (NEWID(), @CommissionsPageId, @ReadActionId, 'Commissions:Read', 'Commissions Read permission', GETDATE(), 'System', 'System', GETDATE(), 1)
                END

                -- Commissions:MarkPaid
                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PageId = @CommissionsPageId AND ActionId = @MarkPaidActionId)
                BEGIN
                    INSERT INTO Permissions (Id, PageId, ActionId, Name, Description, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (NEWID(), @CommissionsPageId, @MarkPaidActionId, 'Commissions:MarkPaid', 'Commissions MarkPaid permission', GETDATE(), 'System', 'System', GETDATE(), 1)
                END

                -- CommissionRules:Create
                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PageId = @CommissionRulesPageId AND ActionId = @CreateActionId)
                BEGIN
                    INSERT INTO Permissions (Id, PageId, ActionId, Name, Description, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (NEWID(), @CommissionRulesPageId, @CreateActionId, 'CommissionRules:Create', 'CommissionRules Create permission', GETDATE(), 'System', 'System', GETDATE(), 1)
                END

                -- UserReferrals:Create
                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PageId = @UserReferralsPageId AND ActionId = @CreateActionId)
                BEGIN
                    INSERT INTO Permissions (Id, PageId, ActionId, Name, Description, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, IsActive)
                    VALUES (NEWID(), @UserReferralsPageId, @CreateActionId, 'UserReferrals:Create', 'UserReferrals Create permission', GETDATE(), 'System', 'System', GETDATE(), 1)
                END
                
                -- Assign Permissions to Roles
                DECLARE @UserRoleId UNIQUEIDENTIFIER
                DECLARE @AdminRoleId UNIQUEIDENTIFIER
                
                SELECT @UserRoleId = Id FROM Roles WHERE Name = 'User'
                SELECT @AdminRoleId = Id FROM Roles WHERE Name = 'Admin'

                -- Assign all Read permissions to User role
                IF @UserRoleId IS NOT NULL
                BEGIN
                    INSERT INTO RolePermissions (Id, RoleId, PermissionId, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, AssignedAt, AssignedBy, IsActive)
                    SELECT NEWID(), @UserRoleId, p.Id, GETDATE(), 'System', 'System', GETDATE(), GETDATE(), '00000000-0000-0000-0000-000000000000', 1
                    FROM Permissions p
                    JOIN Actions a ON p.ActionId = a.Id
                    WHERE a.Code = 'Read'
                    AND NOT EXISTS (SELECT 1 FROM RolePermissions rp WHERE rp.RoleId = @UserRoleId AND rp.PermissionId = p.Id)
                END

                -- Assign ALL permissions to Admin role
                IF @AdminRoleId IS NOT NULL
                BEGIN
                    INSERT INTO RolePermissions (Id, RoleId, PermissionId, CreatedOn, CreatedBy, LastModifiedBy, LastModifiedOn, AssignedAt, AssignedBy, IsActive)
                    SELECT NEWID(), @AdminRoleId, p.Id, GETDATE(), 'System', 'System', GETDATE(), GETDATE(), '00000000-0000-0000-0000-000000000000', 1
                    FROM Permissions p
                    WHERE NOT EXISTS (SELECT 1 FROM RolePermissions rp WHERE rp.RoleId = @AdminRoleId AND rp.PermissionId = p.Id)
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
