-- ==========================================
-- NadeefApp Bundles Seeding Script 🚀
-- ==========================================

BEGIN TRANSACTION;

-- 1. Get the SuperAdmin ID
DECLARE @SuperAdminId NVARCHAR(450);
SELECT TOP 1 @SuperAdminId = Id FROM [Users] WHERE Email = 'superadmin@ndeef.com';

IF @SuperAdminId IS NULL
BEGIN
    SELECT TOP 1 @SuperAdminId = Id FROM [Users] WHERE [Role] = 4; -- Fallback to any SuperAdmin
END

-- 2. Get a service ID for each category
DECLARE @IronServiceId INT;
SELECT TOP 1 @IronServiceId = Id FROM [Services] WHERE [Category] = 3; -- Iron = 3

DECLARE @WashServiceId INT;
SELECT TOP 1 @WashServiceId = Id FROM [Services] WHERE [Category] = 1; -- Wash = 1

DECLARE @DryCleanServiceId INT;
SELECT TOP 1 @DryCleanServiceId = Id FROM [Services] WHERE [Category] = 2; -- DryCleaning = 2

DECLARE @SpecialtyServiceId INT;
SELECT TOP 1 @SpecialtyServiceId = Id FROM [Services] WHERE [Category] = 4; -- Specialty = 4

-- Make sure we have a SuperAdmin ID
IF @SuperAdminId IS NULL
BEGIN
    PRINT 'Error: No SuperAdmin user found. Please create a SuperAdmin user first.';
    ROLLBACK TRANSACTION;
