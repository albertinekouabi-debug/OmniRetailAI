"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";

import api from "@/services/api";

import { useAuthStore } from "@/store/auth-store";

export default function LoginPage() {
  const router = useRouter();

  const setAuth =
    useAuthStore((state) => state.setAuth);

  const [username, setUsername] =
    useState("");

  const [password, setPassword] =
    useState("");

  const [loading, setLoading] =
    useState(false);

  const [error, setError] =
    useState("");

  async function handleLogin(
    e: React.FormEvent
  ) {
    e.preventDefault();

    try {
      setLoading(true);

      setError("");

      const response =
        await api.post("/auth/login", {
          username,
          password,
        });

      const {
        accessToken,
        refreshToken,
        user,
      } = response.data;

      setAuth(
        user,
        accessToken,
        refreshToken
      );

      router.push("/");
    } catch (err) {
      console.error(err);

      setError(
        "Invalid credentials."
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-zinc-950 text-white">
      <form
        onSubmit={handleLogin}
        className="w-full max-w-md rounded-2xl border border-zinc-800 bg-zinc-900 p-8"
      >
        <h1 className="mb-8 text-4xl font-bold">
          OmniRetail AI
        </h1>

        <div className="space-y-4">
          <input
            type="text"
            placeholder="Username"
            value={username}
            onChange={(e) =>
              setUsername(
                e.target.value
              )
            }
            className="w-full rounded-xl bg-zinc-800 p-4 outline-none"
          />

          <input
            type="password"
            placeholder="Password"
            value={password}
            onChange={(e) =>
              setPassword(
                e.target.value
              )
            }
            className="w-full rounded-xl bg-zinc-800 p-4 outline-none"
          />

          {error && (
            <p className="text-red-500">
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full rounded-xl bg-white p-4 font-bold text-black transition hover:bg-zinc-200 disabled:opacity-50"
          >
            {loading
              ? "Loading..."
              : "Login"}
          </button>
        </div>
      </form>
    </main>
  );
}