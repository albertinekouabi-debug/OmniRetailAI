"use client";

import {
  LineChart,
  Line,
  ResponsiveContainer,
  XAxis,
  Tooltip,
} from "recharts";

import { SalesData } from "@/types/dashboard";

type Props = {
  data: SalesData[];
};

export default function SalesChart({ data }: Props) {
  return (
    <div className="mt-8 rounded-2xl border border-zinc-800 bg-zinc-900 p-6">
      <h2 className="mb-6 text-2xl font-bold">
        Sales Analytics
      </h2>

      <div className="h-[300px]">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={data}>
            <XAxis dataKey="month" stroke="#71717a" />

            <Tooltip />

            <Line
              type="monotone"
              dataKey="sales"
              stroke="#ffffff"
              strokeWidth={3}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}