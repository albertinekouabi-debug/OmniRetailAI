"use client";

import { motion } from "framer-motion";

import {
  DollarSign,
  ShoppingCart,
  Users,
  TrendingUp,
} from "lucide-react";

import { useDashboard } from "@/hooks/use-dashboard";

const icons = {
  revenue: DollarSign,
  sales: ShoppingCart,
  customers: Users,
  growth: TrendingUp,
};

export default function KpiCards() {
  const {
    data,
    loading,
    error,
  } = useDashboard();

  if (loading) {
    return (
      <div className="text-zinc-400">
        Loading dashboard...
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="text-red-500">
        Failed to load dashboard.
      </div>
    );
  }

  const cards = [
    {
      title: "Revenue",
      value: data.totalRevenue,
      icon: icons.revenue,
    },
    {
      title: "Sales",
      value: data.totalSales,
      icon: icons.sales,
    },
    {
      title: "Customers",
      value: data.totalCustomers,
      icon: icons.customers,
    },
    {
      title: "Growth",
      value: `${data.growth}%`,
      icon: icons.growth,
    },
  ];

  return (
    <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-4">
      {cards.map((card, index) => {
        const Icon = card.icon;

        return (
          <motion.div
            key={card.title}
            initial={{
              opacity: 0,
              y: 20,
            }}
            animate={{
              opacity: 1,
              y: 0,
            }}
            transition={{
              delay: index * 0.1,
            }}
            className="rounded-2xl border border-zinc-800 bg-zinc-900 p-6"
          >
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-zinc-400">
                  {card.title}
                </p>

                <h3 className="mt-2 text-3xl font-bold text-white">
                  {card.value}
                </h3>
              </div>

              <div className="rounded-xl bg-zinc-800 p-3">
                <Icon size={24} />
              </div>
            </div>
          </motion.div>
        );
      })}
    </div>
  );
}