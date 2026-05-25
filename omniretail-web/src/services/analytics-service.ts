import api from "@/lib/api";

export async function getSalesAnalytics() {
  const response = await api.get("/dashboard/sales-analytics");

  return response.data;
}