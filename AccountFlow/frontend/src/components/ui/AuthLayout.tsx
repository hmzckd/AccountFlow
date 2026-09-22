import type { ReactNode } from "react";

interface AuthLayoutProps {
    children: ReactNode;
    title: string;
    subtitle: string;
    imageTitle?: string;
    imageSubtitle?: string;
}

export function AuthLayout({
    children,
    title,
    subtitle,
    imageTitle = "Welcome Back",
    imageSubtitle = "It is a long established fact that a reader will be distracted by the readable content."
}: AuthLayoutProps) {
    return (
        <div className="min-h-screen w-full flex">
            {/* SOL TARAFA (RESİM ALANI) - Geniş ekranda görünür, mobilde gizlenir */}
            <div className="hidden lg:flex w-1/2 relative bg-gray-900 items-center justify-center overflow-hidden">
                <img
                    src="https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?q=80&w=2564&auto=format&fit=crop"
                    alt="Background"
                    className="absolute inset-0 w-full h-full object-cover opacity-60"
                />
                <div className="absolute inset-0 bg-gradient-to-t from-gray-900 via-gray-900/40 to-transparent"></div>

                <div className="relative z-10 p-12 text-white max-w-lg">
                    <h1 className="text-5xl font-extrabold tracking-tight mb-6 leading-tight">
                        {imageTitle}
                    </h1>
                    <p className="text-lg text-gray-300 leading-relaxed opacity-90">
                        {imageSubtitle}
                    </p>
                </div>
            </div>

            {/* SAĞ TARAF (FORM ALANI) - Beyaz, temiz alan */}
            <div className="w-full lg:w-1/2 flex items-center justify-center bg-white p-8 sm:p-12 lg:p-24">
                <div className="w-full max-w-md space-y-8">
                    <div className="text-left">
                        <h2 className="text-3xl font-bold text-gray-900 tracking-tight">
                            {title}
                        </h2>
                        <p className="mt-2 text-sm text-gray-500">
                            {subtitle}
                        </p>
                    </div>
                    {children}
                </div>
            </div>
        </div>
    );
}
