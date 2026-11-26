IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Role] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2 NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE TABLE [Laundries] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Address] nvarchar(250) NOT NULL,
        [Latitude] decimal(9,6) NOT NULL,
        [Longitude] decimal(9,6) NOT NULL,
        [Status] int NOT NULL,
        [Availability] int NOT NULL,
        [AverageRating] real NOT NULL DEFAULT CAST(0 AS real),
        [TotalReviews] int NOT NULL DEFAULT 0,
        [AdminId] nvarchar(450) NOT NULL,
        [ApplicationUserId] nvarchar(450) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Laundries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Laundries_Users_AdminId] FOREIGN KEY ([AdminId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Laundries_Users_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(100) NOT NULL,
        [Message] nvarchar(500) NOT NULL,
        [Type] int NOT NULL,
        [TargetRole] nvarchar(50) NULL,
        [IsRead] bit NOT NULL DEFAULT CAST(0 AS bit),
        [UserId] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] int NOT NULL IDENTITY,
        [PickupLocation] nvarchar(250) NOT NULL,
        [DeliveryLocation] nvarchar(250) NOT NULL,
        [PickupTime] datetime2 NOT NULL,
        [DeliveryTime] datetime2 NULL,
        [Status] int NOT NULL,
        [TotalPrice] decimal(10,2) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CancellationReason] nvarchar(500) NULL,
        [CustomerId] nvarchar(450) NOT NULL,
        [CourierId] nvarchar(450) NULL,
        [LaundryId] int NOT NULL,
        [ApplicationUserId] nvarchar(450) NULL,
        [ApplicationUserId1] nvarchar(450) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Orders_Laundries_LaundryId] FOREIGN KEY ([LaundryId]) REFERENCES [Laundries] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Orders_Users_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_Orders_Users_ApplicationUserId1] FOREIGN KEY ([ApplicationUserId1]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_Orders_Users_CourierId] FOREIGN KEY ([CourierId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Orders_Users_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE TABLE [Services] (
        [Id] int NOT NULL IDENTITY,
        [ServiceName] nvarchar(100) NOT NULL,
        [Category] int NOT NULL,
        [Price] decimal(10,2) NOT NULL,
        [IsAvailable] bit NOT NULL DEFAULT CAST(1 AS bit),
        [LaundryId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Services] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Services_Laundries_LaundryId] FOREIGN KEY ([LaundryId]) REFERENCES [Laundries] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] int NOT NULL IDENTITY,
        [Amount] decimal(10,2) NOT NULL,
        [PaymentDate] datetime2 NULL,
        [PaymentStatus] int NOT NULL,
        [PaymentMethod] int NOT NULL,
        [OrderId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE TABLE [Reviews] (
        [Id] int NOT NULL IDENTITY,
        [Rating] int NOT NULL,
        [Comment] nvarchar(1000) NULL,
        [CustomerId] nvarchar(450) NOT NULL,
        [LaundryId] int NOT NULL,
        [OrderId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reviews_Laundries_LaundryId] FOREIGN KEY ([LaundryId]) REFERENCES [Laundries] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Reviews_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reviews_Users_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] int NOT NULL IDENTITY,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(10,2) NOT NULL,
        [Subtotal] decimal(10,2) NOT NULL,
        [OrderId] int NOT NULL,
        [ServiceId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderItems_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_Laundries_AdminId] ON [Laundries] ([AdminId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_Laundries_ApplicationUserId] ON [Laundries] ([ApplicationUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_OrderItems_ServiceId] ON [OrderItems] ([ServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_Orders_ApplicationUserId] ON [Orders] ([ApplicationUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_Orders_ApplicationUserId1] ON [Orders] ([ApplicationUserId1]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_Orders_CourierId] ON [Orders] ([CourierId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerId] ON [Orders] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_Orders_LaundryId] ON [Orders] ([LaundryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Payments_OrderId] ON [Payments] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_Reviews_CustomerId] ON [Reviews] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_Reviews_LaundryId] ON [Reviews] ([LaundryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_Reviews_OrderId] ON [Reviews] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [IX_Services_LaundryId] ON [Services] ([LaundryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260312171317_InitialRemoteDb'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260312171317_InitialRemoteDb', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    ALTER TABLE [Laundries] ADD [ImageUrl] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    CREATE TABLE [NotificationPreference] (
        [Id] int NOT NULL IDENTITY,
        [WhatsappEnabled] bit NOT NULL,
        [SmsEnabled] bit NOT NULL,
        [EmailEnabled] bit NOT NULL,
        [PushEnabled] bit NOT NULL,
        [OrderUpdates] bit NOT NULL,
        [Promotions] bit NOT NULL,
        [Newsletter] bit NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_NotificationPreference] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NotificationPreference_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    CREATE TABLE [UserAddress] (
        [Id] int NOT NULL IDENTITY,
        [Type] int NOT NULL,
        [Label] nvarchar(100) NOT NULL,
        [Street] nvarchar(250) NOT NULL,
        [Apt] nvarchar(50) NULL,
        [City] nvarchar(100) NOT NULL,
        [Area] nvarchar(100) NOT NULL,
        [Instructions] nvarchar(500) NULL,
        [IsDefault] bit NOT NULL,
        [Latitude] decimal(9,6) NULL,
        [Longitude] decimal(9,6) NULL,
        [UserId] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_UserAddress] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserAddress_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    CREATE TABLE [UserFavorite] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [LaundryId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_UserFavorite] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserFavorite_Laundries_LaundryId] FOREIGN KEY ([LaundryId]) REFERENCES [Laundries] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserFavorite_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    CREATE TABLE [UserPaymentMethod] (
        [Id] int NOT NULL IDENTITY,
        [Type] int NOT NULL,
        [CardLast4] nvarchar(4) NOT NULL,
        [CardBrand] int NOT NULL,
        [ExpiryMonth] nvarchar(2) NOT NULL,
        [ExpiryYear] nvarchar(4) NOT NULL,
        [CardholderName] nvarchar(100) NOT NULL,
        [IsDefault] bit NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_UserPaymentMethod] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserPaymentMethod_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    CREATE TABLE [UserPoints] (
        [Id] int NOT NULL IDENTITY,
        [TotalPoints] int NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_UserPoints] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserPoints_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    CREATE TABLE [UserSettings] (
        [Id] int NOT NULL IDENTITY,
        [Language] int NOT NULL,
        [Currency] int NOT NULL,
        [PushNotifications] bit NOT NULL,
        [EmailNotifications] bit NOT NULL,
        [SmsNotifications] bit NOT NULL,
        [WhatsappNotifications] bit NOT NULL,
        [ShowProfile] bit NOT NULL,
        [ShareData] bit NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_UserSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserSettings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    CREATE TABLE [PointTransaction] (
        [Id] int NOT NULL IDENTITY,
        [Points] int NOT NULL,
        [Type] int NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [OrderId] int NULL,
        [UserPointsId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_PointTransaction] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PointTransaction_UserPoints_UserPointsId] FOREIGN KEY ([UserPointsId]) REFERENCES [UserPoints] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NotificationPreference_UserId] ON [NotificationPreference] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    CREATE INDEX [IX_PointTransaction_UserPointsId] ON [PointTransaction] ([UserPointsId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    CREATE INDEX [IX_UserAddress_UserId] ON [UserAddress] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    CREATE INDEX [IX_UserFavorite_LaundryId] ON [UserFavorite] ([LaundryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserFavorite_UserId_LaundryId] ON [UserFavorite] ([UserId], [LaundryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    CREATE INDEX [IX_UserPaymentMethod_UserId] ON [UserPaymentMethod] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserPoints_UserId] ON [UserPoints] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserSettings_UserId] ON [UserSettings] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313121222_usermodule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260313121222_usermodule', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313134838_usermodulesfinal'
)
BEGIN
    ALTER TABLE [Users] ADD [FirstName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313134838_usermodulesfinal'
)
BEGIN
    ALTER TABLE [Users] ADD [LastName] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313134838_usermodulesfinal'
)
BEGIN
    ALTER TABLE [Reviews] ADD [Tags] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260313134838_usermodulesfinal'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260313134838_usermodulesfinal', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [NotificationPreference] DROP CONSTRAINT [FK_NotificationPreference_Users_UserId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [PointTransaction] DROP CONSTRAINT [FK_PointTransaction_UserPoints_UserPointsId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [UserAddress] DROP CONSTRAINT [FK_UserAddress_Users_UserId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [UserFavorite] DROP CONSTRAINT [FK_UserFavorite_Laundries_LaundryId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [UserFavorite] DROP CONSTRAINT [FK_UserFavorite_Users_UserId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [UserPaymentMethod] DROP CONSTRAINT [FK_UserPaymentMethod_Users_UserId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [UserPaymentMethod] DROP CONSTRAINT [PK_UserPaymentMethod];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [UserFavorite] DROP CONSTRAINT [PK_UserFavorite];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [UserAddress] DROP CONSTRAINT [PK_UserAddress];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [PointTransaction] DROP CONSTRAINT [PK_PointTransaction];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [NotificationPreference] DROP CONSTRAINT [PK_NotificationPreference];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    EXEC sp_rename N'[UserPaymentMethod]', N'UserPaymentMethods';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    EXEC sp_rename N'[UserFavorite]', N'UserFavorites';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    EXEC sp_rename N'[UserAddress]', N'UserAddresses';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    EXEC sp_rename N'[PointTransaction]', N'PointTransactions';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    EXEC sp_rename N'[NotificationPreference]', N'NotificationPreferences';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    EXEC sp_rename N'[UserPaymentMethods].[IX_UserPaymentMethod_UserId]', N'IX_UserPaymentMethods_UserId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    EXEC sp_rename N'[UserFavorites].[IX_UserFavorite_UserId_LaundryId]', N'IX_UserFavorites_UserId_LaundryId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    EXEC sp_rename N'[UserFavorites].[IX_UserFavorite_LaundryId]', N'IX_UserFavorites_LaundryId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    EXEC sp_rename N'[UserAddresses].[IX_UserAddress_UserId]', N'IX_UserAddresses_UserId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    EXEC sp_rename N'[PointTransactions].[IX_PointTransaction_UserPointsId]', N'IX_PointTransactions_UserPointsId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    EXEC sp_rename N'[NotificationPreferences].[IX_NotificationPreference_UserId]', N'IX_NotificationPreferences_UserId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [Orders] ADD [CourierLatitude] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [Orders] ADD [CourierLongitude] float NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [Orders] ADD [DeliveredAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [Orders] ADD [EstimatedDeliveryTime] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [Orders] ADD [PickedUpAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [UserPaymentMethods] ADD CONSTRAINT [PK_UserPaymentMethods] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [UserFavorites] ADD CONSTRAINT [PK_UserFavorites] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [UserAddresses] ADD CONSTRAINT [PK_UserAddresses] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [PointTransactions] ADD CONSTRAINT [PK_PointTransactions] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [NotificationPreferences] ADD CONSTRAINT [PK_NotificationPreferences] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    CREATE TABLE [CourierProfiles] (
        [Id] int NOT NULL IDENTITY,
        [CourierId] nvarchar(450) NOT NULL,
        [CompletedOrdersCount] int NOT NULL DEFAULT 0,
        [CancellationCount] int NOT NULL DEFAULT 0,
        [CurrentLatitude] float NULL,
        [CurrentLongitude] float NULL,
        [LastLocationUpdate] datetime2 NULL,
        [IsAvailable] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_CourierProfiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CourierProfiles_Users_CourierId] FOREIGN KEY ([CourierId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    CREATE TABLE [CustomerWallets] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] nvarchar(450) NOT NULL,
        [Balance] decimal(18,2) NOT NULL DEFAULT 0.0,
        [TotalCharged] decimal(18,2) NOT NULL DEFAULT 0.0,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_CustomerWallets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerWallets_Users_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    CREATE TABLE [LaundryWallets] (
        [Id] int NOT NULL IDENTITY,
        [LaundryId] int NOT NULL,
        [TotalEarnings] decimal(18,2) NOT NULL DEFAULT 0.0,
        [AvailableBalance] decimal(18,2) NOT NULL DEFAULT 0.0,
        [PendingCommission] decimal(18,2) NOT NULL DEFAULT 0.0,
        [DebtCeiling] decimal(18,2) NOT NULL DEFAULT 200.0,
        [Status] int NOT NULL DEFAULT 1,
        [LastPayoutDate] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_LaundryWallets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LaundryWallets_Laundries_LaundryId] FOREIGN KEY ([LaundryId]) REFERENCES [Laundries] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    CREATE TABLE [WalletTransactions] (
        [Id] int NOT NULL IDENTITY,
        [LaundryWalletId] int NULL,
        [CustomerWalletId] int NULL,
        [OrderId] int NULL,
        [Amount] decimal(18,2) NOT NULL,
        [CommissionRate] decimal(5,2) NOT NULL,
        [CommissionAmount] decimal(18,2) NOT NULL,
        [Type] int NOT NULL,
        [Source] int NOT NULL,
        [PaymobTransactionId] nvarchar(100) NULL,
        [Status] int NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_WalletTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WalletTransactions_CustomerWallets_CustomerWalletId] FOREIGN KEY ([CustomerWalletId]) REFERENCES [CustomerWallets] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WalletTransactions_LaundryWallets_LaundryWalletId] FOREIGN KEY ([LaundryWalletId]) REFERENCES [LaundryWallets] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WalletTransactions_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CourierProfiles_CourierId] ON [CourierProfiles] ([CourierId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomerWallets_CustomerId] ON [CustomerWallets] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LaundryWallets_LaundryId] ON [LaundryWallets] ([LaundryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    CREATE INDEX [IX_WalletTransactions_CustomerWalletId] ON [WalletTransactions] ([CustomerWalletId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    CREATE INDEX [IX_WalletTransactions_LaundryWalletId] ON [WalletTransactions] ([LaundryWalletId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    CREATE INDEX [IX_WalletTransactions_OrderId] ON [WalletTransactions] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [NotificationPreferences] ADD CONSTRAINT [FK_NotificationPreferences_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [PointTransactions] ADD CONSTRAINT [FK_PointTransactions_UserPoints_UserPointsId] FOREIGN KEY ([UserPointsId]) REFERENCES [UserPoints] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [UserAddresses] ADD CONSTRAINT [FK_UserAddresses_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [UserFavorites] ADD CONSTRAINT [FK_UserFavorites_Laundries_LaundryId] FOREIGN KEY ([LaundryId]) REFERENCES [Laundries] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [UserFavorites] ADD CONSTRAINT [FK_UserFavorites_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    ALTER TABLE [UserPaymentMethods] ADD CONSTRAINT [FK_UserPaymentMethods_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260314211228_updateDatabase'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260314211228_updateDatabase', N'8.0.0');
END;
GO

COMMIT;
GO

