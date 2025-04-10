CREATE OR ALTER PROCEDURE dbo.TP_spGetRestaurantReviews
    @RestaurantID int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM TP_Reviews
    WHERE RestaurantID = @RestaurantID
    ORDER BY VisitDate DESC;
END 