IF OBJECT_ID(N'[dynamicform].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'dynamicform') IS NULL EXEC(N'CREATE SCHEMA [dynamicform];');
    CREATE TABLE [dynamicform].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'dynamicform') IS NULL EXEC(N'CREATE SCHEMA [dynamicform];');
GO

IF SCHEMA_ID(N'DynamicForm') IS NULL EXEC(N'CREATE SCHEMA [DynamicForm];');
GO

CREATE TABLE [dynamicform].[DataSources] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(128) NOT NULL,
    [ServiceName] nvarchar(128) NOT NULL,
    [EndpointName] nvarchar(256) NOT NULL,
    [CacheTtlSeconds] int NOT NULL,
    [Enabled] bit NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(max) NOT NULL,
    [LastModifiedOn] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_DataSources] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [dynamicform].[Forms] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [Schema] nvarchar(max) NOT NULL,
    [Version] int NOT NULL DEFAULT 1,
    [IsPublished] bit NOT NULL DEFAULT CAST(0 AS bit),
    [TenantId] nvarchar(100) NOT NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [IsMultiStep] bit NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(100) NOT NULL,
    [LastModifiedOn] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Forms] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [dynamicform].[FormulaDefinitions] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [Expression] nvarchar(max) NOT NULL,
    [Version] int NOT NULL DEFAULT 1,
    [ReturnType] nvarchar(50) NOT NULL,
    [Parameters] nvarchar(max) NOT NULL,
    [Category] nvarchar(100) NOT NULL,
    [Tags] nvarchar(500) NOT NULL,
    [IsPublished] bit NOT NULL DEFAULT CAST(0 AS bit),
    [IsDeprecated] bit NOT NULL,
    [TenantId] nvarchar(100) NOT NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [ValidationRules] nvarchar(max) NOT NULL,
    [Examples] nvarchar(max) NOT NULL,
    [PerformanceMetrics] nvarchar(max) NOT NULL,
    [Dependencies] nvarchar(1000) NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(100) NOT NULL,
    [LastModifiedOn] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_FormulaDefinitions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [dynamicform].[OutboxMessages] (
    [Id] uniqueidentifier NOT NULL,
    [Type] nvarchar(255) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [OccurredOn] datetime2 NOT NULL,
    [ProcessedOn] datetime2 NULL,
    [Error] nvarchar(max) NULL,
    [RetryCount] int NOT NULL DEFAULT 0,
    [MaxRetries] int NOT NULL DEFAULT 3,
    [NextRetryAt] datetime2 NULL,
    [CorrelationId] uniqueidentifier NOT NULL,
    [CausationId] uniqueidentifier NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(max) NOT NULL,
    [LastModifiedOn] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [dynamicform].[Quotes] (
    [Id] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [Consumed] bit NOT NULL DEFAULT CAST(0 AS bit),
    [PricingJson] nvarchar(max) NOT NULL,
    [InputsSnapshotJson] nvarchar(max) NOT NULL,
    [ExternalDataSnapshotJson] nvarchar(max) NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(max) NOT NULL,
    [LastModifiedOn] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Quotes] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [dynamicform].[FormAccessPolicies] (
    [Id] uniqueidentifier NOT NULL,
    [FormId] uniqueidentifier NOT NULL,
    [TenantId] nvarchar(100) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [PolicyType] nvarchar(50) NOT NULL,
    [TargetId] nvarchar(100) NOT NULL,
    [TargetName] nvarchar(200) NOT NULL,
    [Permissions] nvarchar(500) NOT NULL,
    [Conditions] nvarchar(max) NOT NULL,
    [Priority] int NOT NULL DEFAULT 0,
    [IsEnabled] bit NOT NULL DEFAULT CAST(1 AS bit),
    [EffectiveFrom] datetime2 NULL,
    [EffectiveTo] datetime2 NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(100) NOT NULL,
    [LastModifiedOn] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_FormAccessPolicies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FormAccessPolicies_Forms_FormId] FOREIGN KEY ([FormId]) REFERENCES [dynamicform].[Forms] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dynamicform].[FormFields] (
    [Id] uniqueidentifier NOT NULL,
    [FormId] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Label] nvarchar(300) NOT NULL,
    [FieldType] nvarchar(50) NOT NULL,
    [IsRequired] bit NOT NULL DEFAULT CAST(0 AS bit),
    [DefaultValue] nvarchar(1000) NOT NULL,
    [Placeholder] nvarchar(200) NOT NULL,
    [HelpText] nvarchar(500) NOT NULL,
    [ValidationRules] nvarchar(max) NOT NULL,
    [Options] nvarchar(max) NOT NULL,
    [ConditionalLogic] nvarchar(max) NOT NULL,
    [Order] int NOT NULL DEFAULT 0,
    [CssClasses] nvarchar(200) NOT NULL,
    [Attributes] nvarchar(max) NOT NULL,
    [CalculationExpression] nvarchar(1000) NOT NULL,
    [IsCalculated] bit NOT NULL,
    [IsReadOnly] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedBy] nvarchar(100) NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(100) NOT NULL,
    [LastModifiedOn] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_FormFields] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FormFields_Forms_FormId] FOREIGN KEY ([FormId]) REFERENCES [dynamicform].[Forms] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dynamicform].[FormSteps] (
    [Id] uniqueidentifier NOT NULL,
    [FormId] uniqueidentifier NOT NULL,
    [StepNumber] int NOT NULL,
    [Title] nvarchar(300) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [StepSchema] nvarchar(max) NOT NULL,
    [ValidationRules] nvarchar(max) NOT NULL,
    [ConditionalLogic] nvarchar(max) NOT NULL,
    [IsRequired] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CanSkip] bit NOT NULL,
    [IsRepeatable] bit NOT NULL DEFAULT CAST(0 AS bit),
    [IsSkippable] bit NOT NULL DEFAULT CAST(0 AS bit),
    [StepType] nvarchar(50) NOT NULL DEFAULT N'Standard',
    [MinTimeSeconds] int NULL,
    [MaxTimeSeconds] int NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [DependsOnSteps] nvarchar(500) NOT NULL,
    [TenantId] nvarchar(100) NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(100) NOT NULL,
    [LastModifiedOn] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_FormSteps] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_FormSteps_MaxAttempts] CHECK ([MaxAttempts] > 0),
    CONSTRAINT [CK_FormSteps_StepNumber] CHECK ([StepNumber] > 0),
    CONSTRAINT [CK_FormSteps_TimeoutMinutes] CHECK ([TimeoutMinutes] IS NULL OR [TimeoutMinutes] > 0),
    CONSTRAINT [FK_FormSteps_Forms_FormId] FOREIGN KEY ([FormId]) REFERENCES [dynamicform].[Forms] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dynamicform].[FormSubmissions] (
    [Id] uniqueidentifier NOT NULL,
    [FormId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [TenantId] nvarchar(100) NOT NULL,
    [Data] nvarchar(max) NOT NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Draft',
    [ValidationErrors] nvarchar(max) NOT NULL,
    [IpAddress] nvarchar(45) NOT NULL,
    [UserAgent] nvarchar(500) NOT NULL,
    [Referrer] nvarchar(1000) NOT NULL,
    [SubmittedAt] datetime2 NOT NULL,
    [ApprovedAt] datetime2 NULL,
    [ApprovedBy] nvarchar(100) NOT NULL,
    [RejectedAt] datetime2 NULL,
    [RejectedBy] nvarchar(100) NOT NULL,
    [RejectionReason] nvarchar(1000) NOT NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [CurrentStepNumber] int NULL,
    [IsMultiStep] bit NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(100) NOT NULL,
    [LastModifiedOn] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_FormSubmissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FormSubmissions_Forms_FormId] FOREIGN KEY ([FormId]) REFERENCES [dynamicform].[Forms] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dynamicform].[FormVersions] (
    [Id] uniqueidentifier NOT NULL,
    [FormId] uniqueidentifier NOT NULL,
    [Version] int NOT NULL,
    [Schema] nvarchar(max) NOT NULL,
    [TenantId] nvarchar(100) NOT NULL,
    [IsPublished] bit NOT NULL DEFAULT CAST(0 AS bit),
    [IsCurrent] bit NOT NULL DEFAULT CAST(0 AS bit),
    [PublishedAt] datetime2 NULL,
    [PublishedBy] nvarchar(100) NOT NULL,
    [ChangeLog] nvarchar(max) NOT NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [RowVersion] rowversion NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(100) NOT NULL,
    [LastModifiedOn] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_FormVersions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FormVersions_Forms_FormId] FOREIGN KEY ([FormId]) REFERENCES [dynamicform].[Forms] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [DynamicForm].[FormulaVersions] (
    [Id] uniqueidentifier NOT NULL,
    [FormulaDefinitionId] uniqueidentifier NOT NULL,
    [VersionNumber] int NOT NULL,
    [Expression] nvarchar(max) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [ChangeLog] nvarchar(2000) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit),
    [IsPublished] bit NOT NULL DEFAULT CAST(0 AS bit),
    [PublishedAt] datetime2 NULL,
    [PublishedBy] uniqueidentifier NULL,
    [EffectiveFrom] datetime2 NULL,
    [EffectiveTo] datetime2 NULL,
    [ValidationRules] nvarchar(max) NULL,
    [Dependencies] nvarchar(2000) NULL,
    [Metadata] nvarchar(max) NULL,
    [ExecutionCount] int NOT NULL DEFAULT 0,
    [LastExecutedAt] datetime2 NULL,
    [AverageExecutionTime] float(18) NULL,
    [LastError] nvarchar(2000) NULL,
    [LastErrorAt] datetime2 NULL,
    [TenantId] nvarchar(256) NOT NULL,
    [CreatedBy] nvarchar(256) NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(max) NOT NULL,
    [LastModifiedOn] datetime2 NULL,
    CONSTRAINT [PK_FormulaVersions] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_FormulaVersions_ActiveConstraint] CHECK (([IsActive] = 0) OR ([IsActive] = 1 AND [IsPublished] = 1)),
    CONSTRAINT [CK_FormulaVersions_AverageExecutionTime] CHECK ([AverageExecutionTime] IS NULL OR [AverageExecutionTime] >= 0),
    CONSTRAINT [CK_FormulaVersions_EffectiveRange] CHECK (([EffectiveFrom] IS NULL) OR ([EffectiveTo] IS NULL) OR ([EffectiveFrom] <= [EffectiveTo])),
    CONSTRAINT [CK_FormulaVersions_ExecutionCount] CHECK ([ExecutionCount] >= 0),
    CONSTRAINT [CK_FormulaVersions_PublishedConstraint] CHECK (([IsPublished] = 0) OR ([IsPublished] = 1 AND [PublishedAt] IS NOT NULL AND [PublishedBy] IS NOT NULL)),
    CONSTRAINT [CK_FormulaVersions_VersionNumber] CHECK ([VersionNumber] > 0),
    CONSTRAINT [FK_FormulaVersions_FormulaDefinitions_FormulaDefinitionId] FOREIGN KEY ([FormulaDefinitionId]) REFERENCES [dynamicform].[FormulaDefinitions] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dynamicform].[ProductFormulaBindings] (
    [Id] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [FormulaDefinitionId] uniqueidentifier NOT NULL,
    [VersionNumber] int NOT NULL,
    [TenantId] nvarchar(100) NOT NULL,
    [FormulaDefinitionId1] uniqueidentifier NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(max) NOT NULL,
    [LastModifiedOn] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_ProductFormulaBindings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductFormulaBindings_FormulaDefinitions_FormulaDefinitionId] FOREIGN KEY ([FormulaDefinitionId]) REFERENCES [dynamicform].[FormulaDefinitions] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ProductFormulaBindings_FormulaDefinitions_FormulaDefinitionId1] FOREIGN KEY ([FormulaDefinitionId1]) REFERENCES [dynamicform].[FormulaDefinitions] ([Id])
);
GO

