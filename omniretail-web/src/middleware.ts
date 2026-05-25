import { NextResponse }
  from "next/server";

import type {
  NextRequest,
} from "next/server";

export function middleware(
  request: NextRequest
) {
  const token =
    request.cookies.get(
      "accessToken"
    );

  const isLoginPage =
    request.nextUrl.pathname ===
    "/login";

  if (!token && !isLoginPage) {
    return NextResponse.redirect(
      new URL(
        "/login",
        request.url
      )
    );
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    "/",
    "/products/:path*",
    "/analytics/:path*",
    "/users/:path*",
    "/pos/:path*",
  ],
};