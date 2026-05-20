-- ============================================================
-- Migration: 002_CreateUsersTable.sql
-- Schema: public
-- Table: Users
-- Matches: Domain\Entities\Admin\User.cs
-- ============================================================

CREATE TABLE IF NOT EXISTS public."Users" (
    "Id"           UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "Username"     VARCHAR(50)   NOT NULL,
    "Email"        VARCHAR(255)  NOT NULL,
    "PasswordHash" TEXT          NOT NULL,
    "CreatedAt"    TIMESTAMP     NOT NULL DEFAULT NOW()
);

-- Unique constraints
ALTER TABLE public."Users"
    ADD CONSTRAINT "UQ_Users_Username" UNIQUE ("Username");

ALTER TABLE public."Users"
    ADD CONSTRAINT "UQ_Users_Email" UNIQUE ("Email");

-- Index for lookups by email and username
CREATE INDEX IF NOT EXISTS "IX_Users_Email"    ON public."Users" ("Email");
CREATE INDEX IF NOT EXISTS "IX_Users_Username" ON public."Users" ("Username");
