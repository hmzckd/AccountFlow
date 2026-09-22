import { useEffect, useState, useRef, type FormEvent } from "react";
import { resendVerificationEmail, verifyEmail } from "../api/auth";
import { useCallback } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { CheckCircleIcon, XCircleIcon } from "@heroicons/react/24/solid";
import toast from "react-hot-toast";

export default function VerifyEmailPage() {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    const tokenFromUrl = searchParams.get("token");

    const [status, setStatus] = useState<"idle" | "loading" | "success" | "error">("idle");
    const [msg, setMsg] = useState("");

    const attemptRef = useRef(false);

    const handleVerify = useCallback(async (token: string) => {
        setStatus("loading");
        try {
            await verifyEmail({ token });
            setStatus("success");
            toast.success("Email verified successfully! You can login now.");
            setTimeout(() => navigate("/login"), 3000);
        } catch (err) {
            setStatus("error");
            const message = err instanceof Error ? err.message : "Invalid or expired token.";

            toast.error(message);
            setMsg(message);
        }
    }, [navigate]);

    useEffect(() => {
        if (!tokenFromUrl || attemptRef.current) return;

        attemptRef.current = true;
        void handleVerify(tokenFromUrl);
    }, [handleVerify, tokenFromUrl]);

    return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50 p-6">
            <div className="w-full max-w-md bg-white border border-gray-100 rounded-2xl shadow-xl p-8 text-center space-y-6">

                <div className="flex justify-center">
                    {status === "loading" && (
                        <div className="animate-spin rounded-full h-16 w-16 border-t-4 border-b-4 border-indigo-600"></div>
                    )}
                    {status === "success" && <CheckCircleIcon className="h-20 w-20 text-green-500" />}
                    {status === "error" && <XCircleIcon className="h-20 w-20 text-red-500" />}
                    {status === "idle" && !tokenFromUrl && (
                        <div className="h-20 w-20 bg-gray-200 rounded-full flex items-center justify-center text-2xl">✉️</div>
                    )}
                </div>

                <h1 className="text-2xl font-bold text-gray-900">
                    {status === "loading" && "Verifying your email..."}
                    {status === "success" && "Verified!"}
                    {status === "error" && "Verification Failed"}
                    {status === "idle" && "Verify Email"}
                </h1>

                <p className="text-gray-500">
                    {msg || (tokenFromUrl ? "Please wait while we verify your token." : "Please enter your verification token manually if you didn't use the link.")}
                </p>

                {!tokenFromUrl && status !== "success" && (
                    <ManualVerifyForm onVerify={handleVerify} loading={status === "loading"} />
                )}
                {status !== "success" && status !== "loading" && (
                    <ResendVerificationForm />
                )}
                <Link to="/login" className="block text-sm font-medium text-indigo-600 hover:underline">
                    Back to sign in
                </Link>
            </div>
        </div>
    );
}

function ResendVerificationForm() {
    const [email, setEmail] = useState("");
    const [sending, setSending] = useState(false);
    const [message, setMessage] = useState("");

    async function handleSubmit(event: FormEvent) {
        event.preventDefault();
        setSending(true);
        setMessage("");
        try {
            const response = await resendVerificationEmail({ email });
            setMessage(response.message);
        } catch (error) {
            toast.error(error instanceof Error ? error.message : "Could not request a new link.");
        } finally {
            setSending(false);
        }
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-3 border-t border-gray-200 pt-6 text-left">
            <label htmlFor="resend-email" className="block text-sm font-medium text-gray-700">
                Need a new verification link? Enter your email address.
            </label>
            <input
                id="resend-email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                className="w-full rounded-xl border border-gray-300 px-4 py-3 text-sm focus:ring-2 focus:ring-indigo-500 outline-none"
                required
            />
            <button
                type="submit"
                disabled={sending}
                className="w-full rounded-xl bg-indigo-600 py-3 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
            >
                {sending ? "Requesting..." : "Send a new link"}
            </button>
            {message && <p role="status" className="text-sm text-gray-600">{message}</p>}
        </form>
    );
}

function ManualVerifyForm({ onVerify, loading }: { onVerify: (t: string) => void, loading: boolean }) {
    const [input, setInput] = useState("");
    return (
        <form onSubmit={(e) => { e.preventDefault(); onVerify(input); }} className="space-y-4">
            <input
                placeholder="Enter verification code"
                value={input}
                onChange={(e) => setInput(e.target.value)}
                className="w-full rounded-xl border border-gray-300 px-4 py-3 text-sm focus:ring-2 focus:ring-indigo-500 outline-none transition"
                required
            />
            <button
                disabled={loading}
                className="w-full rounded-xl bg-indigo-600 text-white font-semibold py-3 hover:bg-indigo-700 transition disabled:opacity-50"
            >
                {loading ? "Verifying..." : "Verify Manually"}
            </button>
        </form>
    );
}
