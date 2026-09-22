// src/api/admin.ts
import { api } from "./client";
import type { AdminStats } from "../types/admin";

/**
 * Fetches the registration and unverified-user counts from the last 24 hours.
 */
export function getDailyStats(): Promise<AdminStats> {
    return api.get<AdminStats>("/admin/stats/daily");
}