CREATE TABLE [dynamicform].[FormAuditLogs] (
    [Id] uniqueidentifier NOT NULL,
    [FormId] uniqueidentifier NULL,
    [FormSubmissionId] uniqueidentifier NULL,
    [TenantId] nvarchar(100) NOT NULL,
    [Action] nvarchar(50) NOT NULL,
    [EntityType] nvarchar(100) NOT NULL,
    [EntityId] uniqueidentifier NOT NULL,
    [UserId] nvarchar(100) NOT NULL,
    [UserName] nvarchar(200) NOT NULL,
    [IpAddress] nvarchar(45) NOT NULL,
    [UserAgent] nvarchar(500) NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    [OldValues] nvarchar(max) NOT NULL,
    [NewValues] nvarchar(max) NOT NULL,
    [Changes] nvarchar(max) NOT NULL,
    [Details] nvarchar(max) NOT NULL,
    [Reason] nvarchar(500) NOT NULL,
    [SessionId] nvarchar(100) NOT NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [Severity] nvarchar(20) NOT NULL DEFAULT N'Information',
    [Category] nvarchar(50) NOT NULL DEFAULT N'General',
    [Metadata] nvarchar(max) NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(100) NOT NULL,
    [LastModifiedOn] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_FormAuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FormAuditLogs_FormSubmissions_FormSubmissionId] FOREIGN KEY ([FormSubmissionId]) REFERENCES [dynamicform].[FormSubmissions] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_FormAuditLogs_Forms_FormId] FOREIGN KEY ([FormId]) REFERENCES [dynamicform].[Forms] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [dynamicform].[FormStepSubmissions] (
    [Id] uniqueidentifier NOT NULL,
    [FormSubmissionId] uniqueidentifier NOT NULL,
    [FormStepId] uniqueidentifier NOT NULL,
    [StepNumber] int NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [TenantId] nvarchar(100) NOT NULL,
    [StepData] nvarchar(max) NOT NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'NotStarted',
    [ValidationErrors] nvarchar(max) NOT NULL,
    [StartedAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [TimeSpentSeconds] int NOT NULL DEFAULT 0,
    [IsSkipped] bit NOT NULL,
    [SkipReason] nvarchar(500) NOT NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(100) NOT NULL,
    [LastModifiedOn] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_FormStepSubmissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FormStepSubmissions_FormSteps_FormStepId] FOREIGN KEY ([FormStepId]) REFERENCES [dynamicform].[FormSteps] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FormStepSubmissions_FormSubmissions_FormSubmissionId] FOREIGN KEY ([FormSubmissionId]) REFERENCES [dynamicform].[FormSubmissions] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [dynamicform].[FormulaEvaluationLogs] (
    [Id] uniqueidentifier NOT NULL,
    [FormulaDefinitionId] uniqueidentifier NOT NULL,
    [FormulaVersionId] uniqueidentifier NULL,
    [ContextId] uniqueidentifier NULL,
    [ContextType] nvarchar(100) NOT NULL,
    [InputParameters] nvarchar(max) NOT NULL,
    [Result] nvarchar(max) NOT NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Pending',
    [ErrorMessage] nvarchar(1000) NOT NULL,
    [ErrorDetails] nvarchar(max) NOT NULL,
    [ExecutionTimeMs] bigint NOT NULL DEFAULT CAST(0 AS bigint),
    [MemoryUsageBytes] bigint NOT NULL DEFAULT CAST(0 AS bigint),
    [StartedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [TenantId] nvarchar(100) NOT NULL,
    [UserId] uniqueidentifier NULL,
    [SessionId] nvarchar(100) NOT NULL,
    [EvaluationMode] nvarchar(50) NOT NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [IpAddress] nvarchar(45) NOT NULL,
    [UserAgent] nvarchar(500) NOT NULL,
    [FormId] uniqueidentifier NULL,
    [FormSubmissionId] uniqueidentifier NULL,
    [EvaluationContext] nvarchar(100) NOT NULL,
    [CorrelationId] uniqueidentifier NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [CreatedOn] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(100) NOT NULL,
    [LastModifiedOn] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_FormulaEvaluationLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FormulaEvaluationLogs_FormSubmissions_FormSubmissionId] FOREIGN KEY ([FormSubmissionId]) REFERENCES [dynamicform].[FormSubmissions] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_FormulaEvaluationLogs_Forms_FormId] FOREIGN KEY ([FormId]) REFERENCES [dynamicform].[Forms] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_FormulaEvaluationLogs_FormulaDefinitions_FormulaDefinitionId] FOREIGN KEY ([FormulaDefinitionId]) REFERENCES [dynamicform].[FormulaDefinitions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_FormulaEvaluationLogs_FormulaVersions_FormulaVersionId] FOREIGN KEY ([FormulaVersionId]) REFERENCES [DynamicForm].[FormulaVersions] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_DataSource_Service_Endpoint] ON [dynamicform].[DataSources] ([ServiceName], [EndpointName]);
