"use client";

import Link from "next/link";

import {
  LayoutDashboard,
  Users,
  ShoppingCart,
  Package,
  BarChart3,
} from "lucide-react";

import { useAuthStore } from "@/store/auth-store";

const menu = [
  {
    label: "Dashboard",
    href: "/",
    icon: LayoutDashboard,
    roles: ["Admin", "Employee"],
  },
  {
    label: "Products",
    href: "/products",
    icon: Package,
    roles: ["Admin", "Employee"],
  },
  {
    label: "POS",
    href: "/pos",
    icon: ShoppingCart,
    roles: ["Admin", "Employee"],
  },
  {
    label: "Analytics",
    href: "/analytics",
    icon: BarChart3,
    roles: ["Admin"],
  },
  {
    label: "Users",
    href: "/users",
    icon: Users,
    roles: ["Admin"],
  },
];

export default function AppSidebar() {
  const user =
    useAuthStore((state) => state.user);

  const logout =
    useAuthStore((state) => state.logout);

  const filteredMenu =
    menu.filter((item) =>
      item.roles.includes(
        user?.role ?? ""
      )
    );

  return (
    <aside className="flex h-screen w-72 flex-col border-r border-zinc-800 bg-zinc-900 p-6">
      <div>
        <h1 className="text-3xl font-bold text-white">
          OmniRetail AI
        </h1>

        <div className="mt-6 rounded-xl bg-zinc-800 p-4">
          <p className="font-semibold text-white">
            {user?.username}
          </p>

          <p className="text-sm text-zinc-400">
            {user?.role}
          </p>
        </div>
      </div>

      <nav className="mt-10 flex flex-1 flex-col gap-2">
        {filteredMenu.map((item) => {
          const Icon = item.icon;

          return (
            <Link
              key={item.href}
              href={item.href}
              className="flex items-center gap-3 rounded-xl p-4 text-zinc-300 transition hover:bg-zinc-800 hover:text-white"
            >
              <Icon size={20} />

              <span>{item.label}</span>
            </Link>
          );
        })}
      </nav>

      <button
        onClick={logout}
        className="rounded-xl bg-red-500 p-4 font-bold text-white"
      >
        Logout
      </button>
    </aside>
  );
}