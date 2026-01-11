/*
 * 
 * 
 * -- 1. Create Categories Table
CREATE TABLE "Categories" (
    "id" UUID NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "Slug" VARCHAR(200) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAt" TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT "PK_Categories" PRIMARY KEY ("id")
);

-- 2. Create Users Table
CREATE TABLE "Users" (
    "id" UUID NOT NULL,
    "Username" TEXT NOT NULL,
    "IsAdmin" BOOLEAN NOT NULL,
    "Phone" TEXT NULL,
    "Verified" BOOLEAN NOT NULL,
    "LastLoginAt" TIMESTAMP WITH TIME ZONE NULL,
    "Email" TEXT NULL,
    "PasswordHash" TEXT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAt" TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("id")
);

-- 3. Create Products Table (Note: Table name is lowercase in your migration)
CREATE TABLE "products" (
    "id" UUID NOT NULL,
    "Name" VARCHAR(200) NOT NULL,
    "SKU" VARCHAR(64) NULL,
    "Description" VARCHAR(2000) NULL,
    "LongDescription" VARCHAR(8000) NULL,
    "Price" NUMERIC(18,2) NOT NULL,
    "CompareAtPrice" NUMERIC(12,2) NULL,
    "SalePrice" NUMERIC(12,2) NULL,
    "Stock" INTEGER NOT NULL,
    "Weight" NUMERIC(10,3) NULL,
    "IsActive" BOOLEAN NOT NULL,
    "IsFeatured" BOOLEAN NOT NULL,
    "CategoryId" UUID NULL,
    "Type" TEXT NULL,
    "ImageUrl" TEXT NULL,
    "Images" TEXT[] NULL,       -- Array of strings
    "Features" TEXT[] NULL,     -- Array of strings
    "Parameters" JSONB NULL,    -- JSONB type
    "Rating" DOUBLE PRECISION NULL,
    "ReviewCount" INTEGER NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAt" TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT "PK_products" PRIMARY KEY ("id"),
    CONSTRAINT "FK_products_Categories_CategoryId" FOREIGN KEY ("CategoryId") 
        REFERENCES "Categories" ("id") ON DELETE SET NULL
);

-- 4. Create Carts Table
CREATE TABLE "Carts" (
    "id" UUID NOT NULL,
    "UserId" UUID NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAt" TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT "PK_Carts" PRIMARY KEY ("id")
);

-- 5. Create CartItems Table
CREATE TABLE "CartItems" (
    "id" UUID NOT NULL,
    "CartId" UUID NOT NULL,
    "ProductId" UUID NOT NULL,
    "qty" INTEGER NOT NULL DEFAULT 1,
    "unitPrice" NUMERIC(18,2) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAt" TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT "PK_CartItems" PRIMARY KEY ("id"),
    CONSTRAINT "FK_CartItems_Carts_CartId" FOREIGN KEY ("CartId") 
        REFERENCES "Carts" ("id") ON DELETE CASCADE,
    CONSTRAINT "FK_CartItems_products_ProductId" FOREIGN KEY ("ProductId") 
        REFERENCES "products" ("id") ON DELETE RESTRICT
);

-- 6. Create Orders Table
CREATE TABLE "Orders" (
    "id" UUID NOT NULL,
    "UserId" UUID NOT NULL,
    "Status" INTEGER NOT NULL,
    "OrderNumber" TEXT NULL,
    "Subtotal" NUMERIC(18,2) NOT NULL,
    "Shipping" NUMERIC(18,2) NOT NULL,
    "Discount" NUMERIC(18,2) NOT NULL,
    "Tax" NUMERIC(18,2) NOT NULL,
    "Total" NUMERIC(18,2) NOT NULL,
    "FullName" TEXT NULL,
    "Email" TEXT NULL,
    "Phone" TEXT NULL,
    "AddressLine" TEXT NULL,
    "City" TEXT NULL,
    "Country" TEXT NULL,
    "Notes" TEXT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAt" TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT "PK_Orders" PRIMARY KEY ("id")
);

-- 7. Create OrderItems Table
CREATE TABLE "OrderItems" (
    "id" UUID NOT NULL,
    "OrderId" UUID NOT NULL,
    "ProductId" UUID NOT NULL, -- Note: No FK constraint to products table defined in migration
    "qty" INTEGER NOT NULL DEFAULT 1,
    "unitPrice" NUMERIC(18,2) NOT NULL,
    "title" TEXT NULL,
    "imageUrl" TEXT NULL,
    "sku" TEXT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletedAt" TIMESTAMP WITH TIME ZONE NULL,
    CONSTRAINT "PK_OrderItems" PRIMARY KEY ("id"),
    CONSTRAINT "FK_OrderItems_Orders_OrderId" FOREIGN KEY ("OrderId") 
        REFERENCES "Orders" ("id") ON DELETE CASCADE
);

-- 8. Create EmailLogs Table
CREATE TABLE "EmailLogs" (
    "id" UUID NOT NULL,
    "OrderId" UUID NOT NULL,
    "To" VARCHAR(256) NOT NULL,
    "Subject" VARCHAR(500) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "SentAt" TIMESTAMP WITH TIME ZONE NULL,
    "Status" VARCHAR(50) NOT NULL,
    "ErrorMessage" TEXT NULL,
    "BodyContent" TEXT NULL,
    CONSTRAINT "PK_EmailLogs" PRIMARY KEY ("id")
);

-- 9. Create Indexes (Performance)
CREATE INDEX "IX_CartItems_CartId" ON "CartItems" ("CartId");
CREATE INDEX "IX_CartItems_CartId_IsDeleted" ON "CartItems" ("CartId", "IsDeleted");
CREATE INDEX "IX_CartItems_ProductId" ON "CartItems" ("ProductId");
CREATE INDEX "IX_Carts_UserId_IsDeleted" ON "Carts" ("UserId", "IsDeleted");
CREATE INDEX "IX_OrderItems_OrderId" ON "OrderItems" ("OrderId");
CREATE INDEX "IX_Orders_UserId_CreatedAt" ON "Orders" ("UserId", "CreatedAt");
CREATE INDEX "IX_products_CategoryId" ON "products" ("CategoryId");
CREATE INDEX "IX_products_IsActive_IsFeatured" ON "products" ("IsActive", "IsFeatured");
CREATE INDEX "ix_products_name_isdeleted" ON "products" ("Name", "IsDeleted");
CREATE INDEX "ix_products_sku_isdeleted" ON "products" ("SKU", "IsDeleted");




*/