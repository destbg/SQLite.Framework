using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Serialization;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.JsonB;
using SQLite.Framework.Sample.DTOs;
using SQLite.Framework.Sample.Models;

namespace SQLite.Framework.Sample;

/// <summary>
/// SQLite.Framework demo showing various ORM features and LINQ queries.
/// </summary>
public static partial class Program
{
    private static async Task Main()
    {
        Console.WriteLine("=== SQLite.Framework Sample ===\n");

        if (File.Exists("sample.db"))
        {
            File.Delete("sample.db");
        }

        // Initialize database
        SQLiteOptions options = new SQLiteOptionsBuilder("sample.db").Build();
        using SQLiteDatabase db = new(options);

        // Setup database schema
        SetupDatabase(db);

        // Seed sample data
        SeedData(db);

        // === BASIC QUERIES ===
        Console.WriteLine("\n=== 1. BASIC QUERIES ===");
        BasicQueries(db);

        // === WHERE CLAUSES ===
        Console.WriteLine("\n=== 2. WHERE CLAUSES (Filtering) ===");
        WhereClauseExamples(db);

        // === JOIN QUERIES ===
        Console.WriteLine("\n=== 3. JOIN QUERIES ===");
        JoinExamples(db);

        // === COMPLEX PROJECTIONS ===
        Console.WriteLine("\n=== 4. COMPLEX PROJECTIONS (Select & DTOs) ===");
        ComplexProjections(db);

        // === AGGREGATION & GROUP BY ===
        Console.WriteLine("\n=== 5. AGGREGATION & GROUP BY ===");
        AggregationExamples(db);

        // === ORDERING & PAGINATION ===
        Console.WriteLine("\n=== 6. ORDERING & PAGINATION ===");
        OrderingAndPagination(db);

        // === SUBQUERIES ===
        Console.WriteLine("\n=== 7. SUBQUERIES ===");
        SubqueryExamples(db);

        // === DATA MODIFICATION ===
        Console.WriteLine("\n=== 8. DATA MODIFICATION ===");
        DataModification(db);

        // === ASYNC OPERATIONS ===
        Console.WriteLine("\n=== 9. ASYNC OPERATIONS ===");
        await AsyncOperations(db);

        // === ADVANCED QUERIES ===
        Console.WriteLine("\n=== 10. ADVANCED QUERIES ===");
        AdvancedQueries(db);

        // === TRANSACTIONS ===
        Console.WriteLine("\n=== 11. TRANSACTIONS ===");
        TransactionExamples(db);

        // === CUSTOM TYPE CONVERTER (operator + without IAdditionOperators) ===
        Console.WriteLine("\n=== 12. CUSTOM TYPE CONVERTER (Points + Points) ===");
        CustomTypeConverterExample();

        // === JSONB LIST QUERIES ===
        Console.WriteLine("\n=== 13. JSONB LIST QUERIES ===");
        JsonBListDemo();

        // === GROUP BY TO DICTIONARY ===
        Console.WriteLine("\n=== 14. GROUP BY TO DICTIONARY ===");
        await GroupByToDictionaryDemo(db);

        Console.WriteLine("\n=== Sample completed successfully! ===");
    }

    private static void SetupDatabase(SQLiteDatabase db)
    {
        Console.WriteLine("Creating database schema...");

        db.Schema.CreateTable<Category>();
        db.Schema.CreateTable<Product>();
        db.Schema.CreateTable<Customer>();
        db.Schema.CreateTable<Order>();
        db.Schema.CreateTable<OrderItem>();
        db.Schema.CreateTable<Review>();

        Console.WriteLine("Database schema created");
    }

    private static void SeedData(SQLiteDatabase db)
    {
        Console.WriteLine("Seeding data...");

        // Categories
        Category[] categories = new[]
        {
            new Category
            {
                Name = "Electronics",
                Description = "Electronic devices and accessories"
            },
            new Category
            {
                Name = "Books",
                Description = "Physical and digital books"
            },
            new Category
            {
                Name = "Clothing",
                Description = "Apparel and fashion items"
            },
            new Category
            {
                Name = "Home & Garden",
                Description = "Home improvement and garden supplies"
            },
            new Category
            {
                Name = "Sports",
                Description = "Sports equipment and gear"
            }
        };
        db.Table<Category>().AddRange(categories);

        // Products
        Product[] products = new[]
        {
            new Product
            {
                Name = "Laptop Pro",
                Description = "High-performance laptop",
                Price = 1299.99m,
                CategoryId = 1,
                Stock = 15,
                CreatedAt = DateTime.Now.AddDays(-30),
                IsActive = true
            },
            new Product
            {
                Name = "Wireless Mouse",
                Description = "Ergonomic wireless mouse",
                Price = 29.99m,
                CategoryId = 1,
                Stock = 50,
                CreatedAt = DateTime.Now.AddDays(-25),
                IsActive = true
            },
            new Product
            {
                Name = "USB-C Cable",
                Description = "Fast charging cable",
                Price = 12.99m,
                CategoryId = 1,
                Stock = 100,
                CreatedAt = DateTime.Now.AddDays(-20),
                IsActive = true
            },
            new Product
            {
                Name = "Programming in C#",
                Description = "Comprehensive C# guide",
                Price = 49.99m,
                CategoryId = 2,
                Stock = 30,
                CreatedAt = DateTime.Now.AddDays(-15),
                IsActive = true
            },
            new Product
            {
                Name = "Database Design",
                Description = "Learn database architecture",
                Price = 54.99m,
                CategoryId = 2,
                Stock = 25,
                CreatedAt = DateTime.Now.AddDays(-10),
                IsActive = true
            },
            new Product
            {
                Name = "T-Shirt",
                Description = "Cotton casual t-shirt",
                Price = 19.99m,
                CategoryId = 3,
                Stock = 200,
                CreatedAt = DateTime.Now.AddDays(-5),
                IsActive = true
            },
            new Product
            {
                Name = "Jeans",
                Description = "Denim jeans",
                Price = 59.99m,
                CategoryId = 3,
                Stock = 75,
                CreatedAt = DateTime.Now.AddDays(-3),
                IsActive = true
            },
            new Product
            {
                Name = "Garden Tools Set",
                Description = "Complete gardening kit",
                Price = 89.99m,
                CategoryId = 4,
                Stock = 20,
                CreatedAt = DateTime.Now.AddDays(-2),
                IsActive = true
            },
            new Product
            {
                Name = "Running Shoes",
                Description = "Professional running shoes",
                Price = 119.99m,
                CategoryId = 5,
                Stock = 40,
                CreatedAt = DateTime.Now.AddDays(-1),
                IsActive = true
            },
            new Product
            {
                Name = "Yoga Mat",
                Description = "Non-slip yoga mat",
                Price = 34.99m,
                CategoryId = 5,
                Stock = 60,
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Product
            {
                Name = "Old Product",
                Description = "Discontinued item",
                Price = 9.99m,
                CategoryId = 1,
                Stock = 0,
                CreatedAt = DateTime.Now.AddDays(-100),
                IsActive = false
            }
        };
        db.Table<Product>().AddRange(products);

        // Customers
        Customer[] customers = new[]
        {
            new Customer
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@email.com",
                Phone = "555-1001",
                BirthDate = new DateTime(1985, 3, 15),
                RegisteredAt = DateTime.Now.AddDays(-60),
                LastLoginAt = DateTime.Now.AddHours(-2)
            },
            new Customer
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@email.com",
                Phone = "555-1002",
                BirthDate = new DateTime(1990, 7, 22),
                RegisteredAt = DateTime.Now.AddDays(-50),
                LastLoginAt = DateTime.Now.AddHours(-5)
            },
            new Customer
            {
                FirstName = "Bob",
                LastName = "Johnson",
                Email = "bob.johnson@email.com",
                Phone = "555-1003",
                BirthDate = new DateTime(1988, 11, 8),
                RegisteredAt = DateTime.Now.AddDays(-45),
                LastLoginAt = DateTime.Now.AddDays(-1)
            },
            new Customer
            {
                FirstName = "Alice",
                LastName = "Williams",
                Email = "alice.williams@email.com",
                Phone = "555-1004",
                BirthDate = new DateTime(1992, 5, 30),
                RegisteredAt = DateTime.Now.AddDays(-40),
                LastLoginAt = DateTime.Now.AddHours(-10)
            },
            new Customer
            {
                FirstName = "Charlie",
                LastName = "Brown",
                Email = "charlie.brown@email.com",
                Phone = null,
                BirthDate = new DateTime(1995, 9, 12),
                RegisteredAt = DateTime.Now.AddDays(-30),
                LastLoginAt = null
            }
        };
        db.Table<Customer>().AddRange(customers);

