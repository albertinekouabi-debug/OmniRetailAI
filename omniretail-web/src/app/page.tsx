import AuthProvider from "@/components/providers/auth-provider";
import DashboardPage from "@/components/dashboard/dashboard-page";

export default function HomePage() {
  return (
    <AuthProvider>
      <DashboardPage />
    </AuthProvider>
  );
}