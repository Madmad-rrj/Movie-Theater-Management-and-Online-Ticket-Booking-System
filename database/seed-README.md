# Database Seeding Instructions

This seed data configures a realistic baseline for the demo movie theater management system based on the provided PostgreSQL schema[cite: 1, 2].

## Summary of Generated Data
* **Movies Researched & Seeded:** 6 (Avengers: Endgame, Interstellar, MI: Final Reckoning, Your Name, Kingdom of Heaven, World War Z).
* **Cinemas:** 1 (`CINEMA_HCM_01`).
* **Rooms:** 2 (`ROOM_01` Standard, `ROOM_02` VIP).
* **Seats:** 82 total seats (50 in Room 1; 32 in Room 2, including `STANDARD`, `VIP`, and `COUPLE` layouts).
* **Showtimes:** 48 total screenings scheduled between October 28 and November 4, 2026. 
* **Pricing Base:** Tickets range from 80,000 to 110,000 VND[cite: 2].
* **Demo Users/Bookings:** Excluded (authentication defaults to the application's underlying user generation logic to avoid exposing raw credentials).

## Execution Order
The `seed-data.sql` script is largely idempotent, using standard `ON CONFLICT DO NOTHING` statements for distinct reference values and `DO $$` PL/pgSQL blocks for relational lookups[cite: 1].

1. Ensure the PostgreSQL schema (DDL) is fully migrated.
2. Ensure the required `btree_gist` extension is enabled in your database to support showtime exclusion constraints[cite: 1, 2].
3. Run `psql -U your_user -d your_db -f database/seed-data.sql`.

## Validation Report
* **Constraints Checked:** `starts_at < ends_at <= room_available_at` logic respects movie runtime + 15 min cleanup[cite: 1]. Overlaps are avoided programmatically in the PL/pgSQL loops.
* **Pricing & Types:** 3 Ticket Types, 3 Seat Types are populated precisely[cite: 1, 2]. `capacity_units` match `1` (Standard, VIP) and `2` (Couple)[cite: 2].
* **Re-run Safety:** The PL/pgSQL routines check if the specific titles exist before duplicating movie data and calculate dynamic offsets rather than static IDs. Showtime insertions are raw to support the range constraint, but duplicate script runs should be cleared beforehand if you want to avoid clustered schedules (though the GIst exclusion constraint will naturally reject overlapping intervals)[cite: 1, 2].