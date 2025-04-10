-- Script to update restaurant images based on cuisine
-- Run this in your SQL Server Management Studio or other SQL client

-- First, let's see what cuisines we have in the database
SELECT DISTINCT Cuisine FROM TP_Restaurants WHERE Cuisine IS NOT NULL;

-- Now, let's update the LogoPhoto field for each restaurant based on cuisine
-- This assumes you have images in the wwwroot/images/restaurants directory with names like:
-- italian-restaurant.jpg, mexican-restaurant.jpg, etc.

-- Update Italian restaurants
UPDATE TP_Restaurants
SET LogoPhoto = '/images/restaurants/italian-restaurant.jpg'
WHERE Cuisine LIKE '%Italian%' OR Cuisine LIKE '%italian%';

-- Update Mexican restaurants
UPDATE TP_Restaurants
SET LogoPhoto = '/images/restaurants/mexican-restaurant.jpg'
WHERE Cuisine LIKE '%Mexican%' OR Cuisine LIKE '%mexican%';

-- Update Chinese restaurants
UPDATE TP_Restaurants
SET LogoPhoto = '/images/restaurants/chinese-restaurant.jpg'
WHERE Cuisine LIKE '%Chinese%' OR Cuisine LIKE '%chinese%';

-- Update Japanese restaurants
UPDATE TP_Restaurants
SET LogoPhoto = '/images/restaurants/japanese-restaurant.jpg'
WHERE Cuisine LIKE '%Japanese%' OR Cuisine LIKE '%japanese%';

-- Update American restaurants
UPDATE TP_Restaurants
SET LogoPhoto = '/images/restaurants/american-restaurant.jpg'
WHERE Cuisine LIKE '%American%' OR Cuisine LIKE '%american%';

-- Update Indian restaurants
UPDATE TP_Restaurants
SET LogoPhoto = '/images/restaurants/indian-restaurant.jpg'
WHERE Cuisine LIKE '%Indian%' OR Cuisine LIKE '%indian%';

-- Update Thai restaurants
UPDATE TP_Restaurants
SET LogoPhoto = '/images/restaurants/thai-restaurant.jpg'
WHERE Cuisine LIKE '%Thai%' OR Cuisine LIKE '%thai%';

-- Update Mediterranean restaurants
UPDATE TP_Restaurants
SET LogoPhoto = '/images/restaurants/mediterranean-restaurant.jpg'
WHERE Cuisine LIKE '%Mediterranean%' OR Cuisine LIKE '%mediterranean%';

-- For any remaining restaurants without a cuisine-specific image, use the default
UPDATE TP_Restaurants
SET LogoPhoto = '/images/restaurant-placeholder.png'
WHERE LogoPhoto IS NULL;

-- Verify the updates
SELECT RestaurantID, Name, Cuisine, LogoPhoto FROM TP_Restaurants; 