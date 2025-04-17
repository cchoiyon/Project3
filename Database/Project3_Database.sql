-- Project3 Database Script
-- This script contains all the necessary database objects for Project3
-- Including tables and stored procedures

USE []
GO

-- =============================================
-- Drop existing stored procedures
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'TP_spGetRestaurantPhotos')
    DROP PROCEDURE [dbo].[TP_spGetRestaurantPhotos]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'TP_spGetRestaurantReviews')
    DROP PROCEDURE [dbo].[TP_spGetRestaurantReviews]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'TP_spGetRestaurantDetails')
    DROP PROCEDURE [dbo].[TP_spGetRestaurantDetails]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'TP_spAddRestaurantPhoto')
    DROP PROCEDURE [dbo].[TP_spAddRestaurantPhoto]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'TP_spAddReview')
    DROP PROCEDURE [dbo].[TP_spAddReview]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'TP_spAddUser')
    DROP PROCEDURE [dbo].[TP_spAddUser]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'TP_spCheckUser')
    DROP PROCEDURE [dbo].[TP_spCheckUser]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'TP_spCheckUsernameExists')
    DROP PROCEDURE [dbo].[TP_spCheckUsernameExists]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'TP_spDeleteRestaurantPhoto')
    DROP PROCEDURE [dbo].[TP_spDeleteRestaurantPhoto]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'TP_spDeleteReview')
    DROP PROCEDURE [dbo].[TP_spDeleteReview]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'TP_spGetFeaturedRestaurants')
    DROP PROCEDURE [dbo].[TP_spGetFeaturedRestaurants]
GO

-- =============================================
-- Drop existing tables if they exist (in correct order due to dependencies)
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TP_Photos]') AND type in (N'U'))
    DROP TABLE [dbo].[TP_Photos]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TP_Reviews]') AND type in (N'U'))
    DROP TABLE [dbo].[TP_Reviews]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TP_Reservations]') AND type in (N'U'))
    DROP TABLE [dbo].[TP_Reservations]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TP_Restaurants]') AND type in (N'U'))
    DROP TABLE [dbo].[TP_Restaurants]
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TP_Users]') AND type in (N'U'))
    DROP TABLE [dbo].[TP_Users]
GO

-- =============================================
-- Create Tables (in correct order due to dependencies)
-- =============================================

-- Create Users table first (others depend on it)
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
GO

-- Create Restaurants table (depends on Users)
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
GO

-- Create Photos table (depends on Restaurants)
CREATE TABLE [dbo].[TP_Photos] (
    [PhotoID] INT IDENTITY (1, 1) NOT NULL,
    [RestaurantID] INT NOT NULL,
    [PhotoURL] NVARCHAR (MAX) NOT NULL,
    [Caption] NVARCHAR (500) NULL,
    [UploadedDate] DATETIME2 (7) CONSTRAINT [DF_TP_Photos_UploadedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_TP_Photos] PRIMARY KEY CLUSTERED ([PhotoID] ASC),
    CONSTRAINT [FK_TP_Photos_Restaurants] FOREIGN KEY ([RestaurantID]) REFERENCES [dbo].[TP_Restaurants] ([RestaurantID]) ON DELETE CASCADE
);
GO

-- Create Reviews table (depends on Restaurants and Users)
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
GO

-- Create Reservations table (depends on Restaurants and Users)
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
GO

-- =============================================
-- Create Stored Procedures
-- =============================================

-- Add Restaurant Photo SP
CREATE PROCEDURE dbo.TP_spAddRestaurantPhoto
@RestaurantID INT,
@PhotoURL NVARCHAR(MAX),
@Caption NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.TP_Photos (
            RestaurantID,
            PhotoURL,
            Caption
        )
        VALUES (
            @RestaurantID,
            @PhotoURL,
            @Caption
        );
        RETURN @@ROWCOUNT;
    END TRY
    BEGIN CATCH
        PRINT ERROR_MESSAGE();
        RETURN -1;
    END CATCH
END;
GO

-- Add Review SP
CREATE PROCEDURE dbo.TP_spAddReview
@RestaurantID INT,
@UserID INT,
@VisitDate DATETIME2(7),
@Comments NVARCHAR(MAX),
@FoodQualityRating INT,
@ServiceRating INT,
@AtmosphereRating INT,
@PriceRating INT
AS
BEGIN
    SET NOCOUNT ON;
    IF @FoodQualityRating < 1 OR @FoodQualityRating > 5 OR
       @ServiceRating < 1 OR @ServiceRating > 5 OR
       @AtmosphereRating < 1 OR @AtmosphereRating > 5 OR
       @PriceRating < 1 OR @PriceRating > 5
    BEGIN
        PRINT 'Error: Ratings must be between 1 and 5.';
        RETURN -1;
    END
    BEGIN TRY
        INSERT INTO dbo.TP_Reviews (
            RestaurantID,
            UserID,
            VisitDate,
            Comments,
            FoodQualityRating,
            ServiceRating,
            AtmosphereRating,
            PriceRating
        )
        VALUES (
            @RestaurantID,
            @UserID,
            @VisitDate,
            @Comments,
            @FoodQualityRating,
            @ServiceRating,
            @AtmosphereRating,
            @PriceRating
        );
        RETURN @@ROWCOUNT;
    END TRY
    BEGIN CATCH
        PRINT ERROR_MESSAGE();
        RETURN -1;
    END CATCH
