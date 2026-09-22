import type { ButtonHTMLAttributes, ReactNode } from "react";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
    isLoading?: boolean;
    children: ReactNode;
}

export function Button({ isLoading, children, disabled, className = "", ...props }: ButtonProps) {
    return (
        <button
            disabled={isLoading || disabled}
            className={`
                w-full relative flex justify-center items-center py-3.5 px-4
                border border-transparent rounded-xl text-white font-bold text-base
                bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-700 hover:to-purple-700
                shadow-lg shadow-indigo-500/30 hover:shadow-indigo-500/50
                focus:outline-none focus:ring-4 focus:ring-indigo-500/50
                disabled:opacity-70 disabled:cursor-not-allowed disabled:shadow-none
                transform active:scale-[0.98] transition-all duration-200
                ${className}
            `}
            {...props}
        >
            {/* Loading Spinner */}
            {isLoading && (
                <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
            )}
            <span className={isLoading ? "opacity-90" : ""}>
                {children}
            </span>
        </button>
    );
}
