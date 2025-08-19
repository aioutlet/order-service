-- Order Service Sample Seed Data
-- Development Environment Sample Data

-- Sample Orders
INSERT INTO orders (
    Id, CustomerId, OrderNumber, Status, PaymentStatus, ShippingStatus,
    Subtotal, TaxAmount, ShippingCost, DiscountAmount, TotalAmount, Currency,
    CustomerEmail, CustomerPhone, CustomerName,
    ShippingAddress_Street, ShippingAddress_City, ShippingAddress_State, 
    ShippingAddress_PostalCode, ShippingAddress_Country,
    BillingAddress_Street, BillingAddress_City, BillingAddress_State,
    BillingAddress_PostalCode, BillingAddress_Country,
    PaymentProvider, ShippingMethod, CarrierName,
    CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
) VALUES 
-- Order 1: Completed order
('550e8400-e29b-41d4-a716-446655440001', '60b7c5a1f9b7c8e4d5a2b1c3', 'ORD-20250817-001', 2, 3, 3,
 89.99, 7.20, 9.99, 5.00, 102.18, 'USD',
 'john.doe@example.com', '+1-555-0123', 'John Doe',
 '123 Main St', 'New York', 'NY', '10001', 'USA',
 '123 Main St', 'New York', 'NY', '10001', 'USA',
 'stripe', 'standard', 'UPS',
 '2025-08-15 10:30:00', '2025-08-16 14:20:00', 'system', 'system'),

-- Order 2: Pending payment
('550e8400-e29b-41d4-a716-446655440002', '60b7c5a1f9b7c8e4d5a2b1c4', 'ORD-20250817-002', 0, 1, 0,
 149.99, 12.00, 15.99, 0.00, 177.98, 'USD',
 'jane.smith@example.com', '+1-555-0124', 'Jane Smith',
 '456 Oak Ave', 'Los Angeles', 'CA', '90210', 'USA',
 '456 Oak Ave', 'Los Angeles', 'CA', '90210', 'USA',
 'paypal', 'express', 'FedEx',
 '2025-08-17 09:15:00', '2025-08-17 09:15:00', 'system', 'system'),

-- Order 3: Processing order
('550e8400-e29b-41d4-a716-446655440003', '60b7c5a1f9b7c8e4d5a2b1c5', 'ORD-20250817-003', 1, 3, 1,
 75.50, 6.04, 8.99, 10.00, 80.53, 'USD',
 'bob.wilson@example.com', '+1-555-0125', 'Bob Wilson',
 '789 Pine St', 'Chicago', 'IL', '60601', 'USA',
 '789 Pine St', 'Chicago', 'IL', '60601', 'USA',
 'stripe', 'overnight', 'UPS',
 '2025-08-16 16:45:00', '2025-08-17 08:30:00', 'system', 'system'),

-- Order 4: Cancelled order
('550e8400-e29b-41d4-a716-446655440004', '60b7c5a1f9b7c8e4d5a2b1c6', 'ORD-20250817-004', 5, 5, 0,
 299.99, 24.00, 19.99, 30.00, 313.98, 'USD',
 'alice.brown@example.com', '+1-555-0126', 'Alice Brown',
 '321 Elm St', 'Houston', 'TX', '77001', 'USA',
 '321 Elm St', 'Houston', 'TX', '77001', 'USA',
 'square', 'standard', 'USPS',
 '2025-08-14 14:20:00', '2025-08-15 11:30:00', 'system', 'system'),

-- Order 5: Large order
('550e8400-e29b-41d4-a716-446655440005', '60b7c5a1f9b7c8e4d5a2b1c7', 'ORD-20250817-005', 2, 3, 2,
 1250.00, 125.00, 75.00, 100.00, 1350.00, 'USD',
 'david.lee@example.com', '+1-555-0127', 'David Lee',
 '654 Maple Dr', 'Miami', 'FL', '33101', 'USA',
 '654 Maple Dr', 'Miami', 'FL', '33101', 'USA',
 'stripe', 'express', 'DHL',
 '2025-08-13 12:00:00', '2025-08-16 15:45:00', 'system', 'system');

-- Sample Order Items
INSERT INTO order_items (
    Id, OrderId, ProductId, ProductName, ProductSku, Quantity, 
    UnitPrice, TotalPrice, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
) VALUES 
-- Order 1 items
('660e8400-e29b-41d4-a716-446655440001', '550e8400-e29b-41d4-a716-446655440001', 
 'prod_001', 'Wireless Bluetooth Headphones', 'WBH-001', 1, 79.99, 79.99,
 '2025-08-15 10:30:00', '2025-08-15 10:30:00', 'system', 'system'),
('660e8400-e29b-41d4-a716-446655440002', '550e8400-e29b-41d4-a716-446655440001', 
 'prod_002', 'Phone Case Premium', 'PC-PREM-001', 1, 10.00, 10.00,
 '2025-08-15 10:30:00', '2025-08-15 10:30:00', 'system', 'system'),

-- Order 2 items  
('660e8400-e29b-41d4-a716-446655440003', '550e8400-e29b-41d4-a716-446655440002',
 'prod_003', 'Smart Watch Series X', 'SW-X-001', 1, 149.99, 149.99,
 '2025-08-17 09:15:00', '2025-08-17 09:15:00', 'system', 'system'),

-- Order 3 items
('660e8400-e29b-41d4-a716-446655440004', '550e8400-e29b-41d4-a716-446655440003',
 'prod_004', 'USB-C Cable 6ft', 'USB-C-6FT', 3, 12.99, 38.97,
 '2025-08-16 16:45:00', '2025-08-16 16:45:00', 'system', 'system'),
('660e8400-e29b-41d4-a716-446655440005', '550e8400-e29b-41d4-a716-446655440003',
 'prod_005', 'Wireless Charger Pad', 'WCP-001', 2, 18.27, 36.53,
 '2025-08-16 16:45:00', '2025-08-16 16:45:00', 'system', 'system'),

-- Order 4 items (cancelled)
('660e8400-e29b-41d4-a716-446655440006', '550e8400-e29b-41d4-a716-446655440004',
 'prod_006', 'Gaming Laptop Pro', 'GLP-2024', 1, 299.99, 299.99,
 '2025-08-14 14:20:00', '2025-08-14 14:20:00', 'system', 'system'),

-- Order 5 items (bulk order)
('660e8400-e29b-41d4-a716-446655440007', '550e8400-e29b-41d4-a716-446655440005',
 'prod_007', 'Professional Camera Kit', 'PCK-2024', 2, 500.00, 1000.00,
 '2025-08-13 12:00:00', '2025-08-13 12:00:00', 'system', 'system'),
('660e8400-e29b-41d4-a716-446655440008', '550e8400-e29b-41d4-a716-446655440005',
 'prod_008', 'Camera Tripod Professional', 'CTP-PRO', 5, 50.00, 250.00,
 '2025-08-13 12:00:00', '2025-08-13 12:00:00', 'system', 'system');
