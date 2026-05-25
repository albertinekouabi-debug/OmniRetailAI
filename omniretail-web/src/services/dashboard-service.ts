import api from "./api";

import {
  DashboardKpis,
} from "@/types/dashboard";

export async function getDashboardKpis():
Promise<DashboardKpis> {

  const response =
    await api.get(
      "/dashboard/kpis"
    );

  return response.data;
}