GO

CREATE INDEX [IX_FormAccessPolicies_EffectiveFrom] ON [dynamicform].[FormAccessPolicies] ([EffectiveFrom]);
GO

CREATE INDEX [IX_FormAccessPolicies_EffectiveTo] ON [dynamicform].[FormAccessPolicies] ([EffectiveTo]);
GO

CREATE INDEX [IX_FormAccessPolicies_FormId] ON [dynamicform].[FormAccessPolicies] ([FormId]);
GO

CREATE INDEX [IX_FormAccessPolicies_FormId_PolicyType_TargetId] ON [dynamicform].[FormAccessPolicies] ([FormId], [PolicyType], [TargetId]);
GO

CREATE INDEX [IX_FormAccessPolicies_IsEnabled] ON [dynamicform].[FormAccessPolicies] ([IsEnabled]);
GO

CREATE INDEX [IX_FormAccessPolicies_PolicyType] ON [dynamicform].[FormAccessPolicies] ([PolicyType]);
GO

CREATE INDEX [IX_FormAccessPolicies_Priority] ON [dynamicform].[FormAccessPolicies] ([Priority]);
GO

CREATE INDEX [IX_FormAccessPolicies_TargetId] ON [dynamicform].[FormAccessPolicies] ([TargetId]);
GO

