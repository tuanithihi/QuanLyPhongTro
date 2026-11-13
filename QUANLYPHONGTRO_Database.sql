

-- ── 1. TẠO DATABASE NẾU CHƯA TỒN TẠI ──────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'QUANLYPHONGTRO')
BEGIN
    CREATE DATABASE [QUANLYPHONGTRO];
END;
GO

USE [QUANLYPHONGTRO];
GO

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
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE TABLE [tblMenu] (
        [MenuId] int NOT NULL IDENTITY,
        [MenuName] nvarchar(100) NOT NULL,
        [Url] nvarchar(500) NULL,
        [Icon] nvarchar(50) NULL,
        [ParentMenuId] int NULL,
        [SortOrder] int NOT NULL,
        [Position] nvarchar(30) NULL,
        [OpenNewTab] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_tblMenu] PRIMARY KEY ([MenuId]),
        CONSTRAINT [FK_tblMenu_tblMenu_ParentMenuId] FOREIGN KEY ([ParentMenuId]) REFERENCES [tblMenu] ([MenuId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE TABLE [tblPost] (
        [PostId] int NOT NULL IDENTITY,
        [Title] nvarchar(300) NOT NULL,
        [Slug] nvarchar(350) NOT NULL,
        [Summary] nvarchar(500) NULL,
        [Content] nvarchar(max) NULL,
        [ThumbnailImage] nvarchar(300) NULL,
        [Category] nvarchar(100) NULL,
        [IsPinned] bit NOT NULL,
        [IsPublished] bit NOT NULL,
        [PublishedAt] datetime2 NULL,
        [ViewCount] int NOT NULL,
        [MetaTitle] nvarchar(300) NULL,
        [MetaDescription] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_tblPost] PRIMARY KEY ([PostId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE TABLE [tblReview] (
        [ReviewId] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Content] nvarchar(2000) NOT NULL,
        [Rating] int NOT NULL DEFAULT 5,
        [IsApproved] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_tblReview] PRIMARY KEY ([ReviewId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE TABLE [tblRoomType] (
        [RoomTypeId] int NOT NULL IDENTITY,
        [RoomTypeName] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_tblRoomType] PRIMARY KEY ([RoomTypeId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE TABLE [tblService] (
        [ServiceId] int NOT NULL IDENTITY,
        [ServiceName] nvarchar(100) NOT NULL,
        [ServiceType] int NOT NULL,
        [PricingMethod] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [Unit] nvarchar(20) NULL,
        [Description] nvarchar(200) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_tblService] PRIMARY KEY ([ServiceId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE TABLE [tblTenant] (
        [TenantId] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [IdentityNumber] nvarchar(20) NOT NULL,
        [Phone] nvarchar(15) NULL,
        [Email] nvarchar(200) NULL,
        [DateOfBirth] date NULL,
        [Gender] nvarchar(10) NULL,
        [PermanentAddress] nvarchar(500) NULL,
        [IdentityFrontImage] nvarchar(200) NULL,
        [IdentityBackImage] nvarchar(200) NULL,
        [Avatar] nvarchar(200) NULL,
        [Username] nvarchar(50) NULL,
        [PasswordHash] nvarchar(256) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_tblTenant] PRIMARY KEY ([TenantId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE TABLE [tblUser] (
        [UserId] int NOT NULL IDENTITY,
        [Username] nvarchar(50) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [PasswordHash] nvarchar(256) NOT NULL,
        [FullName] nvarchar(100) NULL,
        [Phone] nvarchar(15) NULL,
        [Avatar] nvarchar(200) NULL,
        [Role] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_tblUser] PRIMARY KEY ([UserId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE TABLE [tblRoom] (
        [RoomId] int NOT NULL IDENTITY,
        [RoomCode] nvarchar(20) NOT NULL,
        [RoomName] nvarchar(150) NOT NULL,
        [RoomTypeId] int NOT NULL,
        [RoomPrice] decimal(18,2) NOT NULL,
        [DefaultDeposit] decimal(18,2) NOT NULL,
        [Area] float NOT NULL,
        [Floor] int NOT NULL,
        [MaxOccupants] int NOT NULL,
        [Description] nvarchar(max) NULL,
        [ThumbnailImage] nvarchar(300) NULL,
        [Address] nvarchar(255) NULL,
        [Latitude] float NULL,
        [Longitude] float NULL,
        [Status] int NOT NULL,
        [IsPublished] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_tblRoom] PRIMARY KEY ([RoomId]),
        CONSTRAINT [FK_tblRoom_tblRoomType_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [tblRoomType] ([RoomTypeId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE TABLE [tblChatSession] (
        [SessionId] int NOT NULL IDENTITY,
        [SessionKey] nvarchar(36) NOT NULL,
        [GuestName] nvarchar(100) NOT NULL,
        [GuestPhone] nvarchar(20) NOT NULL,
        [IsOpen] bit NOT NULL,
        [LastMsgAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [TenantId] int NULL,
        [UserId] int NULL,
        CONSTRAINT [PK_tblChatSession] PRIMARY KEY ([SessionId]),
        CONSTRAINT [FK_tblChatSession_tblTenant_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [tblTenant] ([TenantId]) ON DELETE SET NULL,
        CONSTRAINT [FK_tblChatSession_tblUser_UserId] FOREIGN KEY ([UserId]) REFERENCES [tblUser] ([UserId]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE TABLE [tblBookingRequest] (
        [RequestId] int NOT NULL IDENTITY,
        [RoomId] int NOT NULL,
        [FullName] nvarchar(100) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [Email] nvarchar(100) NULL,
        [PreferredDate] nvarchar(50) NULL,
        [Message] nvarchar(1000) NULL,
        [RequestType] int NOT NULL,
        [Status] int NOT NULL,
        [AdminNote] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_tblBookingRequest] PRIMARY KEY ([RequestId]),
        CONSTRAINT [FK_tblBookingRequest_tblRoom_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [tblRoom] ([RoomId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE TABLE [tblContract] (
        [ContractId] int NOT NULL IDENTITY,
        [ContractCode] nvarchar(30) NOT NULL,
        [RoomId] int NOT NULL,
        [TenantId] int NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [MonthlyRent] decimal(18,2) NOT NULL,
        [Deposit] decimal(18,2) NOT NULL,
        [PaymentDayOfMonth] int NOT NULL,
        [InitialElectricIndex] float NOT NULL,
        [InitialWaterIndex] float NOT NULL,
        [Terms] nvarchar(max) NULL,
        [Notes] nvarchar(500) NULL,
        [Status] int NOT NULL,
        [ActualEndDate] datetime2 NULL,
        [TerminationReason] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_tblContract] PRIMARY KEY ([ContractId]),
        CONSTRAINT [FK_tblContract_tblRoom_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [tblRoom] ([RoomId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tblContract_tblTenant_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [tblTenant] ([TenantId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE TABLE [tblRoomReview] (
        [RoomReviewId] int NOT NULL IDENTITY,
        [RoomId] int NOT NULL,
        [FullName] nvarchar(100) NOT NULL,
        [Rating] int NOT NULL DEFAULT 5,
        [Comment] nvarchar(1000) NULL,
        [IsApproved] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_tblRoomReview] PRIMARY KEY ([RoomReviewId]),
        CONSTRAINT [FK_tblRoomReview_tblRoom_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [tblRoom] ([RoomId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE TABLE [tblChatMessage] (
        [MessageId] int NOT NULL IDENTITY,
        [SessionId] int NOT NULL,
        [Content] nvarchar(2000) NOT NULL,
        [SenderType] int NOT NULL,
        [IsReadByAdmin] bit NOT NULL,
        [IsReadByGuest] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_tblChatMessage] PRIMARY KEY ([MessageId]),
        CONSTRAINT [FK_tblChatMessage_tblChatSession_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [tblChatSession] ([SessionId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE TABLE [tblInvoice] (
        [InvoiceId] int NOT NULL IDENTITY,
        [InvoiceCode] nvarchar(30) NOT NULL,
        [RoomId] int NOT NULL,
        [ContractId] int NOT NULL,
        [BillingMonth] int NOT NULL,
        [BillingYear] int NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [ElectricIndexStart] float NOT NULL,
        [ElectricIndexEnd] float NOT NULL,
        [WaterIndexStart] float NOT NULL,
        [WaterIndexEnd] float NOT NULL,
        [RoomRentAmount] decimal(18,2) NOT NULL,
        [TotalServiceAmount] decimal(18,2) NOT NULL,
        [Discount] decimal(18,2) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [PaidDate] datetime2 NULL,
        [PaymentMethod] nvarchar(200) NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_tblInvoice] PRIMARY KEY ([InvoiceId]),
        CONSTRAINT [FK_tblInvoice_tblContract_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [tblContract] ([ContractId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tblInvoice_tblRoom_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [tblRoom] ([RoomId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE TABLE [tblInvoiceDetail] (
        [InvoiceDetailId] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [ServiceId] int NULL,
        [Description] nvarchar(200) NULL,
        [Quantity] float NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_tblInvoiceDetail] PRIMARY KEY ([InvoiceDetailId]),
        CONSTRAINT [FK_tblInvoiceDetail_tblInvoice_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [tblInvoice] ([InvoiceId]) ON DELETE CASCADE,
        CONSTRAINT [FK_tblInvoiceDetail_tblService_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [tblService] ([ServiceId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE INDEX [IX_tblBookingRequest_RoomId] ON [tblBookingRequest] ([RoomId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE INDEX [IX_tblChatMessage_SessionId] ON [tblChatMessage] ([SessionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tblChatSession_SessionKey] ON [tblChatSession] ([SessionKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE INDEX [IX_tblChatSession_TenantId] ON [tblChatSession] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE INDEX [IX_tblChatSession_UserId] ON [tblChatSession] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tblContract_ContractCode] ON [tblContract] ([ContractCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE INDEX [IX_tblContract_RoomId] ON [tblContract] ([RoomId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE INDEX [IX_tblContract_TenantId] ON [tblContract] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE INDEX [IX_tblInvoice_ContractId] ON [tblInvoice] ([ContractId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tblInvoice_InvoiceCode] ON [tblInvoice] ([InvoiceCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE INDEX [IX_tblInvoice_RoomId] ON [tblInvoice] ([RoomId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE INDEX [IX_tblInvoiceDetail_InvoiceId] ON [tblInvoiceDetail] ([InvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE INDEX [IX_tblInvoiceDetail_ServiceId] ON [tblInvoiceDetail] ([ServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE INDEX [IX_tblMenu_ParentMenuId] ON [tblMenu] ([ParentMenuId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tblPost_Slug] ON [tblPost] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tblRoom_RoomCode] ON [tblRoom] ([RoomCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE INDEX [IX_tblRoom_RoomTypeId] ON [tblRoom] ([RoomTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE INDEX [IX_tblRoomReview_RoomId] ON [tblRoomReview] ([RoomId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tblRoomType_RoomTypeName] ON [tblRoomType] ([RoomTypeName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tblTenant_IdentityNumber] ON [tblTenant] ([IdentityNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_tblTenant_Username] ON [tblTenant] ([Username]) WHERE [Username] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tblUser_Email] ON [tblUser] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tblUser_Username] ON [tblUser] ([Username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260512144628_AddRoomReview'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260512144628_AddRoomReview', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520101203_AddBookingIsGuestNotified'
)
BEGIN
    ALTER TABLE [tblBookingRequest] ADD [IsGuestNotified] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520101203_AddBookingIsGuestNotified'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260520101203_AddBookingIsGuestNotified', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520111752_AddRoomGalleryAndAmenities'
)
BEGIN

    IF COL_LENGTH('dbo.tblRoomReview', 'TenantId') IS NULL
        ALTER TABLE dbo.tblRoomReview ADD TenantId int NULL;

    IF COL_LENGTH('dbo.tblRoomReview', 'UserId') IS NULL
        ALTER TABLE dbo.tblRoomReview ADD UserId int NULL;

    IF COL_LENGTH('dbo.tblRoom', 'GalleryImages') IS NULL
        ALTER TABLE dbo.tblRoom ADD GalleryImages nvarchar(max) NULL;

    IF COL_LENGTH('dbo.tblRoom', 'IncludedAmenities') IS NULL
        ALTER TABLE dbo.tblRoom ADD IncludedAmenities nvarchar(max) NULL;

END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260520111752_AddRoomGalleryAndAmenities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260520111752_AddRoomGalleryAndAmenities', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblTenant] ADD [LandlordId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblService] ADD [LandlordId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblRoom] ADD [ApprovalStatus] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblRoom] ADD [Capacity] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblRoom] ADD [IsFeatured] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblRoom] ADD [PropertyId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblRoom] ADD [PublishedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblRoom] ADD [Slug] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblRoom] ADD [Title] nvarchar(250) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblRoom] ADD [ViewCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblReview] ADD [ContractId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblInvoice] ADD [LandlordId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblContract] ADD [LandlordId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblChatSession] ADD [LandlordId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblBookingRequest] ADD [LandlordId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblBookingRequest] ADD [ViewingTime] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE TABLE [tblAmenity] (
        [AmenityId] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Icon] nvarchar(100) NULL,
        [Description] nvarchar(255) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_tblAmenity] PRIMARY KEY ([AmenityId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE TABLE [tblFavorite] (
        [FavoriteId] int NOT NULL IDENTITY,
        [RoomId] int NOT NULL,
        [UserId] int NULL,
        [TenantId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_tblFavorite] PRIMARY KEY ([FavoriteId]),
        CONSTRAINT [FK_tblFavorite_tblRoom_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [tblRoom] ([RoomId]) ON DELETE CASCADE,
        CONSTRAINT [FK_tblFavorite_tblTenant_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [tblTenant] ([TenantId]) ON DELETE SET NULL,
        CONSTRAINT [FK_tblFavorite_tblUser_UserId] FOREIGN KEY ([UserId]) REFERENCES [tblUser] ([UserId]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE TABLE [tblLandlord] (
        [LandlordId] int NOT NULL IDENTITY,
        [UserId] int NULL,
        [FullName] nvarchar(100) NOT NULL,
        [Phone] nvarchar(20) NULL,
        [Email] nvarchar(150) NULL,
        [IdentityNumber] nvarchar(20) NULL,
        [Avatar] nvarchar(255) NULL,
        [Address] nvarchar(255) NULL,
        [Description] nvarchar(max) NULL,
        [BankId] nvarchar(50) NULL,
        [AccountNumber] nvarchar(50) NULL,
        [AccountName] nvarchar(100) NULL,
        [BankName] nvarchar(100) NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_tblLandlord] PRIMARY KEY ([LandlordId]),
        CONSTRAINT [FK_tblLandlord_tblUser_UserId] FOREIGN KEY ([UserId]) REFERENCES [tblUser] ([UserId]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE TABLE [tblProvince] (
        [ProvinceId] int NOT NULL IDENTITY,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        CONSTRAINT [PK_tblProvince] PRIMARY KEY ([ProvinceId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE TABLE [tblRoomImage] (
        [ImageId] int NOT NULL IDENTITY,
        [RoomId] int NOT NULL,
        [Url] nvarchar(500) NOT NULL,
        [IsPrimary] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_tblRoomImage] PRIMARY KEY ([ImageId]),
        CONSTRAINT [FK_tblRoomImage_tblRoom_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [tblRoom] ([RoomId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE TABLE [tblRoomAmenity] (
        [RoomId] int NOT NULL,
        [AmenityId] int NOT NULL,
        CONSTRAINT [PK_tblRoomAmenity] PRIMARY KEY ([RoomId], [AmenityId]),
        CONSTRAINT [FK_tblRoomAmenity_tblAmenity_AmenityId] FOREIGN KEY ([AmenityId]) REFERENCES [tblAmenity] ([AmenityId]) ON DELETE CASCADE,
        CONSTRAINT [FK_tblRoomAmenity_tblRoom_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [tblRoom] ([RoomId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE TABLE [tblDistrict] (
        [DistrictId] int NOT NULL IDENTITY,
        [ProvinceId] int NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        CONSTRAINT [PK_tblDistrict] PRIMARY KEY ([DistrictId]),
        CONSTRAINT [FK_tblDistrict_tblProvince_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [tblProvince] ([ProvinceId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE TABLE [tblWard] (
        [WardId] int NOT NULL IDENTITY,
        [DistrictId] int NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        CONSTRAINT [PK_tblWard] PRIMARY KEY ([WardId]),
        CONSTRAINT [FK_tblWard_tblDistrict_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [tblDistrict] ([DistrictId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE TABLE [tblProperty] (
        [PropertyId] int NOT NULL IDENTITY,
        [LandlordId] int NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Slug] nvarchar(150) NOT NULL,
        [Address] nvarchar(255) NOT NULL,
        [ProvinceId] int NULL,
        [DistrictId] int NULL,
        [WardId] int NULL,
        [Latitude] float NULL,
        [Longitude] float NULL,
        [Description] nvarchar(max) NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_tblProperty] PRIMARY KEY ([PropertyId]),
        CONSTRAINT [FK_tblProperty_tblDistrict_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [tblDistrict] ([DistrictId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tblProperty_tblLandlord_LandlordId] FOREIGN KEY ([LandlordId]) REFERENCES [tblLandlord] ([LandlordId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tblProperty_tblProvince_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [tblProvince] ([ProvinceId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_tblProperty_tblWard_WardId] FOREIGN KEY ([WardId]) REFERENCES [tblWard] ([WardId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblTenant_LandlordId] ON [tblTenant] ([LandlordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblService_LandlordId] ON [tblService] ([LandlordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblRoom_ApprovalStatus] ON [tblRoom] ([ApprovalStatus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblRoom_PropertyId] ON [tblRoom] ([PropertyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblRoom_RoomPrice] ON [tblRoom] ([RoomPrice]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    UPDATE [tblRoom] SET [Slug] = CONCAT('phong-', [RoomId]) WHERE [Slug] = '' OR [Slug] IS NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    UPDATE [tblRoom] SET [Title] = [RoomName] WHERE [Title] IS NULL OR [Title] = '';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tblRoom_Slug] ON [tblRoom] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblReview_ContractId] ON [tblReview] ([ContractId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblInvoice_LandlordId] ON [tblInvoice] ([LandlordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblContract_LandlordId] ON [tblContract] ([LandlordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblChatSession_LandlordId] ON [tblChatSession] ([LandlordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblBookingRequest_LandlordId] ON [tblBookingRequest] ([LandlordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tblAmenity_Name] ON [tblAmenity] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblDistrict_Code] ON [tblDistrict] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblDistrict_ProvinceId] ON [tblDistrict] ([ProvinceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblFavorite_RoomId] ON [tblFavorite] ([RoomId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblFavorite_TenantId] ON [tblFavorite] ([TenantId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblFavorite_UserId] ON [tblFavorite] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblLandlord_UserId] ON [tblLandlord] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblProperty_DistrictId] ON [tblProperty] ([DistrictId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblProperty_LandlordId] ON [tblProperty] ([LandlordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblProperty_ProvinceId] ON [tblProperty] ([ProvinceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tblProperty_Slug] ON [tblProperty] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblProperty_WardId] ON [tblProperty] ([WardId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_tblProvince_Code] ON [tblProvince] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblRoomAmenity_AmenityId] ON [tblRoomAmenity] ([AmenityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblRoomImage_RoomId] ON [tblRoomImage] ([RoomId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblWard_Code] ON [tblWard] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    CREATE INDEX [IX_tblWard_DistrictId] ON [tblWard] ([DistrictId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblBookingRequest] ADD CONSTRAINT [FK_tblBookingRequest_tblLandlord_LandlordId] FOREIGN KEY ([LandlordId]) REFERENCES [tblLandlord] ([LandlordId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblChatSession] ADD CONSTRAINT [FK_tblChatSession_tblLandlord_LandlordId] FOREIGN KEY ([LandlordId]) REFERENCES [tblLandlord] ([LandlordId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblContract] ADD CONSTRAINT [FK_tblContract_tblLandlord_LandlordId] FOREIGN KEY ([LandlordId]) REFERENCES [tblLandlord] ([LandlordId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblInvoice] ADD CONSTRAINT [FK_tblInvoice_tblLandlord_LandlordId] FOREIGN KEY ([LandlordId]) REFERENCES [tblLandlord] ([LandlordId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblReview] ADD CONSTRAINT [FK_tblReview_tblContract_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [tblContract] ([ContractId]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblRoom] ADD CONSTRAINT [FK_tblRoom_tblProperty_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [tblProperty] ([PropertyId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblService] ADD CONSTRAINT [FK_tblService_tblLandlord_LandlordId] FOREIGN KEY ([LandlordId]) REFERENCES [tblLandlord] ([LandlordId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    ALTER TABLE [tblTenant] ADD CONSTRAINT [FK_tblTenant_tblLandlord_LandlordId] FOREIGN KEY ([LandlordId]) REFERENCES [tblLandlord] ([LandlordId]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004125155_AddMultiLandlordModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261004125155_AddMultiLandlordModel', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005020337_AddRejectReasonAndIndexes'
)
BEGIN
    ALTER TABLE [tblRoom] ADD [RejectReason] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005020337_AddRejectReasonAndIndexes'
)
BEGIN
    ALTER TABLE [tblLandlord] ADD [RejectReason] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005020337_AddRejectReasonAndIndexes'
)
BEGIN
    CREATE INDEX [IX_tblRoom_CreatedAt] ON [tblRoom] ([CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005020337_AddRejectReasonAndIndexes'
)
BEGIN
    CREATE INDEX [IX_tblRoom_IsFeatured] ON [tblRoom] ([IsFeatured]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005020337_AddRejectReasonAndIndexes'
)
BEGIN
    CREATE INDEX [IX_tblRoom_IsPublished_ApprovalStatus_Status] ON [tblRoom] ([IsPublished], [ApprovalStatus], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005020337_AddRejectReasonAndIndexes'
)
BEGIN
    CREATE INDEX [IX_tblLandlord_Status] ON [tblLandlord] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005020337_AddRejectReasonAndIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261005020337_AddRejectReasonAndIndexes', N'8.0.10');
END;
GO

COMMIT;
GO



-- ═══════════════════════════════════════════════════════════════════════════════
-- ── 2. DỮ LIỆU KHỞI TẠO CƠ BẢN (INITIAL SEED DATA) ───────────────────────────
-- ═══════════════════════════════════════════════════════════════════════════════

-- A. Khởi tạo Loại phòng trọ tiêu chuẩn (tblRoomType)
IF NOT EXISTS (SELECT 1 FROM [tblRoomType] WHERE [RoomTypeName] = N'Phòng trọ khép kín')
BEGIN
    INSERT INTO [tblRoomType] ([RoomTypeName], [Description], [SortOrder], [IsActive], [CreatedAt])
    VALUES 
    (N'Phòng trọ khép kín', N'Phòng trọ có WC riêng biệt, giờ giấc tự do, an ninh tốt', 1, 1, GETDATE()),
    (N'Chung cư mini', N'Căn hộ mini / chung cư mini 1 phòng ngủ hoặc studio, đầy đủ nội thất', 2, 1, GETDATE()),
    (N'Căn hộ mini', N'Căn hộ mini gia đình hoặc studio đầy đủ tiện nghi', 3, 1, GETDATE()),
    (N'Nhà nguyên căn', N'Nhà nguyên căn cho thuê độc lập, nhiều tầng, thích hợp hộ gia đình hoặc nhóm bạn', 4, 1, GETDATE()),
    (N'Homestay / Căn hộ dịch vụ', N'Không gian sống cao cấp, có dịch vụ dọn dẹp buồng phòng', 5, 1, GETDATE()),
    (N'Ký túc xá cao cấp', N'Giường tầng riêng tư, đầy đủ máy lạnh tủ đồ', 6, 1, GETDATE());
END;
GO

-- B. Khởi tạo Danh mục Tiện ích phòng trọ (tblAmenity)
IF NOT EXISTS (SELECT 1 FROM [tblAmenity] WHERE [Name] = N'Điều hòa')
BEGIN
    INSERT INTO [tblAmenity] ([Name], [Icon], [Description], [IsActive])
    VALUES 
    (N'Điều hòa', N'bi bi-snow', N'Máy điều hòa nhiệt độ hai chiều / một chiều tiết kiệm điện', 1),
    (N'Nóng lạnh', N'bi bi-thermometer-high', N'Bình nóng lạnh an toàn', 1),
    (N'Gác lửng', N'bi bi-layers', N'Gác lửng thông thoáng kiên cố', 1),
    (N'Chỗ để xe', N'bi bi-p-circle', N'Nhà xe an ninh rộng rãi có camera giám sát', 1),
    (N'Wifi', N'bi bi-wifi', N'Internet cáp quang tốc độ cao', 1),
    (N'Giờ giấc tự do', N'bi bi-clock-history', N'Không chung chủ, ra vào khóa vân tay / thẻ từ tự do', 1),
    (N'Cho nuôi thú cưng', N'bi bi-heart', N'Cho phép nuôi chó mèo với điều kiện giữ vệ sinh', 1),
    (N'Tủ lạnh', N'bi bi-box', N'Tủ lạnh có sẵn trong phòng', 1),
    (N'Máy giặt', N'bi bi-arrow-repeat', N'Máy giặt dùng riêng hoặc khu giặt chung tiện lợi', 1),
    (N'Ban công', N'bi bi-sun', N'Ban công thoáng mát đón gió và ánh sáng tự nhiên', 1);
END;
GO

-- C. Khởi tạo Menu điều hướng website (tblMenu)
IF NOT EXISTS (SELECT 1 FROM [tblMenu] WHERE [MenuName] = N'Trang chủ')
BEGIN
    INSERT INTO [tblMenu] ([MenuName], [Url], [Icon], [SortOrder], [Position], [OpenNewTab], [IsActive], [CreatedAt])
    VALUES 
    (N'Trang chủ', N'/', N'bi bi-house-door', 1, N'Header', 0, 1, GETDATE()),
    (N'Thuê phòng', N'/phong-tro', N'bi bi-building', 2, N'Header', 0, 1, GETDATE()),
    (N'Chủ trọ uy tín', N'/danh-sach-chu-tro', N'bi bi-patch-check', 3, N'Header', 0, 1, GETDATE()),
    (N'Kinh nghiệm thuê', N'/Blog', N'bi bi-journal-text', 4, N'Header', 0, 1, GETDATE()),
    (N'Liên hệ & Hỗ trợ', N'/lien-he', N'bi bi-headset', 5, N'Header', 0, 1, GETDATE());
END;
GO

-- D. Khởi tạo Tài khoản Quản trị viên SuperAdmin mặc định (tblUser)
-- Username: admin / Mật khẩu: Admin@123
IF NOT EXISTS (SELECT 1 FROM [tblUser] WHERE [Username] = N'admin')
BEGIN
    INSERT INTO [tblUser] ([Username], [PasswordHash], [FullName], [Email], [Phone], [Role], [IsActive], [CreatedAt])
    VALUES (N'admin', N'3de051a5bbeeb3200b7be938c977bbea0296bee7a8437bf2a6ef1ea00bdbb15b', N'Ban Quản Trị Z-Stay', N'admin@zstay.vn', N'19008888', N'SuperAdmin', 1, GETDATE());
END;
GO

-- E. Khởi tạo Đối tác Chủ trọ mẫu (tblUser & tblLandlord)
-- Username: chutro_hanoi / Mật khẩu: Chutro@123
IF NOT EXISTS (SELECT 1 FROM [tblUser] WHERE [Username] = N'chutro_hanoi')
BEGIN
    INSERT INTO [tblUser] ([Username], [PasswordHash], [FullName], [Email], [Phone], [Role], [IsActive], [CreatedAt])
    VALUES (N'chutro_hanoi', N'f1e0a18f9027a3513973aa86c8495fc90b694f068132e93eb9ab80f7f0d09d83', N'Nguyễn Văn An', N'an.nguyen@phongtrohn.vn', N'0912345678', N'Landlord', 1, GETDATE());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [tblLandlord] WHERE [Phone] = N'0912345678')
BEGIN
    INSERT INTO [tblLandlord] ([FullName], [IdentityNumber], [Phone], [Address], [BankId], [AccountNumber], [AccountName], [BankName], [Status], [IsIdentityVerified], [AverageRating], [TotalReviews], [CreatedAt])
    VALUES (N'Nguyễn Văn An', N'001200112233', N'0912345678', N'Quận Cầu Giấy, Hà Nội', N'970422', N'091234567899', N'NGUYEN VAN AN', N'MB Bank', 1, 1, 4.9, 15, GETDATE());
END;
GO

-- Username: chutro_danang / Mật khẩu: Chutro@123
IF NOT EXISTS (SELECT 1 FROM [tblUser] WHERE [Username] = N'chutro_danang')
BEGIN
    INSERT INTO [tblUser] ([Username], [PasswordHash], [FullName], [Email], [Phone], [Role], [IsActive], [CreatedAt])
    VALUES (N'chutro_danang', N'f1e0a18f9027a3513973aa86c8495fc90b694f068132e93eb9ab80f7f0d09d83', N'Trần Thị Mai', N'mai.tran@phongtrodn.vn', N'0934567890', N'Landlord', 1, GETDATE());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [tblLandlord] WHERE [Phone] = N'0934567890')
BEGIN
    INSERT INTO [tblLandlord] ([FullName], [IdentityNumber], [Phone], [Address], [BankId], [AccountNumber], [AccountName], [BankName], [Status], [IsIdentityVerified], [AverageRating], [TotalReviews], [CreatedAt])
    VALUES (N'Trần Thị Mai', N'048201223344', N'0934567890', N'Quận Hải Châu, Đà Nẵng', N'970415', N'1028736452', N'TRAN THI MAI', N'VietinBank', 1, 1, 5.0, 22, GETDATE());
END;
GO

-- Username: chutro_hcm / Mật khẩu: Chutro@123
IF NOT EXISTS (SELECT 1 FROM [tblUser] WHERE [Username] = N'chutro_hcm')
BEGIN
    INSERT INTO [tblUser] ([Username], [PasswordHash], [FullName], [Email], [Phone], [Role], [IsActive], [CreatedAt])
    VALUES (N'chutro_hcm', N'f1e0a18f9027a3513973aa86c8495fc90b694f068132e93eb9ab80f7f0d09d83', N'Lê Hoàng Long', N'long.le@phongtrohcm.vn', N'0978123456', N'Landlord', 1, GETDATE());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [tblLandlord] WHERE [Phone] = N'0978123456')
BEGIN
    INSERT INTO [tblLandlord] ([FullName], [IdentityNumber], [Phone], [Address], [BankId], [AccountNumber], [AccountName], [BankName], [Status], [IsIdentityVerified], [AverageRating], [TotalReviews], [CreatedAt])
    VALUES (N'Lê Hoàng Long', N'079202334455', N'0978123456', N'Quận Bình Thạnh, TP. Hồ Chí Minh', N'970407', N'19036728192019', N'LE HOANG LONG', N'Techcombank', 1, 1, 4.8, 18, GETDATE());
END;
GO

-- F. Khởi tạo Khách thuê mẫu (tblUser & tblTenant)
-- Username: khachthue_01 / Mật khẩu: Khach@123
IF NOT EXISTS (SELECT 1 FROM [tblUser] WHERE [Username] = N'khachthue_01')
BEGIN
    INSERT INTO [tblUser] ([Username], [PasswordHash], [FullName], [Email], [Phone], [Role], [IsActive], [CreatedAt])
    VALUES (N'khachthue_01', N'2610c0bbae9b2c5e0f7a4c0614620b452e1439887577c33c2853bb4e89dd865b', N'Hoàng Minh Tuấn', N'khach01@gmail.com', N'0988776655', N'User', 1, GETDATE());
END;
GO

PRINT N'═══════════════════════════════════════════════════════════════════════════════';
PRINT N' KHỞI TẠO CƠ SỞ DỮ LIỆU [QUANLYPHONGTRO] THÀNH CÔNG VÀ SẴN SÀNG SỬ DỤNG!';
PRINT N'═══════════════════════════════════════════════════════════════════════════════';
PRINT N' Tài khoản quản trị SuperAdmin : admin       / Mật khẩu: Admin@123';
PRINT N' Tài khoản đối tác Chủ trọ mẫu  : chutro_hanoi / Mật khẩu: Chutro@123';
PRINT N' Tài khoản khách thuê mẫu       : khachthue_01 / Mật khẩu: Khach@123';
PRINT N'═══════════════════════════════════════════════════════════════════════════════';
GO
