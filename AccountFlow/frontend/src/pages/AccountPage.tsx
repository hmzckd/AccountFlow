import {
    ArrowRightStartOnRectangleIcon,
    ShieldCheckIcon,
} from "@heroicons/react/24/outline";
import { useAuth } from "../context/useAuth";

export default function AccountPage() {
    const { logout, role } = useAuth();

    return (
        <div className="min-h-screen bg-gray-50 text-gray-900">
            <header className="border-b border-gray-200 bg-white">
                <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
                    <span className="text-lg font-semibold">AccountFlow</span>
                    <button
                        type="button"
                        onClick={logout}
                        className="inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-100"
                    >
                        <ArrowRightStartOnRectangleIcon className="h-5 w-5" />
                        Sign out
                    </button>
                </div>
            </header>

            <main className="mx-auto max-w-5xl px-6 py-16">
                <div className="max-w-xl border-l-4 border-green-500 pl-6">
                    <ShieldCheckIcon className="mb-5 h-12 w-12 text-green-600" />
                    <h1 className="text-3xl font-bold">Account ready</h1>
                    <p className="mt-3 text-gray-600">
                        You are signed in with a verified {role ?? "User"} account.
                    </p>
                </div>
            </main>
        </div>
    );
}