END;
GO

-- Add User SP
CREATE PROCEDURE dbo.TP_spAddUser
@Username NVARCHAR(100),
@Email NVARCHAR(255),
@UserPassword NVARCHAR(MAX),
@UserType VARCHAR(50),
@SecurityQuestion1 NVARCHAR(255),
@SecurityAnswerHash1 NVARCHAR(MAX),
@SecurityQuestion2 NVARCHAR(255),
@SecurityAnswerHash2 NVARCHAR(MAX),
@SecurityQuestion3 NVARCHAR(255),
@SecurityAnswerHash3 NVARCHAR(MAX),
@VerificationToken NVARCHAR(100),
@VerificationTokenExpiry DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.TP_Users (
        Username,
        Email,
        PasswordHash,
        UserType,
        CreatedDate,
        SecurityQuestion1,
        SecurityAnswerHash1,
        SecurityQuestion2,
        SecurityAnswerHash2,
        SecurityQuestion3,
        SecurityAnswerHash3,
        IsVerified,
        VerificationToken,
        VerificationTokenExpiry
    )
    VALUES (
        @Username,
        @Email,
        @UserPassword,
        @UserType,
        GETDATE(),
        @SecurityQuestion1,
        @SecurityAnswerHash1,
        @SecurityQuestion2,
        @SecurityAnswerHash2,
        @SecurityQuestion3,
        @SecurityAnswerHash3,
        0,
        @VerificationToken,
        @VerificationTokenExpiry
    );
    SELECT SCOPE_IDENTITY() AS NewUserID;
END;
GO

-- Check User SP
CREATE PROCEDURE dbo.TP_spCheckUser
@Username NVARCHAR(100),
@UserPassword NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        UserID,
        UserType,
        PasswordHash,
        IsVerified
    FROM
        dbo.TP_Users
    WHERE
        Username = @Username;
END;
GO

-- Check Username Exists SP
CREATE PROCEDURE dbo.TP_spCheckUsernameExists
@Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM dbo.TP_Users WHERE Username = @Username)
    BEGIN
        SELECT 1 AS DoesExist;
    END
    ELSE
    BEGIN
        SELECT 0 AS DoesExist;
    END
END;
GO

-- Delete Restaurant Photo SP
CREATE PROCEDURE dbo.TP_spDeleteRestaurantPhoto
@PhotoID INT,
@RestaurantID_Check INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DELETE FROM dbo.TP_Photos
        WHERE
            PhotoID = @PhotoID
            AND (@RestaurantID_Check IS NULL OR RestaurantID = @RestaurantID_Check);
        IF @@ROWCOUNT = 0
        BEGIN
            PRINT 'Warning: PhotoID not found or permission denied for deletion.';
            RETURN 0;
        END
        RETURN @@ROWCOUNT;
    END TRY
    BEGIN CATCH
        PRINT ERROR_MESSAGE();
        RETURN -1;
    END CATCH
END;
GO

-- Delete Review SP
CREATE PROCEDURE dbo.TP_spDeleteReview
@ReviewID INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DELETE FROM dbo.TP_Reviews
        WHERE ReviewID = @ReviewID;
        IF @@ROWCOUNT = 0
        BEGIN
            PRINT 'Warning: ReviewID not found for deletion.';
            RETURN 0;
        END
        RETURN @@ROWCOUNT;
    END TRY
    BEGIN CATCH
        PRINT ERROR_MESSAGE();
        RETURN -1;
    END CATCH
END;
GO

-- Get Featured Restaurants SP
CREATE PROCEDURE dbo.TP_spGetFeaturedRestaurants
@TopN INT = 6
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@TopN)
        r.RestaurantID,
        r.Name,
        r.Cuisine,
        r.City,
        r.State,
        r.ProfilePhoto,
        r.MarketingDescription,
        COALESCE(AVG(CAST(rev.FoodQualityRating AS FLOAT)), 0) AS AvgFoodRating,
        COALESCE(AVG(CAST(rev.ServiceRating AS FLOAT)), 0) AS AvgServiceRating,
        COALESCE(AVG(CAST(rev.AtmosphereRating AS FLOAT)), 0) AS AvgAtmosphereRating,
        COALESCE(AVG(CAST(rev.PriceRating AS FLOAT)), 0) AS AvgPriceRating,
        COUNT(DISTINCT rev.ReviewID) AS ReviewCount
    FROM
        dbo.TP_Restaurants r
        LEFT JOIN dbo.TP_Reviews rev ON r.RestaurantID = rev.RestaurantID
    GROUP BY
        r.RestaurantID,
        r.Name,
        r.Cuisine,
        r.City,
        r.State,
        r.ProfilePhoto,
        r.MarketingDescription
    ORDER BY
        ReviewCount DESC,
        r.CreatedDate DESC;
END;
GO

-- [Add the rest of your stored procedures here...] 