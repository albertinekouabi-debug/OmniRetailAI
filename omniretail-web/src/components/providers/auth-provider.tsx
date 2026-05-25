"use client";

import {
  ReactNode,
  useEffect,
} from "react";

import {
  useAuthStore,
} from "@/store/auth-store";

type Props = {
  children: ReactNode;
};

export default function AuthProvider({
  children,
}: Props) {

  const {
    accessToken,
  } = useAuthStore();

  useEffect(() => {
    console.log(
      "JWT:",
      accessToken
    );
  }, [accessToken]);

  return children;
}