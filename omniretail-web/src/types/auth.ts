export type User = {
  id: string;
  username: string;
  role: string;
};

export type LoginResponse = {
  accessToken: string;
  refreshToken: string;
  user: User;
};