        // Orders
        Order[] orders = new[]
        {
            new Order
            {
                CustomerId = 1,
                OrderDate = DateTime.Now.AddDays(-10),
                TotalAmount = 1329.98m,
                Status = OrderStatus.Delivered,
                ShippingAddress = "123 Main St"
            },
            new Order
            {
                CustomerId = 1,
                OrderDate = DateTime.Now.AddDays(-5),
                TotalAmount = 49.99m,
                Status = OrderStatus.Delivered,
                ShippingAddress = "123 Main St"
            },
            new Order
            {
                CustomerId = 2,
                OrderDate = DateTime.Now.AddDays(-8),
                TotalAmount = 139.97m,
                Status = OrderStatus.Shipped,
                ShippingAddress = "456 Oak Ave"
            },
            new Order
            {
                CustomerId = 2,
                OrderDate = DateTime.Now.AddDays(-2),
                TotalAmount = 54.99m,
                Status = OrderStatus.Processing,
                ShippingAddress = "456 Oak Ave"
            },
            new Order
            {
                CustomerId = 3,
                OrderDate = DateTime.Now.AddDays(-7),
                TotalAmount = 209.97m,
                Status = OrderStatus.Delivered,
                ShippingAddress = "789 Pine Rd"
            },
            new Order
            {
                CustomerId = 4,
                OrderDate = DateTime.Now.AddDays(-3),
                TotalAmount = 34.99m,
                Status = OrderStatus.Processing,
                ShippingAddress = "321 Elm St"
            },
            new Order
            {
                CustomerId = 5,
                OrderDate = DateTime.Now.AddDays(-1),
                TotalAmount = 89.99m,
                Status = OrderStatus.Pending,
                ShippingAddress = "654 Maple Dr"
            }
        };
        db.Table<Order>().AddRange(orders);

        // Order Items
        OrderItem[] orderItems = new[]
        {
            new OrderItem
            {
                OrderId = 1,
                ProductId = 1,
                Quantity = 1,
                UnitPrice = 1299.99m,
                Discount = null
            },
            new OrderItem
            {
                OrderId = 1,
                ProductId = 2,
                Quantity = 1,
                UnitPrice = 29.99m,
                Discount = null
            },
            new OrderItem
            {
                OrderId = 2,
                ProductId = 4,
                Quantity = 1,
                UnitPrice = 49.99m,
                Discount = null
            },
            new OrderItem
            {
                OrderId = 3,
                ProductId = 2,
                Quantity = 2,
                UnitPrice = 29.99m,
                Discount = 5.00m
            },
            new OrderItem
            {
                OrderId = 3,
                ProductId = 3,
                Quantity = 4,
                UnitPrice = 12.99m,
                Discount = null
            },
            new OrderItem
            {
                OrderId = 3,
                ProductId = 6,
                Quantity = 2,
                UnitPrice = 19.99m,
                Discount = null
            },
            new OrderItem
            {
                OrderId = 4,
                ProductId = 5,
                Quantity = 1,
                UnitPrice = 54.99m,
                Discount = null
            },
            new OrderItem
            {
                OrderId = 5,
                ProductId = 9,
                Quantity = 1,
                UnitPrice = 119.99m,
                Discount = 10.00m
            },
            new OrderItem
            {
                OrderId = 5,
                ProductId = 10,
                Quantity = 1,
                UnitPrice = 34.99m,
                Discount = null
            },
            new OrderItem
            {
                OrderId = 5,
                ProductId = 7,
                Quantity = 1,
                UnitPrice = 59.99m,
                Discount = 5.00m
            },
            new OrderItem
            {
                OrderId = 6,
                ProductId = 10,
                Quantity = 1,
                UnitPrice = 34.99m,
                Discount = null
            },
            new OrderItem
            {
                OrderId = 7,
                ProductId = 8,
                Quantity = 1,
                UnitPrice = 89.99m,
                Discount = null
            }
        };
        db.Table<OrderItem>().AddRange(orderItems);

