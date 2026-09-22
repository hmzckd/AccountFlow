// Shared client-side validation that mirrors the backend FluentValidation rules,
// so users get instant, specific feedback instead of a round-trip + generic message.

export function validateEmail(email: string): string | null {
    const parts = email.split("@");
    if (parts.length !== 2 || !parts[0] || !parts[1]) {
        return "Please enter a valid email address.";
    }
    const domain = parts[1];
    const tld = domain.slice(domain.lastIndexOf(".") + 1);
    if (!domain.includes(".") || domain.endsWith(".") || tld.length < 2) {
        return "Please enter a valid email domain (e.g. example.com).";
    }
    return null;
}

export function validatePassword(pw: string): string | null {
    if (pw.length < 6) return "Password must be at least 6 characters long.";
    if (!/[A-Z]/.test(pw)) return "Password must contain at least one uppercase letter.";
    if (!/[a-z]/.test(pw)) return "Password must contain at least one lowercase letter.";
    if (!/[0-9]/.test(pw)) return "Password must contain at least one number.";
    if (!/[^a-zA-Z0-9]/.test(pw)) return "Password must contain at least one special character.";
    return null;
}
