import { useState, type FormEvent } from "react";
import { useNavigate, Link } from "react-router-dom";
import { register } from "../api/auth";
import toast from "react-hot-toast";
import { AuthLayout } from "../components/ui/AuthLayout";
import { Input } from "../components/ui/Input";
import { Button } from "../components/ui/Button";
import { validateEmail, validatePassword } from "../utils/validators";

export default function RegisterPage() {
    const nav = useNavigate();
    const [form, setForm] = useState({ name: "", surname: "", email: "", password: "" });
    const [loading, setLoading] = useState(false);

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();

        // Client-side checks first for immediate, specific feedback.
        const emailError = validateEmail(form.email);
        if (emailError) {
            toast.error(emailError);
            return;
        }
        const passwordError = validatePassword(form.password);
        if (passwordError) {
            toast.error(passwordError);
            return;
        }

        setLoading(true);
        try {
            await register(form);
            toast.success("Account created! Please check your email to verify.", { duration: 5000, icon: '🎉' });
            nav("/login");
        } catch (err) {
            // Surface the real reason from the backend (e.g. "Email already exists.").
            const msg = err instanceof Error ? err.message : "Registration failed. Please try again.";
            toast.error(msg);
        } finally {
            setLoading(false);
        }
    }

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        setForm({ ...form, [e.target.name]: e.target.value });
    };

    return (
        <AuthLayout
            title="Create Account"
            subtitle="Join us today and start your journey"
        >
            <form onSubmit={handleSubmit} className="space-y-5">

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <Input
                        label="Name"
                        name="name"
                        placeholder="John"
                        value={form.name}
                        onChange={handleChange}
                        required
                    />
                    <Input
                        label="Surname"
                        name="surname"
                        placeholder="Doe"
                        value={form.surname}
                        onChange={handleChange}
                        required
                    />
                </div>

                <Input
                    label="Email Address"
                    type="email"
                    name="email"
                    placeholder="john@example.com"
                    value={form.email}
                    onChange={handleChange}
                    required
                    autoComplete="email"
                />

                <div>
                    <Input
                        label="Password"
                        type="password"
                        name="password"
                        placeholder="••••••••"
                        value={form.password}
                        onChange={handleChange}
                        required
                        autoComplete="new-password"
                    />
                    <p className="text-xs text-gray-500 mt-1">
                        At least 6 characters, including uppercase, lowercase, a number, and a special character.
                    </p>
                </div>

                <Button type="submit" isLoading={loading} className="mt-6">
                    {loading ? "Creating Account..." : "Create Account"}
                </Button>
            </form>

            <p className="text-center text-sm text-gray-600 mt-8">
                Already have an account?{" "}
                <Link to="/login" className="font-bold text-indigo-600 hover:text-indigo-500 transition-all hover:underline">
                    Sign in instead
                </Link>
            </p>
        </AuthLayout>
    );
}
