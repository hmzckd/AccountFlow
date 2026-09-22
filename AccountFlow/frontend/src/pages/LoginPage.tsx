import { useState, type FormEvent } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuth } from "../context/useAuth";
import toast from "react-hot-toast";
import { AuthLayout } from "../components/ui/AuthLayout";
import { Input } from "../components/ui/Input";
import { Button } from "../components/ui/Button";

export default function LoginPage() {
    const { login } = useAuth();
    const nav = useNavigate();

    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [rememberMe, setRememberMe] = useState(false);
    const [loading, setLoading] = useState(false);

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();
        setLoading(true);

        try {
            const result = await login({ email, password }, rememberMe);
            toast.success("Welcome back!");
            nav(result.role === "Admin" ? "/admin" : "/account");
        } catch (err) {
            const message = err instanceof Error ? err.message : "Unable to sign in.";
            toast.error(message);
            setPassword("");
        } finally {
            setLoading(false);
        }
    }

    return (
        <AuthLayout
            title="Sign in"
            subtitle="Please enter your details."
            imageTitle="Welcome Back"
            imageSubtitle="We help you verify users and manage your dashboard securely. Join us today."
        >
            <form onSubmit={handleSubmit} className="space-y-6">

                <Input
                    label="Email Address"
                    type="email"
                    placeholder="Enter your email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                    autoComplete="email"
                />

                <div className="space-y-2">
                    <Input
                        label="Password"
                        type="password"
                        placeholder="••••••••"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                        autoComplete="current-password"
                    />

                    <div className="flex items-center justify-between mt-2">
                        <div className="flex items-center">
                            <input
                                id="remember-me"
                                type="checkbox"
                                checked={rememberMe}
                                onChange={(event) => setRememberMe(event.target.checked)}
                                className="h-4 w-4 text-indigo-600 border-gray-300 rounded focus:ring-indigo-500"
                            />
                            <label htmlFor="remember-me" className="ml-2 block text-sm text-gray-700">
                                Remember me
                            </label>
                        </div>

                        <Link
                            to="/forgot"
                            className="text-sm font-medium text-indigo-600 hover:text-indigo-500 transition-colors"
                        >
                            Forgot password?
                        </Link>
                    </div>
                </div>

                <Button type="submit" isLoading={loading} className="py-3 text-lg">
                    {loading ? "Signing in..." : "Sign in now"}
                </Button>
            </form>

            <p className="text-center text-sm text-gray-500 mt-6">
                Don't have an account?{" "}
                <Link to="/register" className="font-semibold text-indigo-600 hover:text-indigo-500 hover:underline">
                    Create free account
                </Link>
            </p>
            <p className="text-center text-sm text-gray-500">
                Verification link expired?{" "}
                <Link to="/verify" className="font-semibold text-indigo-600 hover:underline">
                    Request a new link
                </Link>
            </p>
        </AuthLayout>
    );
}
