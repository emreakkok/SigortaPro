/** Backend `UserRole` enum'u ile birebir aynı rol adları (Domain/Enums/UserRole). */
export const UserRoles = {
  Admin: "Admin",
  Personel: "Personel",
  Customer: "Customer",
} as const;

export type UserRole = (typeof UserRoles)[keyof typeof UserRoles];

/** Acente personeli rolleri (backend'deki `Roles.Staff` karşılığı). */
export const STAFF_ROLES: readonly UserRole[] = [UserRoles.Admin, UserRoles.Personel];

/** `POST /auth/{register|login|refresh-token}` yanıtı (backend `AuthResponse` kaydı). */
export interface AuthResponse {
  userId: string;
  email: string;
  roles: UserRole[];
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

/** İstemcide saklanan oturum bilgisi (hassas veri içermez; token'lar zaten opak). */
export interface AuthSession {
  userId: string;
  email: string;
  roles: UserRole[];
  accessToken: string;
  refreshToken: string;
}
