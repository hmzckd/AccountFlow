import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { requestPasswordReset } from "../api/auth";
import toast from "react-hot-toast";
import { AuthLayout } from "../components/ui/AuthLayout";
import { Input } from "../components/ui/Input";
import { Button } from "../components/ui/Button";

export default function ForgotPasswordPage() {
    const [email, setEmail] = useState("");
    const [loading, setLoading] = useState(false);

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();
        setLoading(true);
        try {
            await requestPasswordReset({ email });
            toast.success("Reset link sent! Please check your inbox.", { icon: '📧', duration: 5000 });
            setEmail(""); // Temizle
        } catch (err) {
            const message = err instanceof Error ? err.message : "Error sending email.";
            toast.error(message);
        } finally {
            setLoading(false);
        }
    }

    return (
        <AuthLayout
            title="Reset Password"
            subtitle="Enter your email to receive a reset link"
        >
            <form onSubmit={handleSubmit} className="space-y-6">
                <Input
                    label="Email Address"
                    type="email"
                    placeholder="you@example.com"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                />

                <Button type="submit" isLoading={loading}>
                    {loading ? "Sending Link..." : "Send Reset Link"}
                </Button>
            </form>

            <div className="text-center mt-6">
                <Link to="/login" className="text-sm font-semibold text-gray-600 hover:text-indigo-600 transition-colors flex items-center justify-center gap-2">
                    <span>←</span> Back to Login
                </Link>
            </div>
        </AuthLayout>
    );
}