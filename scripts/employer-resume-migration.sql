BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924090232_AddEmployerWorkspaceAndResumeRequests'
)
BEGIN
    ALTER TABLE [Jobs] ADD [AvailableFrom] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924090232_AddEmployerWorkspaceAndResumeRequests'
)
BEGIN
    CREATE TABLE [EmployerAccounts] (
        [Id] int NOT NULL IDENTITY,
        [CompanyName] nvarchar(200) NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        [NormalizedEmail] nvarchar(254) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [SecurityStamp] nvarchar(36) NOT NULL,
        [FailedLoginCount] int NOT NULL,
        [LockoutUntilUtc] datetime2 NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_EmployerAccounts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924090232_AddEmployerWorkspaceAndResumeRequests'
)
BEGIN
    CREATE TABLE [ResumeServiceRequests] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Email] nvarchar(254) NOT NULL,
        [Service] nvarchar(50) NOT NULL,
        [Notes] nvarchar(2000) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ResumeServiceRequests] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924090232_AddEmployerWorkspaceAndResumeRequests'
)
BEGIN
    CREATE TABLE [EmployerCampaigns] (
        [Id] int NOT NULL IDENTITY,
        [EmployerAccountId] int NOT NULL,
        [JobId] int NULL,
        [Title] nvarchar(200) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [Location] nvarchar(200) NOT NULL,
        [Qualification] nvarchar(200) NOT NULL,
        [Experience] nvarchar(200) NOT NULL,
        [Salary] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [ApplyLink] nvarchar(1000) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [ReviewStatus] nvarchar(30) NOT NULL,
        [ReviewNote] nvarchar(1000) NOT NULL,
        [UpgradeStatus] nvarchar(30) NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [UpdatedUtc] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EmployerCampaigns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployerCampaigns_EmployerAccounts_EmployerAccountId] FOREIGN KEY ([EmployerAccountId]) REFERENCES [EmployerAccounts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EmployerCampaigns_Jobs_JobId] FOREIGN KEY ([JobId]) REFERENCES [Jobs] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924090232_AddEmployerWorkspaceAndResumeRequests'
)
BEGIN
    CREATE TABLE [CampaignInvoices] (
        [Id] int NOT NULL IDENTITY,
        [EmployerCampaignId] int NOT NULL,
        [Number] nvarchar(80) NOT NULL,
        [SellerDetails] nvarchar(500) NOT NULL,
        [BillTo] nvarchar(500) NOT NULL,
        [Description] nvarchar(300) NOT NULL,
        [Amount] decimal(12,2) NOT NULL,
        [IssuedUtc] datetime2 NOT NULL,
        [PaidUtc] datetime2 NULL,
        CONSTRAINT [PK_CampaignInvoices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CampaignInvoices_EmployerCampaigns_EmployerCampaignId] FOREIGN KEY ([EmployerCampaignId]) REFERENCES [EmployerCampaigns] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924090232_AddEmployerWorkspaceAndResumeRequests'
)
BEGIN
    CREATE INDEX [IX_CampaignInvoices_EmployerCampaignId] ON [CampaignInvoices] ([EmployerCampaignId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924090232_AddEmployerWorkspaceAndResumeRequests'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CampaignInvoices_Number] ON [CampaignInvoices] ([Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924090232_AddEmployerWorkspaceAndResumeRequests'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmployerAccounts_NormalizedEmail] ON [EmployerAccounts] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924090232_AddEmployerWorkspaceAndResumeRequests'
)
BEGIN
    CREATE INDEX [IX_EmployerCampaigns_EmployerAccountId] ON [EmployerCampaigns] ([EmployerAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924090232_AddEmployerWorkspaceAndResumeRequests'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_EmployerCampaigns_JobId] ON [EmployerCampaigns] ([JobId]) WHERE [JobId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260924090232_AddEmployerWorkspaceAndResumeRequests'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260924090232_AddEmployerWorkspaceAndResumeRequests', N'10.0.12');
END;

COMMIT;
GO
