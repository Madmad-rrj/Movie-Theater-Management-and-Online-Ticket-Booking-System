# AGENTS.md — Cinema Project Logging Rules

## 1. Purpose

This file defines the rules for AI agents recording work logs in the Cinema Ticket Booking System project# Connection String Supabase PostgreSQL (Session Pooler - IPv4 compatible)
# Copy file nay thanh .env va dien DATABASE PASSWORD cua project Supabase.# AGENTS.md — Cinema Project Logging Rules

## 1. Purpose

This file defines the rules for AI agents recording work logs in the Cinema Ticket Booking System project# Connection String Supabase
The primary goal is to maintain accurate, concise, and traceable records of work performed.

## 2. Before Logging

1. Read this file before creating or updating a work log.
2. Identify the current repository root and the actual log directory.
3. Identify the current member using an explicitly provided member ID or an established project convention. Never guess the member ID.
4. Read the existing log file, if available, to preserve its format and history.
5. Check the actual work performed, changed files, test results, and Git status when relevant.

## 3. Scope Restrictions

Unless explicitly requested, only create or update the current member's work log.

- Do not modify source code, project documentation, configuration, or other members' logs.
- Do not create duplicate log entries for the same task.
- Do not rewrite historical entries merely to make the log look complete.
- Preserve existing user changes and repository content.
- If the correct log file or member identity cannot be determined, ask for clarification rather than guessing.

## 4. Log Content

Record only information supported by actual evidence:

- Date and time, when available.
- Member ID and Git branch, when relevant.
- Task or objective.
- Files created, modified, or deleted.
- Tests or checks actually performed and their results.
- Current status: completed, partially completed, blocked, or awaiting review.
- Outstanding issues or decisions, if any.

Distinguish completed work from proposed work. Never claim that tests passed, changes were committed, or changes were pushed unless the corresponding action was actually verified.

Do not include passwords, access tokens, secrets, private connection strings, or unnecessary personal information.

## 5. Logging Procedure

1. Inspect the existing log and gather evidence about the task.
2. Prepare a concise entry using the established format.
3. Append the entry or make the smallest necessary update.
4. Verify that the resulting log is readable and that previous entries remain intact.
5. Report the log file updated and summarize the recorded work.

Do not commit, push, merge, or perform unrelated Git operations unless explicitly requested.

## 6. Completion Criteria

A logging task is complete when:
# AGENTS.md — Cinema Project Logging Rules