CREATE INDEX [IX_FormAccessPolicies_TenantId] ON [dynamicform].[FormAccessPolicies] ([TenantId]);
GO

CREATE INDEX [IX_FormAccessPolicies_TenantId_FormId_Priority] ON [dynamicform].[FormAccessPolicies] ([TenantId], [FormId], [Priority]);
GO

CREATE INDEX [IX_FormAuditLogs_Action] ON [dynamicform].[FormAuditLogs] ([Action]);
GO

CREATE INDEX [IX_FormAuditLogs_Category] ON [dynamicform].[FormAuditLogs] ([Category]);
GO

CREATE INDEX [IX_FormAuditLogs_CorrelationId] ON [dynamicform].[FormAuditLogs] ([CorrelationId]);
GO

CREATE INDEX [IX_FormAuditLogs_EntityId] ON [dynamicform].[FormAuditLogs] ([EntityId]);
GO

CREATE INDEX [IX_FormAuditLogs_EntityType] ON [dynamicform].[FormAuditLogs] ([EntityType]);
GO

CREATE INDEX [IX_FormAuditLogs_EntityType_EntityId_Timestamp] ON [dynamicform].[FormAuditLogs] ([EntityType], [EntityId], [Timestamp]);
GO

CREATE INDEX [IX_FormAuditLogs_FormId] ON [dynamicform].[FormAuditLogs] ([FormId]);
GO

