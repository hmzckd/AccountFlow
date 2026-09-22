import { useAdminStats } from "../hooks/useAdminStats";
import { useAuth } from "../context/useAuth";
import {
    ArrowRightStartOnRectangleIcon,
    ExclamationTriangleIcon,
    UsersIcon,
} from "@heroicons/react/24/outline";
import StatCard from "../components/StatCard";

export default function AdminDashboardPage() {
    const { loading, error, registrations, unverified } = useAdminStats();
    const { logout } = useAuth();

    return (
        <div className="min-h-screen bg-gray-50">
            <nav className="bg-white shadow-sm border-b border-gray-200 px-6 py-4 flex justify-between items-center">
                <h1 className="text-xl font-bold text-gray-800 tracking-tight">Admin<span className="text-indigo-600">Dashboard</span></h1>
                <button
                    onClick={logout}
                    className="inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium text-red-600 transition-colors hover:bg-red-50 hover:text-red-800"
                >
                    <ArrowRightStartOnRectangleIcon className="h-5 w-5" />
                    Logout
                </button>
            </nav>

            <main className="max-w-5xl mx-auto p-6 space-y-8">
                <div className="space-y-2">
                    <h2 className="text-2xl font-bold text-gray-900">Dashboard Overview</h2>
                    <p className="text-gray-500">Welcome back, here's what happened in the last 24 hours.</p>
                </div>

                {loading ? (
                    <div className="animate-pulse space-y-4">
                        <div className="h-32 bg-gray-200 rounded-2xl"></div>
                        <div className="h-32 bg-gray-200 rounded-2xl"></div>
                    </div>
                ) : error ? (
                    <div className="p-4 bg-red-50 text-red-700 rounded-xl border border-red-200">
                        Error loading stats: {error}
                    </div>
                ) : (
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                        <StatCard
                            label="New Users (24h)"
                            value={registrations}
                            accent="indigo"
                            icon={<UsersIcon className="h-7 w-7" />}
                        />
                        <StatCard
                            label="Unverified Users"
                            value={unverified}
                            accent="orange"
                            icon={<ExclamationTriangleIcon className="h-7 w-7" />}
                        />
                    </div>
                )}
            </main>
        </div>
    );
}