        // Reviews
        Review[] reviews = new[]
        {
            new Review
            {
                ProductId = 1,
                CustomerId = 1,
                Rating = 5,
                Comment = "Excellent laptop! Very fast and reliable.",
                CreatedAt = DateTime.Now.AddDays(-5)
            },
            new Review
            {
                ProductId = 1,
                CustomerId = 3,
                Rating = 4,
                Comment = "Good quality but a bit expensive.",
                CreatedAt = DateTime.Now.AddDays(-3)
            },
            new Review
            {
                ProductId = 2,
                CustomerId = 1,
                Rating = 5,
                Comment = "Perfect wireless mouse, very comfortable.",
                CreatedAt = DateTime.Now.AddDays(-4)
            },
            new Review
            {
                ProductId = 2,
                CustomerId = 2,
                Rating = 4,
                Comment = "Works well but battery could last longer.",
                CreatedAt = DateTime.Now.AddDays(-2)
            },
            new Review
            {
                ProductId = 4,
                CustomerId = 1,
                Rating = 5,
                Comment = "Great book for learning C#!",
                CreatedAt = DateTime.Now.AddDays(-2)
            },
            new Review
            {
                ProductId = 9,
                CustomerId = 3,
                Rating = 5,
                Comment = "Best running shoes I've ever owned!",
                CreatedAt = DateTime.Now.AddDays(-1)
            },
            new Review
            {
                ProductId = 10,
                CustomerId = 4,
                Rating = 4,
                Comment = "Good yoga mat, non-slip as advertised.",
                CreatedAt = DateTime.Now.AddHours(-12)
            }
        };
        db.Table<Review>().AddRange(reviews);

