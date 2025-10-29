-- Migration: V005 - Add CHECK Constraints for Data Integrity
-- Description: Adds CHECK constraints as a safety net to prevent invalid data
--              These are optional last-resort checks; the application code properly
--              handles validation through atomic operations and transaction management
-- Author: Generated with Claude Code
-- Date: 2025-10-29

-- ============================================================================
-- CHECK CONSTRAINTS ON BOOKS TABLE
-- ============================================================================

-- Constraint 1: Ensure AvailableCopies never goes negative
-- This is a safety net; the application code uses atomic UPDATE with WHERE clauses
-- to prevent this scenario
ALTER TABLE Books
ADD CONSTRAINT CHK_Books_AvailableCopies_NonNegative
CHECK (AvailableCopies >= 0);

-- Constraint 2: Ensure AvailableCopies never exceeds TotalCopies
-- This prevents data corruption where available copies would exceed the total inventory
ALTER TABLE Books
ADD CONSTRAINT CHK_Books_AvailableCopies_LTE_TotalCopies
CHECK (AvailableCopies <= TotalCopies);

-- ============================================================================
-- NOTES ON CHECK CONSTRAINTS VS. APPLICATION LOGIC
-- ============================================================================

-- These CHECK constraints serve as a LAST RESORT safety mechanism.
-- The PRIMARY defense against race conditions and data corruption is:
--
-- 1. ATOMIC OPERATIONS: BorrowCopyAsync and ReturnCopyAsync use single UPDATE
--    statements with WHERE conditions that check availability AND update in
--    one atomic operation, preventing TOCTOU (Time-of-Check to Time-of-Use)
--    race conditions
--
-- 2. TRANSACTION MANAGEMENT: All multi-step operations are wrapped in explicit
--    transactions with proper commit/rollback handling
--
-- 3. ISOLATION: SQL Server's default READ COMMITTED isolation level provides
--    adequate protection for our use case
--
-- These CHECK constraints will catch:
-- - Direct SQL manipulations that bypass application logic
-- - Migration errors
-- - Unexpected bugs in application code
--
-- But they should NEVER be relied upon as the primary validation mechanism.
-- See docs/21-transactions.md for detailed explanation of the transaction
-- strategy and race condition prevention.
