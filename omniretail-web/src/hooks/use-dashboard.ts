"use client";

import { useEffect, useState } from "react";

import { getDashboardKpis } from "@/services/dashboard-service";

import { DashboardKpis } from "@/types/dashboard";

export function useDashboard() {
  const [data, setData] =
    useState<DashboardKpis | null>(null);

  const [loading, setLoading] =
    useState(true);

  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    async function loadDashboard() {
      try {
        const result =
          await getDashboardKpis();

        setData(result);
      } catch (err) {
        console.error(err);

        setError("Failed to load dashboard.");
      } finally {
        setLoading(false);
      }
    }

    loadDashboard();
  }, []);

  return {
    data,
    loading,
    error,
  };
}