        Console.WriteLine("Sample data seeded");
    }

    private static void BasicQueries(SQLiteDatabase db)
    {
        // Simple select all
        List<Product> allProducts = db.Table<Product>().ToList();
        Console.WriteLine($"Total products: {allProducts.Count}");

        // Select specific columns
        List<string> productNames = db.Table<Product>()
            .Select(p => p.Name)
            .ToList();
        Console.WriteLine($"Product names count: {productNames.Count}");

        // Count
        int activeProductCount = db.Table<Product>()
            .Count(p => p.IsActive);
        Console.WriteLine($"Active products: {activeProductCount}");

        // First/FirstOrDefault
        Product firstProduct = db.Table<Product>().First();
        Console.WriteLine($"First product: {firstProduct.Name}");

        // Single with condition
        Category electronicsCategory = db.Table<Category>()
            .Single(c => c.Name == "Electronics");
        Console.WriteLine($"Electronics category ID: {electronicsCategory.Id}");
    }

    private static void WhereClauseExamples(SQLiteDatabase db)
    {
        // Equality
        List<Product> electronicsProducts = db.Table<Product>()
            .Where(p => p.CategoryId == 1)
            .ToList();
        Console.WriteLine($"Electronics products: {electronicsProducts.Count}");

        // Comparison operators
        List<Product> expensiveProducts = db.Table<Product>()
            .Where(p => p.Price > 50)
            .ToList();
        Console.WriteLine($"Products over $50: {expensiveProducts.Count}");

        // Multiple conditions (AND)
        List<Product> activeExpensiveProducts = db.Table<Product>()
            .Where(p => p.IsActive && p.Price > 100)
            .ToList();
        Console.WriteLine($"Active expensive products: {activeExpensiveProducts.Count}");

        // OR condition
        List<Product> specificCategories = db.Table<Product>()
            .Where(p => p.CategoryId == 1 || p.CategoryId == 2)
            .ToList();
        Console.WriteLine($"Electronics or Books: {specificCategories.Count}");

        // Null checks
        List<Customer> customersWithPhone = db.Table<Customer>()
            .Where(c => c.Phone != null)
            .ToList();
        Console.WriteLine($"Customers with phone: {customersWithPhone.Count}");

        // String operations
        List<Product> proProducts = db.Table<Product>()
            .Where(p => p.Name.Contains("Pro"))
            .ToList();
        Console.WriteLine($"Products with 'Pro' in name: {proProducts.Count}");

        // List.Contains (IN clause)
        List<int> categoryIds = new()
        {
            1,
            2,
            3
        };
        List<Product> productsInCategories = db.Table<Product>()
            .Where(p => categoryIds.Contains(p.CategoryId))
            .ToList();
        Console.WriteLine($"Products in selected categories: {productsInCategories.Count}");

        // NOT condition
        List<Product> notElectronics = db.Table<Product>()
            .Where(p => !(p.CategoryId == 1))
            .ToList();
        Console.WriteLine($"Non-electronics products: {notElectronics.Count}");

        // Complex nested conditions
        List<Product> complexQuery = db.Table<Product>()
            .Where(p => (p.CategoryId == 1 && p.Price > 50) ||
                        (p.CategoryId == 2 && p.Stock > 20))
            .ToList();
        Console.WriteLine($"Complex condition results: {complexQuery.Count}");

        // Null coalescing
        List<Order> ordersWithNotes = db.Table<Order>()
            .Where(o => (o.Notes ?? "").Length > 0)
            .ToList();
        Console.WriteLine($"Orders with notes: {ordersWithNotes.Count}");
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "All types used in expression trees are referenced directly and will not be trimmed.")]
    private static void JoinExamples(SQLiteDatabase db)
    {
        // INNER JOIN (using join keyword)
        var productsWithCategories = (
            from product in db.Table<Product>()
            join category in db.Table<Category>() on product.CategoryId equals category.Id
            where product.IsActive
            select new
            {
                Product = product.Name,
                Category = category.Name
            }
        ).ToList();
        Console.WriteLine($"Products with categories (INNER JOIN): {productsWithCategories.Count}");

        // LEFT JOIN
        var allCategoriesWithProductCount = (
            from category in db.Table<Category>()
            join product in db.Table<Product>() on category.Id equals product.CategoryId into productGroup
            from product in productGroup.DefaultIfEmpty()
            select new
            {
                Category = category.Name,
                HasProducts = product != null
            }
        ).ToList();
        Console.WriteLine($"Categories (LEFT JOIN): {allCategoriesWithProductCount.Count}");

        // Multiple JOINs
        var orderDetails = (
            from order in db.Table<Order>()
            join customer in db.Table<Customer>() on order.CustomerId equals customer.Id
            join orderItem in db.Table<OrderItem>() on order.Id equals orderItem.OrderId
            join product in db.Table<Product>() on orderItem.ProductId equals product.Id
            where order.Status == OrderStatus.Delivered
            select new
            {
                CustomerName = customer.FirstName + " " + customer.LastName,
                ProductName = product.Name,
                order.OrderDate
            }
        ).ToList();
        Console.WriteLine($"Delivered order details: {orderDetails.Count}");

        // CROSS JOIN
        var cartesianProduct = (
            from category in db.Table<Category>()
            from product in db.Table<Product>()
            where category.Id == 1 && product.CategoryId == 2
            select new
            {
                category.Name,
                ProductName = product.Name
            }
        ).ToList();
        Console.WriteLine($"Cross join results: {cartesianProduct.Count}");

        // Complex join condition
        List<Customer> complexJoin = (
            from order in db.Table<Order>()
            join customer in db.Table<Customer>() on new
            {
                Id = order.CustomerId,
                Active = true
            }
                equals new
                {
                    customer.Id,
                    Active = customer.LastLoginAt != null
                }
            select customer
        ).ToList();
        Console.WriteLine($"Complex join results: {complexJoin.Count}");
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "All types used in expression trees are referenced directly and will not be trimmed.")]
    private static void ComplexProjections(SQLiteDatabase db)
    {
        // Project to DTO with nested object
        List<ProductDTO> productsWithCategory = (
            from product in db.Table<Product>()
            join category in db.Table<Category>() on product.CategoryId equals category.Id
            where product.IsActive
            select new ProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                IsActive = product.IsActive,
                Category = new CategoryDTO
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description
                }
            }
        ).ToList();
        Console.WriteLine($"Products with nested category DTOs: {productsWithCategory.Count}");
        if (productsWithCategory.Count != 0)
        {
            ProductDTO first = productsWithCategory.First();
            Console.WriteLine($"  Example: {first.Name} - {first.Category.Name}");
        }

        // Calculations in projection
        List<CustomerDTO> customerAges = (
            from customer in db.Table<Customer>()
            where customer.BirthDate.HasValue
            select new CustomerDTO
            {
                Id = customer.Id,
                FullName = customer.FirstName + " " + customer.LastName,
                Email = customer.Email,
                Phone = customer.Phone,
                Age = customer.BirthDate.HasValue ? DateTime.Now.Year - customer.BirthDate.Value.Year : null
            }
        ).ToList();
        Console.WriteLine($"Customers with calculated age: {customerAges.Count}");

        // Anonymous type projection
        var orderSummary = (
            from order in db.Table<Order>()
            join customer in db.Table<Customer>() on order.CustomerId equals customer.Id
            select new
            {
                OrderId = order.Id,
                CustomerName = customer.FirstName + " " + customer.LastName,
                FormattedDate = order.OrderDate.ToString(CultureInfo.InvariantCulture),
                TotalWithTax = order.TotalAmount * 1.1m,
                StatusText = order.Status.ToString()
            }
        ).ToList();
        Console.WriteLine($"Order summaries with calculations: {orderSummary.Count}");

        // DISTINCT projection
        List<int> uniqueCategories = db.Table<Product>()
            .Select(p => p.CategoryId)
            .Distinct()
            .ToList();
        Console.WriteLine($"Unique categories in products: {uniqueCategories.Count}");

        // Multiple chained selects
        var transformedData = db.Table<Product>()
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Price
            })
            .Select(p => new
            {
                p.Id,
                UpperName = p.Name.ToUpper()
            })
            .ToList();
        Console.WriteLine($"Chained select transformations: {transformedData.Count}");
    }

    private static void AggregationExamples(SQLiteDatabase db)
    {
        // Simple aggregates
        int totalProducts = db.Table<Product>().Count();
        decimal averagePrice = db.Table<Product>().Average(p => p.Price);
        decimal maxPrice = db.Table<Product>().Max(p => p.Price);
        decimal minPrice = db.Table<Product>().Min(p => p.Price);
        decimal totalValue = db.Table<Product>().Sum(p => p.Price * p.Stock);

        Console.WriteLine($"Total products: {totalProducts}");
        Console.WriteLine($"Average price: ${averagePrice:F2}");
        Console.WriteLine($"Max price: ${maxPrice:F2}");
        Console.WriteLine($"Min price: ${minPrice:F2}");
        Console.WriteLine($"Total inventory value: ${totalValue:F2}");

        // GROUP BY with Count
        var productsByCategory = (
            from product in db.Table<Product>()
            group product by product.CategoryId
            into g
            select new
            {
                CategoryId = g.Key,
                ProductCount = g.Count(),
                TotalStock = g.Sum(p => p.Stock)
            }
        ).ToList();
        Console.WriteLine($"Products grouped by category: {productsByCategory.Count} categories");
        foreach (var group in productsByCategory)
        {
            Console.WriteLine($"  Category {group.CategoryId}: {group.ProductCount} products, {group.TotalStock} items in stock");
        }

        // GROUP BY with HAVING
        var popularCategories = (
            from product in db.Table<Product>()
            group product by product.CategoryId
            into g
            where g.Count() > 2
            select new
            {
                CategoryId = g.Key,
                Count = g.Count()
            }
        ).ToList();
        Console.WriteLine($"Categories with more than 2 products: {popularCategories.Count}");

        // GROUP BY with multiple aggregates
        var categoryStatistics = (
            from product in db.Table<Product>()
            where product.IsActive
            group product by product.CategoryId
            into g
            select new
            {
                CategoryId = g.Key,
                Count = g.Count(),
                AvgPrice = g.Average(p => p.Price),
                MaxPrice = g.Max(p => p.Price),
                MinPrice = g.Min(p => p.Price),
                TotalStock = g.Sum(p => p.Stock)
            }
        ).ToList();
        Console.WriteLine($"Category statistics: {categoryStatistics.Count}");

        // Aggregate with complex expression
        var orderItemSummary = (
            from item in db.Table<OrderItem>()
            group item by item.OrderId
            into g
            select new
            {
                OrderId = g.Key,
                TotalItems = g.Sum(i => i.Quantity),
                SubTotal = g.Sum(i => i.UnitPrice * i.Quantity),
                TotalDiscount = g.Sum(i => i.Discount ?? 0)
            }
        ).ToList();
        Console.WriteLine($"Order item summaries: {orderItemSummary.Count}");

        // GROUP BY with JOIN
        var reviewsByProduct = (
            from review in db.Table<Review>()
            join product in db.Table<Product>() on review.ProductId equals product.Id
            group review by new
            {
                product.Id,
                product.Name
            }
            into g
            select new
            {
                ProductName = g.Key.Name,
                ReviewCount = g.Count(),
                AverageRating = g.Average(r => r.Rating)
            }
        ).ToList();
        Console.WriteLine($"Product review statistics: {reviewsByProduct.Count}");
        foreach (var item in reviewsByProduct.Take(3))
        {
            Console.WriteLine($"  {item.ProductName}: {item.ReviewCount} reviews, avg rating {item.AverageRating:F1}");
        }
    }

    private static void OrderingAndPagination(SQLiteDatabase db)
    {
        // Simple ordering
        List<Product> productsByPrice = db.Table<Product>()
            .OrderBy(p => p.Price)
            .ToList();
        Console.WriteLine($"Products ordered by price (ascending): {productsByPrice.Count}");

        // Descending order
        List<Product> productsByPriceDesc = db.Table<Product>()
            .OrderByDescending(p => p.Price)
            .ToList();
        Console.WriteLine($"Most expensive product: {productsByPriceDesc.First().Name} - ${productsByPriceDesc.First().Price}");

        // Multiple order by
        List<Product> orderedProducts = db.Table<Product>()
            .OrderBy(p => p.CategoryId)
            .ThenByDescending(p => p.Price)
            .ThenBy(p => p.Name)
            .ToList();
        Console.WriteLine($"Products ordered by category, then price (desc), then name: {orderedProducts.Count}");

        // Pagination - Skip and Take
        int pageSize = 5;
        List<Product> page1 = db.Table<Product>()
            .OrderBy(p => p.Id)
            .Take(pageSize)
            .ToList();
        Console.WriteLine($"First page ({pageSize} items): {page1.Count}");

        List<Product> page2 = db.Table<Product>()
            .OrderBy(p => p.Id)
            .Skip(pageSize)
            .Take(pageSize)
            .ToList();
        Console.WriteLine($"Second page ({pageSize} items): {page2.Count}");

        // Top N results
        var top3ExpensiveProducts = db.Table<Product>()
            .OrderByDescending(p => p.Price)
            .Take(3)
            .Select(p => new
            {
                p.Name,
                p.Price
            })
            .ToList();
        Console.WriteLine("Top 3 expensive products:");
        foreach (var p in top3ExpensiveProducts)
        {
            Console.WriteLine($"  {p.Name}: ${p.Price}");
        }

        // Last N (using Skip)
        int totalCount = db.Table<Product>().Count();
        List<Product> lastProducts = db.Table<Product>()
            .OrderBy(p => p.Id)
            .Skip(Math.Max(0, totalCount - 3))
            .ToList();
        Console.WriteLine($"Last 3 products: {lastProducts.Count}");
    }

    private static void SubqueryExamples(SQLiteDatabase db)
    {
        // Subquery in WHERE with Contains
        List<Customer> customersWithOrders = (
            from customer in db.Table<Customer>()
            where (
                from order in db.Table<Order>()
                select order.CustomerId
            ).Contains(customer.Id)
            select customer
        ).ToList();
        Console.WriteLine($"Customers with orders: {customersWithOrders.Count}");

        // Subquery for filtering
        List<Product> productsWithReviews = (
            from product in db.Table<Product>()
            where (
                from review in db.Table<Review>()
                where review.ProductId == product.Id
                select review.Id
            ).Any()
            select product
        ).ToList();
        Console.WriteLine($"Products with reviews: {productsWithReviews.Count}");

        // Subquery in JOIN
        var highRatedProducts = (
            from product in db.Table<Product>()
            join avgRating in from review in db.Table<Review>()
                              group review by review.ProductId
                into g
                              select new
                              {
                                  ProductId = g.Key,
                                  AvgRating = g.Average(r => r.Rating)
                              } on product.Id equals avgRating.ProductId
            where avgRating.AvgRating >= 4.5
            select new
            {
                product.Name,
                avgRating.AvgRating
            }
        ).ToList();
        Console.WriteLine($"High-rated products (4.5+): {highRatedProducts.Count}");

        // Max value from subquery
        decimal maxPrice = db.Table<Product>().Max(p => p.Price);
        Product? mostExpensiveProduct = db.Table<Product>()
            .FirstOrDefault(p => p.Price == maxPrice);
        Console.WriteLine($"Most expensive product: {mostExpensiveProduct?.Name} - ${mostExpensiveProduct?.Price}");
    }

    private static void DataModification(SQLiteDatabase db)
    {
        Console.WriteLine("Testing data modification operations...");

        // INSERT - Add new product
        Product newProduct = new()
        {
            Name = "Test Product",
            Description = "A test product",
            Price = 99.99m,
            CategoryId = 1,
            Stock = 10,
            CreatedAt = DateTime.Now,
            IsActive = true
        };
        db.Table<Product>().Add(newProduct);
        Console.WriteLine($"Added new product: {newProduct.Name}");

        // UPDATE using ExecuteUpdate
        db.Table<Product>()
            .Where(p => p.Name == "Test Product")
            .ExecuteUpdate(s => s
                .Set(p => p.Price, 89.99m)
                .Set(p => p.Stock, p => p.Stock + 5)
            );
        Console.WriteLine("Updated product price and stock");

        // UPDATE using Update method
        Product? productToUpdate = db.Table<Product>()
            .FirstOrDefault(p => p.Name == "Test Product");
        if (productToUpdate != null)
        {
            productToUpdate.Description = "Updated description";
            productToUpdate.UpdatedAt = DateTime.Now;
            db.Table<Product>().Update(productToUpdate);
            Console.WriteLine("✓ Updated product using Update method");
        }

        // DELETE using ExecuteDelete
        db.Table<Product>()
            .Where(p => p.Name == "Test Product")
            .ExecuteDelete();
        Console.WriteLine("Deleted test product");

        // Batch INSERT
        Product[] testProducts = new[]
        {
            new Product
            {
                Name = "Batch Product 1",
                Price = 10m,
                CategoryId = 1,
                Stock = 5,
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Product
            {
                Name = "Batch Product 2",
                Price = 20m,
                CategoryId = 1,
                Stock = 5,
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Product
            {
                Name = "Batch Product 3",
                Price = 30m,
                CategoryId = 1,
                Stock = 5,
                CreatedAt = DateTime.Now,
                IsActive = true
            }
        };
        db.Table<Product>().AddRange(testProducts);
        Console.WriteLine($"Batch inserted {testProducts.Length} products");

        // Batch DELETE using Remove
        db.Table<Product>().RemoveRange(testProducts);
        Console.WriteLine($"Batch deleted {testProducts.Length} products");

        // Clear all from test (commented to preserve data)
        // db.Table<Product>().Clear();
        // Console.WriteLine("Cleared all products");
    }

    private static async Task AsyncOperations(SQLiteDatabase db)
    {
        Console.WriteLine("Testing async operations...");

        // Async query
        List<Product> products = await db.Table<Product>()
            .Where(p => p.IsActive)
            .ToListAsync();
        Console.WriteLine($"Async query returned {products.Count} products");

        // Async FirstOrDefault
        Product? firstProduct = await db.Table<Product>()
            .FirstOrDefaultAsync(p => p.CategoryId == 1);
        Console.WriteLine($"Async FirstOrDefault: {firstProduct?.Name ?? "None"}");

        // Async Single
        Category electronicsCategory = await db.Table<Category>()
            .SingleAsync(c => c.Name == "Electronics");
        Console.WriteLine($"Async Single: {electronicsCategory.Name}");

        // Async Count
        int productCount = await db.Table<Product>()
            .CountAsync(p => p.IsActive);
        Console.WriteLine($"Async Count: {productCount}");

        // Async Any
        bool hasExpensiveProducts = await db.Table<Product>()
            .AnyAsync(p => p.Price > 1000);
        Console.WriteLine($"Async Any (expensive products): {hasExpensiveProducts}");

        // Async All
        bool allActive = await db.Table<Product>()
            .AllAsync(p => p.Stock >= 0);
        Console.WriteLine($"Async All (non-negative stock): {allActive}");

        // Async aggregates
        decimal avgPrice = await db.Table<Product>()
            .AverageAsync(p => p.Price);
        decimal maxPrice = await db.Table<Product>()
            .MaxAsync(p => p.Price);
        decimal minPrice = await db.Table<Product>()
            .MinAsync(p => p.Price);
        decimal totalValue = await db.Table<Product>()
            .SumAsync(p => p.Price);

        Console.WriteLine($"Async aggregates - Avg: ${avgPrice:F2}, Max: ${maxPrice:F2}, Min: ${minPrice:F2}, Sum: ${totalValue:F2}");

        // Async Contains
        bool containsId = await db.Table<Product>()
            .Select(p => p.Id)
            .ContainsAsync(1);
        Console.WriteLine($"Async Contains: {containsId}");

        // Async complex query with JOIN
        List<ProductDTO> productsList = await (
            from product in db.Table<Product>()
            join category in db.Table<Category>() on product.CategoryId equals category.Id
            where product.IsActive && product.Price > 50
            orderby product.Price descending
            select new ProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                IsActive = product.IsActive,
                Category = new CategoryDTO
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description
                }
            }
        ).ToListAsync();
        Console.WriteLine($"Async complex query with JOIN: {productsList.Count} results");
    }

    private static void AdvancedQueries(SQLiteDatabase db)
    {
        // Complex business query: Customer order statistics
        var customerOrderStats = (
            from customer in db.Table<Customer>()
            join order in db.Table<Order>() on customer.Id equals order.CustomerId into orderGroup
            from order in orderGroup.DefaultIfEmpty()
            group order by new
            {
                customer.Id,
                customer.FirstName,
                customer.LastName
            }
            into g
            select new
            {
                CustomerId = g.Key.Id,
                CustomerName = g.Key.FirstName + " " + g.Key.LastName,
                OrderCount = g.Count(o => o != null),
                TotalSpent = g.Sum(o => o != null ? o.TotalAmount : 0),
                AvgOrderValue = g.Average(o => o != null ? o.TotalAmount : 0)
            }
        ).OrderByDescending(x => x.TotalSpent).ToList();

        Console.WriteLine("Customer order statistics:");
        foreach (var stat in customerOrderStats.Take(3))
        {
            Console.WriteLine($"  {stat.CustomerName}: {stat.OrderCount} orders, ${stat.TotalSpent:F2} total, ${stat.AvgOrderValue:F2} avg");
        }

        // Product popularity (by order count)
        var productPopularity = (
            from product in db.Table<Product>()
            join orderItem in db.Table<OrderItem>() on product.Id equals orderItem.ProductId into itemGroup
            from orderItem in itemGroup.DefaultIfEmpty()
            group orderItem by new
            {
                product.Id,
                product.Name
            }
            into g
            select new
            {
                ProductName = g.Key.Name,
                OrderCount = g.Count(i => i != null),
                TotalQuantitySold = g.Sum(i => i != null ? i.Quantity : 0)
            }
        ).OrderByDescending(x => x.TotalQuantitySold).ToList();

        Console.WriteLine("\nProduct popularity (top 5):");
        foreach (var item in productPopularity.Take(5))
        {
            Console.WriteLine($"  {item.ProductName}: {item.TotalQuantitySold} units sold in {item.OrderCount} orders");
        }

        // Orders with full details
        List<OrderDTO> orderDetails = (
            from order in db.Table<Order>()
            join customer in db.Table<Customer>() on order.CustomerId equals customer.Id
            select new OrderDTO
            {
                Id = order.Id,
                Customer = new CustomerDTO
                {
                    Id = customer.Id,
                    FullName = customer.FirstName + " " + customer.LastName,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    Age = customer.BirthDate.HasValue ? DateTime.Now.Year - customer.BirthDate.Value.Year : null
                },
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status
            }
        ).Where(o => o.Status == OrderStatus.Delivered).ToList();

        Console.WriteLine($"\nDelivered orders with customer details: {orderDetails.Count}");

        // Products never ordered
        var neverOrderedProducts = (
            from product in db.Table<Product>()
            where !(from orderItem in db.Table<OrderItem>()
                    select orderItem.ProductId).Contains(product.Id)
            select new
            {
                product.Name,
                product.Price,
                product.Stock
            }
        ).ToList();

        Console.WriteLine($"\nProducts never ordered: {neverOrderedProducts.Count}");

        // Recent customer activity
        var recentActivity = (
            from customer in db.Table<Customer>()
            where customer.LastLoginAt.HasValue &&
                  customer.LastLoginAt.Value > DateTime.Now.AddDays(-7)
            orderby customer.LastLoginAt descending
            select new
            {
                Name = customer.FirstName + " " + customer.LastName,
                LastLogin = customer.LastLoginAt,
                DaysSinceLogin = customer.LastLoginAt.HasValue ? (DateTime.Now - customer.LastLoginAt.Value).Days : 0
            }
        ).ToList();

        Console.WriteLine($"\nCustomers active in last 7 days: {recentActivity.Count}");

        // Conditional aggregation
        var orderStatusBreakdown = (
            from order in db.Table<Order>()
            group order by 1
            into g
            select new
            {
                TotalOrders = g.Count(),
                PendingCount = g.Count(o => o.Status == OrderStatus.Pending),
                ProcessingCount = g.Count(o => o.Status == OrderStatus.Processing),
                ShippedCount = g.Count(o => o.Status == OrderStatus.Shipped),
                DeliveredCount = g.Count(o => o.Status == OrderStatus.Delivered),
                CancelledCount = g.Count(o => o.Status == OrderStatus.Cancelled)
            }
        ).First();

        Console.WriteLine("\nOrder status breakdown:");
        Console.WriteLine($"  Total: {orderStatusBreakdown.TotalOrders}");
        Console.WriteLine($"  Pending: {orderStatusBreakdown.PendingCount}");
        Console.WriteLine($"  Processing: {orderStatusBreakdown.ProcessingCount}");
        Console.WriteLine($"  Shipped: {orderStatusBreakdown.ShippedCount}");
        Console.WriteLine($"  Delivered: {orderStatusBreakdown.DeliveredCount}");
        Console.WriteLine($"  Cancelled: {orderStatusBreakdown.CancelledCount}");
    }

    private static void TransactionExamples(SQLiteDatabase db)
    {
        Console.WriteLine("Testing transaction operations...");

        // Using transaction for multiple operations
        using (SQLiteTransaction transaction = db.BeginTransaction())
        {
            try
            {
                Category newCategory = new()
                {
                    Name = "Transaction Test Category",
                    Description = "Created in transaction"
                };
                db.Table<Category>().Add(newCategory);

                Product newProduct = new()
                {
                    Name = "Transaction Test Product",
                    Price = 50m,
                    CategoryId = newCategory.Id,
                    Stock = 10,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
                db.Table<Product>().Add(newProduct);

                transaction.Commit();
                Console.WriteLine("Transaction committed successfully");

                // Cleanup
                db.Table<Product>().Remove(newProduct);
                db.Table<Category>().Remove(newCategory);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Transaction rolled back: {ex.Message}");
            }
        }

        // Batch operations with transaction
        Product[] batchProducts = new[]
        {
            new Product
            {
                Name = "Batch 1",
                Price = 10m,
                CategoryId = 1,
                Stock = 5,
                CreatedAt = DateTime.Now,
                IsActive = true
            },
            new Product
            {
                Name = "Batch 2",
                Price = 20m,
                CategoryId = 1,
                Stock = 5,
                CreatedAt = DateTime.Now,
                IsActive = true
            }
        };

        db.Table<Product>().AddRange(batchProducts); // Runs in transaction by default
        Console.WriteLine("Batch insert in transaction");

        // Cleanup
        db.Table<Product>().RemoveRange(batchProducts);
        Console.WriteLine("Batch delete in transaction");

        // Non-transactional batch operation
        Product[] nonTransProducts = new[]
        {
            new Product
            {
                Name = "NoTrans 1",
                Price = 10m,
                CategoryId = 1,
                Stock = 5,
                CreatedAt = DateTime.Now,
                IsActive = true
            }
        };

        db.Table<Product>().AddRange(nonTransProducts, false);
        Console.WriteLine("Non-transactional insert");

        db.Table<Product>().RemoveRange(nonTransProducts, false);
        Console.WriteLine("Non-transactional delete");
    }

    private static async Task GroupByToDictionaryDemo(SQLiteDatabase db)
    {
        List<Product> activeProducts = await db.Table<Product>()
            .Where(p => p.IsActive)
            .ToListAsync();

        Dictionary<int, List<Product>> byCategory = activeProducts
            .GroupBy(p => p.CategoryId)
            .ToDictionary(g => g.Key, g => g.ToList());

        Console.WriteLine($"Products grouped by category: {byCategory.Count} categories");
        foreach (KeyValuePair<int, List<Product>> kvp in byCategory.OrderBy(kvp => kvp.Key))
        {
            Console.WriteLine($"  Category {kvp.Key}: {kvp.Value.Count} products");
        }
    }

    private static void CustomTypeConverterExample()
    {
        if (File.Exists("points.db"))
        {
            File.Delete("points.db");
        }

        SQLiteOptions options = new SQLiteOptionsBuilder("points.db")
            .AddTypeConverter<Points>(new PointsConverter())
            .Build();

        using SQLiteDatabase pointsDb = new(options);
        pointsDb.Schema.CreateTable<ScoreRecord>();

        pointsDb.Table<ScoreRecord>().Add(new ScoreRecord
        {
            Name = "Alice",
            Score = new Points(100)
        });
        pointsDb.Table<ScoreRecord>().Add(new ScoreRecord
        {
            Name = "Bob",
            Score = new Points(200)
        });

        Points bonus = new(50);

        var results = pointsDb.Table<ScoreRecord>()
            .Select(r => new
            {
                r.Name,
                Total = Testing.Pass(r.Score) + bonus
            })
            .ToList();

        foreach (var r in results)
        {
            Console.WriteLine($"  {r.Name}: {r.Total}");
        }
    }
}

