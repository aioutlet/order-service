-- Order Service Database Schema
-- Creates tables for the Order Service with proper constraints and indexes

-- Create Orders table
CREATE TABLE IF NOT EXISTS "Orders" (
    "Id" UUID PRIMARY KEY,
    "CustomerId" VARCHAR(255) NOT NULL,
    "OrderNumber" VARCHAR(50) NOT NULL UNIQUE,
    "Status" INTEGER NOT NULL DEFAULT 0, -- 0=Created, 1=Processing, 2=Shipped, 3=Delivered, 4=Cancelled
    "PaymentStatus" INTEGER NOT NULL DEFAULT 0, -- 0=Pending, 1=Paid, 2=Failed, 3=Refunded
    "ShippingStatus" INTEGER NOT NULL DEFAULT 0, -- 0=NotShipped, 1=Shipped, 2=Delivered, 3=Returned
    "Subtotal" DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    "TaxAmount" DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    "ShippingCost" DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    "TotalAmount" DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    "Currency" VARCHAR(3) NOT NULL DEFAULT 'USD',
    "CustomerEmail" VARCHAR(255) NOT NULL,
    "CustomerName" VARCHAR(255) NOT NULL,
    "DeliveredDate" TIMESTAMP NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(255) NOT NULL
);

-- Create OrderItems table
CREATE TABLE IF NOT EXISTS "OrderItems" (
    "Id" UUID PRIMARY KEY,
    "OrderId" UUID NOT NULL,
    "ProductId" VARCHAR(255) NOT NULL,
    "ProductName" VARCHAR(255) NOT NULL,
    "ProductSku" VARCHAR(100) NULL,
    "UnitPrice" DECIMAL(10,2) NOT NULL,
    "Quantity" INTEGER NOT NULL,
    "TotalPrice" DECIMAL(10,2) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Foreign key constraint
    CONSTRAINT "FK_OrderItems_Orders" FOREIGN KEY ("OrderId") REFERENCES "Orders"("Id") ON DELETE CASCADE
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS "IX_Orders_CustomerId" ON "Orders" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IX_Orders_Status" ON "Orders" ("Status");
CREATE INDEX IF NOT EXISTS "IX_Orders_PaymentStatus" ON "Orders" ("PaymentStatus");
CREATE INDEX IF NOT EXISTS "IX_Orders_OrderNumber" ON "Orders" ("OrderNumber");
CREATE INDEX IF NOT EXISTS "IX_Orders_CreatedAt" ON "Orders" ("CreatedAt");

CREATE INDEX IF NOT EXISTS "IX_OrderItems_OrderId" ON "OrderItems" ("OrderId");
CREATE INDEX IF NOT EXISTS "IX_OrderItems_ProductId" ON "OrderItems" ("ProductId");

-- Add triggers to automatically update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers for auto-updating UpdatedAt
DROP TRIGGER IF EXISTS update_orders_updated_at ON "Orders";
CREATE TRIGGER update_orders_updated_at 
    BEFORE UPDATE ON "Orders" 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_order_items_updated_at ON "OrderItems";
CREATE TRIGGER update_order_items_updated_at 
    BEFORE UPDATE ON "OrderItems" 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Add some constraints
ALTER TABLE "Orders" ADD CONSTRAINT "CHK_Orders_Status" 
    CHECK ("Status" IN (0, 1, 2, 3, 4));

ALTER TABLE "Orders" ADD CONSTRAINT "CHK_Orders_PaymentStatus" 
    CHECK ("PaymentStatus" IN (0, 1, 2, 3));

ALTER TABLE "Orders" ADD CONSTRAINT "CHK_Orders_ShippingStatus" 
    CHECK ("ShippingStatus" IN (0, 1, 2, 3));

ALTER TABLE "Orders" ADD CONSTRAINT "CHK_Orders_Amounts" 
    CHECK ("Subtotal" >= 0 AND "TaxAmount" >= 0 AND "ShippingCost" >= 0 AND "TotalAmount" >= 0);

ALTER TABLE "OrderItems" ADD CONSTRAINT "CHK_OrderItems_Amounts" 
    CHECK ("UnitPrice" >= 0 AND "Quantity" > 0 AND "TotalPrice" >= 0);

-- Insert initial status lookup data if needed (for documentation purposes)
COMMENT ON COLUMN "Orders"."Status" IS 'Order Status: 0=Created, 1=Processing, 2=Shipped, 3=Delivered, 4=Cancelled';
COMMENT ON COLUMN "Orders"."PaymentStatus" IS 'Payment Status: 0=Pending, 1=Paid, 2=Failed, 3=Refunded';
COMMENT ON COLUMN "Orders"."ShippingStatus" IS 'Shipping Status: 0=NotShipped, 1=Shipped, 2=Delivered, 3=Returned';