import { useState } from "react";
import type { FormEvent } from "react";
import { sendEmail } from "../api/email";

export default function SendEmailPage() {
    const [to, setTo] = useState("");
    const [subject, setSubject] = useState("");
    const [body, setBody] = useState("");
    const [msg, setMsg] = useState("");
    const [loading, setLoading] = useState(false);

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();
        setMsg("");
        setLoading(true);
        try {
            await sendEmail({ to, subject, body });
            setMsg("✅ Email sent successfully!");
            setTo("");
            setSubject("");
            setBody("");
        } catch (err) {
            // TypeScript: err is unknown
            if (err instanceof Error) {
                setMsg("❌ Failed to send email: " + err.message);
            } else {
                setMsg("❌ Failed to send email: " + String(err));
            }
        } finally {
            setLoading(false);
        }
    }


    return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50 p-6">
            <form
                onSubmit={handleSubmit}
                className="w-full max-w-md bg-white border border-gray-200 rounded-2xl shadow p-6 space-y-4"
            >
                <h1 className="text-2xl font-semibold text-gray-900">
                    Send Email ✉️
                </h1>

                {msg && (
                    <div
                        className={`rounded-xl px-4 py-2 text-sm ${msg.startsWith("✅")
                                ? "border border-green-300 bg-green-50 text-green-700"
                                : "border border-red-300 bg-red-50 text-red-700"
                            }`}
                    >
                        {msg}
                    </div>
                )}

                <div>
                    <label className="block text-sm font-medium text-gray-700">
                        To
                    </label>
                    <input
                        type="email"
                        placeholder="recipient@example.com"
                        value={to}
                        onChange={(e) => setTo(e.target.value)}
                        className="w-full rounded-xl border px-3 py-2 text-sm focus:ring-2 focus:ring-indigo-500"
                        required
                    />
                </div>

                <div>
                    <label className="block text-sm font-medium text-gray-700">
                        Subject
                    </label>
                    <input
                        type="text"
                        placeholder="Subject"
                        value={subject}
                        onChange={(e) => setSubject(e.target.value)}
                        className="w-full rounded-xl border px-3 py-2 text-sm focus:ring-2 focus:ring-indigo-500"
                        required
                    />
                </div>

                <div>
                    <label className="block text-sm font-medium text-gray-700">
                        Body
                    </label>
                    <textarea
                        placeholder="Write your message here..."
                        value={body}
                        onChange={(e) => setBody(e.target.value)}
                        rows={6}
                        className="w-full rounded-xl border px-3 py-2 text-sm focus:ring-2 focus:ring-indigo-500"
                        required
                    />
                </div>

                <button
                    type="submit"
                    disabled={loading}
                    className="w-full rounded-xl bg-indigo-600 text-white text-sm font-medium py-2 hover:bg-indigo-700 transition disabled:opacity-60"
                >
                    {loading ? "Sending..." : "Send Email"}
                </button>
            </form>
        </div>
    );
}