file static class Testing
{
    public static Points Pass(Points record)
    {
        return record;
    }
}

[Table("ScoreRecords")]
file class ScoreRecord
{
    [AutoIncrement]
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
    public Points Score { get; set; }
}

file class PointsConverter : ISQLiteTypeConverter
{
    public SQLiteColumnType ColumnType => SQLiteColumnType.Integer;

    public object? ToDatabase(object? value)
    {
        return value is Points p ? p.Value : null;
    }

    public object FromDatabase(object? value)
    {
        return value is long l ? new Points((int)l) : new Points(0);
    }
}

public static partial class Program
{
    private static void JsonBListDemo()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(":memory:")
            .AddJson()
            .AddTypeConverter<List<string>>(new SQLiteJsonConverter<List<string>>(SampleJsonContext.Default.ListString))
            .Build();

        using SQLiteDatabase db = new(options);

        db.Schema.CreateTable<TaggedProduct>();
        db.Table<TaggedProduct>().Add(new TaggedProduct { Id = 1, Name = "Laptop", Tags = ["electronics", "computers"] });
        db.Table<TaggedProduct>().Add(new TaggedProduct { Id = 2, Name = "Novel", Tags = ["books", "fiction"] });
        db.Table<TaggedProduct>().Add(new TaggedProduct { Id = 3, Name = "Keyboard", Tags = ["electronics", "accessories"] });
        db.Table<TaggedProduct>().Add(new TaggedProduct { Id = 4, Name = "Unlisted", Tags = [] });

