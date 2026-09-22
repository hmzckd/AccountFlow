import { api } from "./client";
import type { EmailDto } from "../types/email";

/**
 * Triggers the email sending process via the backend notification service.
 */
export async function sendEmail(body: EmailDto): Promise<void> {
    await api.post<void>("/email/sendEmail", body);
}