END
ELSE
BEGIN
    -- ── 1. Fresh Start Pack ──────────────────────────────────
    IF NOT EXISTS (SELECT 1 FROM [Bundles] WHERE [Name] = 'Fresh Start Pack')
    BEGIN
        DECLARE @FreshStartId UNIQUEIDENTIFIER = NEWID();
        
        INSERT INTO [Bundles] ([Id], [Name], [Description], [BundlePrice], [OriginalPrice], [Status], [OwnerType], [CreatedByAdminId], [LaundryId], [StartDate], [ExpiryDate], [ImageUrl], [DisplayOrder], [IsDeleted], [CreatedAt], [UpdatedAt])
        VALUES (
            @FreshStartId, 
            N'Fresh Start Pack', 
            N'5 ironing items to get your wardrobe fresh and crisp.', 
            90.00, 
            110.00, 
            2, -- Active
            0, -- Platform
            @SuperAdminId, 
            NULL, 
            NULL, 
            NULL, 
            NULL, 
            1, 
            0, 
            GETUTCDATE(), 
            NULL
        );

        IF @IronServiceId IS NOT NULL
        BEGIN
            INSERT INTO [BundleItems] ([Id], [BundleId], [ServiceId], [Quantity], [AllowedItemCategory], [CreatedAt], [UpdatedAt])
            VALUES (NEWID(), @FreshStartId, @IronServiceId, 5, 3, GETUTCDATE(), NULL);
        END
    END

    -- ── 2. Family Care Bundle ────────────────────────────────
    IF NOT EXISTS (SELECT 1 FROM [Bundles] WHERE [Name] = 'Family Care Bundle')
    BEGIN
        DECLARE @FamilyCareId UNIQUEIDENTIFIER = NEWID();

        INSERT INTO [Bundles] ([Id], [Name], [Description], [BundlePrice], [OriginalPrice], [Status], [OwnerType], [CreatedByAdminId], [LaundryId], [StartDate], [ExpiryDate], [ImageUrl], [DisplayOrder], [IsDeleted], [CreatedAt], [UpdatedAt])
        VALUES (
            @FamilyCareId, 
            N'Family Care Bundle', 
            N'Wash & Iron 10 mixed items for the whole family.', 
            190.00, 
            250.00, 
            2, -- Active
            0, -- Platform
            @SuperAdminId, 
            NULL, 
            NULL, 
            NULL, 
            NULL, 
            2, 
            0, 
            GETUTCDATE(), 
            NULL
        );

        IF @WashServiceId IS NOT NULL
        BEGIN
            INSERT INTO [BundleItems] ([Id], [BundleId], [ServiceId], [Quantity], [AllowedItemCategory], [CreatedAt], [UpdatedAt])
            VALUES (NEWID(), @FamilyCareId, @WashServiceId, 10, 1, GETUTCDATE(), NULL);
        END
    END

    -- ── 3. Wardrobe Reset ────────────────────────────────────
    IF NOT EXISTS (SELECT 1 FROM [Bundles] WHERE [Name] = 'Wardrobe Reset')
    BEGIN
        DECLARE @WardrobeResetId UNIQUEIDENTIFIER = NEWID();

        INSERT INTO [Bundles] ([Id], [Name], [Description], [BundlePrice], [OriginalPrice], [Status], [OwnerType], [CreatedByAdminId], [LaundryId], [StartDate], [ExpiryDate], [ImageUrl], [DisplayOrder], [IsDeleted], [CreatedAt], [UpdatedAt])
        VALUES (
            @WardrobeResetId, 
            N'Wardrobe Reset', 
            N'Dry clean 4 formal items and look your best.', 
            420.00, 
            520.00, 
            2, -- Active
            0, -- Platform
            @SuperAdminId, 
            NULL, 
            NULL, 
            NULL, 
            NULL, 
            3, 
            0, 
            GETUTCDATE(), 
            NULL
        );

        IF @DryCleanServiceId IS NOT NULL
        BEGIN
            INSERT INTO [BundleItems] ([Id], [BundleId], [ServiceId], [Quantity], [AllowedItemCategory], [CreatedAt], [UpdatedAt])
            VALUES (NEWID(), @WardrobeResetId, @DryCleanServiceId, 4, 2, GETUTCDATE(), NULL);
        END
    END

    -- ── 4. Home Refresh Pack ─────────────────────────────────
    IF NOT EXISTS (SELECT 1 FROM [Bundles] WHERE [Name] = 'Home Refresh Pack')
    BEGIN
        DECLARE @HomeRefreshId UNIQUEIDENTIFIER = NEWID();

        INSERT INTO [Bundles] ([Id], [Name], [Description], [BundlePrice], [OriginalPrice], [Status], [OwnerType], [CreatedByAdminId], [LaundryId], [StartDate], [ExpiryDate], [ImageUrl], [DisplayOrder], [IsDeleted], [CreatedAt], [UpdatedAt])
        VALUES (
            @HomeRefreshId, 
            N'Home Refresh Pack', 
            N'Dry clean 3 blankets and keep your home fresh.', 
            380.00, 
            480.00, 
            2, -- Active
            0, -- Platform
            @SuperAdminId, 
            NULL, 
            NULL, 
            NULL, 
            NULL, 
            4, 
            0, 
            GETUTCDATE(), 
            NULL
        );

        IF @DryCleanServiceId IS NOT NULL
        BEGIN
            INSERT INTO [BundleItems] ([Id], [BundleId], [ServiceId], [Quantity], [AllowedItemCategory], [CreatedAt], [UpdatedAt])
            VALUES (NEWID(), @HomeRefreshId, @DryCleanServiceId, 3, 2, GETUTCDATE(), NULL);
        END
    END

    -- ── 5. B Bundle for Bags ─────────────────────────────────
    IF NOT EXISTS (SELECT 1 FROM [Bundles] WHERE [Name] = 'B Bundle for Bags')
    BEGIN
        DECLARE @BagsBundleId UNIQUEIDENTIFIER = NEWID();

        INSERT INTO [Bundles] ([Id], [Name], [Description], [BundlePrice], [OriginalPrice], [Status], [OwnerType], [CreatedByAdminId], [LaundryId], [StartDate], [ExpiryDate], [ImageUrl], [DisplayOrder], [IsDeleted], [CreatedAt], [UpdatedAt])
        VALUES (
            @BagsBundleId, 
            N'B Bundle for Bags', 
            N'Wash 6 bag items and keep them looking brand new.', 
            550.00, 
            650.00, 
            2, -- Active
            0, -- Platform
            @SuperAdminId, 
            NULL, 
            NULL, 
            NULL, 
            NULL, 
            5, 
            0, 
            GETUTCDATE(), 
            NULL
        );

        IF @SpecialtyServiceId IS NOT NULL
        BEGIN
            INSERT INTO [BundleItems] ([Id], [BundleId], [ServiceId], [Quantity], [AllowedItemCategory], [CreatedAt], [UpdatedAt])
            VALUES (NEWID(), @BagsBundleId, @SpecialtyServiceId, 6, 4, GETUTCDATE(), NULL);
        END
    END

    -- ── 6. S Bundle for Shoes ────────────────────────────────
    IF NOT EXISTS (SELECT 1 FROM [Bundles] WHERE [Name] = 'S Bundle for Shoes')
    BEGIN
        DECLARE @ShoesBundleId UNIQUEIDENTIFIER = NEWID();

        INSERT INTO [Bundles] ([Id], [Name], [Description], [BundlePrice], [OriginalPrice], [Status], [OwnerType], [CreatedByAdminId], [LaundryId], [StartDate], [ExpiryDate], [ImageUrl], [DisplayOrder], [IsDeleted], [CreatedAt], [UpdatedAt])
        VALUES (
            @ShoesBundleId, 
            N'S Bundle for Shoes', 
            N'Wash 3 pairs of shoes and step out in style.', 
            435.00, 
            540.00, 
            2, -- Active
            0, -- Platform
            @SuperAdminId, 
            NULL, 
            NULL, 
            NULL, 
            NULL, 
            6, 
            0, 
            GETUTCDATE(), 
            NULL
        );

        IF @SpecialtyServiceId IS NOT NULL
        BEGIN
            INSERT INTO [BundleItems] ([Id], [BundleId], [ServiceId], [Quantity], [AllowedItemCategory], [CreatedAt], [UpdatedAt])
            VALUES (NEWID(), @ShoesBundleId, @SpecialtyServiceId, 3, 4, GETUTCDATE(), NULL);
        END
    END

    COMMIT TRANSACTION;
    PRINT 'Bundles seeded successfully!';
END
