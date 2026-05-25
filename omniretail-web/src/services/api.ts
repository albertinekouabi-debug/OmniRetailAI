import axios from "axios";

import { refreshToken }
  from "@/services/auth-service";

import {
  useAuthStore,
} from "@/store/auth-store";

const api = axios.create({
  baseURL:
    process.env.NEXT_PUBLIC_API_URL,

  headers: {
    "Content-Type":
      "application/json",
  },
});

api.interceptors.request.use(
  (config) => {
    if (typeof window !== "undefined") {
      const token =
        localStorage.getItem(
          "accessToken"
        );

      if (token) {
        config.headers.Authorization =
          `Bearer ${token}`;
      }
    }

    return config;
  }
);

api.interceptors.response.use(
  (response) => response,

  async (error) => {
    const originalRequest =
      error.config;

    if (
      error.response?.status === 401 &&
      !originalRequest._retry
    ) {
      originalRequest._retry = true;

      try {
        const refresh =
          localStorage.getItem(
            "refreshToken"
          );

        if (!refresh)
          throw error;

        const response =
          await refreshToken(refresh);

        localStorage.setItem(
          "accessToken",
          response.accessToken
        );

        localStorage.setItem(
          "refreshToken",
          response.refreshToken
        );

        useAuthStore
          .getState()
          .setAuth(
            response.user,
            response.accessToken,
            response.refreshToken
          );

        originalRequest.headers.Authorization =
          `Bearer ${response.accessToken}`;

        return api(originalRequest);
      } catch (refreshError) {
        useAuthStore
          .getState()
          .logout();

        window.location.href =
          "/login";

        return Promise.reject(
          refreshError
        );
      }
    }

    return Promise.reject(error);
  }
);

export default api;