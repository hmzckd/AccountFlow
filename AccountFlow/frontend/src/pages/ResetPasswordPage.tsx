import { useState, type FormEvent } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { resetPassword } from "../api/auth";
import toast from "react-hot-toast";
import { AuthLayout } from "../components/ui/AuthLayout";
import { Input } from "../components/ui/Input";
import { Button } from "../components/ui/Button";
import { validatePassword } from "../utils/validators";

export default function ResetPasswordPage() {
    const [searchParams] = useSearchParams();
    const nav = useNavigate();

    // Token normally arrives from the email link (?token=...).
    const urlToken = searchParams.get("token") || "";

    const [token, setToken] = useState(urlToken);
    const [newPassword, setNewPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [loading, setLoading] = useState(false);

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();

        if (!token) {
            toast.error("Reset code is missing. Please use the link from your email.");
            return;
        }
        const pwError = validatePassword(newPassword);
        if (pwError) {
            toast.error(pwError);
            return;
        }
        if (newPassword !== confirmPassword) {
            toast.error("Passwords do not match.");
            return;
        }

        setLoading(true);
        try {
            await resetPassword({ token, newPassword });
            toast.success("Password reset successful! You can now login.", { icon: '🔐' });
            setTimeout(() => nav("/login"), 2000);
        } catch (err) {
            const message = err instanceof Error ? err.message : "Reset failed.";
            toast.error(message);
        } finally {
            setLoading(false);
        }
    }

    return (
        <AuthLayout
            title="Set New Password"
            subtitle="Create a strong password for your account"
        >
            <form onSubmit={handleSubmit} className="space-y-6">
                {/* Only ask for the token manually when the link didn't carry one. */}
                {!urlToken && (
                    <Input
                        label="Reset Code (Token)"
                        placeholder="Paste the code from your email"
                        value={token}
                        onChange={(e) => setToken(e.target.value)}
                        required
                    />
                )}

                <div>
                    <Input
                        label="New Password"
                        type="password"
                        placeholder="••••••••"
                        value={newPassword}
                        onChange={(e) => setNewPassword(e.target.value)}
                        required
                    />
                    <p className="text-xs text-gray-500 mt-1">
                        At least 6 characters, including uppercase, lowercase, a number, and a special character.
                    </p>
                </div>

                <Input
                    label="Confirm New Password"
                    type="password"
                    placeholder="••••••••"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    required
                />

                <Button type="submit" isLoading={loading}>
                    {loading ? "Resetting..." : "Update Password"}
                </Button>
            </form>
        </AuthLayout>
    );
}
