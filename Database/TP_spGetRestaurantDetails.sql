CREATE OR ALTER PROCEDURE dbo.TP_spGetRestaurantDetails
    @RestaurantID int
AS
BEGIN
    SET NOCOUNT ON;

    -- Get restaurant details with calculated fields
    SELECT 
        r.*,
        COALESCE(AVG(CAST(rev.FoodQualityRating + rev.ServiceRating + rev.AtmosphereRating AS FLOAT) / 3), 0) as OverallRating,
        COUNT(DISTINCT rev.ReviewID) as ReviewCount,
        COALESCE(AVG(CAST(rev.PriceRating AS FLOAT)), 0) as AveragePriceRating
    FROM 
        TP_Restaurants r
        LEFT JOIN TP_Reviews rev ON r.RestaurantID = rev.RestaurantID
    WHERE 
        r.RestaurantID = @RestaurantID
    GROUP BY 
        r.RestaurantID,
        r.Name,
        r.Address,
        r.City,
        r.State,
        r.ZipCode,
        r.Cuisine,
        r.Hours,
        r.Contact,
        r.MarketingDescription,
        r.WebsiteURL,
        r.SocialMedia,
        r.Owner,
        r.ProfilePhoto,
        r.LogoPhoto,
        r.CreatedDate;
END 