        List<TaggedProduct> electronics = db.Table<TaggedProduct>()
            .Where(p => p.Tags.Contains("electronics"))
            .ToList();
        Console.WriteLine($"Electronics products: {string.Join(", ", electronics.Select(p => p.Name))}");

        List<TaggedProduct> tagged = db.Table<TaggedProduct>()
            .Where(p => p.Tags.Any())
            .ToList();
        Console.WriteLine($"Products with at least one tag: {string.Join(", ", tagged.Select(p => p.Name))}");

        List<int> tagCounts = db.Table<TaggedProduct>()
            .Select(p => p.Tags.Count())
            .ToList();
        Console.WriteLine($"Tag counts: {string.Join(", ", tagCounts)}");

        List<string?> firstTags = db.Table<TaggedProduct>()
            .OrderBy(p => p.Id)
            .Select(p => p.Tags.FirstOrDefault())
            .ToList();
        Console.WriteLine($"First tags: {string.Join(", ", firstTags)}");

        List<string?> lastTags = db.Table<TaggedProduct>()
            .OrderBy(p => p.Id)
            .Select(p => p.Tags.LastOrDefault())
            .ToList();
        Console.WriteLine($"Last tags: {string.Join(", ", lastTags)}");

        string secondTag = db.Table<TaggedProduct>()
            .Where(p => p.Id == 1)
            .Select(p => p.Tags.ElementAt(1))
            .First();
        Console.WriteLine($"Second tag of Laptop: {secondTag}");

