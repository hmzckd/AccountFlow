import type { InputHTMLAttributes } from "react";

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
    label: string;
}

export function Input({ label, id, ...props }: InputProps) {
    // ID yoksa rastgele bir tane oluþtur (eriþilebilirlik için)
    const inputId = id || label.toLowerCase().replace(/\s+/g, '-');

    return (
        <div className="space-y-2 group">
            <label
                htmlFor={inputId}
                className="block text-sm font-medium text-gray-700 ml-1 transition-colors group-focus-within:text-indigo-600"
            >
                {label}
            </label>
            <input
                id={inputId}
                className="w-full rounded-xl border border-gray-300 bg-gray-50/50 px-4 py-3.5 text-gray-900 placeholder-gray-400 shadow-sm transition-all duration-300 focus:border-indigo-500 focus:bg-white focus:ring-4 focus:ring-indigo-500/20 outline-none hover:border-gray-400"
                {...props}
            />
        </div>
    );
}