CREATE INDEX [IX_FormAuditLogs_FormSubmissionId] ON [dynamicform].[FormAuditLogs] ([FormSubmissionId]);
GO

CREATE INDEX [IX_FormAuditLogs_SessionId] ON [dynamicform].[FormAuditLogs] ([SessionId]);
GO

CREATE INDEX [IX_FormAuditLogs_Severity] ON [dynamicform].[FormAuditLogs] ([Severity]);
GO

CREATE INDEX [IX_FormAuditLogs_TenantId] ON [dynamicform].[FormAuditLogs] ([TenantId]);
GO

CREATE INDEX [IX_FormAuditLogs_TenantId_Timestamp] ON [dynamicform].[FormAuditLogs] ([TenantId], [Timestamp]);
GO

CREATE INDEX [IX_FormAuditLogs_Timestamp] ON [dynamicform].[FormAuditLogs] ([Timestamp]);
GO

CREATE INDEX [IX_FormAuditLogs_UserId] ON [dynamicform].[FormAuditLogs] ([UserId]);
GO

CREATE INDEX [IX_FormAuditLogs_UserId_Action_Timestamp] ON [dynamicform].[FormAuditLogs] ([UserId], [Action], [Timestamp]);
GO

CREATE INDEX [IX_FormFields_FieldType] ON [dynamicform].[FormFields] ([FieldType]);
GO

CREATE INDEX [IX_FormFields_FormId] ON [dynamicform].[FormFields] ([FormId]);
GO

CREATE UNIQUE INDEX [IX_FormFields_FormId_Name_Unique] ON [dynamicform].[FormFields] ([FormId], [Name]);
GO

CREATE INDEX [IX_FormFields_FormId_Order] ON [dynamicform].[FormFields] ([FormId], [Order]);
GO

CREATE INDEX [IX_FormFields_Name] ON [dynamicform].[FormFields] ([Name]);
GO

CREATE INDEX [IX_FormFields_Order] ON [dynamicform].[FormFields] ([Order]);
GO

CREATE INDEX [IX_Forms_CreatedOn] ON [dynamicform].[Forms] ([CreatedOn]);
GO

CREATE INDEX [IX_Forms_IsPublished] ON [dynamicform].[Forms] ([IsPublished]);
GO

CREATE INDEX [IX_Forms_Name] ON [dynamicform].[Forms] ([Name]);
GO

CREATE INDEX [IX_Forms_TenantId] ON [dynamicform].[Forms] ([TenantId]);
GO

