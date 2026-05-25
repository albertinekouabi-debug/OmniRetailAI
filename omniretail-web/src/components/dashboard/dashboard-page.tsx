"use client";

import { useEffect, useState } from "react";

import AppSidebar from "@/components/layout/app-sidebar";
import KpiCards from "@/components/dashboard/kpi-cards";

import api from "@/services/api";

export default function DashboardPage() {
  const [kpis, setKpis] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadDashboard();
  }, []);

  async function loadDashboard() {
    try {
      const response =
        await api.get("/dashboard/kpis");

      setKpis(response.data);
    } catch (error) {
      console.error(
        "Dashboard loading error:",
        error
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="flex min-h-screen bg-zinc-950 text-white">
      <AppSidebar />

      <section className="flex-1 p-10">
        <h1 className="text-5xl font-bold tracking-tight">
          Dashboard
        </h1>

        <p className="mt-4 text-zinc-400">
          Enterprise Retail Intelligence
        </p>

        {loading ? (
          <p className="mt-10 text-zinc-500">
            Loading dashboard...
          </p>
        ) : (
          <div className="mt-10">
            <KpiCards data={kpis} />
          </div>
        )}
      </section>
    </main>
  );
}