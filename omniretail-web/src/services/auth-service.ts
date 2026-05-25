import api from "@/services/api";

type LoginData = {
  username: string;
  password: string;
};

export async function login(
  data: LoginData
) {
  const response =
    await api.post(
      "/auth/login",
      data
    );

  return response.data;
}

export async function refreshToken(
  refreshToken: string
) {
  const response =
    await api.post(
      "/auth/refresh",
      {
        refreshToken,
      }
    );

  return response.data;
}