CREATE UNIQUE INDEX [IX_Forms_TenantId_Name_Unique] ON [dynamicform].[Forms] ([TenantId], [Name]);
GO

CREATE INDEX [IX_FormSteps_CreatedOn] ON [dynamicform].[FormSteps] ([CreatedOn]);
GO

CREATE INDEX [IX_FormSteps_FormId] ON [dynamicform].[FormSteps] ([FormId]);
GO

CREATE INDEX [IX_FormSteps_FormId_IsActive_StepNumber] ON [dynamicform].[FormSteps] ([FormId], [IsActive], [StepNumber]);
GO

CREATE UNIQUE INDEX [IX_FormSteps_FormId_StepNumber_Unique] ON [dynamicform].[FormSteps] ([FormId], [StepNumber]);
GO

CREATE INDEX [IX_FormSteps_IsRequired] ON [dynamicform].[FormSteps] ([IsRequired]);
GO

CREATE INDEX [IX_FormSteps_IsSkippable] ON [dynamicform].[FormSteps] ([IsSkippable]);
GO

CREATE INDEX [IX_FormSteps_StepNumber] ON [dynamicform].[FormSteps] ([StepNumber]);
GO

CREATE INDEX [IX_FormSteps_StepType] ON [dynamicform].[FormSteps] ([StepType]);
GO

CREATE INDEX [IX_FormSteps_TenantId] ON [dynamicform].[FormSteps] ([TenantId]);
GO

CREATE INDEX [IX_FormSteps_TenantId_FormId_IsActive] ON [dynamicform].[FormSteps] ([TenantId], [FormId], [IsActive]);
GO

CREATE INDEX [IX_FormSteps_TenantId_StepType_IsActive] ON [dynamicform].[FormSteps] ([TenantId], [StepType], [IsActive]);
GO

CREATE INDEX [IX_FormStepSubmissions_CompletedAt] ON [dynamicform].[FormStepSubmissions] ([CompletedAt]);
GO

CREATE INDEX [IX_FormStepSubmissions_CreatedOn] ON [dynamicform].[FormStepSubmissions] ([CreatedOn]);
GO

CREATE INDEX [IX_FormStepSubmissions_FormStepId] ON [dynamicform].[FormStepSubmissions] ([FormStepId]);
GO

CREATE INDEX [IX_FormStepSubmissions_FormSubmissionId] ON [dynamicform].[FormStepSubmissions] ([FormSubmissionId]);
GO

CREATE INDEX [IX_FormStepSubmissions_FormSubmissionId_Status_StepNumber] ON [dynamicform].[FormStepSubmissions] ([FormSubmissionId], [Status], [StepNumber]);
GO

CREATE UNIQUE INDEX [IX_FormStepSubmissions_FormSubmissionId_StepNumber_Unique] ON [dynamicform].[FormStepSubmissions] ([FormSubmissionId], [StepNumber]);
GO

CREATE INDEX [IX_FormStepSubmissions_StartedAt] ON [dynamicform].[FormStepSubmissions] ([StartedAt]);
GO

CREATE INDEX [IX_FormStepSubmissions_Status] ON [dynamicform].[FormStepSubmissions] ([Status]);
GO

CREATE INDEX [IX_FormStepSubmissions_StepNumber] ON [dynamicform].[FormStepSubmissions] ([StepNumber]);
GO

CREATE INDEX [IX_FormStepSubmissions_TenantId] ON [dynamicform].[FormStepSubmissions] ([TenantId]);
GO

CREATE INDEX [IX_FormStepSubmissions_TenantId_FormSubmissionId_Status] ON [dynamicform].[FormStepSubmissions] ([TenantId], [FormSubmissionId], [Status]);
GO

CREATE INDEX [IX_FormStepSubmissions_TenantId_UserId_Status] ON [dynamicform].[FormStepSubmissions] ([TenantId], [UserId], [Status]);
GO

CREATE INDEX [IX_FormStepSubmissions_UserId] ON [dynamicform].[FormStepSubmissions] ([UserId]);
GO

CREATE INDEX [IX_FormSubmissions_FormId] ON [dynamicform].[FormSubmissions] ([FormId]);
GO

CREATE INDEX [IX_FormSubmissions_Status] ON [dynamicform].[FormSubmissions] ([Status]);
GO

CREATE INDEX [IX_FormSubmissions_SubmittedAt] ON [dynamicform].[FormSubmissions] ([SubmittedAt]);
GO