        bool hasElectronics = db.Table<TaggedProduct>()
            .Where(p => p.Id == 2)
            .Select(p => p.Tags.Any(t => t == "electronics"))
            .First();
        Console.WriteLine($"Novel has 'electronics' tag: {hasElectronics}");

        bool allShort = db.Table<TaggedProduct>()
            .Where(p => p.Id == 1)
            .Select(p => p.Tags.All(t => t != "longword"))
            .First();
        Console.WriteLine($"All Laptop tags != 'longword': {allShort}");

        int matchCount = db.Table<TaggedProduct>()
            .Where(p => p.Id == 3)
            .Select(p => p.Tags.Count(t => t == "electronics"))
            .First();
        Console.WriteLine($"Keyboard 'electronics' count: {matchCount}");

        IEnumerable<string> filtered = db.Table<TaggedProduct>()
            .Where(p => p.Id == 1)
            .Select(p => p.Tags.Where(t => t != "electronics"))
            .First();
        Console.WriteLine($"Laptop non-electronics tags: {string.Join(", ", filtered)}");

        string? sorted = db.Table<TaggedProduct>()
            .Where(p => p.Id == 1)
            .Select(p => p.Tags.OrderBy(t => t).First())
            .First();
        Console.WriteLine($"Laptop first alphabetical tag: {sorted}");

