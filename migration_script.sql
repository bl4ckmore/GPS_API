CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Carts" (
    id uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Carts" PRIMARY KEY (id)
);

CREATE TABLE "Categories" (
    id uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Slug" character varying(200) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Categories" PRIMARY KEY (id)
);

CREATE TABLE "EmailLogs" (
    id uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "To" character varying(256) NOT NULL,
    "Subject" character varying(500) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "SentAt" timestamp with time zone,
    "Status" character varying(50) NOT NULL,
    "ErrorMessage" text,
    "BodyContent" text,
    CONSTRAINT "PK_EmailLogs" PRIMARY KEY (id)
);

CREATE TABLE "Orders" (
    id uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "OrderNumber" text,
    "Subtotal" numeric(18,2) NOT NULL,
    "Shipping" numeric(18,2) NOT NULL,
    "Discount" numeric(18,2) NOT NULL,
    "Tax" numeric(18,2) NOT NULL,
    "Total" numeric(18,2) NOT NULL,
    "FullName" text,
    "Email" text,
    "Phone" text,
    "AddressLine" text,
    "City" text,
    "Country" text,
    "Notes" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Orders" PRIMARY KEY (id)
);

CREATE TABLE "Users" (
    id uuid NOT NULL,
    "Username" text NOT NULL,
    "IsAdmin" boolean NOT NULL,
    "Phone" text,
    "Verified" boolean NOT NULL,
    "LastLoginAt" timestamp with time zone,
    "Email" text,
    "PasswordHash" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Users" PRIMARY KEY (id)
);

CREATE TABLE products (
    id uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "SKU" character varying(64),
    "Description" character varying(2000),
    "LongDescription" character varying(8000),
    "Price" numeric(18,2) NOT NULL,
    "CompareAtPrice" numeric(12,2),
    "SalePrice" numeric(12,2),
    "Stock" integer NOT NULL,
    "Weight" numeric(10,3),
    "IsActive" boolean NOT NULL,
    "IsFeatured" boolean NOT NULL,
    "CategoryId" uuid,
    "Type" text,
    "ImageUrl" text,
    "Images" text[],
    "Features" text[],
    "Parameters" jsonb,
    "Rating" double precision,
    "ReviewCount" integer,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_products" PRIMARY KEY (id),
    CONSTRAINT "FK_products_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" (id) ON DELETE SET NULL
);

CREATE TABLE "OrderItems" (
    id uuid NOT NULL,
    "OrderId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    qty integer NOT NULL DEFAULT 1,
    "unitPrice" numeric(18,2) NOT NULL,
    title text,
    "imageUrl" text,
    sku text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_OrderItems" PRIMARY KEY (id),
    CONSTRAINT "FK_OrderItems_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" (id) ON DELETE CASCADE
);

CREATE TABLE "CartItems" (
    id uuid NOT NULL,
    "CartId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    qty integer NOT NULL DEFAULT 1,
    "unitPrice" numeric(18,2) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "IsDeleted" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_CartItems" PRIMARY KEY (id),
    CONSTRAINT "FK_CartItems_Carts_CartId" FOREIGN KEY ("CartId") REFERENCES "Carts" (id) ON DELETE CASCADE,
    CONSTRAINT "FK_CartItems_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES products (id) ON DELETE RESTRICT
);

CREATE INDEX "IX_CartItems_CartId" ON "CartItems" ("CartId");

CREATE INDEX "IX_CartItems_CartId_IsDeleted" ON "CartItems" ("CartId", "IsDeleted");

CREATE INDEX "IX_CartItems_ProductId" ON "CartItems" ("ProductId");

CREATE INDEX "IX_Carts_UserId_IsDeleted" ON "Carts" ("UserId", "IsDeleted");

CREATE INDEX "IX_OrderItems_OrderId" ON "OrderItems" ("OrderId");

CREATE INDEX "IX_Orders_UserId_CreatedAt" ON "Orders" ("UserId", "CreatedAt");

CREATE INDEX "IX_products_CategoryId" ON products ("CategoryId");

CREATE INDEX "IX_products_IsActive_IsFeatured" ON products ("IsActive", "IsFeatured");

CREATE INDEX ix_products_name_isdeleted ON products ("Name", "IsDeleted");

CREATE INDEX ix_products_sku_isdeleted ON products ("SKU", "IsDeleted");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251203092224_InitialMaster', '8.0.20');

COMMIT;

START TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251206171452_AddEmailLogs', '8.0.20');

COMMIT;

START TRANSACTION;

ALTER TABLE "CartItems" DROP CONSTRAINT "FK_CartItems_products_ProductId";

DROP INDEX "IX_CartItems_CartId_IsDeleted";

ALTER TABLE "CartItems" ALTER COLUMN "unitPrice" TYPE numeric;

ALTER TABLE "CartItems" ALTER COLUMN qty DROP DEFAULT;

ALTER TABLE "CartItems" ADD CONSTRAINT "FK_CartItems_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES products (id) ON DELETE CASCADE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260110225509_InitOrUpdateSchema', '8.0.20');

COMMIT;

START TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260110232505_LATEST', '8.0.20');

COMMIT;