CREATE INDEX [IX_FormSubmissions_TenantId] ON [dynamicform].[FormSubmissions] ([TenantId]);
GO

CREATE INDEX [IX_FormSubmissions_TenantId_FormId_SubmittedAt] ON [dynamicform].[FormSubmissions] ([TenantId], [FormId], [SubmittedAt]);
GO

CREATE INDEX [IX_FormulaDefinitions_Category] ON [dynamicform].[FormulaDefinitions] ([Category]);
GO

CREATE INDEX [IX_FormulaDefinitions_IsPublished] ON [dynamicform].[FormulaDefinitions] ([IsPublished]);
GO

CREATE INDEX [IX_FormulaDefinitions_Name] ON [dynamicform].[FormulaDefinitions] ([Name]);
GO

CREATE INDEX [IX_FormulaDefinitions_ReturnType] ON [dynamicform].[FormulaDefinitions] ([ReturnType]);
GO

CREATE INDEX [IX_FormulaDefinitions_TenantId] ON [dynamicform].[FormulaDefinitions] ([TenantId]);
GO

CREATE INDEX [IX_FormulaDefinitions_TenantId_Category_IsPublished] ON [dynamicform].[FormulaDefinitions] ([TenantId], [Category], [IsPublished]);
GO

CREATE UNIQUE INDEX [IX_FormulaDefinitions_TenantId_Name_Unique] ON [dynamicform].[FormulaDefinitions] ([TenantId], [Name]);
GO

CREATE INDEX [IX_FormulaDefinitions_Version] ON [dynamicform].[FormulaDefinitions] ([Version]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_CompletedAt] ON [dynamicform].[FormulaEvaluationLogs] ([CompletedAt]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_CorrelationId] ON [dynamicform].[FormulaEvaluationLogs] ([CorrelationId]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_EvaluationContext] ON [dynamicform].[FormulaEvaluationLogs] ([EvaluationContext]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_ExecutionTimeMs] ON [dynamicform].[FormulaEvaluationLogs] ([ExecutionTimeMs]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_FormId] ON [dynamicform].[FormulaEvaluationLogs] ([FormId]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_FormSubmissionId] ON [dynamicform].[FormulaEvaluationLogs] ([FormSubmissionId]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_FormulaDefinitionId] ON [dynamicform].[FormulaEvaluationLogs] ([FormulaDefinitionId]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_FormulaDefinitionId_Status_StartedAt] ON [dynamicform].[FormulaEvaluationLogs] ([FormulaDefinitionId], [Status], [StartedAt]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_FormulaVersionId] ON [dynamicform].[FormulaEvaluationLogs] ([FormulaVersionId]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_SessionId] ON [dynamicform].[FormulaEvaluationLogs] ([SessionId]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_StartedAt] ON [dynamicform].[FormulaEvaluationLogs] ([StartedAt]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_Status] ON [dynamicform].[FormulaEvaluationLogs] ([Status]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_TenantId] ON [dynamicform].[FormulaEvaluationLogs] ([TenantId]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_TenantId_StartedAt] ON [dynamicform].[FormulaEvaluationLogs] ([TenantId], [StartedAt]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_UserId] ON [dynamicform].[FormulaEvaluationLogs] ([UserId]);
GO

CREATE INDEX [IX_FormulaEvaluationLogs_UserId_StartedAt] ON [dynamicform].[FormulaEvaluationLogs] ([UserId], [StartedAt]);
GO

CREATE INDEX [IX_FormulaVersions_FormulaDefinitionId] ON [DynamicForm].[FormulaVersions] ([FormulaDefinitionId]);
GO

CREATE INDEX [IX_FormulaVersions_FormulaDefinitionId_IsActive_VersionNumber] ON [DynamicForm].[FormulaVersions] ([FormulaDefinitionId], [IsActive], [VersionNumber]);
GO

CREATE UNIQUE INDEX [IX_FormulaVersions_FormulaDefinitionId_VersionNumber] ON [DynamicForm].[FormulaVersions] ([FormulaDefinitionId], [VersionNumber]);
GO

CREATE INDEX [IX_FormulaVersions_IsActive] ON [DynamicForm].[FormulaVersions] ([IsActive]) WHERE [IsActive] = 1;
GO

CREATE INDEX [IX_FormulaVersions_IsPublished] ON [DynamicForm].[FormulaVersions] ([IsPublished]) WHERE [IsPublished] = 1;
GO

