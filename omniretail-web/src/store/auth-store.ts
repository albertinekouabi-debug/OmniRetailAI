"use client";

import { create } from "zustand";

type User = {
  id: string;
  username: string;
  role: string;
};

type AuthState = {
  user: User | null;

  accessToken: string | null;

  refreshToken: string | null;

  setAuth: (
    user: User,
    accessToken: string,
    refreshToken: string
  ) => void;

  logout: () => void;
};

export const useAuthStore =
  create<AuthState>((set) => ({
    user: null,

    accessToken: null,

    refreshToken: null,

    setAuth: (
      user,
      accessToken,
      refreshToken
    ) => {
      localStorage.setItem(
        "accessToken",
        accessToken
      );

      localStorage.setItem(
        "refreshToken",
        refreshToken
      );

      set({
        user,
        accessToken,
        refreshToken,
      });
    },

    logout: () => {
      localStorage.removeItem(
        "accessToken"
      );

      localStorage.removeItem(
        "refreshToken"
      );

      set({
        user: null,
        accessToken: null,
        refreshToken: null,
      });
    },
  }));