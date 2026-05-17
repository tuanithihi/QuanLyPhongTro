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

