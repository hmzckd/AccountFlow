import type { ReactNode } from "react";

const accentClasses = {
    indigo: "bg-indigo-50 text-indigo-700 border-indigo-100",
    orange: "bg-orange-50 text-orange-700 border-orange-100",
};

export default function StatCard({
    label,
    value,
    accent,
    icon,
}: {
    label: string;
    value: number;
    accent: keyof typeof accentClasses;
    icon: ReactNode;
}) {
    return (
        <div className="rounded-lg border border-gray-200 bg-white p-6 shadow-sm transition-shadow hover:shadow-md">
            <div className="flex items-start justify-between">
                <div>
                    <p className="text-sm font-medium uppercase text-gray-500">{label}</p>
                    <p className="mt-2 text-4xl font-bold text-gray-900">{value}</p>
                </div>
                <div className={`rounded-lg border p-3 ${accentClasses[accent]}`}>
                    {icon}
                </div>
            </div>
        </div>
    );
}
