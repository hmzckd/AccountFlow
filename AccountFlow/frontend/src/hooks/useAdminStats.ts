import { useEffect, useState } from "react";
import { getDailyStats } from "../api/admin";

/**
 * Custom hook to fetch and manage admin dashboard statistics.
 * Aggregates data for daily registrations and unverified user counts.
 */
export function useAdminStats() {
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [registrations, setRegistrations] = useState<number>(0);
    const [unverified, setUnverified] = useState<number>(0);

    useEffect(() => {
        getDailyStats()
            .then((stats) => {
                setRegistrations(stats.registrations);
                setUnverified(stats.unverified);
            })
            .catch((err) => {
                const message =
                    err instanceof Error ? err.message : "Failed to load admin stats";
                setError(message);
            })
            .finally(() => setLoading(false));
    }, []);

    return { loading, error, registrations, unverified };
}
