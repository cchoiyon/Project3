-- Team Project 3 Database Tables
-- All tables are prefixed with TP_ as required

CREATE TABLE [dbo].[TP_Photos] (
    [PhotoID] INT IDENTITY (1, 1) NOT NULL,
    [RestaurantID] INT NOT NULL,
    [PhotoURL] NVARCHAR (MAX) NOT NULL,
    [Caption] NVARCHAR (500) NULL,
    [UploadedDate] DATETIME2 (7) CONSTRAINT [DF_TP_Photos_UploadedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_TP_Photos] PRIMARY KEY CLUSTERED ([PhotoID] ASC),
    CONSTRAINT [FK_TP_Photos_Restaurants] FOREIGN KEY ([RestaurantID]) REFERENCES [dbo].[TP_Restaurants] ([RestaurantID]) ON DELETE CASCADE
);

CREATE TABLE [dbo].[TP_Reservations] (
    [ReservationID] INT IDENTITY (1, 1) NOT NULL,
    [RestaurantID] INT NOT NULL,
    [UserID] INT NULL,
    [ContactName] NVARCHAR (100) NULL,
    [Phone] NVARCHAR (20) NULL,
    [Email] NVARCHAR (100) NULL,
    [ReservationDateTime] DATETIME2 (7) NOT NULL,
    [PartySize] INT NOT NULL,
    [SpecialRequests] NVARCHAR (MAX) NULL,
    [Status] NVARCHAR (50) DEFAULT ('Pending') NOT NULL,
    [CreatedDate] DATETIME2 (7) DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_TP_Reservations] PRIMARY KEY CLUSTERED ([ReservationID] ASC),
    CONSTRAINT [FK_TP_Reservations_Restaurants] FOREIGN KEY ([RestaurantID]) REFERENCES [dbo].[TP_Restaurants] ([RestaurantID]) ON DELETE CASCADE,
    CONSTRAINT [FK_TP_Reservations_Users] FOREIGN KEY ([UserID]) REFERENCES [dbo].[TP_Users] ([UserID])
);

CREATE TABLE [dbo].[TP_Restaurants] (
    [RestaurantID] INT NOT NULL,
    [Name] NVARCHAR (200) NOT NULL,
    [Address] NVARCHAR (255) NULL,
    [City] NVARCHAR (100) NULL,
    [State] NVARCHAR (50) NULL,
    [ZipCode] VARCHAR (10) NULL,
    [Cuisine] NVARCHAR (100) NULL,
    [Hours] NVARCHAR (255) NULL,
    [Contact] NVARCHAR (100) NULL,
    [ProfilePhoto] NVARCHAR (MAX) NULL,
    [LogoPhoto] NVARCHAR (MAX) NULL,
    [MarketingDescription] NVARCHAR (MAX) NULL,
    [WebsiteURL] NVARCHAR (MAX) NULL,
    [SocialMedia] NVARCHAR (MAX) NULL,
    [Owner] NVARCHAR (100) NULL,
    [CreatedDate] DATETIME2 (7) CONSTRAINT [DF_TP_Restaurants_CreatedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_TP_Restaurants] PRIMARY KEY CLUSTERED ([RestaurantID] ASC),
    CONSTRAINT [FK_TP_Restaurants_Users] FOREIGN KEY ([RestaurantID]) REFERENCES [dbo].[TP_Users] ([UserID]) ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE TABLE [dbo].[TP_Reviews] (
    [ReviewID] INT IDENTITY (1, 1) NOT NULL,
    [RestaurantID] INT NOT NULL,
    [UserID] INT NOT NULL,
    [VisitDate] DATETIME2 (7) NOT NULL,
    [Comments] NVARCHAR (MAX) NULL,
    [FoodQualityRating] INT NOT NULL,
    [ServiceRating] INT NOT NULL,
    [AtmosphereRating] INT NOT NULL,
    [PriceRating] INT NOT NULL,
    [CreatedDate] DATETIME2 (7) CONSTRAINT [DF_TP_Reviews_CreatedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_TP_Reviews] PRIMARY KEY CLUSTERED ([ReviewID] ASC),
    CONSTRAINT [FK_TP_Reviews_Restaurants] FOREIGN KEY ([RestaurantID]) REFERENCES [dbo].[TP_Restaurants] ([RestaurantID]) ON DELETE CASCADE,
    CONSTRAINT [FK_TP_Reviews_Users] FOREIGN KEY ([UserID]) REFERENCES [dbo].[TP_Users] ([UserID]),
    CONSTRAINT [CK_TP_Reviews_FoodQualityRating] CHECK ([FoodQualityRating]>=(1) AND [FoodQualityRating]<=(5)),
    CONSTRAINT [CK_TP_Reviews_ServiceRating] CHECK ([ServiceRating]>=(1) AND [ServiceRating]<=(5)),
    CONSTRAINT [CK_TP_Reviews_AtmosphereRating] CHECK ([AtmosphereRating]>=(1) AND [AtmosphereRating]<=(5)),
    CONSTRAINT [CK_TP_Reviews_PriceRating] CHECK ([PriceRating]>=(1) AND [PriceRating]<=(5))
);

CREATE TABLE [dbo].[TP_Users] (
    [UserID] INT IDENTITY (1, 1) NOT NULL,
    [Username] NVARCHAR (100) NOT NULL,
    [Email] NVARCHAR (255) NOT NULL,
    [PasswordHash] NVARCHAR (MAX) NOT NULL,
    [UserType] VARCHAR (50) NOT NULL,
    [CreatedDate] DATETIME2 (7) CONSTRAINT [DF_TP_Users_CreatedDate] DEFAULT (getdate()) NOT NULL,
    [SecurityQuestion1] NVARCHAR (255) NULL,
    [SecurityAnswerHash1] NVARCHAR (MAX) NULL,
    [SecurityQuestion2] NVARCHAR (255) NULL,
    [SecurityAnswerHash2] NVARCHAR (MAX) NULL,
    [SecurityQuestion3] NVARCHAR (255) NULL,
    [SecurityAnswerHash3] NVARCHAR (MAX) NULL,
    [IsVerified] BIT CONSTRAINT [DF_TP_Users_IsVerified] DEFAULT ((0)) NOT NULL,
    [VerificationToken] NVARCHAR (100) NULL,
    [PasswordResetToken] NVARCHAR (100) NULL,
    [ResetTokenExpiry] DATETIME2 (7) NULL,
    [VerificationTokenExpiry] DATETIME2 (7) NULL,
    CONSTRAINT [PK_TP_Users] PRIMARY KEY CLUSTERED ([UserID] ASC),
    CONSTRAINT [UQ_TP_Users_Email] UNIQUE NONCLUSTERED ([Email] ASC),
    CONSTRAINT [UQ_TP_Users_Username] UNIQUE NONCLUSTERED ([Username] ASC)
); 