CREATE INDEX [IX_FormulaVersions_LastExecutedAt] ON [DynamicForm].[FormulaVersions] ([LastExecutedAt]);
GO

CREATE INDEX [IX_FormulaVersions_Published_EffectiveWindow] ON [DynamicForm].[FormulaVersions] ([FormulaDefinitionId], [IsPublished], [EffectiveFrom], [EffectiveTo]);
GO

CREATE INDEX [IX_FormulaVersions_PublishedAt] ON [DynamicForm].[FormulaVersions] ([PublishedAt]);
GO

CREATE INDEX [IX_FormulaVersions_TenantId] ON [DynamicForm].[FormulaVersions] ([TenantId]);
GO

CREATE INDEX [IX_FormulaVersions_TenantId_FormulaDefinitionId_IsActive] ON [DynamicForm].[FormulaVersions] ([TenantId], [FormulaDefinitionId], [IsActive]);
GO

CREATE INDEX [IX_FormulaVersions_TenantId_IsPublished_PublishedAt] ON [DynamicForm].[FormulaVersions] ([TenantId], [IsPublished], [PublishedAt]);
GO

CREATE INDEX [IX_FormVersions_FormId] ON [dynamicform].[FormVersions] ([FormId]);
GO

CREATE INDEX [IX_FormVersions_FormId_IsCurrent] ON [dynamicform].[FormVersions] ([FormId], [IsCurrent]) WHERE [IsCurrent] = 1;
GO

CREATE UNIQUE INDEX [IX_FormVersions_FormId_Version_Unique] ON [dynamicform].[FormVersions] ([FormId], [Version]);
GO

CREATE INDEX [IX_FormVersions_IsCurrent] ON [dynamicform].[FormVersions] ([IsCurrent]);
GO

CREATE INDEX [IX_FormVersions_IsPublished] ON [dynamicform].[FormVersions] ([IsPublished]);
GO

CREATE INDEX [IX_FormVersions_PublishedAt] ON [dynamicform].[FormVersions] ([PublishedAt]);
GO

CREATE INDEX [IX_FormVersions_TenantId] ON [dynamicform].[FormVersions] ([TenantId]);
GO

CREATE INDEX [IX_FormVersions_Version] ON [dynamicform].[FormVersions] ([Version]);
GO

CREATE INDEX [IX_OutboxMessages_NextRetryAt] ON [dynamicform].[OutboxMessages] ([NextRetryAt]);
GO

CREATE INDEX [IX_OutboxMessages_OccurredOn] ON [dynamicform].[OutboxMessages] ([OccurredOn]);
GO

CREATE INDEX [IX_OutboxMessages_ProcessedOn] ON [dynamicform].[OutboxMessages] ([ProcessedOn]);
GO

CREATE INDEX [IX_OutboxMessages_ProcessedOn_NextRetryAt] ON [dynamicform].[OutboxMessages] ([ProcessedOn], [NextRetryAt]) WHERE [ProcessedOn] IS NULL;
GO

CREATE INDEX [IX_OutboxMessages_TenantId_OccurredOn] ON [dynamicform].[OutboxMessages] ([TenantId], [OccurredOn]);
GO

CREATE INDEX [IX_OutboxMessages_Type] ON [dynamicform].[OutboxMessages] ([Type]);
GO

CREATE INDEX [IX_ProductFormulaBindings_FormulaDefinitionId] ON [dynamicform].[ProductFormulaBindings] ([FormulaDefinitionId]);
GO

CREATE INDEX [IX_ProductFormulaBindings_FormulaDefinitionId1] ON [dynamicform].[ProductFormulaBindings] ([FormulaDefinitionId1]);
GO

CREATE UNIQUE INDEX [UX_ProductFormulaBinding_ProductId_Version] ON [dynamicform].[ProductFormulaBindings] ([ProductId], [VersionNumber]);
GO

CREATE INDEX [IX_Quote_Consumed] ON [dynamicform].[Quotes] ([Consumed]);
GO

CREATE INDEX [IX_Quote_ExpiresAt] ON [dynamicform].[Quotes] ([ExpiresAt]);
GO

CREATE INDEX [IX_Quote_Product_Consumed_Expires] ON [dynamicform].[Quotes] ([ProductId], [Consumed], [ExpiresAt]);
GO

CREATE INDEX [IX_Quote_ProductId] ON [dynamicform].[Quotes] ([ProductId]);
GO

INSERT INTO [dynamicform].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251007084438_AddQuoteIndexes', N'8.0.8');
GO

COMMIT;
GO

