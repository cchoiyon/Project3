CREATE OR ALTER PROCEDURE dbo.TP_spGetRestaurantPhotos
    @RestaurantID int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM TP_Photos
    WHERE RestaurantID = @RestaurantID;
END 