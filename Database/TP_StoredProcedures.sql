-- Team Project 3 Stored Procedures
-- All stored procedures are prefixed with TP_ as required

-- Drop if exists (optional)
-- IF OBJECT_ID('dbo.TP_spAddRestaurantPhoto', 'P') IS NOT NULL
-- DROP PROCEDURE dbo.TP_spAddRestaurantPhoto;
-- GO
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
-- UploadedDate has a default constraint
)
VALUES (
@RestaurantID,
@PhotoURL,
@Caption
);
-- Return the new PhotoID (optional)
-- SELECT SCOPE_IDENTITY() AS NewPhotoID;
RETURN @@ROWCOUNT; -- Return number of rows affected (should be 1)
END TRY
BEGIN CATCH
PRINT ERROR_MESSAGE();
RETURN -1; -- Indicate error
END CATCH
END;

-- Drop if exists (optional)
-- IF OBJECT_ID('dbo.TP_spAddReview', 'P') IS NOT NULL
-- DROP PROCEDURE dbo.TP_spAddReview;
-- GO
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
-- Input validation for ratings (optional, as CHECK constraints exist)
IF @FoodQualityRating < 1 OR @FoodQualityRating > 5 OR
@ServiceRating < 1 OR @ServiceRating > 5 OR
@AtmosphereRating < 1 OR @AtmosphereRating > 5 OR
@PriceRating < 1 OR @PriceRating > 5
BEGIN
-- Handle invalid rating error (e.g., RAISERROR or return error code)
PRINT 'Error: Ratings must be between 1 and 5.';
RETURN -1; -- Indicate error
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
-- CreatedDate has a default constraint
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
-- Return the new ReviewID (optional)
-- SELECT SCOPE_IDENTITY() AS NewReviewID;
RETURN @@ROWCOUNT; -- Return number of rows affected (should be 1)
END TRY
BEGIN CATCH
-- Handle error (log, re-throw, etc.)
PRINT ERROR_MESSAGE();
RETURN -1; -- Indicate error
END CATCH
END;

CREATE PROCEDURE dbo.TP_spAddUser
-- User details
@Username NVARCHAR(100), -- User name
@Email NVARCHAR(255), -- User email
@UserPassword NVARCHAR(MAX), -- Expecting already hashed password from C#
@UserType VARCHAR(50), -- 'reviewer' or 'restaurantRep'
-- Security Q&A stuff
@SecurityQuestion1 NVARCHAR(255),
@SecurityAnswerHash1 NVARCHAR(MAX), -- HASHED answer from C#
@SecurityQuestion2 NVARCHAR(255),
@SecurityAnswerHash2 NVARCHAR(MAX), -- HASHED answer from C#
@SecurityQuestion3 NVARCHAR(255),
@SecurityAnswerHash3 NVARCHAR(MAX), -- HASHED answer from C#
-- Verification info
@VerificationToken NVARCHAR(100), -- New parameter for verification code
@VerificationTokenExpiry DATETIME2 -- New parameter for expiry
AS
BEGIN
SET NOCOUNT ON;
-- Add the user to TP_Users table
INSERT INTO dbo.TP_Users (
Username,
Email,
PasswordHash, -- Column name in my table
UserType,
CreatedDate,
SecurityQuestion1,
SecurityAnswerHash1, -- Store the hash
SecurityQuestion2,
SecurityAnswerHash2, -- Store the hash
SecurityQuestion3,
SecurityAnswerHash3, -- Store the hash
IsVerified, -- Set initial status
VerificationToken, -- New column
VerificationTokenExpiry -- New column
)
VALUES (
@Username,
@Email,
@UserPassword, -- Already hashed from C#
@UserType,
GETDATE(),
@SecurityQuestion1,
@SecurityAnswerHash1, -- The HASHED answer
@SecurityQuestion2,
@SecurityAnswerHash2, -- The HASHED answer
@SecurityQuestion3,
@SecurityAnswerHash3, -- The HASHED answer
0, -- Default to not verified
@VerificationToken, -- Store verification token
@VerificationTokenExpiry -- Store expiry date
);
SELECT SCOPE_IDENTITY() AS NewUserID;
END;

