import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "../context/useAuth";

type Props = {
    children: ReactNode;
    // When set, the logged-in user must have this role (e.g. "Admin") to see the route.
    requireRole?: string;
};

export default function ProtectedRoute({ children, requireRole }: Props) {
    const { isAuthenticated, role, logout } = useAuth();

    if (!isAuthenticated) {
        return <Navigate to="/login" replace />;
    }

    // Authenticated but lacking the required role: show a 403 instead of bouncing to /login
    // (which would be confusing since the user is logged in). The backend enforces this too.
    if (requireRole && role !== requireRole) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50 p-6">
                <div className="w-full max-w-md bg-white border border-gray-100 rounded-2xl shadow-xl p-8 text-center space-y-4">
                    <div className="mx-auto h-16 w-16 rounded-full bg-red-50 flex items-center justify-center text-3xl">
                        🔒
                    </div>
                    <h1 className="text-2xl font-bold text-gray-900">Access denied</h1>
                    <p className="text-gray-500">
                        This area is for administrators only. Your account doesn't have permission to view it.
                    </p>
                    <button
                        onClick={logout}
                        className="w-full rounded-xl bg-indigo-600 text-white font-semibold py-3 hover:bg-indigo-700 transition"
                    >
                        Sign in with a different account
                    </button>
                </div>
            </div>
        );
    }

    return <>{children}</>;
}