        IEnumerable<string> skipped = db.Table<TaggedProduct>()
            .Where(p => p.Id == 1)
            .Select(p => p.Tags.Skip(1))
            .First();
        Console.WriteLine($"Laptop tags after skip(1): {string.Join(", ", skipped)}");

        IEnumerable<string> taken = db.Table<TaggedProduct>()
            .Where(p => p.Id == 1)
            .Select(p => p.Tags.Take(1))
            .First();
        Console.WriteLine($"Laptop first tag only: {string.Join(", ", taken)}");

        IEnumerable<string> distinct = db.Table<TaggedProduct>()
            .Where(p => p.Id == 1)
            .Select(p => p.Tags.Distinct())
            .First();
        Console.WriteLine($"Laptop distinct tags: {string.Join(", ", distinct)}");

        List<string> extra = ["peripherals"];
        IEnumerable<string> concat = db.Table<TaggedProduct>()
            .Where(p => p.Id == 1)
            .Select(p => p.Tags.Concat(extra))
            .First();
        Console.WriteLine($"Laptop tags + extra: {string.Join(", ", concat)}");

        string? minTag = db.Table<TaggedProduct>()
            .Where(p => p.Id == 1)
            .Select(p => p.Tags.Min())
            .First();
        Console.WriteLine($"Min tag of Laptop: {minTag}");

        string? maxTag = db.Table<TaggedProduct>()
            .Where(p => p.Id == 1)
            .Select(p => p.Tags.Max())
            .First();
        Console.WriteLine($"Max tag of Laptop: {maxTag}");

        string? singleTag = db.Table<TaggedProduct>()
            .Where(p => p.Id == 4)
            .Select(p => p.Tags.SingleOrDefault())
            .First();
        Console.WriteLine($"Single tag of Unlisted (empty): {singleTag}");

        int laptopIndex = db.Table<TaggedProduct>()
            .Where(p => p.Id == 1)
            .Select(p => p.Tags.IndexOf("computers"))
            .First();
        Console.WriteLine($"Index of 'computers' in Laptop: {laptopIndex}");
    }
}

[Table("TaggedProducts")]
file class TaggedProduct
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
    public List<string> Tags { get; set; } = [];
}

[JsonSerializable(typeof(List<string>))]
internal partial class SampleJsonContext : JsonSerializerContext;