CREATE PROCEDURE dbo.TP_spCheckUser
@Username NVARCHAR(100),
@UserPassword NVARCHAR(MAX) -- this is the plain pssword user typed
AS
BEGIN
SET NOCOUNT ON;
-- get user details if usrname matches
-- C# code needs to check the pssword hash
SELECT
UserID,
UserType,
PasswordHash, -- the stored hash
IsVerified -- check if verified
FROM
dbo.TP_Users -- users table
WHERE
Username = @Username; -- find by username
END;

CREATE PROCEDURE dbo.TP_spCheckUsernameExists
@Username NVARCHAR(100) -- the username to check
AS
BEGIN
SET NOCOUNT ON;
-- Check if the username exists in TP_Users table
IF EXISTS (SELECT 1 FROM dbo.TP_Users WHERE Username = @Username)
BEGIN
-- Return 1 if it exists
SELECT 1 AS DoesExist;
END
ELSE
BEGIN
-- Return 0 if it doesnt exist
SELECT 0 AS DoesExist;
END
END;

-- Drop if exists (optional)
-- IF OBJECT_ID('dbo.TP_spDeleteRestaurantPhoto', 'P') IS NOT NULL
-- DROP PROCEDURE dbo.TP_spDeleteRestaurantPhoto;
-- GO
CREATE PROCEDURE dbo.TP_spDeleteRestaurantPhoto
@PhotoID INT,
@RestaurantID_Check INT = NULL -- Optional: Pass RestaurantID (Rep's UserID) for security check
AS
BEGIN
SET NOCOUNT ON;
BEGIN TRY
DELETE FROM dbo.TP_Photos
WHERE
PhotoID = @PhotoID
-- Optional security check: ensure the photo belongs to the rep deleting it
AND (@RestaurantID_Check IS NULL OR RestaurantID = @RestaurantID_Check);
IF @@ROWCOUNT = 0
BEGIN
PRINT 'Warning: PhotoID not found or permission denied for deletion.';
RETURN 0; -- Indicate no rows deleted
END
RETURN @@ROWCOUNT; -- Return number of rows affected (should be 1)
END TRY
BEGIN CATCH
PRINT ERROR_MESSAGE();
RETURN -1; -- Indicate error
END CATCH
END;

-- Drop if exists (optional)
-- IF OBJECT_ID('dbo.TP_spDeleteReview', 'P') IS NOT NULL
-- DROP PROCEDURE dbo.TP_spDeleteReview;
-- GO
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
RETURN 0; -- Indicate no rows deleted
END
RETURN @@ROWCOUNT; -- Return number of rows affected (should be 1)
END TRY
BEGIN CATCH
PRINT ERROR_MESSAGE();
RETURN -1; -- Indicate error
END CATCH
END;

CREATE PROCEDURE dbo.TP_spGetFeaturedRestaurants
@TopN INT = 6 -- How many to get, default 6
AS
BEGIN
SET NOCOUNT ON;
-- Select top N restaurants based on overall average rating
-- Need to calculate avg ratings from TP_Reviews table
SELECT TOP (@TopN)
R.RestaurantID,
R.Name,
R.Cuisine,
R.City,
R.State,
R.LogoPhoto, -- For display
ISNULL(AVG_Ratings.OverallRating, 0) AS OverallRating,
ISNULL(AVG_Ratings.ReviewCount, 0) AS ReviewCount,
ISNULL(AVG_Ratings.AveragePriceRating, 0) AS AveragePriceRating
FROM
dbo.TP_Restaurants R
LEFT JOIN
(
-- Subquery to calculate average ratings and counts
SELECT
RV.RestaurantID,
AVG((CAST(RV.FoodQualityRating AS FLOAT) + CAST(RV.ServiceRating AS FLOAT) + CAST(RV.AtmosphereRating AS FLOAT)) / 3.0) AS OverallRating,
COUNT(*) AS ReviewCount,
AVG(CAST(RV.PriceRating AS FLOAT)) AS AveragePriceRating
FROM
dbo.TP_Reviews RV
GROUP BY
RV.RestaurantID
) AS AVG_Ratings ON R.RestaurantID = AVG_Ratings.RestaurantID
ORDER BY
OverallRating DESC, -- Highest rated first
ReviewCount DESC; -- Then by most reviews
END;

CREATE PROCEDURE dbo.TP_spGetPendingReservations
@RestaurantID INT, -- This is the UserID of the restaurant representative
@MaxCount INT = 10 -- Optional limit
AS
BEGIN
SET NOCOUNT ON;
-- Select top N pending reservations for this restaurant
SELECT TOP (@MaxCount)
ReservationID,
RestaurantID,
UserID, -- Could join TP_Users to get username if needed
ReservationDateTime,
PartySize,
ContactName,
Phone,
Email,
SpecialRequests,
Status,
CreatedDate
FROM
dbo.TP_Reservations
WHERE
RestaurantID = @RestaurantID
AND Status = 'Pending' -- Filter for pending status
ORDER BY
ReservationDateTime ASC; -- Show soonest first
END;

CREATE PROCEDURE dbo.TP_spGetRecentReviews
@RestaurantID INT, -- This is the UserID of the restaurant representative
@MaxCount INT = 5 -- Optional limit
AS
BEGIN
SET NOCOUNT ON;
-- Select top N recent reviews for this restaurant
-- Join with TP_Users to get reviewer username
SELECT TOP (@MaxCount)
RV.ReviewID,
RV.RestaurantID,
RV.UserID,
U.Username AS ReviewerUsername, -- Get username
RV.VisitDate,
RV.Comments,
RV.FoodQualityRating,
RV.ServiceRating,
RV.AtmosphereRating,
RV.PriceRating,
RV.CreatedDate
FROM
dbo.TP_Reviews RV
INNER JOIN
dbo.TP_Users U ON RV.UserID = U.UserID
WHERE
RV.RestaurantID = @RestaurantID
ORDER BY
RV.CreatedDate DESC; -- Show newest first
END;

-- Drop if exists (optional)
-- IF OBJECT_ID('dbo.TP_spGetRestaurantPhotos', 'P') IS NOT NULL
-- DROP PROCEDURE dbo.TP_spGetRestaurantPhotos;
-- GO
CREATE PROCEDURE dbo.TP_spGetRestaurantPhotos
@RestaurantID INT
AS
BEGIN
SET NOCOUNT ON;
SELECT
PhotoID,
RestaurantID,
PhotoURL,
Caption,
UploadedDate
FROM
dbo.TP_Photos
WHERE
RestaurantID = @RestaurantID
ORDER BY
UploadedDate ASC; -- Or PhotoID ASC
END;

CREATE PROCEDURE dbo.TP_spGetRestaurantProfile
@RestaurantID INT -- This is the UserID of the restaurant representative
AS
BEGIN
SET NOCOUNT ON;
-- Select profile info from TP_Restaurants table
SELECT
RestaurantID, -- which is the UserID
Name,
Address,
City,
State,
ZipCode,
Cuisine,
Hours,
Contact,
ProfilePhoto,
LogoPhoto,
MarketingDescription,
WebsiteURL,
SocialMedia,
Owner,
CreatedDate
FROM
dbo.TP_Restaurants
WHERE
RestaurantID = @RestaurantID; -- Match the UserID
END;

CREATE PROCEDURE dbo.TP_spGetReviewById
@ReviewID INT
AS
BEGIN
SET NOCOUNT ON;
-- Select review details and join to get restaurant name
SELECT
RV.ReviewID,
RV.RestaurantID,
R.Name AS RestaurantName,
RV.UserID,
RV.VisitDate,
RV.Comments,
RV.FoodQualityRating,
RV.ServiceRating,
RV.AtmosphereRating,
RV.PriceRating,
RV.CreatedDate
FROM
dbo.TP_Reviews RV
INNER JOIN
dbo.TP_Restaurants R ON RV.RestaurantID = R.RestaurantID
WHERE
RV.ReviewID = @ReviewID;
END;

CREATE PROCEDURE dbo.TP_spGetReviewsByUser
@UserID INT
AS
BEGIN
SET NOCOUNT ON;
-- Select reviews and join to get restaurant name
SELECT
RV.ReviewID,
RV.RestaurantID,
R.Name AS RestaurantName, -- Get restaurant name
RV.UserID,
RV.VisitDate,
RV.Comments,
RV.FoodQualityRating,
RV.ServiceRating,
RV.AtmosphereRating,
RV.PriceRating,
RV.CreatedDate
FROM
dbo.TP_Reviews RV
INNER JOIN
dbo.TP_Restaurants R ON RV.RestaurantID = R.RestaurantID
WHERE
RV.UserID = @UserID
ORDER BY
RV.CreatedDate DESC; -- Show newest first
END;

CREATE PROCEDURE dbo.TP_spGetUserByUsernameOrEmail
@UsernameOrEmail NVARCHAR(255) -- Input can be username or email
AS
BEGIN
SET NOCOUNT ON;
-- Find user by either username or email
SELECT
UserID,
Email,
IsVerified -- Also return this to check if they can reset
FROM
dbo.TP_Users
WHERE
Username = @UsernameOrEmail OR Email = @UsernameOrEmail;
END;

CREATE PROCEDURE dbo.TP_spGetUserForVerification
@VerificationToken NVARCHAR(100)
AS
BEGIN
SET NOCOUNT ON;
-- Get user details if verification token matches and hasn't expired
SELECT
UserID,
Username,
Email,
IsVerified,
VerificationToken,
VerificationTokenExpiry,
CASE
WHEN VerificationTokenExpiry > GETDATE() THEN 1
ELSE 0
END AS TokenIsValid
FROM
dbo.TP_Users
WHERE
VerificationToken = @VerificationToken;
END;

CREATE PROCEDURE dbo.TP_spGetUserSecurityQuestions
@UserID INT
AS
BEGIN
SET NOCOUNT ON;
-- Get the questions
SELECT
SecurityQuestion1,
SecurityQuestion2,
SecurityQuestion3
FROM
dbo.TP_Users
WHERE
UserID = @UserID;
END;

CREATE PROCEDURE dbo.TP_spInvalidatePasswordResetToken
@UserID INT
-- Or could use @PasswordResetToken as input
AS
BEGIN
SET NOCOUNT ON;
-- Clear the token fields so it cant be used again
UPDATE dbo.TP_Users
SET
PasswordResetToken = NULL,
ResetTokenExpiry = NULL
WHERE
UserID = @UserID;
-- Or WHERE PasswordResetToken = @PasswordResetToken;
END;

CREATE PROCEDURE dbo.TP_spSearchRestaurants
@CuisineList NVARCHAR(MAX) = NULL, -- Comma-separated list of cuisines, or NULL for all
@City NVARCHAR(100) = NULL,
@State NVARCHAR(50) = NULL
AS
BEGIN
SET NOCOUNT ON;
-- Temp table to hold split cuisine values if list is provided
DECLARE @CuisinesTable TABLE (CuisineName NVARCHAR(100));
IF @CuisineList IS NOT NULL AND LEN(@CuisineList) > 0
BEGIN
-- Simple split function assumption (SQL Server 2016+ has STRING_SPLIT)
-- For older SQL Server, you might need a custom split function
INSERT INTO @CuisinesTable (CuisineName)
SELECT value FROM STRING_SPLIT(@CuisineList, ',');
END;
-- Main query
SELECT
R.RestaurantID,
R.Name,
R.Cuisine,
R.City,
R.State,
R.LogoPhoto,
ISNULL(AVG_Ratings.OverallRating, 0) AS OverallRating,
ISNULL(AVG_Ratings.ReviewCount, 0) AS ReviewCount,
ISNULL(AVG_Ratings.AveragePriceRating, 0) AS AveragePriceRating
FROM
dbo.TP_Restaurants R
LEFT JOIN
(
-- Subquery to calculate average ratings and counts
SELECT
RV.RestaurantID,
AVG((CAST(RV.FoodQualityRating AS FLOAT) + CAST(RV.ServiceRating AS FLOAT) + CAST(RV.AtmosphereRating AS FLOAT)) / 3.0) AS OverallRating,
COUNT(*) AS ReviewCount,
AVG(CAST(RV.PriceRating AS FLOAT)) AS AveragePriceRating
FROM
dbo.TP_Reviews RV
GROUP BY
RV.RestaurantID
) AS AVG_Ratings ON R.RestaurantID = AVG_Ratings.RestaurantID
WHERE
-- City filter (optional)
(@City IS NULL OR R.City LIKE '%' + @City + '%') -- Use LIKE for partial match? Or = for exact.
-- State filter (optional)
AND (@State IS NULL OR R.State = @State) -- Exact match for state usually
-- Cuisine filter (optional)
AND (
(@CuisineList IS NULL OR LEN(@CuisineList) = 0) -- No cuisine filter applied
OR
(EXISTS (SELECT 1 FROM @CuisinesTable CT WHERE R.Cuisine = CT.CuisineName)) -- Match any cuisine in the list
)
ORDER BY
OverallRating DESC,
ReviewCount DESC;
END;

CREATE PROCEDURE dbo.TP_spSetUserVerified
@UserID INT
AS
BEGIN
SET NOCOUNT ON;
-- Update user status and clear token and expiry
UPDATE dbo.TP_Users
SET
IsVerified = 1, -- Set to true
VerificationToken = NULL, -- Clear the token
VerificationTokenExpiry = NULL -- Clear the expiry date
WHERE
UserID = @UserID;
END;

CREATE PROCEDURE dbo.TP_spStorePasswordResetToken
@UserID INT,
@PasswordResetToken NVARCHAR(100), -- The generated token
@ResetTokenExpiry DATETIME2 -- When the token expires
AS
BEGIN
SET NOCOUNT ON;
-- Update the user record with the token and expiry
UPDATE dbo.TP_Users
SET
PasswordResetToken = @PasswordResetToken,
ResetTokenExpiry = @ResetTokenExpiry
WHERE
UserID = @UserID;
END;

-- Drop if exists (optional)
-- IF OBJECT_ID('dbo.TP_spUpdateReservationStatus', 'P') IS NOT NULL
-- DROP PROCEDURE dbo.TP_spUpdateReservationStatus;
-- GO
CREATE PROCEDURE dbo.TP_spUpdateReservationStatus
@ReservationID INT,
@NewStatus NVARCHAR(50),
@RestaurantID_Check INT = NULL -- Optional: Pass RestaurantID (Rep's UserID) for security check
AS
BEGIN
SET NOCOUNT ON;
-- Optional: Add validation for allowed statuses
IF @NewStatus NOT IN ('Confirmed', 'Cancelled', 'Pending', 'Completed', 'No Show') -- Example statuses
BEGIN
PRINT 'Error: Invalid status value provided.';
RETURN -1;
END
BEGIN TRY
UPDATE dbo.TP_Reservations
SET Status = @NewStatus
WHERE
ReservationID = @ReservationID
-- Optional security check: ensure the reservation belongs to the rep updating it
AND (@RestaurantID_Check IS NULL OR RestaurantID = @RestaurantID_Check);
IF @@ROWCOUNT = 0
BEGIN
PRINT 'Error: ReservationID not found or permission denied for update.';
RETURN 0; -- Indicate no rows updated
END
RETURN @@ROWCOUNT; -- Return number of rows affected (should be 1)
END TRY
BEGIN CATCH
PRINT ERROR_MESSAGE();
RETURN -1; -- Indicate error
END CATCH
END;

-- Drop if exists (optional)
-- IF OBJECT_ID('dbo.TP_spUpdateRestaurantProfile', 'P') IS NOT NULL
-- DROP PROCEDURE dbo.TP_spUpdateRestaurantProfile;
-- GO
CREATE PROCEDURE dbo.TP_spUpdateRestaurantProfile
@RestaurantID INT, -- This is the UserID of the representative
@Name NVARCHAR(200),
@Address NVARCHAR(255) = NULL,
@City NVARCHAR(100) = NULL,
@State NVARCHAR(50) = NULL,
@ZipCode VARCHAR(10) = NULL, -- Keep VARCHAR as per table def
@Cuisine NVARCHAR(100) = NULL,
@Hours NVARCHAR(255) = NULL,
@Contact NVARCHAR(100) = NULL,
@ProfilePhoto NVARCHAR(MAX) = NULL,
@LogoPhoto NVARCHAR(MAX) = NULL,
@MarketingDescription NVARCHAR(MAX) = NULL,
@WebsiteURL NVARCHAR(MAX) = NULL,
@SocialMedia NVARCHAR(MAX) = NULL,
@Owner NVARCHAR(100) = NULL
AS
BEGIN
SET NOCOUNT ON;
BEGIN TRY
UPDATE dbo.TP_Restaurants
SET
Name = @Name,
Address = @Address,
City = @City,
State = @State,
ZipCode = @ZipCode,
Cuisine = @Cuisine,
Hours = @Hours,
Contact = @Contact,
ProfilePhoto = @ProfilePhoto,
LogoPhoto = @LogoPhoto,
MarketingDescription = @MarketingDescription,
WebsiteURL = @WebsiteURL,
SocialMedia = @SocialMedia,
Owner = @Owner
-- CreatedDate should not be updated
WHERE
RestaurantID = @RestaurantID;
IF @@ROWCOUNT = 0
BEGIN
PRINT 'Error: RestaurantID not found for update.';
RETURN 0; -- Indicate no rows updated
END
RETURN @@ROWCOUNT; -- Return number of rows affected (should be 1)
END TRY
BEGIN CATCH
PRINT ERROR_MESSAGE();
RETURN -1; -- Indicate error
END CATCH
END;

-- Drop if exists (optional)
-- IF OBJECT_ID('dbo.TP_spUpdateReview', 'P') IS NOT NULL
-- DROP PROCEDURE dbo.TP_spUpdateReview;
-- GO
CREATE PROCEDURE dbo.TP_spUpdateReview
@ReviewID INT,
@VisitDate DATETIME2(7),
@Comments NVARCHAR(MAX),
@FoodQualityRating INT,
@ServiceRating INT,
@AtmosphereRating INT,
@PriceRating INT
-- Note: Do not allow changing RestaurantID or UserID here
AS
BEGIN
SET NOCOUNT ON;
-- Input validation for ratings (optional, as CHECK constraints exist)
IF @FoodQualityRating < 1 OR @FoodQualityRating > 5 OR
@ServiceRating < 1 OR @ServiceRating > 5 OR
@AtmosphereRating < 1 OR @AtmosphereRating > 5 OR
@PriceRating < 1 OR @PriceRating > 5
BEGIN
PRINT 'Error: Ratings must be between 1 and 5.';
RETURN -1; -- Indicate error
END
BEGIN TRY
UPDATE dbo.TP_Reviews
SET
VisitDate = @VisitDate,
Comments = @Comments,
FoodQualityRating = @FoodQualityRating,
ServiceRating = @ServiceRating,
AtmosphereRating = @AtmosphereRating,
PriceRating = @PriceRating
-- CreatedDate should generally not be updated
WHERE
ReviewID = @ReviewID;
IF @@ROWCOUNT = 0
BEGIN
PRINT 'Error: ReviewID not found for update.';
RETURN 0; -- Indicate no rows updated
END
RETURN @@ROWCOUNT; -- Return number of rows affected (should be 1)
END TRY
BEGIN CATCH
PRINT ERROR_MESSAGE();
RETURN -1; -- Indicate error
END CATCH
END;

CREATE PROCEDURE dbo.TP_spUpdateUserPassword
@UserID INT,
@NewPasswordHash NVARCHAR(MAX) -- The NEW HASHED password from C#
AS
BEGIN
SET NOCOUNT ON;
-- Update the password hash
UPDATE dbo.TP_Users
SET
PasswordHash = @NewPasswordHash
WHERE
UserID = @UserID;
END;

CREATE PROCEDURE dbo.TP_spValidatePasswordResetToken
@UserID INT,
@PasswordResetToken NVARCHAR(100) -- Token from the email link
AS
BEGIN
SET NOCOUNT ON;
-- Check if token matches for the user and hasn't expired
IF EXISTS (
SELECT 1
FROM dbo.TP_Users
WHERE UserID = @UserID
AND PasswordResetToken = @PasswordResetToken
AND ResetTokenExpiry > GETDATE() -- Check if expiry is in the future
)
BEGIN
SELECT 1 AS IsValid; -- Token is valid
END
ELSE
BEGIN
SELECT 0 AS IsValid; -- Token is invalid or expired
END
END;

CREATE PROCEDURE dbo.TP_spValidateSecurityAnswers
@UserID INT,
@QuestionNumber INT, -- Which question (1, 2, or 3)
@AnswerToCheckHash NVARCHAR(MAX) -- The HASHED answer user typed (hashed in C#)
AS
BEGIN
SET NOCOUNT ON;
DECLARE @StoredAnswerHash NVARCHAR(MAX);
-- Get the correct stored hash based on question number
IF @QuestionNumber = 1
SELECT @StoredAnswerHash = SecurityAnswerHash1 FROM dbo.TP_Users WHERE UserID = @UserID;
ELSE IF @QuestionNumber = 2
SELECT @StoredAnswerHash = SecurityAnswerHash2 FROM dbo.TP_Users WHERE UserID = @UserID;
ELSE IF @QuestionNumber = 3
SELECT @StoredAnswerHash = SecurityAnswerHash3 FROM dbo.TP_Users WHERE UserID = @UserID;
ELSE
BEGIN
SELECT 0 AS IsCorrect; -- Invalid question number
RETURN;
END
-- Compare the hash provided with the stored hash
IF @StoredAnswerHash IS NOT NULL AND @StoredAnswerHash = @AnswerToCheckHash
BEGIN
SELECT 1 AS IsCorrect; -- Answer is correct
END
ELSE
BEGIN
SELECT 0 AS IsCorrect; -- Answer is incorrect or not found
END
END;

CREATE PROCEDURE dbo.TP_spValidateVerificationToken
@UserID INT,
@VerificationToken NVARCHAR(100) -- Token from email link
AS
BEGIN
SET NOCOUNT ON;
-- Check if token matches for the user
-- Assumes VerificationToken column exists in TP_Users
IF EXISTS (
SELECT 1
FROM dbo.TP_Users
WHERE UserID = @UserID
AND VerificationToken = @VerificationToken
-- AND IsVerified = 0 -- Optional: only validate if not already verified
)
BEGIN
SELECT 1 AS IsValid; -- Token is valid
END
ELSE
BEGIN
SELECT 0 AS IsValid; -- Token is invalid or doesnt match
END
END;

CREATE PROCEDURE dbo.TP_GetUserByUsername
@Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        UserID,
        Username,
        Email,
        UserType,
        IsVerified,
        SecurityQuestion1,
        SecurityQuestion2,
        SecurityQuestion3,
        VerificationToken,
        VerificationTokenExpiry
    FROM dbo.TP_Users
    WHERE Username = @Username;
END; 