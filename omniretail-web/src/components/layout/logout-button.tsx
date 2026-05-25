"use client";

import { useRouter } from "next/navigation";

import { useAuthStore } from "@/store/auth-store";

export default function LogoutButton() {
  const router = useRouter();

  const { logout } =
    useAuthStore();

  function handleLogout() {
    logout();

    router.push("/login");
  }

  return (
    <button
      onClick={handleLogout}
      className="rounded-xl bg-red-500 px-4 py-2 text-white"
    >
      Logout
    </button>
  );
}