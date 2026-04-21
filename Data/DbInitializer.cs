using EcoLilly.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoLilly.Data
{
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (context.Set<Product>().Any())
                return;

            var products = new List<Product>
            {
                new Product
                {
                    Name = "Bamboo Water Bottle",
                    Price = 29.99m,
                    Description = "Sustainable bamboo exterior with stainless steel interior. Keeps drinks cold for 24 hours or hot for 12 hours.",
                    Image = "https://images.pexels.com/photos/4498151/pexels-photo-4498151.jpeg?auto=compress&cs=tinysrgb&w=800",
                    Category = "Drinkware",
                    EcoFeatures = "100% Biodegradable,Zero Plastic,Carbon Neutral Shipping",
                    InStock = true,
                    Reviews = new List<Review>
                    {
                        new Review{ User="Alice", Rating=5, Comment="Love this bottle!", Date=DateTime.Parse("2024-01-15") },
                        new Review{ User="Bob", Rating=4, Comment="Great quality.", Date=DateTime.Parse("2024-01-10") }
                    }
                },

                new Product { Name="Organic Cotton Tote Bag", Price=19.99m, Description="Handwoven organic cotton tote bag.", Image="https://images.pexels.com/photos/6347888/pexels-photo-6347888.jpeg?auto=compress&cs=tinysrgb&w=800", Category="Bags", EcoFeatures="Organic Cotton,Fair Trade,Reusable", InStock=true },

                new Product { Name="Recycled Glass Candle", Price=24.99m, Description="Hand-poured soy wax candle.", Image="https://images.pexels.com/photos/6510276/pexels-photo-6510276.jpeg?auto=compress&cs=tinysrgb&w=800", Category="Home", EcoFeatures="Recycled Materials,Natural Ingredients,Reusable Container", InStock=true },

                new Product { Name="Bamboo Cutlery Set", Price=15.99m, Description="Travel-friendly bamboo cutlery set.", Image="https://images.pexels.com/photos/4498145/pexels-photo-4498145.jpeg?auto=compress&cs=tinysrgb&w=800", Category="Kitchen", EcoFeatures="Biodegradable,Reusable,Plastic-Free", InStock=true },

                new Product { Name="Solar Power Bank", Price=49.99m, Description="Portable solar charger.", Image="https://images.pexels.com/photos/5082579/pexels-photo-5082579.jpeg?auto=compress&cs=tinysrgb&w=800", Category="Electronics", EcoFeatures="Solar Powered,Recyclable,Energy Efficient", InStock=true },

                new Product { Name="Hemp Yoga Mat", Price=69.99m, Description="Natural hemp yoga mat.", Image="https://images.pexels.com/photos/4498293/pexels-photo-4498293.jpeg?auto=compress&cs=tinysrgb&w=800", Category="Fitness", EcoFeatures="Natural Materials,Biodegradable,Non-Toxic", InStock=true },

                new Product { Name="Beeswax Food Wraps", Price=18.99m, Description="Organic cotton wraps.", Image="https://images.pexels.com/photos/4498142/pexels-photo-4498142.jpeg?auto=compress&cs=tinysrgb&w=800", Category="Kitchen", EcoFeatures="Reusable,Compostable,Zero Waste", InStock=true },

                new Product { Name="Recycled Notebook", Price=12.99m, Description="Hardcover recycled notebook.", Image="https://images.pexels.com/photos/4498114/pexels-photo-4498114.jpeg?auto=compress&cs=tinysrgb&w=800", Category="Stationery", EcoFeatures="Recycled Paper,Tree-Free,Sustainable", InStock=true }
            };

            context.Set<Product>().AddRange(products);
            context.SaveChanges();
        }
    }
}